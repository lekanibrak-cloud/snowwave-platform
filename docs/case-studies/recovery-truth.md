# Recovery Truth: Replay Is Not Recovery

## Established mechanisms
Service Bus queues, dead-letter queues, redelivery, retry, and idempotent processing are established messaging/reliability mechanisms.

## Snowwave-specific problem
An early recovery path could make operational action look like successful processing. Snowwave corrected that semantic error.

```text
operator / tool requests replay
        ↓
dlq-replay queue
        ↓
ProcessDlqReplays
        ↓
canonical ingest / correction path
        ↓
downstream outcome
        ↓
only then reconcile recovery state
```

`RECOVERED` is not written merely because a message was re-sent. The live C# recovery worker is Service Bus-triggered; true reprocess invokes the canonical parcel-ingest core. Manual-correction recovery also reconciles only after the correction path succeeds.

The Python recovery module is operator tooling, not the deployed processing worker, and cannot declare domain success.

## Snowwave claim
**I separated recovery intent from recovery truth and made downstream processing confirmation the authority for successful recovery.**

This is not a claim to have invented DLQs, replay, retries, or reconciliation.

## Evidence
See `evidence/recovery/confirmed-recovery.sanitized.md`.

## Authoritative references
- Azure Service Bus dead-letter queues  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues
- Azure Service Bus message transfers, locks, and settlement  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement
- Azure Architecture Center — Retry pattern  
  https://learn.microsoft.com/en-us/azure/architecture/patterns/retry
