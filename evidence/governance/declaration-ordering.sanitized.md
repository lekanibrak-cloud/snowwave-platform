# Declaration-door ordering — sanitized evidence

The private test suite freezes this ordering:

```text
transport/request shape
→ authentication
→ profile/tenant authorization
→ parcel ownership
→ declaration completeness + reason-code validation
→ domain action
```

The architecture test discovers every Function endpoint that invokes the reason-code gate and asserts that authentication and ownership calls occur earlier in that endpoint's source.

The private test explicitly documents its trade-off: it is a source-level heuristic tied to known helper names. That limitation is intentional and visible; if the implementation pattern changes, the test must change or fail.

**Why this belongs in the portfolio:** it demonstrates an architecture rule being converted from prose into a build-time mechanism.
