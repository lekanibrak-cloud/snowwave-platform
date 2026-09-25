# Deployment Intent vs. Runtime Truth

## Established concepts
Immutable artifact identity, deployment provenance, separation of infrastructure and application deployment ownership, concurrency control, and post-deployment verification are established delivery/reliability practices.

## Snowwave-specific incident
Snowwave encountered a non-production false-green deployment: infrastructure and application workflows both affected `WEBSITE_RUN_FROM_PACKAGE`. A race could restore a stale package pointer while both workflows reported success.

The architecture was changed so infrastructure owns resources/configuration while application deployment owns the running artifact.

A later hardening step distinguished:

```text
configured BUILD_SHA
      =
deployment intent

loaded assembly SourceRevisionId
      =
runtime evidence
```

`BuildIdentity` deliberately has no configuration fallback. If the loaded assembly cannot prove its identity, it reports `unknown` rather than borrowing the configured SHA.

## Snowwave claim
**I used a real false-green deployment to tighten ownership boundaries and make loaded-binary identity independently verifiable from deployment intent.**

This does not claim invention of artifact provenance, immutable deployments, deployment concurrency controls, or post-deploy verification.

## Evidence
See `evidence/deployment/loaded-binary-truth.sanitized.md`.

## Historical nuance
ADR-044's first build-identity implementation described `BUILD_SHA` verification. The current `BuildIdentity` implementation is stricter and separates configured intent from assembly-stamped reality. The portfolio describes the evolved implementation rather than pretending the first version already had the final invariant.
