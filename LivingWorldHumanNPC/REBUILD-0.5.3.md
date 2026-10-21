# Living World Human NPC 0.5.3 saved-state migration

The 2026-10-21 19:02 live run loaded Human NPC 0.5.2 and telemetry 0.5.1/schema 6.
It reused the previously saved Asa Guthormsdottir and Knut Ragnvaldsson rather
than running a new `lwparty` transaction.

## Live result

- The visible-hand readiness correction passed. Telemetry recorded 140 leader
  samples with both hands empty; those samples were not classified as armed.
- Both companions acquired multiple targets. Knut used both bow and sword. Their
  maximum target distances were 18.86 and 22.02 metres, and the leader-centred
  outer boundary intervened once per actor rather than continuously churning.
- Both survived. Asa's measured minimum health was 164.39 and Knut's was 86.45;
  both recovered to 250 and returned to `IdleGuard` with weapons stowed.
- Previously persisted `tamed=true` remained on both loaded actors, producing the
  native missing-`Tameable` warning even though 0.5.2 no longer writes tame state.

## Correction

0.5.3 changes only the binding/save migration boundary. On the locally owned
actor's first valid binding check, an old true tame bit is written false once.
Already-correct actors receive no tame write. The migration does not recreate an
actor or change creation, identity, appearance, equipment, movement, formation,
targeting, combat values, or combat state decisions.

Telemetry remains version 0.5.1/schema 6 and now records whether the one-time
legacy migration occurred. The build rejects any return of `SetTamed(true)` and
requires the explicit false-only migration while old persistent actors remain
compatible.
