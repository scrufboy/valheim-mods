# Native Meadows Village geometry audit

2026-10-20. **Offline measurements complete; live navigation survey not run.**
Scope: architectural reference for the settlement lab, not an install package or
permission to change the world. No game, NPC, profile, or reclamation code changed.

## Findings that affect the lab

- PoP Villages supplies spawn configuration, not house blueprints. Its repaired
  rules refer to native Meadows Village rooms and two separate stone-tower locations.
- All ten Meadows Village room references resolved from the installed assets.
  Their placement boxes are **not** their building footprints, interior dimensions,
  or usable navigation lanes.
- The native single wooden door has a **1.400 m frame gap**. The great hall uses
  **two wood gates**, not two small doors; the outer gate posts leave **3.400 m**.
  These are static post separations, not certified actor-clear widths.
- There are **zero `Bed` components** in the ten module graphs. Bed-like decoration
  is not a usable owned bed. Supply real beds for lab sleeping/ownership tests.
- The tower has upper-floor spawners and three stepladders. A creature spawning
  upstairs is not evidence that a replacement guard can navigate upstairs.
- `WoodVillage1` applies random damage, and the modules have conditional
  `RandomSpawn` objects. A generated village is not an intact copy of every
  serialized piece. Native origin does not establish “100% NPC-friendly.”

## Evidence and reproducibility

Measured installed **Valheim 1.0.20**, as identified by the active lab startup log.
Do not relabel these measurements as the project's older 1.0.17 evidence.

`assembly_valheim.dll` SHA-256:
`94998885b103d6e5f5480123eaee0a093e82af2ddeeee37536c15bf555f24f3a`.

Sources:

- Installed `valheim_Data/StreamingAssets/SoftRef/manifest` **and**
  `manifest_extended`. The extended manifest alone does not contain the individual
  room entries.
- `Assets/world/dungeon/DG_MeadowsVillage.prefab`, bundle `c4210710`.
- `Assets/Systems/RoomLists/_RoomList.prefab`, bundle `d59cfac`.
- `Assets/world/Locations/Meadows/WoodVillage1.prefab`, bundle `5521083d`,
  asset ID `0c74cde9ef71f8e4f9b22e2c8d2283f3`.
- Repaired project `work/VillageReclaim/package/config/popvillages/`
  `spawn_that.local_spawners.popvillages.cfg`; original and 1.0.2 inspected copies.
- Native method inspection: `DungeonGenerator.GenerateCampRadial`,
  `GetCampRoomRotation`, `PlaceWall`, `TestCollision`, `SetupColliders`;
  `DungeonDB.SetupRooms`; `Pathfinding.GetPath`.

Run [audit_native_village.py](audit_native_village.py) with an existing UnityPy
installation. It reads installed files, resolves the manifests and selected room
registry, traverses only the selected prefab graphs and referenced geometry, and
prints measurements as JSON. It does not export meshes, textures, bundles or game
code and does not start Unity. Reading the shared bundle requires substantial
temporary RAM; this is an offline audit tool, not runtime NPC logic.

```powershell
python -B audit_native_village.py --self-test
python -B audit_native_village.py --data 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data' --python-libs '<existing UnityPy library directory>'
```

## Coordinates and what the numbers mean

All dimensions below are meters. Local vectors use **(X,Y,Z)**: Y up; X/Z horizontal.
Root-local `(0,0,0)` is the module origin, **not** its authoring-scene position,
door threshold, lowest foundation point, or a world coordinate.

The script accumulates descendant translation, quaternion rotation and scale;
it excludes the root's authoring transform. Bounds use enabled, non-trigger
colliders on serialized-active descendants. Mesh bounds come from transformed
native mesh AABBs, not a triangle-level solid-volume calculation. Renderer
envelopes use serialized-active MeshRenderer/MeshFilter pairs. These are
**conservative serialized-state envelopes**, including relevant props, foundations
and conditional objects. They can change after randomization, damage, door motion
and terrain placement. A floor envelope may include gaps and multiple levels.

No table entry below certifies a continuous walking surface or head clearance.

## Measured reusable module catalog

Names below have the prefix `meadowsvillage_`. Placement is `Room.m_size`, X×Z;
all ten placement boxes have Y size 8 m. Vertical ranges are collision-envelope
min/max Y relative to the module origin, **not interior headroom**.

| Module | Placement X×Z | Collision envelope X×Z | Collision Y min…max | Render envelope X×Z | Wood-floor envelope X×Z |
|---|---:|---:|---:|---:|---:|
| greathall | 16×20 | 12.546×16.546 | -2.000…11.213 | 13.033×17.028 | 8×14 |
| greathallruin | 16×20 | 12.546×16.546 | -2.000…7.213 | 13.033×17.028 | 8×14 |
| longhouse | 16×16 | 16.425×12.450 | -2.213…6.268 | 16.425×12.450 | 8×12 |
| longhouseruin | 16×16 | 16.425×12.625 | -2.213…5.083 | 16.425×12.625 | 8×12 |
| house1 | 13×13 | 14.425×8.608 | -2.213…6.215 | 14.425×8.608 | 6×8 |
| house2 | 13×13 | 14.425×8.608 | -2.213…6.215 | 14.425×8.608 | 6×8 |
| tower | 9×9 | 4.546×4.546 | -0.078…11.213 | 4.957×4.736 | 4×4.005, multiple levels |
| towerruin | 15×15 | 4.518×4.523 | -0.078…8.000 | 4.686×4.692 | 4×4.005, multiple levels |
| stakewall3 | 8×3 | 9.432×1.875 | -0.167…3.300 | 9.535×2.077 | None |
| road1 | 4×17 | 8.285×0.952 | -0.500…3.500 | 8.393×1.158 | None; road is terrain paint |

The road's collision envelope describes its **gate/fence objects**, not the road.
The house and longhouse overhangs/supports exceed their placement boxes in X.
Do not space independent lab buildings using `Room.m_size` alone.

### Module identity and generation flags

All ten registry entries and Room components are enabled, theme mask 32,
`m_faceCenter=true`, `m_entrance=false`, `m_endCap=false`.

| Module | Bundle | Perimeter | Weight |
|---|---|---:|---:|
| greathall | e87f0d42 | No | 1 |
| greathallruin | 55d042f8 | No | 1 |
| longhouse | 9cedc369 | No | 1 |
| longhouseruin | 9a42df6b | No | 1 |
| house1 | b90911e9 | No | 1 |
| house2 | 77d1842e | Yes | 40 |
| tower | 32b711de | Yes | 5 |
| towerruin | 47ceeb80 | Yes | 5 |
| stakewall3 | ca730a8d | Yes | 5 |
| road1 | acc8476b | Yes | 5 |

`house1` and `house2` have the same measured envelopes and spawn coordinates here,
but different placement flags/weights. These facts do not imply every property of
the two graphs is identical. `unused_meadowsvillage_townhouse1` appears in the PoP
configuration but is **not in this ten-room native registry**; do not build a lab
dependency on it.

## Entrances, floors, stairs and roads

| Module | Door/gate component origins, root-local | Wooden-floor collider top Y levels |
|---|---|---|
| greathall / greathallruin | Gates (-1,1,6), (1,1,6) | 0.0969, 1.0969 |
| longhouse / longhouseruin | Door (0,1,6) | 0.0969 |
| house1 / house2 | Door (0,1,4) | 0.0969 |
| tower | Door (-1,1,2) | 0.0969, 2.0969, 3.8469, 5.8469 |
| towerruin | Door (-1,1,2) | 0.0969, 2.0969 |
| road1 | Gates (1,0.75,-2.25), (-1,0.75,-2.25) | No wooden floor |

The tower stepladder origins are `(0,0,-1.5)`, `(1.5,2,0)`, `(0,3.75,1.5)`.
Small intermediate landings and direction changes need actual ascent/descent
tests with the companion's capsule and navigation-agent settings.

Native piece measurements:

- `wood_floor`: collider 2×2 m in X/Z, Y `-0.0329…0.0969` (0.1298 m thick).
  Local X bounds `-1.0096…0.9904`, Z `-1.0148…0.9852`.
- `wood_floor_1x1`: collider 1×1 m, same Y range; X
  `-0.5096…0.4904`, Z `-0.5148…0.4852`. The collider is slightly off-center.
- `wood_door`: jamb inner X faces at -0.700 and +0.700, gap **1.400 m**.
  Jamb depth 0.500 m. Closed leaf collision box is 1.3945×1.8778×0.200 m.
  Its nominal frame occupies approximately Y -1…+1 about the component origin.
  The bottom frame collider lies at Y -1.0100…-1.0082. At the module's usual
  door origin Y=1 this is near Y=-0.01, below the normal floor-collider top.
  Adjacent beams, open leaf angle and current terrain determine actual passage.
- Great-hall paired `wood_gate` posts occupy X -2…-1.7 and +1.7…+2,
  Y 0…3, Z 5.75…6.25: **3.400 m between posts**, no center post.
  Each closed leaf collider is approximately 1.6812×3×0.2 m.
  This is not the same fixture as two `wood_door` pieces.
- All inventoried doors have guard-stone checking enabled and
  `m_canNotBeClosed=false`. Geometry does not grant an NPC access permission.

`road1` has six round terrain-paint patches. X/Z centers and radii:
`(0.5,3):2`, `(0,-0.6):2`, `(-0.5,-3.2):2.5`, `(0,-5.96):2`,
`(-0.5,6.04):2`, `(0.5,8.54):1.5`. Their union's bounding rectangle is
X `-3…2.5`, Z `-7.96…10.04` (5.5×18 m); it is **not a uniform-width lane**.
These modifiers paint cleared ground with leveling and smoothing disabled.
Painting a path therefore does not prove a flat or navigable road.

## Native spawn markers, not resident capacity

`D` = Draugr, `R` = Draugr_Ranged, `E` = Draugr_Elite;
`P` = body pile (`SpawnArea`). Coordinates are root-local marker origins, not
guaranteed final creature feet. Conditional spawning can remove objects; pile
spawn areas emit actors around the pile, not only at its origin.

| Module | Potential native marker coordinates |
|---|---|
| greathall | D (3,.25,4), (-2.75,.25,4), (0,.25,3.25); R (-1.5,1.25,-4.75), (1.5,1.25,-4.75); E (0,1.25,-6.25); P (0,1,-5.298) |
| greathallruin | D (3,.25,4), (-3,.25,0); R (-3,1.25,-6.25); P (.5,0,-5.05) |
| longhouse | D (.75,.25,2.25), (-.75,.25,2.75); R (-1.5,.25,-3.25), (1.5,.25,-3.25); E (0,.25,-2); P (-1.5,0,.202) |
| longhouseruin | D (-.75,.25,2.75); R (.5,.25,-2.5); P (.5,0,.2) |
| house1 / house2 | D (1.5,0,1.25), (-1.75,0,.75); R (0,0,-1.05); no pile |
| tower | D (1,.25,-.5), (-.75,.25,.5); R (1,6,-1), (-.5,6,0); no pile |
| towerruin | D (1,.25,-.5), (-.75,.25,.5); no pile |
| stakewall3 / road1 | No CreatureSpawner or SpawnArea |

PoP's 20 config sections are room/prefab **selectors**, not 20 individual points.
One selector can match several markers. The repaired config's three-minute
respawn, 60 m trigger distance and patrol-point flag are spawn policy, not floor
dimensions or dwelling capacity. The old reclaim implementation's 80 m horizontal /
50 m vertical proximity check is likewise not a surveyed settlement boundary.
`StoneTowerRuins05` and `StoneTowerRuins07` are additional location selectors, not
members of the Meadows Village room registry.

## Layout/origin convention

`WoodVillage1` is a small wrapper whose generator is at local `(0,0,0)`.
Its Location component has exterior radius 32 m, clear-area enabled, random damage
enabled and no separate interior. The generator uses **CampRadial**, not a fixed
house-grid blueprint:

- Serialized camp radius range 28–32 m; room target min/max settings 20/30;
  perimeter-section target 10; maximum accepted ground tilt setting 25 degrees;
  minimum-altitude setting 1 m. Targets do not guarantee placed counts.
- Placement samples real ground and tests room-box collision. Perimeter rooms use
  the selected camp radius, while other rooms are sampled inside it.
- With `m_faceCenter`, rotation faces the center and snaps yaw to **22.5-degree
  increments**. `m_tileWidth=10` and `m_gridSize=4` are also serialized, but must
  not be interpreted as the active radial algorithm's walking grid.
- There are **zero RoomConnection components** in these ten room graphs.
  They are not snapped together using dungeon door sockets.
- Native collision placement checks use `Room.m_size` (candidate reduced by
  0.1 m per dimension), not all roof/support meshes. This explains why passing
  generation checks does not certify pedestrian space between structures.

For a placed module, convert a measured local point using its actual transform:
`worldPoint = moduleTransform.TransformPoint(localPoint)`. Record the inverse for
survey exports. Do not use editor-authoring root positions (for example, the
great hall's serialized authoring X=244.5) as layout offsets.
Seed plus a web-map icon is not a surveyed, modified village instance.

## Live Site Survey Tooler probe specification

This is a **proposed probe contract**, not a claim the controls already exist.
No live survey or actor movement was performed in this audit. Keep all values
below `unknown/not_run` until observed.

1. **Identity and scope.** Record world UID, seed, game/assembly hash, Tooler/NPC
   versions, survey timestamp, chosen `WoodVillage1` location position, loaded
   extent and module placement records. Prefer the generator's saved room
   hash/position/rotation over fuzzy name matching. If the instance no longer
   retains a room root, mark reconstructed association and confidence explicitly.
   Never attach a nearby unrelated tower merely because it falls inside a circle.
2. **Read-only capture.** Export root transforms, native prefab IDs, persistent
   object IDs where available, actual enabled collider bounds, damage/missing
   pieces, door leaf transforms/state/access, functional beds and ownership,
   furniture, fires and station-use anchors. Do not call Interact, door RPCs,
   spawn/cleanup, damage, terrain application or ZDO mutation during capture.
3. **Ground and floor.** Start with a 1 m module grid; refine doorway, landing and
   obstruction corridors to 0.10 m spacing. Raycast all relevant solid surfaces
   and keep separate stories; record exact floor/ground hits, normals, water,
   gaps, step-up/down and collider identity. A top-down ray alone will hit a roof
   and miss interiors; repeat at known story bands. Keep terrain separate from
   raised floors. Measure both sides of every threshold.
4. **Clearance.** At each candidate lane station measure nearest left/right solid
   boundaries, minimum overhead solid clearance, door sweep volume, and a capsule
   sweep using the **actual current companion capsule** and its
   `BaseAI.m_pathAgentType`. Record capsule radius/height/center, agent settings,
   collision mask and skin margin; do not hard-code a supposed player radius.
   Furniture and open doors remain obstacles. Report geometric clearance
   separately from navigation success.
5. **Queries before actor trials.** Use current
   `Pathfinding.GetPath(from,to,List<Vector3>,agentType,bool,bool,bool)` with the
   same verified flag meanings as the current door/ambient module. Record the
   flags, return value, all returned corners, endpoint error and query time.
   A `true` result alone is insufficient. Cover exterior→door→deep interior and
   reverse; each floor→tower landing→upper post and reverse; hall→house→road;
   dwelling→bed/seat/station approach. Use **both directions**. Record closed,
   manually opened and partially obstructed door states as distinct observations.
6. **Behavior gate, separately authorized.** With two current companions, test
   one-way passage, opposing traffic, turning inside, opening, crossing, closing
   without trapping the other actor, upstairs relief, workstation approach and
   return. Record actual completion, timeout/stuck, near-goal oscillation,
   fallback/teleport, collisions, door owner and resulting state. Query success
   and player traversal are not substitutes for these outcomes. Do not perform
   these movements on the user's behalf as part of a read-only scan.
7. **Bounded cost.** Proposed defaults: at most 32 physics samples/frame and a
   2 ms soft time budget checked between samples; at most 2 path queries/second;
   cancellation between jobs; cache per module/state revision. Cap total sample
   count and report truncated coverage. Serialize only on completion/cancel,
   never every frame. No permanent monitoring or background actor simulation.
8. **Output.** Under a Living World-owned survey output directory, emit JSON plus
   a short report containing `surveyId`, `worldUid`, `moduleId`, `localToWorld`,
   `sampleCoverage`, `actualBounds`, `floorBands`, `thresholds`,
   `doorStates`, `clearanceSamples`, `paths`, `actorTrials`, `unresolved`.
   Every derived field carries units, evidence kind (`static_asset`,
   `live_physics`, `nav_query`, `actor_trial`) and failure/unknown reason.
   This is a future survey export contract; this audit wrote no profile files.

**Still requires live evidence:** continuous interior lane widths, lowest
headroom along each route, current threshold step heights, stair/landing
reachability, door-open usable clearance, workstation/fire/smoke safety, access
permissions, bed occupancy, and module-to-module roads in the actual world.

## Independent Living World construction blueprints

Use these measurements to author **new layouts** with purposeful wide passages,
protected work/bed approaches and explicit station anchors. Do not copy a whole
third-party village file or package this extracted native graph as original art.

An original blueprint schema can contain:
`schemaVersion`, `blueprintId`, `authorship/provenance`, `supportedGameBuild`,
`originConvention`, approved native `prefabId` references with local transforms,
own connection/approach anchors, floor bands, no-build lanes and survey evidence.
The installer supplies our code/data; each user's installed game supplies vanilla
assets. Keep native prefab graphs, meshes, textures, bundles, decompiled game
source, other mods' assets and other authors' blueprints out of the release.
Technical reference-by-name is not a blanket permission determination.

For local reconstruction research, capture an explicitly selected instance into
root-relative coordinates from its owner's installed game, excluding NPCs,
spawners, loot, generated IDs, terrain modifiers and private state. Keep that
capture private/reference-only; a release blueprint should be independently
authored, provenance-reviewed, and validated. Placement must be a separate
preview/confirmation operation, never a side effect of surveying.

For the lab specification, retain module **proportions**, but do not silently
equate proposed 8×10 or 12×20 clear-interior rooms with these native structures.
A great-hall reference supplies an 8×14 floor envelope, and a house supplies 6×8.
If the lab needs more working space, author an enlarged layout and validate it as
new geometry. Supply real beds and station-use space, and gate the tower on
verified ascent/descent rather than placing guards upstairs and declaring success.

## Checks and limits

- Transform math checks: identity bounds, rotated nonuniform scale, parent
  translation, empty bounds and SoftRef ID ordering **PASS**.
- Selected native graphs: 10/10 rooms, wrapper and generator resolved;
  collider/mesh/script references **resolved** after loading installed Unity
  default resources for the pile's primitive geometry.
- Fresh-process CLI rerun: **PASS**, exit 0, 10 modules, 4 native piece
  measurements, zero unresolved references. Recorded assembly hash independently
  checked against the installed file.
- Static inventory: doors/gates, floor bands, terrain paint and native creature
  marker positions measured; zero Bed and RoomConnection components observed.
- Native generation/API inspection: completed against the installed assembly.
- Live survey, NPC traversal, multiplayer/ownership and gameplay: **NOT RUN**.
- No package produced, no game launched, no world/profile/mod changes.
