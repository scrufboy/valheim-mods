# Current shared handoff

Updated: 2026-10-17

## Governing direction

Stabilize and prove the active Human NPC companion without deleting working behavior. Combat, facing, bow execution, hands/stow, torch, pursuit, regrouping, doors, wetness, formation, identity, and commands remain independent protected contracts.

## Historical audited snapshot

The first full Codex audit captured candidate 0.3.31:

- DLL SHA-256: `7443EA847975413F71270A71ED17777660E1CE66006502F3D7B12576638775A0`
- offline gate: passed;
- live gameplay: not proven by that audit;
- packaging integrity problem: stale 0.3.12 `START-HERE.txt`, while build metadata reports 0.3.31 and marks `ProfileInstalled: false`; a chat separately reported installation.

This snapshot is evidence, not a declaration that 0.3.31 is still the newest local candidate. Later local 0.3.32–0.3.34 directories exist and require their own evidence-backed handoff before promotion.

## Immediate rules

- Verify the actual active profile DLL and startup version before any gameplay claim.
- Preserve accepted behavior and the last known-good artifacts.
- Fix only an evidenced failing ownership boundary.
- Do not call weapon selection a successful bow shot.
- Do not let utility/social systems reclaim hands or movement during an encounter.
- Do not interpret postponed ideas as current work.
- Do not overwrite prior submissions in this exchange.

## Full first handoff

See `CODEX-OUTBOX/2026-10-17-full-project-audit/HANDOFF.md`.
