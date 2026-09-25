# Transferable Engineering Lens

Snowwave is intentionally concrete: it models last-mile logistics. This document separates the **domain implementation** from the **engineering capabilities demonstrated by it**.

## Domain specificity is a feature

Good architecture starts with the actual domain. Snowwave's parcel states, loading rules, reason semantics, correction windows, and operational workflows should not be copied blindly into banking, healthcare, manufacturing, insurance, or another logistics company.

The transferable capability is not the state names.

It is the ability to discover and formalize the rules behind them.

## Cross-industry patterns

### External integration

**Snowwave example:** retailer/manifest ingestion through controlled interfaces.

**Transferable question:** How does data cross an organizational or system boundary, and how do we know what was accepted?

Possible analogues include orders, claims, payment instructions, supplier records, referrals, reservations, and manufacturing work orders.

### Asynchronous processing

**Snowwave example:** Service Bus-backed processing.

**Transferable question:** What happens when work is decoupled in time and may be delivered more than once?

The implementation must consider idempotency, correlation, retries, poison work, observability, and durable state.

### Recovery

**Snowwave example:** DLQ records, replay, correction, downstream-confirmed reconciliation.

**Transferable question:** Does the system record what an operator *asked it to do*, or what actually succeeded?

This distinction applies to many workflows where commands cross asynchronous boundaries.

### Business exceptions

**Snowwave example:** operational issues that require intervention but are not infrastructure failures.

**Transferable question:** How does the system represent work that is technically valid but cannot progress without a business decision?

### Governed business rules

**Snowwave example:** versioned reason-code semantics.

**Transferable question:** Which business concepts have become important enough that scattered strings and duplicated predicates are no longer safe?

### Concurrency

**Snowwave example:** ETag compare-and-swap for manifest-job transitions.

**Transferable question:** What happens when two workers legitimately try to advance the same business entity at the same time?

### Authorization

**Snowwave example:** authentication and ownership checks before reason validation.

**Transferable question:** Is business information itself protected, or does validation reveal information before authorization?

### Executable architecture

**Snowwave example:** source-level test enforcing declaration-door ordering.

**Transferable question:** Which architectural decisions are important enough that documentation alone is insufficient?

## The boundary

These patterns are transferable. Their exact implementation is not automatically transferable.

A payments platform may require stronger consistency and regulatory controls. A healthcare system may introduce privacy and clinical-safety requirements. Manufacturing may prioritize edge connectivity and equipment integration. High-frequency financial systems may have entirely different latency constraints.

The architecture must follow the domain.

The engineering skill is being able to **discover those constraints, make them explicit, and build evidence that the system respects them**.
