# Research & Design Provenance

## Purpose

This portfolio distinguishes between:

1. **Established engineering knowledge** — public concepts, patterns, platform capabilities, standards, and practices.
2. **Snowwave-specific reasoning** — deciding how those ideas apply to this system and domain.
3. **Snowwave-specific implementation** — code, rules, tests, infrastructure, ADRs, and operational models created for this project.

The portfolio claims authorship of (2) and (3), not invention of (1).

## Examples

| Established concept | Snowwave-specific application |
|---|---|
| Source systems and ingestion boundaries | Modelling retailer/manifest input and explicit ingest outcomes |
| Message queues and DLQs | Service Bus processing plus Snowwave recovery records and replay semantics |
| Idempotency | Applying duplicate-safe behaviour to Snowwave parcel/driver workflows |
| Optimistic concurrency | Cosmos ETag/CAS on Snowwave manifest-job transitions |
| API gateways | APIM boundary for Snowwave retailer ingestion |
| Correlation IDs | Propagating Snowwave request identity across ingress/processing |
| RBAC / authorization | Snowwave tenant, ownership, and declaration-door ordering |
| Versioned data | Reason-code semantics as governed Snowwave business documents |
| ADRs | Recording why Snowwave chose or changed an architectural boundary |
| CI/CD verification | Snowwave loaded-binary identity and post-deployment controls |

## Research behaviour

Researching an established solution before implementing an unfamiliar problem is part of engineering practice.

A typical Snowwave loop was:

```text
observe a failure or gap
→ identify the underlying class of problem
→ research public documentation / established patterns
→ compare the pattern with Snowwave's constraints
→ implement an adapted solution
→ test it
→ observe the result
→ revise the architecture when necessary
```

## Attribution rule

A specific source should be cited when:

- wording, diagrams, or code are quoted or closely adapted;
- a named framework or reference architecture is being followed;
- a particular publication materially shaped a design decision and the source can be established.

A generic concept does not become a Snowwave invention merely because Snowwave implements it.

Conversely, the portfolio should not manufacture citations after the fact. If the exact historical source cannot be reliably identified, the portfolio describes the concept as established industry practice without assigning a false source.

## Employer boundary

Operational experience may motivate a problem without transferring ownership of an employer's implementation.

The author did not have access to the employer's backend/source architecture for the Snowwave design described here. The portfolio therefore speaks about observable operational behaviour and the independently designed Snowwave response, not about how an employer's private system works.


## Source-level map

For the current Microsoft/Azure reference map across ingestion, messaging, ETags, APIM, identity, correlation, recovery, governance, and deployment, see [Architecture Research & Source Provenance](ARCHITECTURE-SOURCE-PROVENANCE.md).
