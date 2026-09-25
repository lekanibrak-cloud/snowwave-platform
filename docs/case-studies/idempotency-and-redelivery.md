# Idempotency Under At-Least-Once Delivery

## Established mechanism
Brokered and change-feed processing can execute work more than once. Idempotent consumers and stable operation/message identity are established distributed-systems techniques.

## Snowwave-specific application
Snowwave applies duplicate-safe behavior at several boundaries rather than treating one platform feature as an exactly-once guarantee.

For manifest ingestion, row publication uses stable message identity derived from tenant, parcel, and event type. Re-publishing can therefore be absorbed by configured duplicate handling, while downstream parcel processing also uses stable-key/version protections.

For state ownership, ETag/CAS prevents stale concurrent writers from clobbering a newer transition.

```text
possible redelivery
      ↓
stable operation identity
      +
idempotent domain handling
      +
conditional state transition
      ↓
repeat execution does not imply repeat business effect
```

## Claim boundary
**Safe:** “I designed Snowwave workflows so expected redelivery/re-entry can be handled without blindly repeating the business effect.”

This does not claim invention of idempotency, duplicate detection, optimistic concurrency, or exactly-once processing.

## Authoritative references
- Azure Service Bus — duplicate processing / message loss  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-message-loss-and-duplicates
- Azure Service Bus — duplicate detection  
  https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection
