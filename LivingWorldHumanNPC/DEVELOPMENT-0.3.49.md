# Human NPC 0.3.49 leader readiness and stealth restoration

## Proven 0.3.48 failures

The 2026-10-18 18:59 runtime log and telemetry established three separate ordering errors:

1. The alert voice was first instantiated as a live scene object. Its effect lifecycle destroyed it before Jotunn's later ZNetScene registration pass, producing `NullReferenceException` in `PrefabManager.RegisterToZNetScene`.
2. Peaceful nighttime torch ownership was evaluated before the established leader-readiness signal. A visible torch could therefore prevent companions from drawing weapons when the leader drew weapons.
3. Crouch mimicry read `PresenceLeader`, a peaceful-only view which disappears during heightened awareness. Telemetry consequently recorded zero crouched companion states even when the bound leader entered stealth.

One compiled regression check also explicitly prohibited leader weapon mirroring. That check contradicted the required companion behavior and has been replaced.

## Corrections

- The private alert sound is created with Jotunn's `CreateClonedPrefab`, which stages it directly under the inactive prefab container before registration. The NPC root is still independently composed and is not cloned.
- Hand arbitration is now ordered: committed combat, leader weapon readiness, peaceful utility torch, peaceful stow.
- Both the torch update and torch hand claim yield to leader weapon readiness.
- Releasing an already absent torch no longer alternates combat off-reasons in the log every update.
- Posture mimicry reads the persistent bound `PartyLeader`; heightened awareness therefore retains leader crouch, while committed combat still forces an upright fighting posture.

No target-selection, attack, damage, formation, wetness, door, healing, or combat-number logic changed.

## Required runtime evidence

1. Jotunn reports both custom prefabs without a registration warning.
2. Calm darkness equips stable torches.
3. Drawing the leader's weapons removes torches and draws companion weapons even without an authorized hostile target.
4. Stowing the leader's weapons returns to peaceful stow or torch behavior.
5. Leader crouch produces companion crouch during traversal and heightened awareness, but not during committed combat.

