# Living World Human NPC 0.6.0 settled guard coverage

0.6.0 adds the first peaceful activity module to the accepted 0.5.3 foundation.
It does not change native-human creation, identity, appearance, loadouts, the
exactly-two `lwparty` transaction, combat values, target selection, combat
movement, equipment posture, formation geometry, or saved-state migration.

## Why this module exists

The 2026-10-21 19:02 run ended correctly in `IdleGuard`, but `IdleGuard` had no
runtime activity attached to it. The companions therefore reported a sensible
state while visibly doing nothing. Earlier accepted project evidence established
that useful guard behavior is coverage-driven rather than random wandering.

## Exclusive ownership

- `HumanSettledGuardModule` is eligible only in `IdleGuard`.
- It owns temporary sector selection and the completed visual scan.
- It cannot target, attack, equip, stow, or issue `MoveTo`, `SetMoveDir`,
  `SetFollowTarget`, or any other movement command.
- It requests a peaceful destination from `HumanTraversalModule`, which remains
  the sole native Follow/pathfinding writer.
- Travel, readiness, native alert, a creature target, combat, combat-clear,
  regroup, leader displacement, unsafe body state, or lost authority cancels the
  action and releases all claims immediately.

## Coverage behavior

- The party shares twelve temporary world-space sectors.
- A companion selects an unclaimed safe sector using completed/stale coverage
  memory and maximum separation from every sector currently watched by buddies.
- Candidate ground, body clearance, fire safety, direct passage, leader
  exclusion, and full native path completion are checked before commitment.
- Native Follow's arrival radius is compensated by a separately validated hidden
  follow point; the visible actor destination remains about 4.6 metres from the
  leader.
- Arrival begins a six-second left/center/right field-of-view sweep.
- Only the finished sweep resolves coverage and makes its opposing sector the
  next preference.
- Formation slots provide deterministic sub-second staggering, avoiding
  synchronized movement without introducing random, untraceable decisions.
- Opening a build/menu interface or using a workstation shortens only the initial
  settled delay; it does not grant a different activity or bypass safety.

## Verification

The build treats all warnings as errors. Pure tests verify first-sector choice,
opposing-sector preference, multi-actor separation, and new-area reset. Source
guards reject any settled-guard target, attack, equipment, or movement writer.
Telemetry 0.6.0/schema 7 records guard phase, sector, status, state, traversal,
targets, equipment, position, health, and the combat boundary.

Live acceptance must show two non-synchronized companions leaving formation for
different guard sectors, completing visible sweeps, returning through native
Follow, and cancelling instantly when the leader moves or combat begins.
