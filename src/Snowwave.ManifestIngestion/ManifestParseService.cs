using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Snowwave.ManifestIngestion;

/// <summary>
/// Core manifest worker. Claims work with ETag CAS, downloads the durable blob,
/// parses rows, records canonical row errors, publishes deterministic-message-id
/// events, and advances the job ledger.
///
/// Processing failures are recorded and returned normally because Cosmos Change
/// Feed has no DLQ. Cancellation still propagates so host shutdown can redeliver.
/// </summary>
public sealed class ManifestParseService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan StaleClaimTimeout = TimeSpan.FromMinutes(10);

    private static bool IsClaimable(ManifestJob job, DateTimeOffset now)
        => job.Status is "RECEIVED" or "RETRY_PENDING"
           || (job.Status is "PARSING" or "PUBLISHING"
               && job.ClaimedAtUtc is { } claimed
               && now - claimed > StaleClaimTimeout);

    private readonly IManifestJobStore _jobStore;
    private readonly IManifestRowErrorStore _rowErrorStore;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ParcelIngestQueueService _parcelIngestQueue;
    private readonly ILogger<ManifestParseService> _logger;

    public ManifestParseService(
        IManifestJobStore jobStore,
        IManifestRowErrorStore rowErrorStore,
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ParcelIngestQueueService parcelIngestQueue,
        ILogger<ManifestParseService> logger)
    {
        _jobStore = jobStore;
        _rowErrorStore = rowErrorStore;
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["MANIFEST_UPLOAD_CONTAINER_NAME"] ?? "manifest-uploads";
        _parcelIngestQueue = parcelIngestQueue;
        _logger = logger;
    }

    public async Task ProcessChangeAsync(ManifestJob changed, CancellationToken cancellationToken)
    {
        if (!IsClaimable(changed, DateTimeOffset.UtcNow)) return;

        var current = await _jobStore.GetWithEtagAsync(changed.Id, changed.TenantId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (current is null || !IsClaimable(current.Value.Job, now)) return;

        var (job, etag) = current.Value;

        if (job.Status == "RETRY_PENDING" && job.NextAttemptAtUtc is { } due && due > now)
        {
            return;
        }

        // RECEIVED/RETRY_PENDING (or stale in-flight) -> PARSING. A lost CAS
        // means another worker owns the transition, so this worker exits.
        var claimed = job with
        {
            Status = "PARSING",
            OwnerInstanceId = Environment.MachineName,
            ClaimedAtUtc = now,
            NextAttemptAtUtc = null,
            LastTransitionAtUtc = now,
        };
        var (claimOk, claimEtag) = await _jobStore.TryReplaceAsync(claimed, etag, cancellationToken);
        if (!claimOk || claimEtag is null) return;

        job = claimed;
        etag = claimEtag;
        var phase = "PARSE";

        try
        {
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(_containerName)
                .GetBlobClient(job.BlobPath);

            ManifestCsvParseResult parseResult;
            try
            {
                var download = await blobClient.DownloadContentAsync(cancellationToken);
                using var stream = download.Value.Content.ToStream();
                parseResult = ManifestCsvParser.Parse(stream);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                throw new ManifestParseException($"source blob missing: {job.BlobPath}", ex);
            }

            var rowErrors = parseResult.RejectedRows
                .Select(r => new ManifestRowError(
                    Id: $"{job.Id}:row{r.RowNumber}",
                    TenantId: job.TenantId,
                    BatchId: job.Id,
                    RetailerId: job.RetailerId,
                    FileName: job.OriginalFileName,
                    ManifestRowNumber: r.RowNumber,
                    RawRow: r.RawRow,
                    ErrorCode: r.ErrorCode,
                    ErrorMessage: r.ErrorMessage,
                    Classification: r.Classification,
                    CreatedAtUtc: DateTimeOffset.UtcNow))
                .ToList();

            job = job with
            {
                RowsParsed = parseResult.ValidRows.Count,
                RowsRejected = parseResult.TotalRows - parseResult.ValidRows.Count,
                TotalRows = parseResult.TotalRows,
            };

            await _rowErrorStore.UpsertManyAsync(rowErrors, cancellationToken);

            if (parseResult.TotalRows == 0)
                throw new ManifestParseException("manifest contains no data rows");

            if (parseResult.ValidRows.Count == 0)
                throw new ManifestParseException(
                    $"all {parseResult.TotalRows} rows failed canonical validation - see row errors");

            var publishing = job with
            {
                Status = "PUBLISHING",
                LastTransitionAtUtc = DateTimeOffset.UtcNow,
            };
            var (pubOk, pubEtag) = await _jobStore.TryReplaceAsync(publishing, etag, cancellationToken);
            if (!pubOk || pubEtag is null) return;

            job = publishing;
            etag = pubEtag;
            phase = "PUBLISH";

            var createdAt = DateTimeOffset.UtcNow;
            var events = new List<ParcelIngestedEvent>(parseResult.ValidRows.Count);
            foreach (var row in parseResult.ValidRows)
            {
                events.Add(new ParcelIngestedEvent(
                    EventId: Guid.NewGuid().ToString(),
                    EventType: "PARCEL_INGESTED",
                    TenantId: job.TenantId,
                    BatchId: job.Id,
                    CorrelationId: $"{job.Id}:row{row.RowNumber}",
                    Parcel: new ParcelIngestParcelRequest(
                        ParcelId: row.ParcelId,
                        RecipientName: row.CustomerName,
                        AddressLine1: row.AddressLine1,
                        AddressLine2: row.UnitNumber,
                        City: row.City,
                        PostalCode: row.PostalCode,
                        ServiceLevel: null,
                        CustomerPhone: row.CustomerPhone,
                        DeliveryInstructions: row.Notes,
                        CustomerEmail: row.CustomerEmail,
                        BuzzerCode: row.BuzzerCode),
                    SourceSystem: job.RetailerId,
                    CreatedAtUtc: createdAt,
                    RouteId: job.RouteId,
                    OrderReference: row.OrderReference,
                    ManifestRowNumber: row.RowNumber,
                    RouteDate: job.RouteDate));
            }

            var published = await _parcelIngestQueue.EnqueueManyAsync(events, cancellationToken);
            if (published != parseResult.ValidRows.Count)
            {
                throw new InvalidOperationException(
                    $"published {published} of {parseResult.ValidRows.Count} rows - partial publish is a failure");
            }

            var queued = job with
            {
                Status = "QUEUED",
                RowsPublished = published,
                LastPublishedRow = parseResult.ValidRows.Max(r => r.RowNumber),
                LastError = null,
                LastTransitionAtUtc = DateTimeOffset.UtcNow,
            };
            var (doneOk, _) = await _jobStore.TryReplaceAsync(queued, etag, cancellationToken);
            if (!doneOk)
            {
                _logger.LogWarning(
                    "Manifest job {BatchId}: QUEUED checkpoint lost CAS after publish; dedup covers re-drive",
                    job.Id);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ManifestParseException ex)
        {
            await TrySetFailureAsync(
                job, etag, "PARSE_FAILED", ex.Message,
                incrementAttempt: false, nextAttemptAtUtc: null, cancellationToken);
        }
        catch (Exception ex)
        {
            var attempts = job.AttemptCount + 1;
            var terminal = phase == "PARSE" ? "PARSE_FAILED" : "PUBLISH_FAILED";
            var status = attempts >= MaxAttempts ? terminal : "RETRY_PENDING";
            DateTimeOffset? nextAttempt = status == "RETRY_PENDING"
                ? DateTimeOffset.UtcNow.AddMinutes(attempts == 1 ? 1 : 5)
                : null;

            _logger.LogError(
                ex,
                "Manifest job {BatchId} {Phase} failure (attempt {Attempt}/{Max}) -> {Status}",
                job.Id, phase, attempts, MaxAttempts, status);

            await TrySetFailureAsync(
                job, etag, status, $"{ex.GetType().Name}: {ex.Message}",
                incrementAttempt: true, nextAttempt, cancellationToken);
        }
    }

    private async Task TrySetFailureAsync(
        ManifestJob job,
        string etag,
        string status,
        string error,
        bool incrementAttempt,
        DateTimeOffset? nextAttemptAtUtc,
        CancellationToken cancellationToken)
    {
        var failed = job with
        {
            Status = status,
            AttemptCount = incrementAttempt ? job.AttemptCount + 1 : job.AttemptCount,
            LastError = error,
            OwnerInstanceId = null,
            ClaimedAtUtc = null,
            NextAttemptAtUtc = nextAttemptAtUtc,
            LastTransitionAtUtc = DateTimeOffset.UtcNow,
        };

        var (ok, _) = await _jobStore.TryReplaceAsync(failed, etag, cancellationToken);
        if (!ok)
        {
            _logger.LogWarning(
                "Manifest job {BatchId}: failure-state write ({Status}) lost CAS",
                job.Id, status);
        }
    }
}
