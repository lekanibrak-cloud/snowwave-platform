using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Snowwave.ManifestIngestion;

/// <summary>Cosmos Change Feed is the normal relay from committed job to processing.</summary>
public sealed class ManifestParseWorker
{
    private readonly ManifestParseService _service;
    private readonly ILogger<ManifestParseWorker> _logger;

    public ManifestParseWorker(ManifestParseService service, ILogger<ManifestParseWorker> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("ManifestParseWorker")]
    public async Task Run(
        [CosmosDBTrigger(
            databaseName: "%COSMOS_DATABASE_NAME%",
            containerName: "%COSMOS_MANIFEST_JOBS_CONTAINER%",
            Connection = "CosmosTriggerConnection",
            LeaseContainerName = "leases",
            LeaseContainerPrefix = "manifest-parse",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<ManifestJob> changes,
        CancellationToken cancellationToken)
    {
        if (changes.Count == 0) return;
        _logger.LogInformation("Manifest parse worker received {Count} change(s)", changes.Count);
        foreach (var change in changes)
        {
            await _service.ProcessChangeAsync(change, cancellationToken);
        }
    }
}
