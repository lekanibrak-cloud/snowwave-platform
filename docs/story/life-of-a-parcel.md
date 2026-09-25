# The Life of a Parcel

This walkthrough uses one conceptual parcel to connect Snowwave's integration, state, recovery, business-exception, and governance architecture.

It is not a claim that every parcel traverses every branch. The purpose is to show what the platform must know when the happy path diverges.

## 1. Entry: how did this parcel get here?

For retailer API ingestion, Azure API Management is the external boundary.

The current infrastructure defines a parcel-ingestion API, subscription-required retailer products, a Function backend, and an operation policy that preserves or generates a correlation ID and echoes it to the caller. Bearer authentication is forwarded to the Function, where application authorization remains the primary authentication boundary.

Snowwave also models manifest ingestion as durable work rather than treating an upload request as the entire transaction.

The system needs to distinguish:

```text
request accepted
        ≠
manifest fully processed
        ≠
every parcel successfully materialized
```

That distinction is the beginning of the truth model.

## 2. Processing: what does Snowwave believe?

Parcel ingestion is processed asynchronously through Azure Service Bus.

`ProcessParcelIngests` delegates canonical processing to `ParcelIngestService.ProcessParcelIngestCoreAsync` and handles explicit outcomes. The worker can complete successfully, recognize already-applied work, classify invalid/missing input, create recovery records, and dead-letter messages that cannot safely continue.

This matters because at-least-once systems must expect duplicates and retries. A duplicate is not automatically a second business event.

For manifest job transitions, Snowwave also uses Cosmos DB ETags as compare-and-swap guards. If another worker wins the transition, the loser receives a 412 and does not overwrite the winner.

## 3. Technical failure: recovery and truth

When processing cannot safely continue, the important question becomes:

> What does "recovered" mean?

Snowwave's later recovery model rejects a convenient but incorrect definition: **message re-enqueued = recovered**.

`ProcessDlqReplays` processes corrected/replayed DLQ work. For supported no-correction parcel-ingest replays, it can re-enter the canonical ingest core. For manual-review correction paths, it applies the correction and reconciles only after successful downstream work.

The worker's responsibility is deliberately asymmetric:

```text
replay requested
      ↓
processing starts
      ↓
processing succeeds ───────► reconcile recovery
      │
      └── processing fails ─► remain unresolved / dead-letter
```

The recovery record therefore represents what actually happened downstream rather than what the operator asked the system to attempt.

## 4. A different problem: the system works, but the business situation does not

Not every exception belongs in DLQ recovery.

A parcel may be technically healthy while an operational condition still requires human resolution. Snowwave therefore separates:

| Concern | Source of truth |
|---|---|
| Parcel lifecycle | parcel state |
| Business exception workflow | parcel exception-review state + audit/history |
| Technical recovery | DLQ/recovery state |

That prevents an operator from having to infer business readiness from technical recovery status, or vice versa.

The architecture question changes from:

> Can the message be processed?

to:

> Is the parcel ready for the next legitimate business action?

## 5. Why a reason is more than a label

Business Exceptions exposed a vocabulary problem.

A reason such as an address issue, damage, customer outcome, or pickup condition can affect much more than UI text.

Conceptually, the system may need to know:

```text
REASON
  ├─ where may it be used?
  ├─ who may declare it?
  ├─ does it count as an attempt?
  ├─ is evidence required?
  ├─ who owns the resulting work?
  ├─ is it terminal for this flow?
  └─ which version of the meaning applied?
```

The Reason Code Catalogue makes those semantics explicit and versioned instead of scattering them across client arrays and backend predicates.

## 6. Governance changes security

Once reason semantics are governed business information, even validation can leak information.

The declaration boundary therefore requires:

```text
authentication
    before
tenant/profile authorization
    before
parcel ownership
    before
reason-code validation
```

`DeclarationDoorOrderingTests` scans every gated declaration endpoint and fails the build if the ordering regresses.

This is an example of architecture moving from documentation into executable evidence.

## 7. What this parcel story demonstrates

Following one parcel reveals several different kinds of truth:

- **integration truth** — what request entered and how it is correlated;
- **processing truth** — whether canonical processing actually applied;
- **state truth** — what Snowwave currently believes about the parcel;
- **concurrency truth** — which worker owns a transition;
- **recovery truth** — whether downstream processing actually recovered;
- **business truth** — whether the parcel is operationally ready;
- **governance truth** — what a reason means and who may declare it;
- **security truth** — what a caller is allowed to learn before authorization.

The platform is therefore less about moving a record through a happy-path state machine and more about preserving a defensible account of what happened when physical operations, people, and asynchronous software disagree.
