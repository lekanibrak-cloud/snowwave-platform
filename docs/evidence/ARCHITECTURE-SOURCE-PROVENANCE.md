# Architecture Research & Source Provenance

This document separates three things:

1. **Established engineering/platform knowledge** — concepts and Azure behavior documented publicly.
2. **Snowwave's design decision** — how that knowledge was evaluated against Snowwave's problem.
3. **Snowwave evidence** — the implementation or test that supports the portfolio claim.

It is not a claim that every reference below was the exact historical page read during development. A source is labeled **design influence** only when that historical connection is known. Otherwise it is a **current authoritative reference** that documents the established mechanism.

---

## 1. Durable manifest ingestion: Change Feed, CAS, and stale-job sweeping

### Established knowledge

Azure Cosmos DB Change Feed is a persistent change stream. Azure Functions can consume it through the Cosmos DB trigger, which uses Change Feed Processor infrastructure and a lease container to maintain processing state across instances.

Microsoft's current troubleshooting guidance also documents an important failure consideration: depending on retry configuration, unhandled function failures can result in missing downstream processing, so application-level error handling and observability remain necessary.

**Authoritative references**
- Microsoft Learn — Use Change Feed with Azure Functions  
  https://learn.microsoft.com/en-us/azure/cosmos-db/change-feed-functions
- Microsoft Learn — Read the Azure Cosmos DB change feed  
  https://learn.microsoft.com/en-us/azure/cosmos-db/read-change-feed
- Microsoft Learn — Troubleshoot the Azure Functions trigger for Cosmos DB  
  https://learn.microsoft.com/en-us/azure/cosmos-db/troubleshoot-changefeed-functions

### Verified Snowwave implementation

Snowwave **does use Cosmos Change Feed** for durable manifest processing.

The upload path writes the raw CSV to Blob and commits a `manifestJobs` document as `RECEIVED`. It does not also enqueue the parse job. A Cosmos Change Feed-triggered `ManifestParseWorker` derives work from the committed ledger.

Snowwave then adds a second progress mechanism: a **one-minute retry/stale-job sweeper**. The durable ledger is queried for:

- due `RETRY_PENDING` jobs;
- stale `RECEIVED` jobs, covering bootstrap/missed-event scenarios;
- stale `PARSING` / `PUBLISHING` claims, covering crashed-worker reclamation.

Every ownership/state transition is protected with Cosmos ETag compare-and-swap. A stale claim can be reclaimed, but reclamation is itself CAS-protected so only one worker wins.

### Claim boundary

**Safe:** “I designed Snowwave's durable manifest relay around a committed Cosmos job ledger, Change Feed-triggered processing, ETag/CAS ownership, and a timer sweeper that rediscovers due retries and stale work.”

**Do not claim:** invention of Change Feed, optimistic concurrency, scheduled retry, or the sweeper/watchdog pattern.

The Snowwave-specific engineering is the lifecycle and combination of these mechanisms: durable source artifact + durable processing ledger + Change Feed normal path + CAS ownership + stale-work rediscovery + bounded retry + visible terminal states.

**Portfolio case study**
- `docs/case-studies/durable-manifest-ingestion.md`

---

## 2. Event-driven processing, redelivery, and idempotency

### Established knowledge

Azure Service Bus Peek Lock provides at-least-once delivery: a message can be redelivered if processing or settlement does not complete. Microsoft explicitly recommends idempotent consumers. Duplicate detection protects against some duplicate sends but does not replace receiver-side idempotency.

**Authoritative references**
- Microsoft Learn — Prevent message loss and duplicate processing in Azure Service Bus  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-message-loss-and-duplicates
- Microsoft Learn — Service Bus duplicate detection  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection
- Azure Architecture Center — Minimize coordination  
  https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/minimize-coordination

### Snowwave-specific decision

Snowwave uses Service Bus-triggered processing and treats duplicate-safe/idempotent behavior as an application responsibility. Its recovery model also refuses to equate “replay requested” with “successfully recovered.”

### Claim boundary

**Safe:** “I designed Snowwave consumers and recovery paths for at-least-once messaging semantics and duplicate-safe processing.”

**Do not claim:** that Snowwave invented idempotent consumers, at-least-once delivery, DLQs, or duplicate detection.

**Important:** Do not state that Service Bus duplicate detection is enabled unless configuration evidence proves it.

---

## 3. Dead-lettering, retry, and recovery truth

### Established knowledge

Microsoft documents finite retries, dead-letter handling for work that cannot be processed, and idempotency as core reliability concerns. A DLQ is an established messaging mechanism.

**Authoritative references**
- Azure Architecture Center — Transient fault handling  
  https://learn.microsoft.com/en-us/azure/architecture/best-practices/transient-faults
- Microsoft Learn — Service Bus message loss and duplicate processing  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-message-loss-and-duplicates

### Snowwave-specific decision

Snowwave's distinctive implementation decision is its **truth semantics**:

```text
replay requested != recovered
```

The recovery record becomes `RECOVERED` only after downstream processing confirms success. Earlier behavior that marked recovery too early was corrected.

This is an application/domain invariant built on top of established queue/DLQ/retry mechanisms.

**Portfolio evidence**
- `evidence/recovery/confirmed-recovery.sanitized.md`

---

## 4. Cosmos DB ETags / optimistic concurrency

### Established knowledge

Cosmos DB implements optimistic concurrency through resource ETags. A conditional write can use `If-Match`; if the supplied ETag is stale, Cosmos rejects the operation with HTTP `412 Precondition Failed`.

**Authoritative references**
- Microsoft Learn — Azure Cosmos DB FAQ: concurrency / ETags  
  https://learn.microsoft.com/en-us/azure/cosmos-db/faq
- Azure Architecture Center — Minimize coordination  
  https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/minimize-coordination

### Snowwave-specific decision

Snowwave applies this platform mechanism to manifest-job transition ownership. It reads state with its ETag and performs replacement conditionally. A 412 is interpreted as **the CAS was lost; another writer owns the winning transition**, rather than overwriting newer state.

**Portfolio evidence**
- `evidence/concurrency/etag-cas.sanitized.md`

### Claim boundary

**Safe:** “I applied Cosmos DB optimistic concurrency/ETag conditional writes to prevent stale workers from overwriting manifest-job transitions.”

**Do not claim:** invention of optimistic concurrency, ETags, CAS, or HTTP conditional-write semantics.

---

## 5. API Management as the retailer integration boundary

### Established knowledge

Azure API Management provides a gateway boundary for published APIs. Microsoft documents subscription keys as an access-control/product mechanism, but explicitly cautions that a subscription key alone is not strong authentication and recommends pairing it with another authentication/authorization method where appropriate.

**Authoritative references**
- Microsoft Learn — API Management authentication and authorization  
  https://learn.microsoft.com/en-us/azure/api-management/authentication-authorization-overview
- Microsoft Learn — API Management subscriptions  
  https://learn.microsoft.com/en-us/azure/api-management/api-management-subscriptions
- Azure Well-Architected — API Management guidance  
  https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-api-management

### Snowwave-specific decision

Snowwave's APIM boundary uses product/subscription controls as an outer integration control while the Function application's Entra validation remains the primary identity/authentication gate. Correlation identity is propagated through ingress.

**Portfolio evidence**
- `evidence/integration/apim-boundary.sanitized.md`

### Claim boundary

**Safe:** “I designed Snowwave's retailer ingress boundary using APIM for API/product controls while keeping application identity validation at the backend boundary.”

**Do not claim:** that subscription keys alone provide strong authentication, or that APIM validates JWTs unless the deployed policy actually does.

---

## 6. Managed identity and RBAC

### Established knowledge

Microsoft recommends managed identities with Microsoft Entra ID for Azure Functions connections where supported, reducing the need to store service credentials. Access still depends on appropriate data-plane permissions/RBAC.

**Authoritative reference**
- Microsoft Learn — Configure Azure Functions connections to remote services  
  https://learn.microsoft.com/en-us/azure/azure-functions/functions-identity-based-connections-tutorial

### Snowwave-specific decision

Snowwave uses managed identity/RBAC across verified Azure service connections rather than presenting credentials as application-owned secrets. The portfolio should name only connections actually verified from source/configuration.

### Claim boundary

**Safe:** describe the verified managed-identity connections and assigned roles.

**Do not claim:** universal “secretless everything” unless every relevant connection has been verified.

---

## 7. Correlation and distributed traceability

### Established knowledge

Distributed/event-driven systems need stable correlation metadata because brokered communication decouples producers from consumers. Azure Architecture Center recommends correlation identifiers so related operations can be joined into an end-to-end trace.

**Authoritative reference**
- Azure Architecture Center — Publisher-Subscriber pattern  
  https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber

### Snowwave-specific decision

Snowwave preserves or creates a correlation ID at retailer ingress and uses that identity as part of its traceability model.

### Claim boundary

**Safe:** “I implemented correlation propagation for Snowwave's ingestion flow.”

**Do not claim:** invention of correlation IDs or distributed tracing.

---

## 8. Authorization ordering

### Established knowledge

Authentication, authorization, tenant isolation, ownership validation, and information-disclosure prevention are established security concerns.

### Snowwave-specific decision

The notable Snowwave contribution is the **specific executable invariant** discovered from its declaration doors:

```text
request shape
→ authentication
→ tenant/profile authorization
→ ownership
→ business-rule validation
→ domain action
```

A source-level architecture test scans gated endpoints and fails the build if the ordering regresses.

This portfolio can legitimately emphasize the implementation and test because the claim is not that Snowwave invented authentication-before-business-validation. The claim is that the project converted its required ordering into an executable architectural constraint after finding a real ordering defect.

**Portfolio evidence**
- `evidence/governance/declaration-ordering.sanitized.md`

---

## 9. Versioned reason semantics

### Established knowledge

Versioned configuration, governed reference data, schema evolution, compatibility, and lifecycle management are established software/data-governance practices.

### Snowwave-specific decision

The Snowwave-specific work was discovering measured vocabulary drift across its own layers and moving delivery/pickup reason semantics into a backend-owned business document with properties such as applicability, declarer, attempt semantics, evidence requirements, ownership, terminal behavior, lifecycle state, supersession, and legacy wire values.

**Portfolio evidence**
- `evidence/governance/reason-code-catalogue.sanitized.md`

### Claim boundary

**Safe:** “I modelled Snowwave's reason semantics as versioned governed business data after an audit found drift across application layers.”

**Do not claim:** invention of reference-data governance, versioning, or policy-as-data as general concepts.

---

## 10. Deployment intent versus loaded runtime identity

### Established knowledge

CI/CD validation, artifact identity, immutable build provenance, and post-deployment verification are established delivery/reliability practices.

### Snowwave-specific decision

Snowwave experienced a non-production false-green deployment in which configuration could indicate a fresh deployment while an older package was serving. The resulting implementation separates configured deployment intent from identity embedded in the loaded assembly and tests that fresh settings cannot make an unstamped/stale binary appear current.

**Portfolio evidence**
- `evidence/deployment/loaded-binary-truth.sanitized.md`

### Claim boundary

**Safe:** describe the observed Snowwave failure, the resulting invariant, and its tests.

**Do not claim:** invention of artifact provenance or deployment verification.

---

# Attribution levels used in this portfolio

Use these labels when adding future references:

### A. Historical design influence
Use only when there is evidence or reliable recollection that a specific source materially influenced the decision at the time.

### B. Current authoritative reference
The source documents the established platform behavior or pattern, but the portfolio does **not** claim it was necessarily the exact page used historically.

### C. Comparative reference
A source used later to explain how Snowwave's implementation relates to established practice.

This version intentionally uses **B** for the Microsoft references above unless a historical design influence can be established.

# Core rule

> Researching an established pattern does not transfer authorship of the pattern to Snowwave, and using an established pattern does not remove authorship of the Snowwave-specific reasoning and implementation.

The portfolio therefore gives credit in both directions: Microsoft/Azure and the broader engineering discipline receive attribution for documented platform mechanisms and established patterns; the author receives credit for the Snowwave-specific problem discovery, architectural decisions, implementation, testing, failure analysis, and revisions.
