# HANDOFF — Rebuild Troll, Bear and Marauder the right way (Claude → ChatGPT), 2026-11-05

Owner: Brian. Owner instruction (verbatim): "can you fix the troll and the bear please and make them correctly".
Owner method (verbatim): "the robot was supposed to keep the shape of the purchased troll...and make a new prefab.. like trolls revamped did and Unjohnny did with his yeti in Outsiders".

## 0. STRICT DIRECTIVE — read first, obey throughout

1. **Deliverable is one installable candidate per creature, fast.** Not reports, not metric suites, not hashes-of-hashes.
2. **The first gate is how it looks in the real game.** Build → load in game → look at it next to the purchased source. If it does not look like the purchased model, it fails, whatever any probe says.
3. **Never move a vertex. Never change a bone length.** The ONLY allowed geometry change is ONE uniform scale on the model root to set overall size. Any region fit, BoxFit, radial/length scaling, foot projection, or "fit to native bounds" is forbidden. (That is what made Þurs 0.2.x look deformed: `troll_runtime/NativeTrollSkin.cs` BoxFit / `Scale(radial,length,radial)` / `ProjectFoot`.)
4. **Do not drive the purchased model with vanilla Troll/Bjorn clips or bones.** No rotation bridges, no HumanPoseHandler copying, no rebinding to the 57 native bones.
5. **No new verification frameworks.** Allowed checks: does it load, does it look right, does it walk/attack/hit/die/leave a corpse, are there errors in the log. Nothing else blocks the candidate.
6. **If blocked for more than ~20 minutes on one thing, stop and tell Brian in one paragraph** what is blocked and the options. Do not silently pivot to a different method.
7. **Never show Brian images that did not come from the actual game build being delivered.**
8. **Record state in Git before you stop, every time** (what is built, where, what is installed, what is next). Anyone must be able to take over from Git alone.
9. Installation into a profile needs Brian's explicit go (CLAUDE.md). Build the package to Exports, tell him it is ready, ask once.

## 1. The method (verified from the two reference mods' actual asset bundles)

Claude extracted and read the bundles inside `Ruijven-Trolls_Revamped-2.1.1` and `UnJohnny-Outsiders-1.6.5` (Brian supplied both zips).

| | Trolls Revamped `Troll_ru` | Outsiders `Out_Yeti` |
|---|---|---|
| Skeleton | its own (100 bones) | its own (61 bones) |
| Avatar | own `Troll5.2Avatar` | own `YetiAvatar` |
| Animator controller | own `troll_animator_ru` | own (named `troll_animator`) |
| Clips | its own (15) | its own (18) |
| Controller parameters | **vanilla Troll parameter names** | same |
| Components | only vanilla: Humanoid, MonsterAI, ZNetView, ZSyncTransform, ZSyncAnimation, CharacterDrop, VisEquipment, FootStep, CharacterAnimEvent, LevelEffects, Rigidbody, CapsuleCollider | (same family) |

So: **purchased model + its own skeleton + its own clips + a controller that exposes the vanilla Troll's parameter names**, and vanilla Valheim components on top. The game's Troll AI sets `forward_speed`, fires `swing_logv` etc., and the purchased model plays its own animation for it. Shape is untouched. This is also exactly why the Undead Colossus works.

### Reference controller values (copy these; from `troll_animator_ru`)

Parameters (name, type):
`sideway_speed` F, `forward_speed` F, `turn_speed` F, `idle` F, `jump` Trigger, `inWater` Bool, `dead` Bool, `footstep` F, `random` F, `attack` Trigger, `punch` Trigger, `throw` Trigger, `swing_logv` Trigger, `swing_logh` Trigger, `stagger` Trigger, `wakeup` Bool.

States and transitions:
- `Movement` = 2D blend on (`turn_speed`, `forward_speed`): idle (0,0), walk (0,3), run (0,6), turn left (-0.5,0), turn right (0.5,0).
- `In Water` = blend on `forward_speed`: tread (0) / swim (1). Movement ↔ In Water on `inWater` true/false.
- Any State → `attack`/`punch`/`throw`/`swing log V`/`swing log H`/`Stagger` on the matching trigger; each returns to Movement on exit time (reference: attack 0.8, punch/throw 0.88, swings 1.0, stagger 1.5). Reference state speeds: swings 0.25–0.7 on their (fast) clips — tune by eye.
- `wakeup` bool gates an optional spawn/roar state.

Animation events Valheim needs on attack clips (reference values, functionName @ normalized-ish time):
- `Stop` (before the hit; stops rotation), `OnAttackTrigger` (the hit frame), optional `Speed` with float param (slow wind-up 0.75–0.8, fast swing 1.5–1.8, recover 0.5–1.0), optional `TrailOn`/`TrailOff`.
- Example `Zombie Attack`: Speed 0.75 @0.0, Stop @0.47, Speed 1.8 @0.53, OnAttackTrigger @0.90, Speed 1.0 @1.15.

Purchased clips have **0 events** → duplicate each clip into an owned `.anim` and add events with `AnimationUtility.SetAnimationEvents`. Do not edit the vendor FBX importer.

## 2. Troll

Source (Brian's PC): `Documents/LWC Model Skin Studio/Unity Projects/Troll-6000.0.75f1-20261104-445f4ff0`, vendor package `Assets/cave_troll/` (`prefabs/troll.prefab`, `meshes/troll_.fbx`, `animation_controller/troll_anim.controller`, textures). Editor 6000.0.75f1.
Purchased clips (8): idle, idle_break, walk, run, attack1, attack2, take_damage, death. They already play on the model's own skeleton — no retargeting.

Clip mapping for v1:
- Movement: idle (0,0), walk (0,3), run (0,6), turns = walk at reduced speed (no turn clips).
- `swing_logv` → attack1; `swing_logh` → attack2; `punch` → attack1 (faster); `throw` → attack2 (OnAttackTrigger at release); `attack` → attack1.
- `stagger` → take_damage. `In Water` → walk/idle (no swim clip). idle_break → `idle`/`random` variation if trivial, else skip.
- Corpse: see §4.

Build steps (Editor script, one menu item, rerunnable):
1. Instantiate vendor `troll.prefab` as an owned copy. Keep its skeleton, meshes, materials (convert shader to one Valheim renders, as the Colossus/Marauder pipeline already does), and mace.
2. One uniform root scale so height ≈ vanilla Troll height. Nothing else.
3. Create owned controller per §1 with owned event-carrying clips per mapping above.
4. Put Animator (Generic avatar from the FBX is fine) + `CharacterAnimEvent` on the visual root. Hand attach points: the model's own hand bones.
5. Export AssetBundle with the matching 6000.0.75f1 editor.

Runtime (Jotunn plugin, package **Þurs**, next version 0.3.0; keep the name Brian chose):
- "Put the new troll model on the old troll": on the vanilla `Troll` prefab, replace the `Visual` child with ours, point Humanoid/VisEquipment/LevelEffects/LODGroup at our renderers and hand bones, keep every vanilla gameplay field, item set (log/unarmed), drop and spawn. Do the same visual swap for its corpse (§4). Remove the old Þurs 0.2.x runtime entirely (no NativeTrollSkin / TrollPoseBridge).
- Log variant: vanilla Troll's log items still attach to our right-hand bone; the purchased mace is the visible weapon only if Brian wants it — default: keep vanilla log/unarmed sets so behaviour is unchanged.

## 3. Bear (Bjorn)

Source: `Unity Projects/Bear-6000.0.75f1-20261104-b5345d61`, vendor `Assets/Shepherd_Valley_BEAR/HDRP/` (`Models/SK_Bear.fbx`, `Prefabs/P_Bear.prefab`, `Animations/A_Bear_{Idle,Walk,Run,Attack}_01.fbx`, textures). Quadruped → Generic avatar, own clips.
Vanilla Bjorn parameters (from ChatGPT's own Nov 4 inventory): floats `forward_speed`, `sideway_speed`, `turn_speed`, `footstep`, `idle`; bools `inWater`, `sleeping`; triggers `jump`, `wakeup`, `stagger`, `consume`, `attack_bite`, `attack_claws`, `attack_taunt`, `attack_swipe_r`, `attack_swipe_l`, `attack_swipe_combo`, `attack_slam`. **Verify against the live Bjorn controller before building** (read parameter list only).
Only 4 purchased clips exist. v1 mapping: all attack_* triggers → Attack_01 (vary state speed per trigger), `consume`/`attack_taunt` → Attack_01 slowed or Idle, `stagger` → short Idle, `sleeping` → Idle at 0.3 speed, water → Walk. Tell Brian which actions lack proper clips (death, stagger, swim, sleep) — that is an honest gap, not a reason to stop.
Same build/runtime pattern as the Troll; package **Bjorn_Reborn**, next version 0.2.0; remove the rotation-bridge runtime.
HDRP materials must be converted to a shader Valheim renders.

## 3b. Sunken / Marsh Marauder — fix the weapons the practical way

Owner (verbatim, 2026-11-05): it "left the models original axes in hand.. but gave it spear chucking animations. it never thought to use unity and our tools kit to just put dual pickaxes into the sunken marayders hands instaed of it's own axes... then just give the creature ealy axes animations". The pickaxe on the back is "an empty prop model attached to the back", not an equipable item.

Verified current code (`creature_runtime/`): default "axes" loadout = invisible GoblinSword clone (`swing_longsword`) while the model's own axes stay visible; spear-throw comes from the `ExperimentalOozeBomb` GoblinSpear path; back pickaxe is built as a VisEquipment two-handed back slot with geometry checks (`MarauderCarry.cs`).

Fix:
1. New loadout `pickaxes` (make it the default; keep `axes` as a config option, per the Nov 3 scope): hide `FrogMarauder_AxeL/R`, parent a pickaxe mesh (vanilla `PickaxeIron`/`PickaxeAntler` visual, plain MeshRenderer, no ItemDrop) to the model's left and right hand sockets. The attack item is the right-hand one-handed chop; left is visual.
2. Attack animation = Fuling one-handed swing (`swing_longsword` is the verified one-handed action on the Goblin controller). If the Goblin controller has a closer axe-chop action, read its action list once and use it; do not import player animations.
3. Back pickaxe = static mesh parented to the upper-spine bone, on the living body and the corpse. Delete the back-slot/VisEquipment/geometry-check path.
4. Bomb stays off.
5. Voice: native Neck sounds now (approved fallback); frog picks later.

## 4. Corpse (both)

Vanilla death spawns `<creature>_ragdoll`. Make our own ragdoll from our visual: Rigidbody + Capsule/Box collider + CharacterJoint on the main bones (hips, spine, head, upper/lower limbs), vanilla `Ragdoll` component settings copied from the vanilla ragdoll, pose copied from the living visual by bone name at death, and swap it into the creature's death effect. Gate: it falls and lies on the ground, same look, no floating. (Valheim-side pose transfer for owned corpses already exists in the Marauder/Colossus code — reuse it.)

## 5. Order of work

0. Marauder pickaxes (§3b) — smallest change, do it first.
1. Troll: Editor builder → bundle → plugin → load in Arena-copy / Sandbox (not the telemetry baseline) → look → screenshot from the game → tell Brian.
2. Bear: same.
3. Only then: trophy (head from the correctly shaped troll on `TrophyFrostTroll`), wet skin, Gravenhide paints.

## 6. Out of scope now
Trophy neck caps, LOD probes, wet pipeline, branding, Workbench features, Frog/Marauder/Colossus changes, raid edits, multiplayer proofs.

## 7. Context facts (verified by Claude 2026-11-05)
- Installed in LWC Formidable Arena (Brian's r2modman screenshot): Þurs 0.2.1 (deformed), Bjorn_Reborn 0.1.1 (rotation bridge), Marsh_Marauder_Prototype 0.2.2, Undead_Colossus_Prototype 0.1.2.
- Last Workbench commit `1aff482` (Nov 4 06:48). Exports newest = Thurs 0.2.1. No later "corrected" builds are in Git or Exports. Native15 wet/dry troll is referenced in a trophy handoff but not recorded anywhere.
- The Colossus's row is in `lwc_formidable_souls_swamp` (zombie raid) inside the telemetry Arena profile; Brian saw it there. Colossus still needs zombie/Draugr sounds.
