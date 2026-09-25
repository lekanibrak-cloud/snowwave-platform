# Deployment evidence — intent is not loaded reality

Snowwave encountered a non-production deployment failure where infrastructure and application workflows could both report green while an older package was still serving.

The resulting design separates:

```text
configured deployment intent
        from
identity of the assembly actually loaded
```

The private implementation reads `AssemblyInformationalVersion` stamped with `SourceRevisionId`. It deliberately has **no fallback** from an unreadable assembly identity to the `BUILD_SHA` application setting.

A regression test sets a fresh configured SHA while presenting an unstamped assembly and requires the loaded identity to remain `unknown`.

```text
fresh configuration + stale/unverifiable binary
                ≠
verified deployment
```

ADR-044 also separates ownership: infrastructure deployment owns resources and infrastructure-derived configuration; application deployment owns the code package and build identity.

**Transferable concern:** deployment control planes can describe intended state without proving runtime state.

**Boundary:** the motivating incident and validation occurred in Snowwave's non-production environment.
