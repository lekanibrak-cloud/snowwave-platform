# When two operators edit a service commitment

An operator opens a retailer’s service settings, another operator changes them, and the first operator saves the older screen. If the application silently accepts both edits, the team can lose a decision that someone believes is in force. A second ambiguity arises when a save times out. The operator cannot tell from the timeout whether the change took effect.

## What I changed

I built versioned service-policy administration. Authorized operators can inspect the current settings and their history, publish a revision and restore earlier settings as a new revision. The service checks the caller’s authority and detects stale edits. The dashboard surfaces conflicting changes and lets an operator confirm an uncertain save. This preserves both the decision history and the distinction between a rejected edit and a lost response.

## What the evidence supports

The protected administration API and dashboard are implemented. Backend and frontend tests cover competing edits and recovery after a timed-out save. These checks establish how the administration workflow behaves. Applying a commitment throughout a parcel’s operational lifecycle requires separate evidence.

The later intake pilot brings business validation into an additional workflow. It is implemented, tested and disabled by default in source. Its build reached non-production. The pilot’s live configuration and end-to-end operational outcome remain unverified. Broader workflow changes remain proposals.

[Verification snapshot](../evidence/TECHNICAL-EVIDENCE-INDEX.md) · [Earlier business-rule case](business-rules-as-governed-data.md)
