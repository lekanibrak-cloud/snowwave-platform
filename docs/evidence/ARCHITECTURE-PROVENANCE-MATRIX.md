# Architecture Provenance Matrix

This matrix is the final claim-attribution check for the public portfolio.

| Snowwave area | Established mechanism / knowledge | Snowwave-specific engineering | Evidence |
|---|---|---|---|
| Manifest relay | Cosmos Change Feed + Functions trigger + leases | committed job ledger drives work; upload avoids enqueue dual-write | durable-manifest case study |
| Stale work | watchdog/sweeper + scheduled retry | one-minute sweeper rediscovers due retries, stale RECEIVED and stale claims | durable-manifest case study |
| Concurrency | optimistic concurrency / ETags | CAS ownership for manifest transitions and stale-claim reclamation | ETag/CAS evidence |
| Messaging | at-least-once delivery, duplicate detection | stable row message identity + duplicate-safe domain processing | idempotency case study |
| Recovery | DLQ, replay, retry | replay request is not recovery; downstream confirmation owns RECOVERED | recovery-truth case study |
| API boundary | API gateway, subscriptions, bearer identity | APIM outer product boundary + backend Entra/tenant/ownership enforcement | integration-security case study |
| Security ordering | authentication/authorization principles | source-level test freezes Snowwave declaration-door ordering | declaration-order evidence |
| Business rules | versioned/reference data governance | measured vocabulary drift → backend-owned semantic reason catalogue | governed-data case study |
| Deployment | artifact provenance / post-deploy verification | false-green incident → deployment ownership split + loaded-binary truth | deployment-runtime case study |
| Observability | distributed tracing / telemetry | Snowwave correlation and state/age signals around its workflows | architecture/evidence docs |

## Attribution rule

The portfolio may say **“I designed Snowwave's X”** when X means the Snowwave-specific composition, rule, lifecycle, implementation, or invariant.

It should not use that phrasing to imply invention of the underlying industry mechanism.

## Source rule

Official Microsoft/Azure documentation is used primarily as **current authoritative reference** for platform behavior. A page is called a historical design influence only when that historical relationship can actually be established.
