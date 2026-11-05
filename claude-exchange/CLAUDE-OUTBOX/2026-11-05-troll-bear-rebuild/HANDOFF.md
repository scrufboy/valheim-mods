# ORDERS — Marauder, Troll, Bear, Colossus. 2026-11-05. From Brian via Claude to ChatGPT.

Brian: "can you fix the troll and the bear please and make them correctly." "This needs to be fixed now."

## RULES. NO EXCEPTIONS.

1. **Ship complete candidates. Brian tests them in a real scenario with video. You fix from his video.** That is the test loop.
2. **No micro-tests.** No metric suites, pose counts, hash ledgers, compile-only probes, LOD/neck-cap/gate frameworks. Allowed checks: it loads, no errors in the log, it looks like the source model in the game. Nothing else blocks delivery.
3. **Read the owner's words below and do exactly that.** Do not reinterpret. Do not upgrade a prop into a system.
4. **Never move vertices or change bone lengths.** One uniform root scale for size is the only geometry change allowed. Region fitting / BoxFit / radial-length scaling / foot projection is banned (that is what deformed Þurs 0.2.x, `troll_runtime/NativeTrollSkin.cs`).
5. **Never put vanilla Troll/Bjorn animations or bones on the purchased models.** No rotation bridges, no pose copying, no rebinding to native bones. That failed twice.
6. **Stuck 20 minutes → stop, tell Brian in one paragraph, give options.** Never silently switch method.
7. **Only show Brian images from the actual game build you are delivering.**
8. **Before you stop for any reason, write to Git: what is built, where, what is installed, what is next.**
9. Build packages to `Documents/LWC Model Skin Studio/Exports`. Ask Brian once to install. Test profile: not the telemetry baseline unless he says so.

## ORDER OF WORK
1. Marauder (smallest). 2. Troll. 3. Bear. 4. Colossus polish. Deliver each the moment it is done; do not wait for the batch.

---

## 1. SUNKEN MARAUDER (frog)

Brian's words: the back pickaxe is "a fake empty prop". "put two pickaxes, make them usable, and treat it as if they were early axes." The model's own axes "aren't Valheim axes". "it can't just put spears in the hand and items in the hand like it's supposed to."

Do:
- **Hide the model's own axes** (`FrogMarauder_AxeL/R`) in every loadout except an optional `axes` config value.
- **Two pickaxes in the hands, real and usable, behaving as early axes.** Right hand = the attack item: a pickaxe-looking weapon with early-axe stats/damage type (slash, low tier). Left hand = matching pickaxe visual. Attack = Fuling one-handed swing (`swing_longsword`, verified on the Goblin controller). Make `pickaxes` the default loadout.
- **Hands must accept normal Fuling/vanilla items** (club, spear, sword, shield) through the existing loadout config, using the model's own hand sockets.
- **Back pickaxe = static prop mesh** parented to the upper-spine bone on body and corpse. No ItemDrop, no VisEquipment slot, no back-slot logic, no geometry checks. Delete `MarauderCarry` back-slot code path.
- Bomb off.
- Keep what already works: crypt/swamp spawning, rise-from-ground emergence (skeleton wakeup), underwater/swamp presence, hunting Leeches, groups.
- Optional, only if cheap: idle "mining" — swinging the pickaxe at stone/iron with no result while unaware. Skip if it costs more than an hour.
- Voice: native Neck sounds now.

## 2. TROLL (Þurs)

Brian's words: "keep the shape of the purchased troll... and make a new prefab.. like trolls revamped did and Unjohnny did with his yeti in Outsiders".

**The only method: learn how Trolls Revamped and Outsiders did it and do that.** Verified (Claude read both mods' bundles): own skeleton + own avatar + own clips + **own Animator controller that uses the vanilla Troll's parameter names**, vanilla components only (Humanoid, MonsterAI, ZNetView, ZSyncTransform, ZSyncAnimation, CharacterDrop, VisEquipment, FootStep, CharacterAnimEvent, LevelEffects).

Source: `Documents/LWC Model Skin Studio/Unity Projects/Troll-6000.0.75f1-20261104-445f4ff0/Assets/cave_troll/` (`prefabs/troll.prefab`, `meshes/troll_.fbx`). Editor 6000.0.75f1.
Purchased clips (play on its own skeleton): idle, idle_break, walk, run, attack1, attack2, take_damage, death.

Controller (values copied from Trolls Revamped `troll_animator_ru`):
- Params: `sideway_speed` F, `forward_speed` F, `turn_speed` F, `idle` F, `jump` T, `inWater` B, `dead` B, `footstep` F, `random` F, `attack` T, `punch` T, `throw` T, `swing_logv` T, `swing_logh` T, `stagger` T, `wakeup` B.
- `Movement` 2D blend (`turn_speed`,`forward_speed`): idle (0,0), walk (0,3), run (0,6), turns ±0.5 (use walk).
- Any State → attack states on their triggers, back to Movement on exit time.
- Map: `swing_logv`→attack1, `swing_logh`→attack2, `punch`→attack1, `throw`→attack2, `attack`→attack1, `stagger`→take_damage, water→walk/idle.
- Duplicate clips to owned `.anim`, add events: `Stop` before hit, `OnAttackTrigger` at hit frame, optional `Speed` (wind-up ~0.75, swing ~1.8, recover ~1.0). Reference: Stop @0.47, Speed 1.8 @0.53, OnAttackTrigger @0.90.
- Swap this visual onto the vanilla `Troll` prefab (keep all vanilla gameplay, items, drops, spawns) and its corpse. Remove the old Þurs 0.2.x runtime. Package Þurs 0.3.0.

Then (only after the troll looks right in game): trophy = head of THIS troll on `TrophyFrostTroll`, Gravenhide skins, wet look.

## 3. BEAR (Bjorn_Reborn)

Same method as the Troll, Generic avatar (quadruped).
Source: `Unity Projects/Bear-6000.0.75f1-20261104-b5345d61/Assets/Shepherd_Valley_BEAR/HDRP/` (`Models/SK_Bear.fbx`, `Prefabs/P_Bear.prefab`, clips `A_Bear_{Idle,Walk,Run,Attack}_01.fbx`). HDRP materials → a shader Valheim renders.
Read the live vanilla Bjorn controller's parameter list (names only) and expose the same names. Only 4 clips exist: every attack trigger → Attack_01 (vary speed), stagger → short idle, sleep → slow idle, water → walk. Tell Brian which actions have no real clip (death, stagger, swim, sleep). Remove the rotation-bridge runtime. Package Bjorn_Reborn 0.2.0.

## 4. COLOSSUS (works — polish only)

Brian: "phenomenal". Do not touch its rig, behavior or animation.
- Skin: one pass darker and more grotesque.
- Sounds: zombie/Draugr sounds plus specialized heavy Colossus sounds layered on top.
- Death: the biggest poison explosion — a large poison cloud/blast when it dies (vanilla poison/ooze effect, scaled up).

## Corpses (Troll, Bear)
Own ragdoll from our visual (rigidbodies, colliders, joints on main bones; vanilla `Ragdoll` settings; pose copied by bone name; reuse the Marauder/Colossus owned-corpse code). It must fall and lie on the ground looking like the creature.

## Facts (verified 2026-11-05)
- Installed in LWC Formidable Arena: Þurs 0.2.1 (deformed), Bjorn_Reborn 0.1.1 (rotation bridge), Marsh_Marauder_Prototype 0.2.2, Undead_Colossus_Prototype 0.1.2.
- Last Workbench commit `1aff482` (Nov 4 06:48). Nothing newer in Git or Exports. "Native15" is mentioned in a trophy handoff but recorded nowhere.
- Colossus row lives in `lwc_formidable_souls_swamp` (zombie raid) in the Arena profile.
