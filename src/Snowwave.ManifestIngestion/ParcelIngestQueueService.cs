using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Snowwave.ManifestIngestion;

public sealed class ParcelIngestQueueService : IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public ParcelIngestQueueService(ServiceBusClient serviceBusClient, IConfiguration configuration)
    {
        var queueName = configuration["PARCEL_INGEST_QUEUE_NAME"] ?? "parcel-ingest";
        _sender = serviceBusClient.CreateSender(queueName);
    }

    public ParcelIngestQueueService(ServiceBusSender sender) => _sender = sender;

    public async Task<int> EnqueueManyAsync(
        IReadOnlyList<ParcelIngestedEvent> events,
        CancellationToken cancellationToken,
        int chunkSize = 100)
    {
        if (events.Count == 0) return 0;
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var published = 0;
        for (var i = 0; i < events.Count; i += chunkSize)
        {
            var slice = events.Skip(i).Take(chunkSize).ToList();
            using var batch = await _sender.CreateMessageBatchAsync(cancellationToken);

            foreach (var evt in slice)
            {
                var parcelId = evt.Parcel?.ParcelId ?? "unknown";
                var msg = new ServiceBusMessage(BinaryData.FromObjectAsJson(evt))
                {
                    // Stable identity lets Service Bus duplicate detection absorb
                    // a whole-batch re-publish after an ambiguous failure.
                    MessageId = $"{evt.TenantId}:{parcelId}:PARCEL_INGESTED",
                    CorrelationId = evt.CorrelationId,
                    ContentType = "application/json",
                    Subject = evt.EventType,
                };

                msg.ApplicationProperties["tenantId"] = evt.TenantId;
                msg.ApplicationProperties["parcelId"] = parcelId;
                msg.ApplicationProperties["batchId"] = evt.BatchId ?? string.Empty;

                if (!batch.TryAddMessage(msg))
                {
                    if (batch.Count > 0)
                    {
                        await _sender.SendMessagesAsync(batch, cancellationToken);
                    }

                    using var solo = await _sender.CreateMessageBatchAsync(cancellationToken);
                    if (!solo.TryAddMessage(msg))
                    {
                        throw new InvalidOperationException($"Event too large to publish: {parcelId}");
                    }
                    await _sender.SendMessagesAsync(solo, cancellationToken);
                }
            }

            if (batch.Count > 0)
            {
                await _sender.SendMessagesAsync(batch, cancellationToken);
            }
            published += slice.Count;
        }
        return published;
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
