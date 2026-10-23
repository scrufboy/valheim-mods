# Living World Human NPC 0.6.2

## Scope

This release repairs the leader-centred native encounter boundary. It does not alter the accepted 0.6.1 visible back stow, identity generation, party transaction, traversal writer, settled guard coverage, weapon selection, or native attack behavior.

## Evidence and root cause

The completed 0.6.1 telemetry session showed that Hild attempted 335 combat-boundary releases and Geir attempted 58. Hild consequently remained in `CombatReadiness` on a native alert without a committed target while Geir had already returned to guard.

Inspection of the current Valheim assembly proved that `MonsterAI.SetTarget(Character)` returns without changing anything when its argument is null. The controller therefore changed only its local decision for that tick while the real native target survived and was rejected again on the next tick.

## Correction

The boundary now performs the same complete abandon transaction used by Valheim's own `MonsterAI.UpdateTarget`:

- clear native alert;
- clear creature and static targets;
- reset time since attack;
- apply Valheim's five-second target-update delay.

This makes a boundary release a real state transition instead of a repeated no-op. The build gate rejects any return to `SetTarget(null)` and requires the complete release transaction.

## Acceptance target

One out-of-bound encounter may produce one boundary release. A companion must then return through combat clear/regroup into traversal or guard rather than accumulating continuous releases or remaining indefinitely in targetless readiness.

## Live raid evidence — 2026-10-22

The completed 630-second schema-7 session ran `army_theelder` on the installed 0.6.2 build. Telemetry wrote 4,638 records with zero drops.

- The broken repeated no-op was removed: the two companions recorded 10 and 12 total boundary releases instead of the previous 58 and 335. Release counts remained discrete across multiple encounters rather than increasing every decision tick.
- Both actors entered native combat and acquired greydwarfs, greylings, shamans, brutes, and incidental wildlife. The controller did not replace native attack selection.
- Raid cohesion did not pass. During the 125-second event window the companions were more than 10 metres apart for 41.5 seconds and reached 24.4 metres apart.
- Geir took 388 total damage over the session, reached 4.4 health, and disappeared immediately after the final sample while committed to a Greydwarf Brute. Hild survived and recovered to 247.4/250 health.
- Hild returned from the raid cleanup through combat clear/readiness into traversal at 514.9 seconds, so the repaired boundary can recover to companion movement. The required two-companion survival outcome was not achieved.

The encounter-release repair is accepted independently. The raid as a formidable-party acceptance test is failed because combat cohesion and two-member survival were not achieved.

At 20:05:29, more than two minutes after the raid music stopped, Valheim began repeating `ZNetScene.RemoveObjects` null-reference errors while crossing the Meadows/Black Forest boundary. The stack contains no Living World method; it proves a destroyed `ZNetView` remained in Valheim's scene-instance table, but this session alone does not identify which mod or object created that stale entry. Treat this as a separate scene-lifecycle fault until an instrumented run identifies the object.
