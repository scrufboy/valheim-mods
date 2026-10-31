# Tooler 2.6.4 installed: correct the RRR error attribution before repair

Claude's `113a39c` correctly preserved the 299-entry `ZNetScene.RemoveObjects` burst as a compatibility failure, but its attribution to `RRRN_OrcBrute` is not proven. Brian identified a second missing actor: the blue Black Forest Child, `RRRN_NinoBosque`.

## Evidence correction

- **LIVE-PROVEN:** `RRRN_NinoBosque` logged `Spawned ... x 1` at 11:43:30.
- **LIVE-PROVEN:** it then produced no awareness, target, action, damage, death or kill record. Brian did not see the blue actor in the video/session.
- **LIVE-PROVEN:** the 299-entry removal burst began at 11:45:38.
- **LIVE-PROVEN:** `RRRN_OrcBrute` never logged a completed spawn.
- **SOURCE-INSPECTED:** both actors were eligible around both observed error windows: NinoBosque and OrcBrute in Black Forest natural spawning, and both in the forest training raid.
- **INFERENCE ONLY:** either actor may leave a damaged or improperly removed network object. Eligibility timing cannot distinguish them.

The earlier claim that OrcBrute was "very likely" the cause is therefore withdrawn. NinoBosque has the stronger direct anomaly (a logged spawn with no observable actor), while OrcBrute remains unresolved (no completed spawn).

## Implemented containment

- Branch `work/tooler-current-raids-2.6.4`, head `b1e64fd`.
- `HuntPlayer=true` remains on all eight definitions; do not duplicate or revert the already-installed pursuit correction.
- Temporarily disabled only the unresolved raid definitions: forest NinoBosque specialist and the forest/swamp OrcBrute heavy slots.
- Temporarily disabled natural `[WorldSpawner.9001]` NinoBosque and `[WorldSpawner.9004]` OrcBrute in the active Arena profile.
- Five Souls actor types that produced valid combat remain active: Pillager, WoodsSoul, Neckman, Servant, plus the other configured common/pair entries.
- No stats, awareness, companion AI, faction, group size, range, population cap or combat rule changed.

This is containment, not a root-cause fix. Both definitions remain in source and must return after compatibility repair.

## Required compatibility work

Trace NinoBosque and OrcBrute separately through the RRR compatibility layer: prefab build, item/visual application, `ZNetView` registration, instance activation, AI start, despawn/unload and network destruction. Compare each stage with working `RRRN_WoodsSoul`. Record the prefab name and instance/network identity at every failure boundary. Do not use a blind active-world `spawn` command as proof; use an isolated diagnostic world or deterministic fixture with lifecycle logging.

