# Human NPC 0.3.56 — native combat ownership

Date: 2026-10-19

## Root cause

0.3.55 described combat as native-owned, but its Harmony boundary still made
Living World a second combat authority:

- a prefix on `MonsterAI.UpdateTarget` required a Living World authorization
  before Valheim could run its normal target selection;
- a postfix on `BaseAI.IsEnemy` hid every hostile except the one target already
  authorized by Living World;
- `MonsterAI.SetTarget`, `DoAttack`, and the eventual animation attack trigger
  were all gated by the same authorization record;
- the archer adapter could replace the weapon chosen by native
  `EquipBestWeapon` with a separate role-policy decision.

This explains delayed acquisition, standing through attacks, target stalls, and
weapon indecision. The Souls of the Land combat values were already present;
the companion layer was preventing them from operating as one native loop.

## Correction

- `MonsterAI.UpdateTarget` now runs without a Living World prefix.
- `BaseAI.IsEnemy` is no longer patched by the companion.
- Native target selection is observed in the `UpdateTarget` postfix. A legal
  native target immediately becomes the one combat owner for the actor.
- Post-selection validation remains as a safety fence: players, tame creatures,
  companions, non-enemies, unprovoked wildlife reserved for Hunt mode,
  stealth-hold violations, and targets outside the bounded party envelope are
  rejected.
- Explicit player orders and attacks on the protected party may still hand a
  legal target directly to native combat.
- `EquipBestWeapon` owns bow-versus-melee selection. Living World only prevents
  a utility torch from becoming a combat attack and supplies the existing bow
  release compatibility bridge.
- Follow, ambient gaze/work, formation, torch, peaceful posture and door-closing
  continue to read the single combat-owner state and cannot steer active combat.

The configured 30 m view, 90-degree view angle, 9,999 hearing setting, 20 m alert
range, zero minimum attack interval, interception and 8/6/8 circling values are
the verified Souls of the Land Krauguk Legion/Common Folk combat values already
used by this actor. They were not changed in this correction.

## Verification boundary

Offline tests prove that no pre-target or global enemy filter remains, native
weapon selection is not overwritten, and existing subsystem lockouts remain
connected. Live Unity combat is still required to accept the candidate.
