# Human NPC 0.3.53 — Shield, craft and door-follow repair

## Measured failures

The 0.3.52 telemetry showed improved attack continuity, but every observed
close-combat state had a bronze sword in the right hand and an empty left hand.
No shield was equipped, so the defense adapter could not produce a block attempt.

The peaceful workstation scheduler reached its perform phase, but the posture
adapter rejected every craft request when the sword and shield were correctly
stowed in the hidden/back slots.

Door-follow usually retained a formation or personal-space destination instead
of the actual leader order anchor. That prevented the bounded door adapter from
owning a directly observed door on the leader route.

## Repair

- Native `MonsterAI.SelectBestAttack` still chooses the weapon. A postfix pairs
  an already-owned shield only when native selection returns a one-handed weapon
  during authorized combat.
- Shield pairing never moves, faces, targets, attacks, unequips, or overrides the
  weapon selected by native AI.
- Hidden/back-slot equipment is no longer treated as an occupied hand lease for
  peaceful crafting animation.
- A door must be the first solid obstacle directly observed on the current leader
  segment. That route may retain the actual leader goal within the bounded 12 m
  scan, and a committed crossing keeps that goal while the leader advances.
- Nearby side doors and alternate entrances are still not discovered or chosen.

## Separate resident milestone

Bed use is not implemented in the current runtime. Sleep appears in the policy
vocabulary, but the runtime does not detect leader sleep, discover an owned bed,
or execute a native bed/sleep contract. It is intentionally not faked in this
combat/navigation repair.

## Live acceptance

The next live run must confirm an equipped left-hand shield and native block
events, visible workstation animation with weapons still on the back, and a
companion following the leader through a directly blocking door. The installed
RRR/Souls of the Land set is a behavior and tuning reference, not copied runtime
logic.
