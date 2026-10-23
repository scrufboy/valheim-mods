# Living World C.O.R.E. NPC 1.0.0 — Claude handoff

Date: 2026-10-23. Branch: `claude/lwcore-npc-1.0.0`, parent `handoff-rimebound-emberworn@4c09a79` (Human NPC 0.6.5).
Source: `LivingWorldCORE/npc/`. Design and module guide: `LivingWorldCORE/npc/README.md`.

## Intent

Brian asked for a working LW CORE NPC built from the ground up after the repeated regression loops.
1.0.0 replaces the 0.6.5 runtime (same plugin GUID, same prefab names, same saved ZDO keys).

## Root causes addressed

- SOURCE-INSPECTED (IL of installed assembly_valheim 1.0.20): `Humanoid.EquipItem` clears both
  `m_hiddenLeftItem` and `m_hiddenRightItem` on every path. `UnequipItem` clears a hidden slot only
  when that exact item is hidden. `MonsterAI.SelectBestAttack` changes hands only when it has a
  creature/static target. This is why the torch kept erasing the back-stowed weapons.
- SOURCE-INSPECTED: 0.6.5 has three modules that each re-check the native target in LateUpdate and
  override the arbiter. Its clear rule is 50 targetless ticks. Its cohesion return releases even an
  adjacent attacker.

## Structure

One kernel per NPC: Sense, then the pure PhaseArbiter, then the DomainScheduler (Movement/Hands/Look/Posture,
priority order, all-or-nothing). Each behavior runs Enter, then Tick, then Exit, and exits run before new owners
enter. NpcBody is the only native writer and refuses callers that don't hold the domain. A build-time IL gate
fails the build if any other class writes native NPC state.

## Carried over unchanged (reported live-accepted)

The 0.4.1 factory and identity; 0.5.x formation, anchor and pace; the 0.6.1/0.6.3 back-stow presentation sync;
0.6.0 guard coverage; the 0.6.2 native encounter release; and the 0.6.4 one-time EquipBestWeapon handoff.

## Restored or fixed

- Torch with verified back-slot preservation; the torch is removed from inventory on every exit.
- Torch hysteresis.
- Crouch with the leader.
- Sit with the leader (native IsSitting).
- Rain Wet plus drip.
- Local self-defense during the cohesion return.
- Verified clear: 5 s with no target, no hostile damage and no alerted hostile within 30 m.
- Friendly fire no longer starts combat.
- `lwparty` no longer deletes existing companions unless `new` is given.
- Rollback now tracks an actor before validating it.

## Not implemented

Doors, workstation pose, beds, the cleric role, the town/resident context and hire/dismiss.

## Unknowns

- UNKNOWN: all live behavior. Nothing in 1.0.0 has run in Unity.
- UNKNOWN: NPC crouch via animator `crouching` with a sneak pace. The animation parameter is from the legacy
  live code; whether the pace looks right is untested.
- UNKNOWN: torch light/flame visuals on a non-Player Humanoid under 1.0.0.
- UNKNOWN: how long the native alert flag stays raised after a fight. Readiness (weapons drawn, still
  following) holds while it is raised.
