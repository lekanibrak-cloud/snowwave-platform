# Evidence-backed Evolution Timeline

This timeline is deliberately conservative. It records what repository history supports; it does not force Snowwave into a cleaner chronology than the commits show.

| Date (2026) | Repository evidence | What it demonstrates |
|---|---|---|
| Mar 2 | `d41f7bd2` — Service Bus processing added alongside scan ingestion/realtime | Event-driven processing foundation existed before the later retailer-ingestion work. |
| Mar 21 | `edde7d1f` — Recovery Console with DLQ board, retry/escalate actions, realtime updates | Recovery was already an explicit concern; the first implementation was not the final recovery semantic. |
| Mar 24 | `eb9a53af` — APIM-based parcel ingestion pipeline with traceability | Retailer-facing ingestion became a defined integration boundary. |
| Mar 25 | `95e00932` — established/pilot retailer APIM products | Different external consumers gained distinct access boundaries. |
| Apr 4 | Project journal — Manual Review v1 E2E validation | Recovery semantics matured: replay initiation no longer implied successful recovery. |
| Apr 5 | `7f87e1ff` — parcel-side Exception Review | Parcel lifecycle, business exception workflow, and technical recovery were explicitly separated. |
| May 2 | `7127471e` — retailer manifest ingestion for pickup assignments | Manifest data became an expected-truth layer for pickup operations. |
| Jun 19–20 | `b37e9f3d`, `6520093c`, `66907fba` | Canonical ingest processing was extracted and the replay worker gained a true reprocess path rather than a parallel approximation. |
| Aug 7 audit snapshot | Business Exception domain audit | Multiple reason vocabularies and concrete client/backend/policy drift were measured. |
| Aug 14 | `42203195` — Reason Code Catalogue infrastructure | Reason semantics moved toward a versioned backend authority. |
| Aug 31 | `33fefea9` — seed/assert catalogue on deploy + ADR-069 | Catalogue existence became a deployment invariant; declaration-door security ordering was documented/enforced. |

## Interpretation

The evidence does **not** support the simplistic sequence:

```text
finish ingestion → invent recovery → invent exceptions → invent catalogue
```

The supported story is more interesting:

```text
ingestion and recovery co-evolve
        ↓
recovery semantics become stricter
        ↓
technical recovery is separated from business exception handling
        ↓
business-exception modelling exposes vocabulary drift
        ↓
reason semantics become governed data
        ↓
governed semantics create deployment and security invariants
```

That is the narrative used throughout this public portfolio.
