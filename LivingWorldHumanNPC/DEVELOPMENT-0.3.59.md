# Human NPC 0.3.59 — Formidable shadow observer

Evidence status: source changed; offline build pending; not installed; not live-proven.

## Purpose

This candidate begins FormidableAI runtime observation without changing combat behavior. A new
`HumanFormidableShadow` component is attached to the independent human prefab and remains disabled
by default through `FormidableAI/ShadowTelemetry`.

When explicitly enabled in a future isolated lab run, only the locally authoritative actor may
sample. At 10 Hz it:

1. reads the persisted `HumanProfile` identity;
2. observes the public native target and current body action state;
3. constructs an immutable Formidable `CombatFrame`;
4. evaluates a class proposal through the shared arbitrator;
5. compares the predicted Attack-domain owner/action with the native observation;
6. stores at most 64 chronological comparison records;
7. exposes only a summarized `Describe()` line to the existing status command.

It has no API for movement, facing, targeting, attack execution, defense execution, ZDO writes,
ownership claims or domain transfer.

## Deliberate limitations

- Non-cleric lab actors are provisionally observed as Warden; clerics as Mystic. This is telemetry
  scaffolding, not persisted class assignment.
- Firing-lane, range-band, ally-health and precise strike-impact facts are not yet sourced, so their
  corresponding class/defense predictions remain inactive or conservative.
- Attack identity is not inferred through private reflection. Initial comparison therefore covers
  actor identity, target identity, owner, action and timing only.
- The component scans potential personal threats only while shadow telemetry is explicitly enabled.

## Required gates before installation

- Complete offline Human NPC build and all pre-existing regression suites.
- Binary inspection proving the component has no calls to movement, target, attack, ZDO-set or
  ownership-claim methods.
- Actor-count benchmark for observation and trace cost.
- Review package/version/hash consistency.
- Brian approval for isolated profile installation.

Compilation is not permission to install and is not live combat evidence.

## Native update reinspection

The first offline build correctly stopped because the installed read-only native assembly changed
on 2026-10-28. On 2026-10-30 the new SHA-256
`9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769` was inspected with Mono.Cecil.
The required unique bodies and IL routes for `ZNetScene.CreateObject`, `ZNetView.Awake`,
`MonsterAI.SetFollowTarget`, `BaseAI.GetPatrolPoint`, `Character.GetHealth`,
`Character.GetMaxHealth`, `Humanoid.Start`, `MonsterAI.UpdateAI`, `UpdateTarget`, `SetTarget` and
`DoAttack` remain present. The migration contract hash was updated only after that inspection.
