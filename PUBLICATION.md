# Public Release Manifest

**Candidate:** Snowwave Portfolio v2.4  
**Purpose:** curated public engineering portfolio  
**Source relationship:** sanitized representation of a separately maintained private project; not a repository mirror.

## Publication rules

This candidate intentionally excludes:

- private Git history;
- secrets, credentials, tokens, connection strings, and SAS values;
- tenant/subscription/object/application/principal identifiers;
- private Azure resource names and deployment URLs;
- operational records or customer data;
- employer backend/source claims;
- unrelated private implementation.

## Provenance boundary

The author's last-mile work informed the **observable operational problem space**. The portfolio does not claim access to, copying from, or knowledge of an employer's backend architecture, source code, databases, or internal implementation.

## Evidence convention

Representative excerpts are selected to demonstrate architectural mechanisms. They are not intended to reproduce the full private codebase.

Claims are scoped as:

- **Built** — implementation exists.
- **Verified** — additional test/deployment/non-production evidence exists.
- **Designed** — specified but not represented as complete.
- **Not yet proven** — no result is claimed.

## Before publishing

The final public Git repository should be initialized from this candidate with a **fresh `.git` directory**. Do not copy private Git metadata or history.

## Intellectual / design provenance

Established architecture patterns are not claimed as inventions of Snowwave. See [Research & Design Provenance](docs/evidence/RESEARCH-DESIGN-PROVENANCE.md).
