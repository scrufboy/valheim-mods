# Living World Human NPC 0.5.1 movement-ownership correction

0.5.1 preserves the accepted 0.4.1 human creation and appearance factory. No
identity, sex, body, hair, beard, name, loadout, or exactly-two laboratory party
creation code was changed.

## Live 0.5.0 failure evidence

The 2026-10-21 18:07 test produced valid 0.5.0 logs and 1,579 undropped schema-4
telemetry records. The video and telemetry identified four structural faults:

- Turning while nearly stationary rotated the formation frame and moved leased
  destinations roughly 10–17 metres, making both actors sprint around the player.
- Native follow stopped inside three metres while a second late movement command
  tried to close to 1.15 metres. Those writers fought and produced visible jitter.
- Native `IsAlerted` without an acquired creature target was classified as
  committed combat. One released actor wandered as far as 135.8 metres from the
  leader because neither formation nor a leader tether remained active.
- Catch-up outranked readiness, so an armed companion could alternate between
  stowed and drawn equipment while closing distance.

No exception caused this failure. The movement and state ownership were wrong.

## Correction

- Formation orientation now derives from actual leader travel direction and is
  frozen while the leader is stationary.
- Native `MonsterAI.Follow` is the only traversal/pathfinding writer. The custom
  layer supplies the destination anchor and pace only.
- `IsAlerted` without a creature target enters `CombatReadiness`; only an actual
  `GetTargetCreature()` result commits `NativeCombat`.
- Readiness outranks catch-up, while the traversal module can still select a run
  pace without changing equipment ownership.
- The actor is made a native tamed companion and retains the leader as its native
  follow target during combat and combat-clear. The formation lease remains
  released, so native target pursuit, facing, blocking, and attacks still own the
  fight while Valheim's native tamed alert-range rule bounds it to the party.
- The fixed spawn-point chase limit remains disabled. Valheim measures that field
  from where the actor originally spawned, which is invalid after a companion has
  travelled with the player; the moving native leader tether is the combat bound.

## Verification boundary

The build compiles with warnings treated as errors. Deterministic contracts pass
for identity, combat-clear ownership, invalid-input retention, alert-versus-target
classification, readiness precedence, catch-up hysteresis, and regroup release.
Telemetry 0.5.0 adds continuous leader motion/facing samples plus formation-frame,
alert, target, readiness, and tamed-tether evidence.

Live Unity verification is still required. The next acceptance run is deliberately
not a raid: stationary turning, straight traversal, sprint/catch-up, posture
changes, and one natural hostile encounter must pass before raid balance resumes.

## 2026-10-21 live result

The 18:28 run was a partial pass, not an acceptance pass. It produced 5,002
undropped schema-5 records across 610 seconds plus synchronized video commentary.

- Stationary formation stability passed: 98-99% of settled samples stayed below
  one metre per second, with no return of the severe orbiting regression.
- Native combat acquired real targets, used the archer's bow, remained much more
  tightly bounded than the prior 135-metre failure, and both companions survived.
- Calm/readiness failed because `GetCurrentWeapon()` returned Valheim's invisible
  unarmed attack while the player's visible hands were empty. This held both
  actors in readiness for roughly 215 seconds, leaving weapons drawn and
  suppressing calm activity exactly as the user's commentary reported.
- Follow still showed a smaller stop/start cadence because native Follow stops
  inside three metres of the destination while the destination itself was the
  intended formation lease.
- `SetTamed(true)` produced repeated native warnings because these human actors
  correctly have `MonsterAI` but deliberately do not implement the complete pet,
  feeding, and interaction lifecycle supplied by `Tameable`.

These measured corrections are implemented in `REBUILD-0.5.2.md`.
