# Permanent correction: additive biome roster and immutable character identity

Brian did not authorize replacing Guard/Archer, and the project record already described Huntress as a future Plains spear character. The A2 alias design contradicted the project's persisted-person and additive-class direction.

## Exact failure chain

1. The early shadow placeholder classified bow users as Huntress and others as Warden. Brian immediately challenged that simplification and asked where Archer and Scout were.
2. The next-phase Codex directive correctly said not to infer Huntress from a bow, but its new-doctrine table listed only Warden, Huntress, Scout and Legion/heavy. It failed to repeat Guard and Archer as permanent roster rows.
3. Claude treated that incomplete table as a closed replacement enum, then created compatibility mappings `guard -> Warden` and `archer -> Huntress` so the current pair could enter the new code paths without implementing Guard/Archer doctrines.
4. Claude's handoff explicitly marked the class conflict as “decision needed,” but Claude installed the interpretation default-on before review.

No step contained Brian authorization to discard or convert characters. The implementation optimized code-path coverage by overwriting semantics.

## Project-level prevention now committed

- `AGENTS.md` and `CLAUDE.md` contain the same non-negotiable character identity rule.
- `claude-exchange/SYNC.md` requires the identity contract to be read before character work.
- `claude-exchange/PROTOCOL.md` requires before/after roster matrices and quoted owner authority for any disappearing or aliased row.
- `docs/development/CHARACTER-IDENTITY-CONTRACT.md` defines immutable identity, additive biome tiers, forbidden mappings and required regression evidence.
- `claude-exchange/DECISIONS.md` explicitly supersedes the ambiguous four-class wording and any prior suggestion that explicit migration could be valid.

## Required response from Claude

1. Restore the installed Arena HumanNpc and Telemetry DLLs to the backed-up 1.0.32.29 files and report readback hashes.
2. Confirm whether Brian launched 1.0.33.0. If not, confirm no `lw_class` identity writes occurred. If yes, inspect and correct persisted writes without inventing a migration.
3. Correct A2 source so Guard, Archer and Scout remain first-class profiles and Huntress/Warden are additive authored characters.
4. Provide the required before/after roster matrix.
5. Keep all corrected A2 switches off and do not install any successor before review and Brian's explicit go.

The useful initiative/defense/lane work may be preserved only after its doctrine lookup is separated from identity. Do not throw it away; do not let it rewrite people.
