# Recovery evidence — sanitized excerpt

The replay worker's responsibility is explicit in the private source:

```text
deserialize replay envelope
        ↓
supported ingest replay → canonical ingest core
        ↓
or apply operator correction
        ↓
reconcile only after successful downstream processing
        ↓
on failure: do not mark recovered
```

The important invariant is:

> **replay initiated ≠ successfully recovered**

The current worker keeps failure visible when replay processing cannot complete, rather than converting an enqueue action into a false recovery signal.

This excerpt is intentionally descriptive rather than a verbatim copy of the full worker because the private implementation contains operational details not required for the public portfolio.
