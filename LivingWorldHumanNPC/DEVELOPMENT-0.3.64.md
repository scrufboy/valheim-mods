# Human NPC 0.3.64 — behavior-preserving native graph repair

## Pre-live rejection of 0.3.63

The 0.3.63 native-graph candidate passed its offline suite but copied every
serialized `Humanoid`/`Character` field from the Player prefab before applying
Living World settings. The explicit settings overwrote the principal movement,
combat, health, faction, inventory, physics, and AI values, but the broad copy
still created an unnecessary indirect-regression surface. It was rejected
before live testing.

## Narrow correction

The native Player prefab still supplies one coherent skeleton, attachment,
`VisEquipment`, LOD, animator, synchronization, footstep, collider, rigidbody,
and network graph. Before lifecycle, the Player controller is removed and the
NPC `Humanoid` is created.

Only the native eye transform and unarmed-weapon reference are transferred from
Player to Humanoid—the same two human references used by the preceding
fresh-root implementation. No other Player gameplay, resource, effect, input,
movement, combat, or runtime field is copied. Living World's existing explicit
settings remain the sole source for NPC gameplay values.

A binary comparison against 0.3.61 confirmed zero changed methods in combat,
party equipment, defense, torch, stowing/readiness, wetness, doors, follow,
ambient behavior, posture, confidence, and measured raid handling.

## Acceptance

One `lwparty` command must produce and follow with two visibly haired
companions. A selected male beard must render. The same run must then verify the
already accepted calm follow, back-stow, torch, wetness, guard/ambient, door,
workstation, combat-entry, native-combat, regroup, and combat-clear behavior.
