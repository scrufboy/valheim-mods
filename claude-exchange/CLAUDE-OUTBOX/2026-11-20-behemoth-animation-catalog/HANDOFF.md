# HANDOFF — Behemoth animation catalog (giant / golem / ogre sets)

**From:** Claude · **Date:** 2026-11-20 · **Type:** research data (no mod source, no binaries, no animation assets)

## Intent

Brian asked for "a full catalog of Giant, golem, ogre and any other qualifying animation sets... all the attacks categorized, the idles, the walking... we need a set for the Behemoth so he moves realistically."
This submission is that catalog, plus a recommended clip set for the Draugr Behemoth (`LWC_DraugrBehemoth`).

## What is in this folder

| Path | What it is |
| --- | --- |
| `Behemoth-Animation-Catalog.xlsx` | The workbook (also live as a Claude Sheet: https://claude.ai/artifact/2iLRTi7ENAjRoCrP9RD6TQ) |
| `data/catalog.json` | Machine-readable: one object per clip, 505 clips, same columns as the "All Clips" tab |
| `data/csv/*.csv` | One CSV per workbook tab |
| `baseline/` | The Behemoth's current native clips (GoblinBruteAnimator, 11 clips) and AttackRage events, copied from Brian's 0.1.29 RageRoll audit |
| `tools/` | The Python scripts that produced everything (re-runnable, see VERIFICATION.md) |

## Workbook tabs

1. **Behemoth Set**: per slot (Idle1, Idle2, Walk1, WalkAlerted, Attack1, Attack2, AttackRage, Taunt, Stagger, Swimming, Treading Water, plus NEW charge/roll, recovery, turn, grab, throw, Neck-captive story, death), a Primary pick and alternates, with native clip length beside it.
2. **Attacks**: 133 clips, by type: melee strike, low sweep, spin/rear sweep, stomp/kick, ground slam, leap, charge, combo, power smash, smash-down, throw, grab-bite-throw, unarmed, vendor-unnamed skill.
3. **Idles**: 50 clips. 4. **Locomotion**: 156 clips (walk/run 8-way, start/stop, turn, jump, dash, evade). 5. **Reactions & Death**: 121 clips. 6. **Taunt Story Ambient**: 45 clips.
7. **Speed Match**: playback-speed formula per walk/run clip; needs Behemoth hip height + walk/run speed (yellow inputs, UNKNOWN today).
8. **Packs**: the 10 packs, their rigs, readiness, root-motion options and on-disk paths.
9. **All Clips**: everything in one table. 10. **Method & Limits**.

## Scope (exact)

Included, the 10 packs in Brian's Unity Asset Store cache that are giant / golem / ogre class or direct donors:
HQA3 Monster Animations Giant, Golem, Hunter Ogre, Shamanic Sorcerer · Kevin Iglesias Giant Animations · RamsterZ Giant Golem, Goblin Anims · wemakethegame Giant Golem AnimSet · Explosive Brute Warrior Mecanim · Silvano Junior Creature Cave Troll.

NOT included: Valheim's own creature clips (Troll, Stone Golem, etc.: those live in the game's asset bundles, not the Asset Store cache); other owned packs that carry creature or human animation but are not giant class (Monster Mutant 7, Orc Shaman One/Two, Reptile Humanoid, goblin archer/scout/warrior models, Kevin Iglesias human packs, Explosive archer/sorceress/spearman, cMonkeys mocap library, etc.).

## Key findings (each labelled per PROTOCOL.md)

- SOURCE-INSPECTED: RamsterZ, Kevin Iglesias, wemakethegame, Explosive and RamsterZ Goblin ship **Humanoid** clips, which can retarget to the Behemoth's Humanoid avatar now.
- SOURCE-INSPECTED: all four HQA3 packs ship **Generic** on a standard 3ds Max Biped (`Bip001`) skeleton. A Generic → Humanoid re-import is expected to work. UNKNOWN: retarget quality (not done).
- SOURCE-INSPECTED: Cave Troll is Generic on a custom CAT rig: own model only.
- SOURCE-INSPECTED: RamsterZ Giant Golem is club-authored (ships `StaticMesh_GiantClub`) and includes a Chained set (Idle / Stressed / BreakoutAttempt / BreakFree 17.7 s) that fits the Neck-shaman captive lore.
- SOURCE-INSPECTED: RamsterZ and wemakethegame flag nearly every clip Loop Time = on, including attacks. One-shots must be unticked.
- SOURCE-INSPECTED: only Explosive (Hit/FootL/FootR) and Kevin Iglesias throws (SpawnRock/GrabRock/ThrowRock/RemoveRock) ship animation events. Every other attack needs Valheim `Hit` (and trail) events authored.
- OFFLINE-TESTED: motion numbers (travel, speed, body rise/drop, fastest limb) from world-space bone evaluation; see VERIFICATION.md for the validation.
- UNKNOWN: no clip has been played or viewed on the Behemoth. Every recommendation needs a Workshop preview.

## Remaining unknowns

- Behemoth hip height and live walk/run speeds (needed for Speed Match).
- Visual quality of every pick on the scaled Behemoth mesh.
- HQA3 "Skill_xx" clips are unnamed by vendor; descriptions are measured motion only.
- Licensing for public release: bundling any of these clips in a Thunderstore package is redistribution. The wemakethegame PDF explicitly forbids redistribution/posting. Owner decision; not legal advice.

## Licensing note for this folder

This folder holds **metadata only** (clip names, frame counts, measured numbers). No FBX, `.anim`, `.unitypackage` or extracted animation data is included.
