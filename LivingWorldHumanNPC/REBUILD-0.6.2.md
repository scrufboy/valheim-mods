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
