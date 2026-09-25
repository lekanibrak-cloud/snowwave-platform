# Ingestion evidence — sanitized excerpt

The private worker uses a Service Bus trigger and delegates business processing to one canonical ingest service.

```csharp
[Function("ProcessParcelIngestEvent")]
public async Task Run(
    [ServiceBusTrigger("%PARCEL_INGEST_QUEUE_NAME%", Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message,
    ServiceBusMessageActions actions,
    CancellationToken ct)
{
    var correlationId = GetCorrelationId(message);
    var result = await _ingestService.ProcessParcelIngestCoreAsync(
        message.Body.ToString(), correlationId, ct);

    switch (result.Outcome)
    {
        case IngestOutcome.Success:
            return;

        case IngestOutcome.AlreadyApplied:
            await actions.CompleteMessageAsync(message, ct);
            return;

        // Invalid or incomplete work is recorded for recovery
        // and can be dead-lettered rather than silently dropped.
    }
}
```

**What this proves:** asynchronous ingestion has explicit outcomes and one canonical processing seam.

**What this does not prove by itself:** every upstream integration path or every downstream parcel transition.
