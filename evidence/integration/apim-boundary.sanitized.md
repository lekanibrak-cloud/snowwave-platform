# Integration evidence — API Management boundary

Snowwave's retailer parcel-ingestion boundary is represented in Bicep and APIM policy.

The reviewed configuration establishes:

- a versioned parcel-ingestion API;
- HTTPS;
- subscription-required access;
- a Function App backend;
- established and pilot retailer products;
- correlation-ID preservation/generation;
- forwarding of the caller's bearer token to the Function;
- diagnostics to the platform's logging workspace.

The operation policy treats correlation as an end-to-end concern:

```text
caller
  ↓ x-correlation-id (preserve or generate)
APIM
  ↓
Function ingress
  ↓
Service Bus
  ↓
processor
  ↓
parcel / recovery evidence
```

Function-level Entra validation remains the primary authentication gate; the APIM subscription key is an outer access-control layer.

**Boundaries:** source for a pilot rate-limit policy exists, but this portfolio does not claim runtime attachment/enforcement without deployment evidence. An admin product exists for future controlled APIs; this is not presented as a completed admin API surface.
