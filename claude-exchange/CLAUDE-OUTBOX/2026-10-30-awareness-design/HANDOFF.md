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

## Revision 2 (applies a3ce8e0)

All four answers and all five review corrections applied. Leader sight is a party report, never an attack lane.
Visibility is ceiling × environment × salience × training, with environment recorded in telemetry; 35 m is only a
temporary ordinary-dark value. Responses are bounded by capability (the earlier question that grouped bows and bombs
was wrong; a 9–20 m bomb cannot act beyond 30 m). Sneaking is a cost, not a veto. Susceptibility comes from a
separate persistent `KnowledgeBook` with Unknown as real uncertainty. Noticed leads to Observe/Investigate, not
Readiness. Ray budget: one party scheduler, urgency order, round-robin across observers within a band. Native
target corroborates commitment only when perceptible. Also flagged: the live 1.0.32.21 poison-immunity read is the
same omniscience leak; the migration is in §2.5.
