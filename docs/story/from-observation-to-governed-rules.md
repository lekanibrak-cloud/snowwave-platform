# From Physical Observation to Governed Rules

## 1. I could observe the operation, not the backend

Manifest ingestion was already part of the Snowwave roadmap. A last-mile platform needed a way for parcel information to enter the system.

A physical operational incident changed the priority of that work.

I did not have access to my employer's backend, source code, architecture, databases, or implementation. I could observe the physical side: parcels entering an operation, being scanned and moved, and the operational consequences when information and physical reality did not align.

That changed the question from:

> How should Snowwave import parcel data?

to:

> What should Snowwave do when parcel data is incomplete, invalid, duplicated, delayed, or cannot be processed safely?

I paused the broader roadmap and developed my own ingestion architecture around that question.

This is an important provenance boundary: **the operational observation informed the problem; the Snowwave implementation is my independent design.**

## 2. Ingestion exposed failure

As ingestion became asynchronous, failure became part of the architecture rather than an edge case.

A message can be valid enough to enter a queue and still fail downstream. A malformed payload may be poison. A dependency may fail. Delivery may be retried. A duplicate may arrive after state has already advanced.

Snowwave therefore evolved toward explicit outcomes rather than silent disappearance: process, no-op/idempotent completion, visible recovery record, retry, or dead-letter.

The repository history shows ingestion and recovery co-evolving. It would be inaccurate to claim that a final ingestion system was completed first and recovery was conceived only afterward. The stronger statement is that making ingestion real repeatedly exposed what recovery had to mean.

## 3. Recovery changed the definition of success

The first recovery controls were not the final model.

The project later identified a truth-model problem:

> **Replay initiated is not the same as successfully recovered.**

That distinction changed the architecture. A resubmission can place work back onto a queue, but the recovery record should not become `RECOVERED` merely because enqueue succeeded.

The later recovery path waits for downstream processing and reconciliation. When processing fails, the recovery state remains unresolved so an operator can see and act on it.

This was a shift from **command success** to **business/system truth**.

## 4. Technical recovery exposed a different kind of exception

Recovery then made another distinction unavoidable.

Some failures are technical:

- ingestion cannot deserialize a payload;
- processing exhausts retries;
- a dependency fails;
- a message requires replay or correction.

Other exceptions exist even when the infrastructure is functioning correctly:

- a delivery fails because information needs correction;
- a pickup outcome needs review;
- an address or contact detail is incomplete;
- an operational decision is required before the parcel is ready for another attempt.

Snowwave separated these concerns.

`dlqMessages` represents technical recovery/replay state. Parcel lifecycle remains on the parcel. Business exception workflow is represented separately rather than overloading either one.

The question had changed from **"How do I recover a failed message?"** to **"What work should exist when the system is healthy but the business situation is unresolved?"**

## 5. Business Exceptions exposed vocabulary drift

Once Business Exceptions became a domain, reason codes could no longer be treated as harmless UI strings.

A later repository audit measured reason vocabulary across backend gates, policy constants, the driver application, return workflows, and the operations UI. It found multiple independent vocabularies and concrete contradictions: policy values that could not pass the corresponding API gate, and driver-visible choices with no matching backend code.

That changed the next question:

> Who owns the meaning of a reason?

The answer became the Reason Code Catalogue.

## 6. Reason codes became governed business data

The catalogue turns a reason from a string into a versioned business document.

Depending on the reason, Snowwave can model:

- where the reason applies;
- which authority may declare it;
- whether it counts as an attempt;
- whether evidence is required;
- who owns the resulting work;
- whether it terminates a flow;
- lifecycle/version history;
- compatibility with older wire values.

The important architectural change is not the number of codes. It is the movement from **distributed literals** to **one governed semantic authority**.

## 7. Governance created a security boundary

Once the catalogue contained operational meaning, validation order mattered.

If a caller could ask the reason-code gate whether a value was valid before authenticating or proving ownership, the validation response itself could reveal business vocabulary.

Snowwave therefore froze declaration-door ordering:

```text
transport/request shape
→ authentication
→ profile/tenant authorization
→ parcel ownership
→ declaration completeness + reason-code validation
→ domain action
```

A source-level architecture test now scans every declaration door using the gate and fails the build if authentication or ownership moves behind validation.

The progression had reached another level:

> an operational observation eventually produced not just application behaviour, but governed business semantics and an executable security invariant.

## The recurring pattern

```text
Observe
  ↓
Model
  ↓
Build
  ↓
Encounter failure
  ↓
Recover
  ↓
Distinguish technical from business state
  ↓
Formalize business meaning
  ↓
Govern
  ↓
Verify with mechanisms
```

Snowwave's architecture is useful to me precisely because it did **not** emerge perfectly. Each implementation created evidence. That evidence either strengthened the design or exposed the next problem.
