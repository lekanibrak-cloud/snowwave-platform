# Snowwave Platform

**Azure distributed-systems portfolio by Olalekan Ibrahim**

> **Business → Operations → Engineering → Architecture**  
> **Observe → Model → Build → Fail → Recover → Formalize → Govern → Verify**

Snowwave is a **non-production Azure platform for last-mile operations** that I built to explore a broader engineering problem:

## How does a distributed system keep telling the truth when the happy path fails?

A parcel is the concrete example. The underlying concerns are transferable: external integrations, asynchronous processing, idempotency, concurrency, recovery, authorization, human/business exceptions, versioned rules, observability, infrastructure as code, and deployment verification.

**Core stack:** .NET 8 Azure Functions · Service Bus · Cosmos DB · API Management · Entra ID · SignalR · Bicep · GitHub Actions OIDC · Application Insights / Log Analytics · React · React Native

**Status:** built and deployed to **non-production Azure**; not serving real customers. This repository is a curated, sanitized portfolio rather than a mirror of the private source repository.

### Start here

| If you have… | Read this |
|---|---|
| **30 seconds** | This page: problem, architecture, evidence, engineer |
| **2 minutes** | [The Life of a Parcel](docs/story/life-of-a-parcel.md) |
| **10+ minutes** | [Technical Evidence Index](docs/evidence/TECHNICAL-EVIDENCE-INDEX.md) + case studies |
| **Interested in evolution** | [Evidence-backed timeline](docs/evidence/EVOLUTION-TIMELINE.md) |

---

## Architecture at a glance

```text
Retailer / Manifest
        │
        ▼
Azure API Management
        │
        ▼
Azure Functions ─────────────► Cosmos DB
        │                         durable state
        ▼
Azure Service Bus
        │
        ├──────── success ─────► parcel workflow
        │
        └──────── failure ─────► recovery / DLQ
                                      │
                                 correct / replay
                                      │
                                      ▼
                              canonical processing
                                      │
                              downstream confirmation
                                      │
                                      ▼
                               reconciled truth

Identity: Entra ID + managed identities + RBAC
Realtime: SignalR (notification, not durable truth)
Delivery: Bicep + GitHub Actions OIDC + post-deploy validation
Observability: Application Insights + Log Analytics
```

The architecture is less about moving a parcel through a happy-path state machine and more about preserving a defensible account of what happened when people, physical operations, and asynchronous software disagree.

[Follow the parcel journey](docs/story/life-of-a-parcel.md)

---

## Engineering decisions worth inspecting

### Durable ingestion: event-driven normal path, stale-work safety net

Snowwave's manifest intake commits a durable job ledger and uses Cosmos Change Feed as the normal relay. A one-minute sweeper independently rediscovers due retries, stale `RECEIVED` jobs, and stale worker claims; ETag/CAS prevents the relay and sweeper from both owning the same transition.

**Case study:** [durable manifest ingestion](docs/case-studies/durable-manifest-ingestion.md)

### Additional decisions

### 1. Recovery means confirmed processing, not “message resent”

Snowwave distinguishes **replay requested** from **recovered**. Supported replays re-enter canonical processing; the recovery record advances only after downstream success. A failed replay remains visible rather than creating false success.

**Evidence:** [confirmed recovery](evidence/recovery/confirmed-recovery.sanitized.md)

### 2. Concurrency is handled as an ownership problem

Manifest-job transitions use Cosmos DB ETags as compare-and-swap guards. If two workers race, the stale writer loses with HTTP 412 rather than overwriting the winning transition.

**Evidence:** [ETag/CAS](evidence/concurrency/etag-cas.sanitized.md)

### 3. Business vocabulary became versioned business data

A Business Exceptions audit found reason semantics distributed across multiple layers. Snowwave moved those semantics into a versioned Reason Code Catalogue carrying applicability, declaration authority, evidence requirements, attempt semantics, ownership, terminality, and compatibility information.

**Evidence:** [Reason Code Catalogue](evidence/governance/reason-code-catalogue.sanitized.md)

### 4. Authorization order became executable architecture

A declaration endpoint once validated a reason before authentication. The corrected boundary is:

```text
request shape
→ authentication
→ tenant/profile authorization
→ parcel ownership
→ business-rule validation
→ domain action
```

An architecture test scans every gated declaration door and fails the build if the ordering regresses.

**Evidence:** [declaration-door ordering](evidence/governance/declaration-ordering.sanitized.md)

### 5. Deployment intent is not runtime truth

A non-production false-green deployment showed that fresh configuration could describe a new deployment while an older package was still serving. Snowwave now separates configured intent from identity stamped into the loaded assembly, with regression tests preventing one from impersonating the other.

**Evidence:** [loaded-binary truth](evidence/deployment/loaded-binary-truth.sanitized.md)

[Browse all technical evidence](docs/evidence/TECHNICAL-EVIDENCE-INDEX.md)

### Provenance-reviewed deep dives

[Durable manifest ingestion](docs/case-studies/durable-manifest-ingestion.md) ·
[Recovery truth](docs/case-studies/recovery-truth.md) ·
[Idempotency & redelivery](docs/case-studies/idempotency-and-redelivery.md) ·
[Integration & security](docs/case-studies/integration-security-boundary.md) ·
[Governed business rules](docs/case-studies/business-rules-as-governed-data.md) ·
[Deployment runtime truth](docs/case-studies/deployment-runtime-truth.md)

[See the architecture provenance matrix](docs/evidence/ARCHITECTURE-PROVENANCE-MATRIX.md)


---

## The parcel is the example. The engineering method is broader.

Snowwave is logistics-specific by design. The implementation should **not** be copied blindly into another industry. The transferable capability is turning business reality into explicit boundaries, state, ownership, failure handling, rules, and evidence.

| Snowwave concept | Broader systems concern |
|---|---|
| Parcel | Business entity / transaction |
| Retailer / manifest ingestion | External-system integration |
| Service Bus processing | Asynchronous workflow |
| Correlation ID | Distributed traceability |
| DLQ + replay | Failure recovery |
| Business Exception | Human/business intervention |
| Reason Code Catalogue | Versioned business-rule authority |
| Cosmos ETag/CAS | Concurrency control |
| Tenant + ownership checks | Authorization boundary |
| Architecture tests | Executable governance |

In another domain the entity could be a payment, claim, purchase order, referral, manufacturing job, support case, or reservation. The exact consistency, privacy, regulatory, latency, and availability requirements would change; the discipline of discovering and enforcing those constraints remains.

[Read the transferable engineering lens](docs/story/transferable-engineering.md)

---

## How the architecture evolved

Snowwave did not emerge from a finished architecture diagram.

```text
Operational observation
        ↓
ingestion + recovery co-evolve
        ↓
replay success is separated from recovery truth
        ↓
technical failure is separated from business exception
        ↓
exception modelling exposes vocabulary drift
        ↓
reason semantics become governed data
        ↓
governed data creates security + deployment invariants
        ↓
important architecture rules become executable evidence
```

This is intentionally not presented as a perfect linear build. Repository history shows that ingestion and recovery overlapped and that later evidence corrected earlier assumptions.

[Read the origin story](docs/story/from-observation-to-governed-rules.md) · [See the evidence-backed timeline](docs/evidence/EVOLUTION-TIMELINE.md)

---

## Research and design provenance

Snowwave was built through a combination of operational observation, implementation, testing, failure analysis, and research into established software and cloud-architecture practices.

When I encountered an unfamiliar problem, I researched how that class of problem is handled publicly before deciding what fit Snowwave. Examples include ingestion and source-system boundaries, asynchronous failure handling, dead-letter/replay patterns, idempotency, optimistic concurrency, API gateways, identity and authorization, correlation, observability, and deployment verification.

Those concepts are **established industry knowledge** and are not presented here as inventions of Snowwave.

What this portfolio claims as my work is the Snowwave-specific engineering: identifying the problem in this domain, deciding which established ideas applied, modelling the resulting boundaries and rules, implementing them, testing them, observing where assumptions failed, and revising the system.

For example:

```text
Snowwave exposes a silent-failure problem
        ↓
research the established problem space
        ↓
understand source-system / ingestion responsibilities
        ↓
decide what applies to Snowwave
        ↓
implement Snowwave-specific validation + failure handling
        ↓
test the behaviour
        ↓
revise when evidence contradicts the design
```

Where a specific external publication materially shaped a design and can be reliably identified, it should be acknowledged in the relevant design record. This portfolio does not invent retrospective citations for sources that cannot be established.

[See the architecture source-provenance map](docs/evidence/ARCHITECTURE-SOURCE-PROVENANCE.md), which separates authoritative Microsoft/Azure references from Snowwave-specific decisions and evidence.

---

## Evidence boundaries

| State | Meaning |
|---|---|
| **Built** | Implementation exists in the private source repository. |
| **Verified** | Tests, deployment checks, or observed non-production behaviour provide evidence. |
| **Designed** | Specified, but not represented as completed implementation. |
| **Not yet proven** | Snowwave does not claim the result or experience. |

Snowwave does **not** claim production customer traffic, production/on-call experience from this project, or regulated-enterprise production operation.

One example of this discipline: the Reason Code Catalogue implementation and release seed/assert logic exist, but the private repository snapshot reviewed for this portfolio recorded the release-time seed step as **merged but not yet green**. This portfolio preserves that distinction rather than converting implementation intent into runtime proof.

The public repository excludes private URLs, environment identifiers, secrets, operational records, and unrelated private-source implementation.

---

## About the engineer

**Olalekan Ibrahim — Toronto, Canada**

My route into cloud engineering did not begin with software.

I grew up around my family's agricultural business in Nigeria and later took on management responsibility in that environment. My experience included agricultural raw-material supply, poultry operations, purchasing, inventory, suppliers, customers, and the day-to-day coordination required to keep a small business moving.

I did not call that systems thinking at the time. But it taught me an early version of it: **a business is a network of dependencies**. Purchasing affects availability. Inventory affects sales. Suppliers affect fulfilment. Information affects decisions. A breakdown in one part of the operation becomes somebody else's problem downstream.

I later studied **International Business Management at Niagara College Toronto**. That added a more formal business perspective to what I had learned operationally: organizations, finance, supply chains, decision-making, and how different functions contribute to an outcome.

Working at **a last-mile courier in Toronto** brought me into last-mile operations. My work has included parcel sorting, scanning, routing support, loading, and accuracy checks. Being close to the physical operation made me increasingly curious about the software behind it.

The question that eventually became Snowwave was essentially:

> **What software and architecture have to exist behind an operation like this for the system's version of reality to remain aligned with what is physically happening?**

I began studying Azure, completed **AZ-900**, and moved from learning individual cloud concepts toward building a distributed system. Snowwave became the environment where I could translate operational questions into software: Azure Functions, Service Bus, Cosmos DB, API Management, managed identities, RBAC, SignalR, observability, infrastructure as code, CI/CD, recovery workflows, concurrency controls, versioned business rules, and executable architecture invariants.

### Three perspectives now shape how I approach systems

**Business** — Why does the system exist? What outcome does it support? Who owns the decisions and consequences?

**Operations** — What actually happens when the designed process meets people, physical constraints, incomplete information, exceptions, and failure?

**Engineering** — How can those realities become explicit state, interfaces, rules, security boundaries, recovery mechanisms, infrastructure, tests, and observable evidence?

Snowwave sits at the intersection of those three perspectives.

Logistics is the domain of this project. **The engineering method is intended to be transferable.** I am interested in systems where business processes cross application and organizational boundaries: cloud platforms, integrations, distributed workflows, operational systems, and the reliability mechanisms that keep them trustworthy.

My current direction is **Azure cloud, platform, and integration engineering**, with a long-term focus on **solution architecture** — connecting technical architecture to the business and operational reality it is supposed to support.

---

## What I want this portfolio to demonstrate

Not that I already know every industry.

Not that a non-production portfolio project is equivalent to operating a large regulated production platform.

And not that one architecture should be copied everywhere.

The evidence here is meant to demonstrate a way of working:

```text
understand the business context
        ↓
observe the real operation
        ↓
identify the system boundary
        ↓
model state and ownership explicitly
        ↓
design for failure, not only success
        ↓
implement and test the rule
        ↓
observe what contradicts the design
        ↓
revise the architecture
        ↓
turn important lessons into durable mechanisms
```

That is the capability I want to carry into other domains.
