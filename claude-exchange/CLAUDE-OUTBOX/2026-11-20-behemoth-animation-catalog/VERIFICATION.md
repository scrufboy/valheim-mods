# VERIFICATION — Behemoth animation catalog

No mod code changed. Nothing installed. No live test. Labels per `../../PROTOCOL.md`.

## Inputs (SOURCE-INSPECTED)

Read from `%APPDATA%/Unity/Asset Store-5.x/` on Brian's PC (`alienware`) via the desktop bridge on 2026-11-20, extracted as tar.gz without Unity:

| Package | Bytes | Clip rows |
| --- | ---: | ---: |
| HQA3/Animation/Monster Animations Giant.unitypackage | 4,637,540 | see data |
| HQA3/Animation/Monster Animations Golem.unitypackage | 16,587,311 | |
| HQA3/Animation/Monster Animations Hunter Ogre.unitypackage | 4,769,183 | |
| HQA3/Animation/Monster Animations Shamanic SorcererThe Wand Wielder.unitypackage | 5,117,073 | |
| Kevin Iglesias/Animation/Giant Animations.unitypackage | 15,287,285 | |
| RamsterZ/Animation/Giant Golem.unitypackage | 21,674,574 | |
| RamsterZ/Animation/Goblin Anims.unitypackage | 13,902,833 | |
| wemakethegame/Animation/Giant Golem AnimSet.unitypackage | 12,356,069 | |
| Explosive/Animation/Brute Warrior Mecanim Animation Pack.unitypackage | 5,948,243 | |
| Silvano Junior/3D ModelsCharactersCreatures/Creature Cave Troll.unitypackage | 67,516,700 | |

Total: 505 clip rows (demo, T-pose and model-only takes excluded). Prop FBXs that failed bone evaluation (Rock, Tree, Stage, Club, etc.) are not animations and are excluded.

## Method

- `tools/catalog.py`: per FBX `.meta` (ModelImporter): animationType, clipAnimations (name, first/last frame, loopTime, events); FBX AnimationStack timing and frame rate. All packs are 30 fps.
- `tools/rows.py`: one row per clip, seconds = frame range / fps.
- `tools/motion2.py`: binary FBX parser; evaluates hips, both hands and both feet in world space through the full parent chain (Lcl T/R/S + PreRotation, per-node rotation order) at 30 fps.
- `tools/build.py`: classification, per-clip metrics in hip-heights (pack reference hip height = median frame-0 hip height of idle/walk files), foot-contact ground speed, fastest-limb estimate.
- `tools/xlsx.py`: workbook.

## Checks (OFFLINE-TESTED)

1. **Root-motion vs in-place twins.** Vendor twin pairs read as expected: wemakethegame `move_walk_front` Inplace = 0.00 travel, root-motion = 0.77 hip-h/s. Kevin Iglesias `Walk01 - Forward` = 0.00, `[RM]` = 0.87. Explosive `Attack2` travels 3.68, `Attack2_S` = 0.00.
2. **Foot-contact speed vs measured root travel** (clips with real root motion):

   | Clip | Root travel (hip-h/s) | Foot-contact (hip-h/s) |
   | --- | ---: | ---: |
   | KI Walk01 Forward [RM] | 0.87 | 0.86 |
   | RamsterZ Move_Walk_Forward | 0.45 | 0.44 |
   | wemake move_walk_front | 0.77 | 0.76 |
   | Explosive Walk | 1.57 | 1.69 |
   | KI Run01 Forward [RM] | 1.75 | 1.63 |
   | wemake move_run_front | 2.65 | 2.49 |
   | Explosive Run | 3.20 | 2.06 |

   Result: walks within ~5% (Explosive +8%); runs under-read 5–35% (flight phase). Labelled as an estimate in the sheet.
3. **Reproducibility.** `catalog.py` → `rows.py` reproduced `rows.json` byte-identical; re-running `build.py` + `xlsx.py` from the shipped tools in a clean folder reproduced `catalog.json` exactly.
   `data/catalog.json` sha256 `91c0d93e155a8405360e3b8d482a79f855685e1595875fbe48682a352112cbc0`
4. **Sheet import.** The workbook imported into the Claude Sheet with 10 tabs, 0 items not brought in, 0 unknown functions.

## Not verified (UNKNOWN)

- No clip viewed or played. Fastest-limb time is a hit-frame *candidate*, not a confirmed impact.
- HQA3 Generic → Humanoid conversion not performed.
- Unity's own root-motion extraction depends on import bake flags; the numbers here are raw bone travel.

## Re-run

```
# extract each .unitypackage (tar.gz) to work/pk/<PackName_with_underscores>/
python3 tools/catalog.py work/pk work/raw.json
python3 tools/rows.py work/raw.json work/rows.json
python3 tools/motion2.py work/pk work/traj.json
LWC_ANIM_WORK=work python3 tools/build.py
LWC_ANIM_WORK=work python3 tools/xlsx.py
```
Needs Python 3 with numpy and openpyxl.
