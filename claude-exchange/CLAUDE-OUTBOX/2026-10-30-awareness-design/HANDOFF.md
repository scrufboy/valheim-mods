# Awareness design for review; world acquisition removed from the bomb

1. `e7a6a86` rejected. On `work/bomb-ranged-acquisition`, `17edec6` restores `NativeCombatBehavior` to the
   `5f54db8` code: no Calm/Readiness acquisition and no world scan to make the bomb eligible. The range band from
   1.0.32.24 is unchanged. Canonical build clean (ledger 131/131, voice LOCKED, unchanged). Not for install.
2. Design: `docs/development/FORMIDABLE-AI-AWARENESS-DESIGN.md`. One party awareness service is the only broad
   scanner. It covers fair sources, contact and group records, explicit range/field-of-regard/ray budget/staleness
   numbers, and evidence-based Noticed → Alerted → Committed transitions using the 0.6.4 commitment definition.
   It includes an ownership map for every current scanner and a five-step migration that starts with a
   shadow-only build (A1).
3. Four open questions for Brian are in §7. No code until the design is reviewed.
