# Durable Manifest Ingestion: Change Feed + Sweeper

## The problem

The manifest path needed to accept a retailer file without confusing **accepted input** with **completed processing**.

A direct implementation could write the uploaded file/job state and separately enqueue work. That creates a dual-write failure window: one durable operation can succeed while the other does not.

Snowwave instead makes the uploaded blob the durable source artifact and `manifestJobs` the processing ledger. The normal relay is derived from the committed ledger through Azure Cosmos DB Change Feed.

```text
Retailer
   │
   ▼
Upload endpoint
   │
   ├── Blob: raw source artifact
   │
   └── Cosmos manifestJobs: RECEIVED
                         │
                         ▼
                  Cosmos Change Feed
                         │
                         ▼
                   Parse worker
                         │
                  ETag/CAS claim
                         │
             PARSING → PUBLISHING
                         │
                         ▼
                Service Bus row events
                         │
                         ▼
                       QUEUED
```

The HTTP upload does **not** also enqueue the parse job.

## Why Change Feed is not the whole reliability model

Azure Functions' Cosmos DB trigger is an established Azure mechanism built on the Cosmos Change Feed processor and uses a lease container to maintain processing state across instances.

Snowwave does not treat successful trigger delivery as its only forward-progress mechanism.

A one-minute timer sweeper queries the durable job ledger for:

- `RETRY_PENDING` jobs whose `nextAttemptAtUtc` is due;
- `RECEIVED` jobs older than the stale threshold;
- stale `PARSING` or `PUBLISHING` claims.

The last two cases are a bootstrap / missed-event / crashed-worker safety net.

```text
                     normal path
manifestJobs ──────► Change Feed ──────► Parse worker
     │                                      │
     │                                      │ transient failure
     │                                      ▼
     │                                RETRY_PENDING
     │                                      │
     │                                      │ nextAttemptAtUtc
     │                                      ▼
     └──────────────► 1-minute sweeper ◄────┘
                         │
                         ├── stale RECEIVED
                         ├── stale PARSING/PUBLISHING claim
                         └── due RETRY_PENDING
                                   │
                                   ▼
                              re-drive safely
```

This is intentionally **belt-and-suspenders** reliability: Change Feed is the low-latency normal relay; the durable ledger gives the system something independent to interrogate when progress stops.

## Single-writer ownership

A sweeper and Change Feed worker can observe the same job. Snowwave therefore cannot rely on timing to prevent double processing.

Every transition uses Cosmos optimistic concurrency:

```text
read job + ETag
      │
      ▼
If-Match conditional replace
      │
      ├── success → this worker owns the transition
      │
      └── HTTP 412 → another worker won; stop
```

A stale in-flight claim is reclaimable, but reclamation itself uses the same CAS guard.

## Retry scheduling

A transient processing failure does not immediately spin through the Change Feed.

The worker records:

```text
status = RETRY_PENDING
attemptCount += 1
nextAttemptAtUtc = scheduled time
```

The Change Feed will see the write, but the worker ignores it while the due time is in the future. The sweeper later re-drives the job when `nextAttemptAtUtc` has elapsed.

Retries are bounded. A permanent parse problem, or a transient problem that exhausts its attempt budget, becomes visible terminal state rather than looping indefinitely.

## Poison-job behavior

The Change Feed trigger is not treated like a Service Bus queue with an application DLQ.

Processing failures are caught inside the manifest worker. The worker records failure state and normally returns so a bad manifest does not indefinitely block relay progress.

Snowwave's application states provide the failure surface:

```text
PARSE_FAILED
PUBLISH_FAILED
```

rather than pretending the underlying Change Feed itself supplied a Snowwave business recovery workflow.

## Idempotent re-entry

Reclaim and replay are safe only if repeated work does not corrupt state.

Snowwave combines:

- ETag/CAS state transitions;
- stable Service Bus message identity for row publication;
- idempotent downstream parcel handling;
- full-success gating before the job reaches `QUEUED`.

If only part of a row batch publishes, the job does not claim successful completion.

## Observability

The durable job state is also the observable progress model.

Important signals include:

```text
oldest RECEIVED age
oldest PARSING/PUBLISHING age
RETRY_PENDING age / attempt progression
terminal failure counts
Change Feed lease progress
```

Aging `RECEIVED` alone can mean either backpressure or a stalled relay. Combining job age with lease/processor progress gives a stronger diagnosis.

## Established Azure mechanisms vs. Snowwave engineering

| Layer | Established mechanism | Snowwave-specific application |
|---|---|---|
| Change detection | Cosmos DB Change Feed / Functions trigger | `manifestJobs` is the committed ledger that drives parse work |
| Processor state | Cosmos Change Feed leases | Dedicated manifest relay lease configuration |
| Concurrency | Cosmos `_etag` + `If-Match` | CAS ownership on every manifest-job transition |
| Retry | Scheduled retry is a common resilience technique | `RETRY_PENDING` + `nextAttemptAtUtc` + one-minute sweeper |
| Stale-work recovery | General watchdog/sweeper pattern | Query stale `RECEIVED` and stale manifest claims and re-drive them |
| Idempotency | Established distributed-systems requirement | Stable row message identity + downstream parcel behavior |
| Failure surfacing | Fail-loud / poison handling are established concerns | `PARSE_FAILED` / `PUBLISH_FAILED`, retained source artifact and operator visibility |
| Observability | Established reliability discipline | Job-state age + Change Feed progress used together |

The portfolio does not claim invention of Change Feed, leases, optimistic concurrency, scheduled retry, idempotency, or the sweeper/watchdog pattern.

The engineering claim is narrower and stronger: **these mechanisms were combined and adapted to Snowwave's manifest lifecycle so accepted work remains durable, concurrent recovery is safe, stale work can be rediscovered, and completion is not reported before row publication succeeds.**

## Authoritative platform references

These references document the Azure mechanisms; they are not presented as proof that they were necessarily the exact historical pages consulted during implementation.

- Microsoft Learn — Use Change Feed with Azure Functions  
  https://learn.microsoft.com/en-us/azure/cosmos-db/change-feed-functions
- Microsoft Learn — Read the Azure Cosmos DB change feed  
  https://learn.microsoft.com/en-us/azure/cosmos-db/read-change-feed
- Microsoft Learn — Troubleshoot the Azure Functions trigger for Cosmos DB  
  https://learn.microsoft.com/en-us/azure/cosmos-db/troubleshoot-changefeed-functions
- Microsoft Learn — Change Feed modes  
  https://learn.microsoft.com/en-us/azure/cosmos-db/change-feed-modes

## Evidence boundary

Verified in the private source snapshot used for this portfolio:

- Cosmos Change Feed-triggered `ManifestParseWorker`;
- lease container configuration;
- `ManifestJobStore` ETag reads and `IfMatchEtag` replacement;
- 412-as-lost-CAS behavior;
- due-job query for `RETRY_PENDING`, stale `RECEIVED`, and stale `PARSING/PUBLISHING`;
- parse service stale-claim reclamation;
- scheduled `nextAttemptAtUtc`;
- bounded retry / terminal failure behavior;
- architecture and resilience documentation identifying the worker, sweeper, CAS store, and poison guard as built.

This case study describes non-production engineering evidence. It does not claim production traffic or production SLO validation.
