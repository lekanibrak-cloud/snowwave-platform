# Giving an exception reason a consistent meaning

An audit of the Business Exceptions workflow found that driver and backend vocabularies had drifted. A label a driver could select did not always correspond to a reason the service understood. An operator reading the resulting record needed to know what had actually been observed.

I moved the shared vocabulary into a backend-owned, versioned catalogue. Updating a reason’s meaning now has a history, and clients can use a common definition. The catalogue addresses the meaning of a reported exception. The separate service-policy administration workflow addresses the retailer’s service commitment.

Earlier non-production releases exposed problems seeding the catalogue. On November 25, 2026, the non-production release completed its post-deployment catalogue assertions.

[Catalogue evidence](../../evidence/governance/reason-code-catalogue.sanitized.md) · [Service commitment case](service-commitments.md)
