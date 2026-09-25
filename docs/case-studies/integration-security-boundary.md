# Retailer Integration & Security Boundary

## Established mechanisms
API gateways, API products/subscriptions, bearer-token identity, authorization, TLS, throttling, and correlation are established API-management and security mechanisms.

## Verified Snowwave boundary
Snowwave's Bicep defines an HTTPS APIM ingestion API, retailer products with subscription requirements, backend certificate validation, diagnostics to Log Analytics, and correlation metadata.

The important security boundary is layered:

```text
external retailer
      ↓
APIM product / subscription boundary
      ↓
bearer token forwarded
      ↓
Function application
      ↓
Entra authentication
      ↓
tenant/profile authorization
      ↓
ownership
      ↓
business validation
```

Subscription keys are not presented as the primary identity mechanism.

Snowwave's declaration-door work further made ordering executable: one endpoint had performed reason-code validation before authentication, allowing unauthenticated callers to distinguish valid vocabulary. The correction moved authentication/authorization ahead of business-rule validation and added an architecture test to guard the order.

## Claim boundary
The portfolio claims the **Snowwave-specific composition and enforcement** of these boundaries, not invention of API gateways, OAuth/bearer tokens, RBAC, tenant isolation, or authentication-before-authorization principles.

## Evidence
- `evidence/integration/apim-boundary.sanitized.md`
- `evidence/governance/declaration-ordering.sanitized.md`

## Authoritative references
- Microsoft — API Management authentication and authorization  
  https://learn.microsoft.com/en-us/azure/api-management/authentication-authorization-overview
- Microsoft — API Management subscriptions  
  https://learn.microsoft.com/en-us/azure/api-management/api-management-subscriptions
- Microsoft identity platform — access tokens  
  https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens
