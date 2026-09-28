using Microsoft.Azure.Cosmos;

namespace Snowwave.ManifestIngestion;

public interface IManifestRowErrorStore
{
    Task UpsertManyAsync(IReadOnlyList<ManifestRowError> rowErrors, CancellationToken ct);
}

public sealed class ManifestRowErrorStore : IManifestRowErrorStore
{
    private readonly Container _container;

    public ManifestRowErrorStore(Container container) => _container = container;

    public async Task UpsertManyAsync(IReadOnlyList<ManifestRowError> rowErrors, CancellationToken ct)
    {
        foreach (var rowError in rowErrors)
        {
            await _container.UpsertItemAsync(
                rowError,
                new PartitionKey(rowError.TenantId),
                cancellationToken: ct);
        }
    }
}
