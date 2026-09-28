using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Snowwave.ManifestIngestion;

public interface IManifestJobStore
{
    Task CreateAsync(ManifestJob job, CancellationToken ct);
    Task<ManifestJob?> GetAsync(string batchId, string tenantId, CancellationToken ct);
    Task<(ManifestJob Job, string ETag)?> GetWithEtagAsync(string batchId, string tenantId, CancellationToken ct);
    Task<(bool Success, string? NewETag)> TryReplaceAsync(ManifestJob job, string etag, CancellationToken ct);
    Task<IReadOnlyList<ManifestJob>> GetDueJobsAsync(DateTimeOffset now, CancellationToken ct);
}

/// <summary>
/// Cosmos-backed job store. Every mutable state transition uses ETag
/// compare-and-swap. HTTP 412 is treated as a normal lost race, not as an
/// instruction to overwrite the winning writer.
/// </summary>
public sealed class ManifestJobStore : IManifestJobStore
{
    private readonly Container _container;
    private readonly ILogger<ManifestJobStore> _logger;

    public ManifestJobStore(Container container, ILogger<ManifestJobStore> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task CreateAsync(ManifestJob job, CancellationToken ct)
    {
        await _container.CreateItemAsync(job, new PartitionKey(job.TenantId), cancellationToken: ct);
    }

    public async Task<(ManifestJob Job, string ETag)?> GetWithEtagAsync(
        string batchId, string tenantId, CancellationToken ct)
    {
        try
        {
            var response = await _container.ReadItemAsync<ManifestJob>(
                batchId, new PartitionKey(tenantId), cancellationToken: ct);
            return (response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ManifestJob?> GetAsync(string batchId, string tenantId, CancellationToken ct)
        => (await GetWithEtagAsync(batchId, tenantId, ct))?.Job;

    public async Task<(bool Success, string? NewETag)> TryReplaceAsync(
        ManifestJob job, string etag, CancellationToken ct)
    {
        try
        {
            var response = await _container.ReplaceItemAsync(
                job,
                job.Id,
                new PartitionKey(job.TenantId),
                new ItemRequestOptions { IfMatchEtag = etag },
                ct);
            return (true, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            _logger.LogInformation(
                "Manifest job {BatchId} CAS lost (412); another worker owns the transition",
                job.Id);
            return (false, null);
        }
    }

    public async Task<IReadOnlyList<ManifestJob>> GetDueJobsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE " +
                "(c.status = 'RETRY_PENDING' AND (NOT IS_DEFINED(c.nextAttemptAtUtc) OR IS_NULL(c.nextAttemptAtUtc) OR c.nextAttemptAtUtc <= @now)) " +
                "OR (c.status = 'RECEIVED' AND c.lastTransitionAtUtc < @staleBefore) " +
                "OR ((c.status = 'PARSING' OR c.status = 'PUBLISHING') AND c.claimedAtUtc < @staleBefore)")
            .WithParameter("@now", now)
            .WithParameter("@staleBefore", now.AddMinutes(-10));

        var results = new List<ManifestJob>();
        using var iterator = _container.GetItemQueryIterator<ManifestJob>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(ct);
            results.AddRange(page);
        }
        return results;
    }
}
