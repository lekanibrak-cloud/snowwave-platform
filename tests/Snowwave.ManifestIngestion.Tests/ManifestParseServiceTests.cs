using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Snowwave.ManifestIngestion;

namespace Snowwave.ManifestIngestion.Tests;

public sealed class ManifestParseServiceTests
{
    private const string ValidCsv =
        "parcelId,customerName,addressLine1,city,postalCode\n" +
        "P1,Alice,1 Main,Toronto,M5V1A1\n" +
        "P2,Bob,2 King,Toronto,M5V2B2\n";

    [Fact]
    public async Task SuccessfulJob_ClaimsPublishesAndQueues()
    {
        var (service, store, sender, _) = Build(ValidCsv);
        var job = NewJob();
        store.Seed(job);

        await service.ProcessChangeAsync(job, CancellationToken.None);

        Assert.Equal(["PARSING", "PUBLISHING", "QUEUED"], store.Transitions);
        Assert.Equal("QUEUED", store.Current!.Status);
        Assert.Equal(2, store.Current.RowsPublished);
        Assert.Equal(2, sender.SentMessages.Count);
        Assert.Equal("tenant-1:P1:PARCEL_INGESTED", sender.SentMessages[0].MessageId);
    }

    [Fact]
    public async Task LostClaimCas_ExitsWithoutPublishing()
    {
        var (service, store, sender, _) = Build(ValidCsv);
        var job = NewJob();
        store.Seed(job);
        store.FailNextReplace = true;

        await service.ProcessChangeAsync(job, CancellationToken.None);

        Assert.Empty(store.Transitions);
        Assert.Empty(sender.SentMessages);
        Assert.Equal("RECEIVED", store.Current!.Status);
    }

    [Fact]
    public async Task ServiceBusFailure_BecomesRetryPending()
    {
        var (service, store, sender, _) = Build(ValidCsv);
        var job = NewJob();
        store.Seed(job);
        sender.ThrowOnSend = true;

        await service.ProcessChangeAsync(job, CancellationToken.None);

        Assert.Equal("RETRY_PENDING", store.Current!.Status);
        Assert.Equal(1, store.Current.AttemptCount);
        Assert.NotNull(store.Current.NextAttemptAtUtc);
    }

    [Fact]
    public async Task PermanentParseFailure_GoesTerminalWithoutRetryBudget()
    {
        var (service, store, _, _) = Build("name,city\nAlice,Toronto\n");
        var job = NewJob();
        store.Seed(job);

        await service.ProcessChangeAsync(job, CancellationToken.None);

        Assert.Equal("PARSE_FAILED", store.Current!.Status);
        Assert.Equal(0, store.Current.AttemptCount);
        Assert.Contains("parcelId", store.Current.LastError);
    }

    [Fact]
    public async Task FutureRetryPending_DoesNotRunBeforeDueTime()
    {
        var (service, store, sender, _) = Build(ValidCsv);
        var job = NewJob() with
        {
            Status = "RETRY_PENDING",
            NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        store.Seed(job);

        await service.ProcessChangeAsync(job, CancellationToken.None);

        Assert.Empty(store.Transitions);
        Assert.Empty(sender.SentMessages);
    }

    private static (ManifestParseService Service, FakeJobStore Store, RecordingServiceBusSender Sender, RecordingRowErrorStore Errors)
        Build(string csv)
    {
        var store = new FakeJobStore();
        var errors = new RecordingRowErrorStore();
        var sender = new RecordingServiceBusSender();
        var queue = new ParcelIngestQueueService(sender);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MANIFEST_UPLOAD_CONTAINER_NAME"] = "manifest-uploads"
        }).Build();

        var service = new ManifestParseService(
            store,
            errors,
            new FakeBlobServiceClient(csv),
            config,
            queue,
            NullLogger<ManifestParseService>.Instance);

        return (service, store, sender, errors);
    }

    private static ManifestJob NewJob() => new(
        Id: "batch-1",
        TenantId: "tenant-1",
        RetailerId: "retailer-1",
        BlobPath: "tenant-1/manifests/batch-1/manifest.csv",
        OriginalFileName: "manifest.csv",
        Status: "RECEIVED",
        AttemptCount: 0,
        LastError: null,
        CreatedAtUtc: DateTimeOffset.UtcNow,
        LastTransitionAtUtc: DateTimeOffset.UtcNow,
        UploadedBy: "portfolio-test",
        RouteId: null,
        RouteDate: "2026-09-27",
        RowsParsed: 0,
        RowsRejected: 0,
        RowsPublished: 0,
        LastPublishedRow: 0,
        TotalRows: null,
        OwnerInstanceId: null,
        ClaimedAtUtc: null,
        NextAttemptAtUtc: null);
}
