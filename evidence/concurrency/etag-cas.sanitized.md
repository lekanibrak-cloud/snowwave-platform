# Concurrency evidence — ETag compare-and-swap

A durable workflow can have more than one worker observe the same state. Snowwave's manifest-job store uses the Cosmos DB ETag as a compare-and-swap token rather than assuming one worker will always arrive first.

```csharp
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
    catch (CosmosException ex)
        when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
    {
        return (false, null);
    }
}
```

**Invariant:** a stale worker may lose the transition, but it must not overwrite the worker that already advanced the job.

**Transferable concern:** concurrent consumers updating a shared business entity.

**Boundary:** this excerpt demonstrates optimistic concurrency on this workflow; it is not a claim that every Snowwave write uses this exact mechanism.
