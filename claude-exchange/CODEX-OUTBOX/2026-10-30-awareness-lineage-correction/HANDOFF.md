# Awareness lineage correction — do not install e7a6a86

Brian's objection is correct. `e7a6a86` makes the architectural error clearer: allowing the bomb behavior
to scan in Calm does not create awareness. It lets one weapon skill search the world for an opportunity to
activate itself. That is backwards and must not ship.

The history already contains the ordering we must preserve:

- 0.6.4 `HumanBehaviorSignal` separated `NativeAlert` from `CommittedThreat`.
- 0.6.4 `HumanBehaviorStateMachine` mapped alert/leader-armed to `CombatReadiness` and a committed threat
  to `NativeCombat`.
- 0.6.4 defined commitment as the native target, then handed that target to native combat.
- 0.6.5 removed the failed attempt to rewrite Valheim's private literal target-scan intervals and explicitly
  says: **"No replacement scan loop is added."** It preserved the state ordering but did not deliver extended
  perception.
- 1.0.4 restored engagement rules: no wildlife/unprovoked fights; a creature targeting or hurting the party
  is engaged; alerted visible hostiles can join during an encounter.
- Current 1.0 target selection still uses native `CanSenseTarget`/`CanSeeTarget`, while the kernel's separate
  hostile scan is 30 m and requires the enemy already be alerted.

Therefore Brian remembered the important design correctly: noticing/readiness/commitment came before action.
The missing piece is a proper fair-range contact service, not a Calm-phase bomb poll.

Souls supplies the native throw execution/animation/projectile recipe. It does not own our tactical reasoning.
Living World C.O.R.E./FormidableAI owns perception, interpretation, arbitration and learning.
