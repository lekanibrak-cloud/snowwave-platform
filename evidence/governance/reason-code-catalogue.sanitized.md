# Versioned exception reasons

A driver reporting an observation and an operator deciding what it means for delivery have different responsibilities. Snowwave represents exception reasons as versioned business data owned by the backend. This gives clients a consistent vocabulary and preserves the meaning of an earlier report when the vocabulary changes.

The November 25, 2026 non-production release passed the catalogue assertions against the deployed service. This verifies the catalogue checks exercised by that release.

[Business-rule case study](../../docs/case-studies/business-rules-as-governed-data.md)
