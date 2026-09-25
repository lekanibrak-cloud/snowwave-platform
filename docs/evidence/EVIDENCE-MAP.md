# Evidence Map

This file maps public portfolio claims to private-source evidence reviewed before publication. It intentionally avoids environment-specific identifiers and private URLs.

| Public claim | Evidence reviewed | Status |
|---|---|---|
| Parcel ingestion uses a Service Bus-triggered worker and canonical ingest service | `ProcessParcelIngests.cs`, `ParcelIngestService` | Built |
| Invalid/missing ingest payloads can produce recovery records and dead-letter outcomes | `ProcessParcelIngests.cs` | Built |
| Recovery replay is distinct from recovery success | `ProcessDlqReplays.cs`, recovery architecture docs, project journal | Built / nonprod validated for manual-review loop |
| Supported replay can re-enter canonical parcel ingest processing | `ProcessDlqReplays.cs`, June ingest/recovery commits | Built |
| Business exception state is separate from technical recovery state | architecture docs + Apr 5 exception-review commit | Built / documented |
| Reason vocabulary drift was measured before catalogue implementation | Business Exception audit + catalogue review | Measured |
| Reason codes are versioned semantic documents | `ReasonCodeModel.cs`, catalogue service/tests, Build Journal | Built |
| Declaration doors authenticate/authorize/verify ownership before reason validation | ADR-069 + `DeclarationDoorOrderingTests.cs` | Built / build-enforced |
| Manifest job transitions use ETag compare-and-swap | `ManifestJobStore.cs` | Built |
| APIM retailer ingestion preserves/generates a correlation ID and routes to Function backend | APIM Bicep + `parcel-ingest-operation.xml` | Built/configured |
| APIM has separate established/pilot retailer products | APIM Bicep | Built/configured |
| Pilot rate-limit policy source exists | `pilot-retailer-product.xml` | Source exists; runtime attachment not claimed |
| Admin APIM product exists for future controlled operations APIs | APIM Bicep | Provisioned; admin API surface not claimed |
| Snowwave serves real production customers | — | **Not claimed** |
| Snowwave demonstrates regulated-enterprise production/on-call experience | — | **Not claimed** |

## Publication rule

A source file is not automatically public evidence. Before copying implementation into this repository, it must be separately reviewed for:

1. secrets and credentials;
2. tenant/resource identifiers;
3. private URLs;
4. employer or contributor-sensitive information;
5. stale/superseded claims;
6. licensing/ownership;
7. whether the excerpt is necessary to substantiate the portfolio claim.


## v2.2 publication evidence

The following public excerpts were added after re-checking the corresponding private source:

- Cosmos manifest-job ETag/CAS transition handling.
- BuildIdentity loaded-assembly identity and its regression tests.
- Reason-code deterministic seed/version semantics.
- APIM retailer-ingestion boundary.
- ADR-044 deployment ownership correction.
- CI/CD evidence boundaries.

The public excerpts are intentionally sanitized and may be descriptive rather than verbatim where full source is unnecessary.
