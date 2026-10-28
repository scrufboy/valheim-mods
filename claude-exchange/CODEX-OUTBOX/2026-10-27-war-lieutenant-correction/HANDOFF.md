# War-lieutenant faction correction

Date: 2026-10-27

## Correction

The installed `RRRN_OrcBrute` ignores Fulings because its Souls definition uses
Valheim's `PlainsMonsters` faction. That is inherited combat filtering, not owner-
approved lore. The previous Codex handoff incorrectly inferred a political Fuling
alliance and proposed “Idolbound.” That inference is withdrawn.

## Canon direction

Treat the replacement as a dark battlefield lieutenant hostile to everybody: a
solitary or sparse pressure actor that enters conflict, disrupts formations, and
attacks players, Hearthbound, Fulings, Pillagers, Guardians, and rival hostiles.
Temporary target selection is tactical and creates no allegiance.

Preserve the useful Souls loadout and combat identity. Do not preserve accidental
faction immunity. Final display name and exact origin remain intentionally open.
Do not use “Eitrherjar,” “Idolbound,” or another factional name without owner
approval.

## Runtime boundary

This correction changes canonical documentation and dialogue data only. Current
overlay code and live faction values are untouched. A later implementation must
define an everyone-hostile relationship safely without making every other faction
indiscriminately fight every other faction.

