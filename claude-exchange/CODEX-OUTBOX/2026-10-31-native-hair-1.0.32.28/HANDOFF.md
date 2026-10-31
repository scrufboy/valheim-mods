# 1.0.32.28 native facial-hair repair

Date: 2026-10-31

## Intent

Repair the live 1.0.32.27 regression in which every fresh companion timed out with a nonzero selected hair hash but `rendered=0, object=missing`.

## Evidence

- **LIVE-PROVEN failure:** current Arena logs repeatedly recorded `waiting for visible Hair* (selected=<nonzero>, rendered=0, object=missing)` and the party transaction correctly refused every partial party.
- **SOURCE-INSPECTED cause:** the 1.0.x `NpcAppearance` port kept the public selection setters and readiness observer, but omitted the owner-side native attachment transaction already proven in 0.3.66-0.3.68 (`b654162`, `7ca1e57`, `0415114`).
- **SOURCE-INSPECTED independent review:** Cursor independently compared those commits and reached the same diagnosis and repair boundary. It made no edits.

## Candidate

- Branch: `work/native-hair-attachment-1.0.32.28`
- Source commit: `af31a58`
- Parent: `7ff725e` (A1 revision 5 plus analyzer correction)
- Version: `1.0.32.28`

The repair restores the proven transaction:

1. dirty the five cloned `VisEquipment` request caches before fresh setters;
2. run the public model/color/hair/beard setters;
3. commit and read back `ZDOVars.s_hairItem` and `s_beardItem`;
4. invoke native `UpdateVisuals`, `SetHairEquipped`, `SetBeardEquipped`, `UpdateLodgroup`, and `UpdateColors`;
5. require an active, materialized renderer inside the native rig/LOD before readiness;
6. on a saved marked actor, repair only the native presentation from the immutable saved hair/beard names—never call `CreateIdentity`, reroll, or rewrite the identity.

No combat, awareness, movement, voice, raid, equipment, or telemetry behavior was changed.

## Required next action

Build `af31a58` with the canonical Linux/Mono path, run every gate, inspect the generated IL, and report the hashes. Do not re-lock or weaken the combat-voice gate. If all gates pass, install 1.0.32.28 into `LWC Formidable Arena` only after backing up 1.0.32.27. The first live gate is one `lwparty`: both companions must reach `APPEARANCE READY`, show native hair (and male beard/female no beard), and bind as one complete party.

