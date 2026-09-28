using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Snowwave.ManifestIngestion;

namespace Snowwave.ManifestIngestion.Tests;

internal sealed class FakeJobStore : IManifestJobStore
{
    private int _version;
    public ManifestJob? Current { get; private set; }
    public List<string> Transitions { get; } = [];
    public bool FailNextReplace { get; set; }

    public void Seed(ManifestJob job)
    {
        Current = job;
        _version = 1;
    }

    public Task CreateAsync(ManifestJob job, CancellationToken ct)
    {
        Seed(job);
        return Task.CompletedTask;
    }

    public Task<ManifestJob?> GetAsync(string batchId, string tenantId, CancellationToken ct)
        => Task.FromResult(Current);

    public Task<(ManifestJob Job, string ETag)?> GetWithEtagAsync(
        string batchId, string tenantId, CancellationToken ct)
        => Task.FromResult<(ManifestJob, string)?>(Current is null ? null : (Current, _version.ToString()));

    public Task<(bool Success, string? NewETag)> TryReplaceAsync(
        ManifestJob job, string etag, CancellationToken ct)
    {
        if (FailNextReplace)
        {
            FailNextReplace = false;
            return Task.FromResult<(bool, string?)>((false, null));
        }

        if (etag != _version.ToString())
            return Task.FromResult<(bool, string?)>((false, null));

        Current = job;
        _version++;
        Transitions.Add(job.Status);
        return Task.FromResult<(bool, string?)>((true, _version.ToString()));
    }

    public Task<IReadOnlyList<ManifestJob>> GetDueJobsAsync(DateTimeOffset now, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ManifestJob>>(Current is null ? [] : [Current]);
}

internal sealed class RecordingRowErrorStore : IManifestRowErrorStore
{
    public List<ManifestRowError> Errors { get; } = [];

    public Task UpsertManyAsync(IReadOnlyList<ManifestRowError> rowErrors, CancellationToken ct)
    {
        Errors.AddRange(rowErrors);
        return Task.CompletedTask;
    }
}

internal sealed class FakeBlobClient(string csv, bool missing = false) : BlobClient
{
    public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken)
    {
        if (missing) throw new RequestFailedException(404, "BlobNotFound");
        var result = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(csv));
        return Task.FromResult(Response.FromValue(result, null!));
    }
}

internal sealed class FakeBlobContainerClient(string csv, bool missing = false) : BlobContainerClient
{
    public override BlobClient GetBlobClient(string blobName) => new FakeBlobClient(csv, missing);
}

internal sealed class FakeBlobServiceClient(string csv, bool missing = false) : BlobServiceClient
{
    public override BlobContainerClient GetBlobContainerClient(string blobContainerName)
        => new FakeBlobContainerClient(csv, missing);
}

internal sealed class RecordingServiceBusSender : ServiceBusSender
{
    private readonly Dictionary<ServiceBusMessageBatch, List<ServiceBusMessage>> _messagesByBatch = [];
    public List<ServiceBusMessage> SentMessages { get; } = [];
    public bool ThrowOnSend { get; set; }

    public override ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var store = new List<ServiceBusMessage>();
        var batch = ServiceBusModelFactory.ServiceBusMessageBatch(
            batchSizeBytes: 1024 * 1024,
            batchMessageStore: store,
            batchOptions: new CreateMessageBatchOptions(),
            tryAddCallback: _ => true);
        _messagesByBatch[batch] = store;
        return ValueTask.FromResult(batch);
    }

    public override Task SendMessagesAsync(
        ServiceBusMessageBatch messageBatch,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend) throw new InvalidOperationException("simulated send failure");
        SentMessages.AddRange(_messagesByBatch[messageBatch]);
        return Task.CompletedTask;
    }
}
