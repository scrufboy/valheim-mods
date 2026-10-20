# Human NPC 0.3.63 — native human graph repair

## Rejected live builds

The 0.3.61 video showed companions with no visible head hair or beard even
though initialization reported concrete `Hair14`, `Beard22`, and `Hair34`
selections. Version 0.3.62 then made the failure worse: every actor timed out on
its native hair attachment. Because readiness correctly remained false, party
preparation could not equip the archer, create the cleric, start follow, or
recover the party. Both builds are rejected.

## Root cause and correction

The failed factory instantiated only the `Visual` subtree of a Player object
under a separately assembled root and tried to reconstruct every visual and
equipment reference. A non-null attachment object was not proof that this
reconstructed graph would render it, and forcing additional visual passes did
not repair the broken boundary.

The factory now uses Jotunn's inactive-container Player-prefab clone as one
coherent native human graph. While that definition is still inactive it:

- creates the NPC `Humanoid` from the Player component's serialized human base;
- removes `Player`, `PlayerController`, `Talker`, and `Skills` before lifecycle;
- adds exactly one `MonsterAI` and the existing Living World modules;
- retains the prefab's native skeleton, attachment points, `VisEquipment`, LOD,
  animation synchronization, footsteps, collider, rigidbody, and network view;
- replaces gameplay values, inventory defaults, AI values and mutable lists
  with Living World-owned values;
- applies appearance with one ordinary native visual update and requires an
  active, materialized hair/beard renderer in the local human LOD.

No combat, encounter, torch, stowing, formation, door, wetness, ambient,
workstation, resource, name, equipment, or stat policy is redesigned here.

## Acceptance

With Valheim closed, install the candidate. In a clean local test state, one
`lwparty` command must produce both companions, start follow, and report one
successful appearance initialization per actor. Both must show head hair; a
male profile with a non-empty beard choice must show that beard. `lwtest party
status` must report scout and cleric loaded. Any failure rejects the candidate.
