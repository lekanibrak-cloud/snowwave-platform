# Governed business rules — Reason Code Catalogue

A reason code began as something that could look like a UI label. The Business Exceptions audit showed that it was actually a cross-layer business contract.

The catalogue models a reason as versioned data. Representative fields include:

```text
code
version
state
displayLabel
description
appliesTo[]
declarableBy[]
countsAsAttempt
requiresEvidence
terminalForFlow
defaultOwnership
supersedes
legacyWireValues[]
```

One rule demonstrates why this matters: a driver may report an observable fact, while a business conclusion such as a confirmed cancellation can require a different authority.

The seed factory is deterministic and frozen: changing the meaning of an existing row is treated as a new version rather than an in-place rewrite.

**Transferable concern:** business vocabulary that has accumulated workflow, authorization, evidence, ownership, and lifecycle semantics should not remain duplicated literals across clients and services.

**Current boundary:** catalogue code and tests exist. The private README snapshot reviewed for this portfolio records that release-time seed/assert had been merged but the first release runs were still failing during the seed step. This portfolio therefore does not claim that release-time catalogue convergence was green at that snapshot.
