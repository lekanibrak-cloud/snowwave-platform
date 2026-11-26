# Technical Evidence Index

The public portfolio is intentionally not a source-code mirror. This index gives a reviewer a short path from architectural claim to representative evidence.

| Capability | Public evidence | What it demonstrates |
|---|---|---|
| Asynchronous ingestion | [Ingestion worker](../../evidence/ingestion/ProcessParcelIngests.sanitized.md) | Explicit outcomes and canonical processing seam |
| Recovery | [Confirmed recovery](../../evidence/recovery/confirmed-recovery.sanitized.md) | Replay request is not recovery success |
| Concurrency | [ETag CAS](../../evidence/concurrency/etag-cas.sanitized.md) | Optimistic single-writer transition control |
| Business-rule governance | [Reason Code Catalogue](../../evidence/governance/reason-code-catalogue.sanitized.md) | Versioned semantic authority |
| Security architecture | [Declaration ordering](../../evidence/governance/declaration-ordering.sanitized.md) | Authorization before business-rule validation |
| Integration | [APIM boundary](../../evidence/integration/apim-boundary.sanitized.md) | Controlled external API + correlation |
| Deployment truth | [Loaded binary truth](../../evidence/deployment/loaded-binary-truth.sanitized.md) | Runtime identity distinct from configured intent |
| Delivery engineering | [CI/CD evidence](../../evidence/delivery/ci-cd-evidence.md) | Failures converted into pipeline controls |

## Verification snapshot · November 25, 2026

| Evidence scope | Passed | Failed | Skipped | What was checked |
|---|---:|---:|---:|---|
| Public ingestion slice | 9 | 0 | 0 | Local .NET 8 run and the public baseline CI run. |
| Private non-production release, backend | 1,410 | 0 | 10 | Backend suite on the released revision. The opt-in live storage tests were skipped. |
| Same release, web frontend | 441 | 0 | 0 | Web frontend suite across 39 test files. |
| Later private local candidate, backend only | 1,441 | 0 | 11 | Separate, unmerged test work. The opt-in live checks were skipped. |

The two suites from the same release total **1,851 passed of 1,861**. The local candidate reran overlapping backend coverage. Neither the candidate nor the public slice is added to the release total. These are test outcomes, not counts of customers, deployments or operating scenarios.

| Capability | Evidence status |
|---|---|
| [Service commitment administration](../case-studies/service-commitments.md) | Implemented and tested across the protected API and operator UI. |
| Running build and catalogue | Deployed and verified in non-production. The loaded build matched the release and the catalogue assertions passed. |
| Newer intake pilot | Implemented and tested. Disabled by default in source. Live configuration and end-to-end operation remain unverified. |
| Broader workflow changes | Proposed. Operational acceptance remains open. |
