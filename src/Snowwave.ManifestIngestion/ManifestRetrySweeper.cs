using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;

namespace Snowwave.ManifestIngestion;

/// <summary>
/// One-minute safety net for due retries, stale RECEIVED jobs, and abandoned
/// PARSING/PUBLISHING claims. Feed/sweeper races are resolved by the same ETag CAS.
/// </summary>
public sealed class ManifestRetrySweeper
{
    private readonly IManifestJobStore _jobStore;
    private readonly ManifestParseService _service;
    private readonly ILogger<ManifestRetrySweeper> _log;

    public ManifestRetrySweeper(
        IManifestJobStore jobStore,
        ManifestParseService service,
        ILogger<ManifestRetrySweeper> logger)
    {
        _jobStore = jobStore;
        _service = service;
        _log = logger;
    }

    [Function("ManifestRetrySweeper")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        var due = await _jobStore.GetDueJobsAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (due.Count == 0) return;

        _log.LogInformation("Manifest retry sweeper: {Count} due job(s)", due.Count);
        foreach (var job in due)
        {
            await _service.ProcessChangeAsync(job, cancellationToken);
        }
    }
}
