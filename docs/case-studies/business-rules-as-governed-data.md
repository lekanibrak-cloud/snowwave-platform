# Business Rules as Governed Data

## Established concepts
Reference-data governance, versioning, compatibility, policy/rule metadata, and lifecycle management are established software and data-management practices.

## Snowwave-specific discovery
Snowwave's Business Exceptions work exposed a concrete internal problem: reason vocabulary and semantics had drifted across layers. At audit time, multiple hard-coded vocabularies existed without a single shared authority, and some driver declarations did not align with backend vocabulary.

The response was not merely to move strings into one file. Snowwave modelled reason codes as backend-owned business documents carrying behavior:

```text
code
+ appliesTo
+ declarableBy
+ countsAsAttempt
+ requiresEvidence
+ terminalForFlow
+ defaultOwnership
+ lifecycle state
+ supersedes / history
+ legacy compatibility
```

The catalogue therefore governs what a reason **means**, not just how it is spelled.

## Snowwave claim
**I converted measured application-layer vocabulary drift into a versioned backend-owned business-rule authority and used projections/gates to consume it.**

That does not claim invention of reference data, versioning, policy-as-data, or lifecycle governance.

## Evidence
See `evidence/governance/reason-code-catalogue.sanitized.md`.

## Runtime caveat
The implementation and release seed/assert logic exist, but the private snapshot reviewed for this portfolio recorded the release-time seed/assert path as merged without a fully green non-production release result. The portfolio therefore does not convert implementation into unverified runtime convergence.
