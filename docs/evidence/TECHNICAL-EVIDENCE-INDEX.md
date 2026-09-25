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

## Why selected evidence instead of a code dump?

A mature technical portfolio should make review easier, not make the reviewer reverse-engineer an entire private system.

Each excerpt therefore answers four questions:

1. **What architectural claim is being made?**
2. **What implementation mechanism supports it?**
3. **What broader engineering concern does it demonstrate?**
4. **What does the evidence *not* prove?**

That final question is deliberate. Evidence is stronger when its boundary is explicit.
