# Living World settlement and combat lab build specification

Status: design only. This document authorizes no world edits, mod installation, or
companion/combat-code changes.

## Charter and coordinate system

The maypole is the settlement authority marker and local origin `(0,0,0)`. It sits
at the western end of the combined campus spine; its resident circle may still
extend west of the marker. A distinct **Living World Combat Ward** at `(192,0,0)`
is the arena authority marker at the eastern end. Face east from the maypole to
define `+X`; north is `+Z`. Record the world seed, world
name, maypole map coordinates, ground height, build version, and screenshots before
placing or removing anything.

- Resident boundary: 64 m horizontal radius from the maypole.
- Working settlement: 0–48 m. Residents may own beds, work, eat, socialize, store
  materials, and patrol here.
- Transition ring: 48–64 m. Gates, muster points, delivery handoff, and release
  preparation belong here; no resident bed or irreplaceable storage does.
- Release destination: north gate at `(0,0,+64)`, followed by a marked road to a
  release beacon at `(0,0,+96)`. A released resident is outside settlement authority
  only after reaching the beacon. The release route is perpendicular to the arena
  corridor and must never become an arena approach.
- Contamination rule: settlement tests use only actors assigned to the settlement.
  Arena actors never cross the separation corridor or settlement perimeter. The
  Combat Ward governs arena-only spawns, objectives, resets, and cleanup; it does
  not extend settlement residency eastward.

Use numbered signs or noninteractive markers for every test station. Measure from
the maypole rather than relying on visual estimates.

## Preferred site and alternatives

### Purpose-built settlement — preferred

Choose level Meadow or cleared Black Forest terrain with a shallow shoreline or
stream along the settlement's north or south edge. Reserve a minimum rectangle of
320 × 200 m: settlement on the west, separation corridor in the middle, arena on
the east. Avoid boss sites,
important forests, farms, portals, and permanent player storage.

This option gives repeatable geometry, clean ownership, measurable path widths,
and predictable reset behavior. Its architecture should reuse the proportions and
navigation lessons of vanilla `WoodVillage1` / Draugr-village location modules.
Those modules are native Valheim geometry. PopVillages may populate native location
modules, but it does not provide independent building blueprints and must not be
credited as the source of their structures.

### Complete large Draugr village conversion

Use only a complete village whose occupied footprint fits inside the 48 m working
radius. Before conversion:

1. Duplicate the world save and record its hash/date; never experiment on the only copy.
2. Photograph every building and mark the untouched village perimeter.
3. Inventory spawners, body piles, loot, doors, beds, fires, workstations, terrain,
   and vegetation before clearing anything.
4. Establish a new maypole origin at the village's western campus end and survey
   every retained structure into the same coordinate plan.
5. Put new test fixtures in reversible freestanding annexes. Do not demolish unique
   buildings merely to make the grid exact.

If the village cannot provide the required 2 m and 1 m navigation lanes, dead end,
ramp, water route, and safe resource plot, reject it and use the purpose-built plan.

## Native-module reference and survey gate

Use these modules as architectural references, not assumed ready-made blueprints.
Their identifiers and village use are established; exact outside dimensions, door
clearances, floor elevations, roof volumes, piece counts, and local origins remain
**runtime survey required** until measured in the installed game version.

| Lab use | Native reference | What to preserve | Runtime survey still required |
|---|---|---|---|
| Long hall/social hall | `meadowsvillage_greathall` | Central aisle, hearth/social focus, paired-entry behavior | Full footprint, ridge height, door clear width, hearth offset |
| Damaged-path/recovery fixture | `meadowsvillage_greathallruin` | Broken edges, partial cover, alternate ingress | Collision footprint, gaps, drops, unsafe pieces |
| Craft hall | `meadowsvillage_longhouse` | Long work aisle, roofed stations, end-to-end travel | Footprint, bay spacing, door positions, smoke clearance |
| Navigation/repair annex | `meadowsvillage_longhouseruin` | Obstructed route, dead-end and recovery opportunities | Traversable lanes, floor discontinuities, roof coverage |
| Dwelling A | `meadowsvillage_house1` | Compact owned-bed and table arrangement | Interior clear dimensions, door swing, valid bed/roof cells |
| Dwelling B/kitchen | `meadowsvillage_house2` | Alternate small-house circulation and hearth arrangement | Interior clear dimensions, station/fire spacing |
| Guard/observation post | `meadowsvillage_tower` | Vertical guard position and sightline test | Stair/ladder path, platform area, fall edges, total height |
| Ruined guard/dead end | `meadowsvillage_towerruin` | Partial-height cover and failed-route recovery | Accessible levels, collision gaps, turnaround space |
| Roads and clearances | relevant `WoodVillage1` roads/yard gaps | Proven village spacing, intersections, approach lanes | Road widths, slopes, module-to-module offsets, vegetation clearance |

Do not finalize an automated blueprint from the proposed envelopes below. First
spawn or visit each native module in a disposable survey world, mark its oriented
bounds, and replace every `survey required` field in the campus manifest with the
observed value and game build.

## Proposed settlement envelopes

All dimensions are clear interior/path dimensions, not outside wall dimensions.

| Zone | Center from maypole | Size | Required contents |
|---|---:|---:|---|
| Charter green | `(0,0,0)` | 16 m diameter | Maypole, four approach paths, no clutter within 4 m |
| Dwelling A (`house1`) | `(-18,0,+14)` | 8 × 10 m provisional | Two owned covered beds, two chairs, table, fire-safe hearth |
| Dwelling B (`house2`) | `(-18,0,-14)` | 8 × 10 m provisional | Two owned covered beds, bench, table, campfire |
| Long hall (`greathall`) | `(+10,0,+18)` | 12 × 20 m provisional | Paired hall doors, table, six seats, covered fire |
| Craft hall (`longhouse`) | `(+12,0,-18)` | 18 × 18 m provisional | Covered workbench and forge; roof clearance and fire separation |
| Kitchen (`house2` reference) | `(+28,0,-8)` | 10 × 12 m provisional | Cauldron, cooking spit, food racks, table, covered fire |
| Storehouse | `(-32,0,0)` | 10 × 12 m | Labeled storage, inbound/outbound material points |
| Navigation course (`longhouseruin`/`towerruin`) | `(+30,0,+18)` | 18 × 22 m provisional | Doors, paired doors, lanes, dead end, ramp, incline |
| Guard circuit | radius 38–46 m | 250–290 m loop | Four guard posts at cardinal points and clear sight breaks |
| Resource plot | `(-42,0,-32)` | 30 × 30 m | Sacrificial trees, log drop, stump area, material delivery |
| North gate/muster | `(0,0,+58)` | 10 × 12 m | Gate, guard positions, release queue, no beds |

The envelope sizes are test-campus reservations, not claims about native prefab
dimensions. Expand an envelope after survey rather than scaling or distorting native
geometry to force it into these preliminary numbers.

### Required navigation fixtures

- Main paths: 2.5 m clear width, maximum routine slope 15 degrees.
- Narrow lane: 1.0 m clear width and 8 m long, with a 2 × 2 m turn pocket.
- Paired hall doors: two adjacent normal door openings with a 2 m combined clear route.
- Dead end: 1.5 m wide, 8 m deep, 3 × 3 m turnaround at its closed end.
- Ramp: 2 m wide, 8 m run, 2 m rise; rail only one side so recovery is observable.
- Rough incline: 3 m wide, 12 m run, 3 m rise, natural ground rather than floor tiles.
- Water crossing A: 1.5 m-wide bridge, 10–14 m long, over shallow non-swimming water.
- Water crossing B: 8 m ford, water deep enough to slow movement but not force swimming.
- Door loop: one ordinary door on the outbound route and paired doors on return.

### Ownership and work fixtures

Provide at least four individually attributable beds under valid cover. Chairs and
benches must include examples facing toward and away from nearby tables. Every fire
and workstation gets a safe standing area at least 2 × 2 m. Keep the forge and
kitchen at least 6 m from beds to distinguish work selection from rest selection.

The storehouse has four labeled points: raw wood, ore/stone, food, and finished
goods. Put a delivery marker 4 m outside its door and a consumption marker beside
each relevant workstation. Begin every run with recorded quantities and empty
worker inventories.

The resource plot contains only deliberately planted or expendable trees. Maintain
a 12 m clear felling buffer inside the plot and at least 20 m between standing test
trees and any important structure. Fell toward the empty southwest escape lane.
Never use old-growth scenery, structural trees, portal groves, or the settlement's
outer windbreak for woodcutter testing.

## Observation and reset plan

Observation points are elevated platforms outside normal paths:

| ID | Position | Observes |
|---|---:|---|
| O1 | `(0,+6,-8)` | Maypole charter, idle/social choices |
| O2 | `(-8,+5,0)` | Dwellings, beds, seating, storehouse deliveries |
| O3 | `(+20,+6,0)` | Craft court, kitchen, paired doors |
| O4 | `(+38,+5,+24)` | Navigation course, ramp, incline, guard circuit |
| O5 | `(-48,+6,-38)` | Woodcutter plot and structure safety buffer |
| O6 | arena west tower | Entire arena without entering combat space |

Settlement actor spawn point S1 is `(0,0,-12)`. Visitor spawn S2 is the west gate
`(-58,0,0)`. Release verification begins at the north muster point and ends at the
north 96 m beacon. The settlement reset point is a protected sign/console at `(0,0,-6)`;
it is a procedural checkpoint, not a place to store valuable items.

For every run record: world copy ID, actor roster, time/weather, door states, fire
states, container counts, assigned beds, starting positions, and enabled telemetry
schema. Reset actors, loose items, doors, workstation queues, fires, containers,
felled trees, and damaged test pieces. Restore the world copy if terrain or permanent
structures changed; do not attempt an improvised partial reset.

## Adjacent combat arena

Place the **Living World Combat Ward** and arena center 192 m east of the maypole at
local `(192,0,0)`. The 96 × 96 m arena safety perimeter begins at `X=144`, leaving
an 80 m empty corridor after the settlement boundary ends at `X=64`. No settlement
bed, workstation, tame, storage, or patrol route may enter that corridor or arena.
The Combat Ward is an arena authority marker, not a resident maypole.

- Outer safety perimeter: 96 × 96 m, clearly marked.
- Combat floor: 64 × 64 m, flat, replaceable surface.
- Combat Ward/objective origin: `(192,0,0)` campus coordinates.
- Central objective pad: 8 m diameter for defend/destroy tests.
- Team A and B starts: `(-24,0,0)` and `(+24,0,0)` relative to arena center.
- Neutral reset/spawn vestibules: 8 × 12 m behind each team start, with closed gates.
- Ranged lines: marks at 8, 16, 24, and 32 m from the center.
- Cover: four symmetric 3 × 6 m barriers; no settlement-like furniture.
- Terrain strip: one 3 m-wide incline, one shallow trench, one short bridge, all
  mirrored so faction comparisons remain fair.
- Observation towers: west and south, outside the 64 m combat floor.

Keep four open raid approaches centered on the north, east, south, and west sides.
Each approach is at least 16 m wide and continues at least 32 m into open ground
beyond the outer perimeter. Put no wall, trench, tower, portal, settlement furniture,
or permanent spawn blocker in those lanes. Observation towers sit between approaches.

Controlled faction fights use only explicit team spawns and a written cleanup pass.
Natural raid tests are separate runs: use a minimal, arena-only raid trigger outpost
near the arena perimeter, verify the event circle does not intersect the 64 m
settlement boundary, and start from a clean world copy. Never run a natural raid and
a settlement-behavior trial simultaneously. After each fight confirm zero surviving
combatants, projectiles, drops, temporary effects, or alerted settlement residents
before returning to settlement testing.

## Minimum flat-site staking before any blueprint

Brian should not have to infer either center. A survey tool or guided placement pass
computes the campus frame from four captured site corners, then places labeled stakes.
Before automation exists, reserve the same frame with measured stakes only:

1. Stake M0 at the maypole/settlement center `(0,0,0)` and M-W `(-64,0,0)`, M-E
   `(+64,0,0)`, M-N `(0,0,+64)`, and M-S `(0,0,-64)`.
2. Stake the release beacon at `(0,0,+96)` and keep a 4 m-clear path from M-N.
3. Stake corridor lines C-W at `X=64` and C-E at `X=144`, with endpoints at
   `Z=-48` and `Z=+48`. Place nothing permanent between those lines.
4. Stake CW0, the Combat Ward/arena center, at `(192,0,0)`.
5. Stake arena safety corners `(144,±48)` and `(240,±48)` and combat-floor corners
   `(160,±32)` and `(224,±32)`.
6. Mark four 16 m-wide open approach lanes and extend them 32 m beyond the perimeter.
7. Walk the 320 × 200 m reservation. Reject it if protected locations, important
   construction, steep terrain, or water intrudes on a boundary, corridor, or approach.
8. Record measurements and screenshots before surveying native modules or grading.

## Staged construction checklist

### Stage 0 — preserve and survey

- [ ] Valheim closed; source world duplicated; backup path and hash recorded.
- [ ] Seed, map coordinates, terrain, nearby locations, and baseline screenshots recorded.
- [ ] Purpose-built or Draugr-conversion option explicitly selected.
- [ ] Computed maypole center, Combat Ward center, axes, boundary, release route, corridor, arena corners, and open approaches previewed and staked.

### Stage 1 — terrain and isolation

- [ ] Settlement paths graded without flattening beyond the planned footprint.
- [ ] Arena floor and separation corridor measured.
- [ ] Water routes established without altering unrelated shoreline.
- [ ] No important forest, building, portal, farm, or location is inside the resource plot.

### Stage 2 — resident core

- [ ] Maypole green and four approaches complete.
- [ ] Dwellings, four covered beds, long hall, seats, benches, tables, and fires complete.
- [ ] Bed ownership and roof/fire validity manually confirmed.

### Stage 3 — work and logistics

- [ ] Covered workbench, forge, cauldron, spit, racks, and safe standing areas complete.
- [ ] Storehouse and four material streams labeled.
- [ ] Delivery/consumption points measured and starting inventories documented.
- [ ] Sacrificial resource plot and safe felling direction confirmed.

### Stage 4 — navigation and security

- [ ] Ordinary doors, paired doors, narrow lane, dead end, ramp, incline, bridge, and ford complete.
- [ ] Guard circuit and four cardinal posts walkable.
- [ ] S1, S2, release muster, release beacon, reset point, and O1–O5 labeled.

### Stage 5 — arena

- [ ] 96 m perimeter and 64 m floor measured from the Combat Ward.
- [ ] Four 16 m-wide raid approaches remain open for 32 m beyond the perimeter.
- [ ] Symmetric starts, objective, range marks, cover, terrain strip, vestibules, and O6 complete.
- [ ] Settlement boundary and arena event/aggro envelope do not overlap in a dry run.
- [ ] Controlled-fight cleanup and natural-raid reset procedures rehearsed separately.

### Stage 6 — acceptance before actors

- [ ] Full empty-site walkthrough recorded from every observation point.
- [ ] Door, path, roof, workstation, fire, water, and container states documented.
- [ ] Reset from a disposable run restores the exact baseline.
- [ ] Brian approves the physical layout before any behavior or combat observation begins.

## Later blueprint or prefab capture

Capture only after physical acceptance. Keep the maypole as blueprint origin and
save modular sections rather than one opaque settlement: charter/roads, each
dwelling, hall, craft court, kitchen/storehouse, navigation course, guard ring,
resource plot, and arena. Record each module's local offset and rotation.

Blueprint capture should exclude inventories, loose drops, living actors, planted
test trees, active fires, terrain assumptions, and temporary telemetry markers.
Create a manifest containing tool/mod version, source world copy, piece count,
dependencies, dimensions, origin, screenshots, and SHA-256 for every exported file.
Test restoration into a second disposable world before calling any capture reusable.
If a future tool creates true registered prefabs rather than construction blueprints,
give them new Living World IDs; never overwrite vanilla or third-party prefab names.

## Appendix A — live four-corner survey and cut/fill preview

This is a required preview gate. It runs before any terrain modification and produces
temporary tagged markers plus a saved survey record. Capturing the four outside
campus corners is the only player placement input; the player does **not** manually
identify the maypole center or Combat Ward center.

### A1. Corner capture

Capture four live ground points in clockwise order: southwest `P_SW`, northwest
`P_NW`, northeast `P_NE`, and southeast `P_SE`. Each capture records world UID,
full world position, ground height, biome, slope, timestamp, and the hit surface.
Reject a point placed on a structure, vehicle, vegetation, or temporary object.

The preview validates:

- no crossed edges and consistent clockwise winding;
- opposite-edge length difference no greater than 5%;
- corner-to-best-fit-plane height range reported, never silently discarded;
- usable east–west length at least 320 m and north–south width at least 200 m;
- no protected location or existing important structure inside planned alteration zones.

### A2. Computed campus frame

Compute west midpoint `W=(P_SW+P_NW)/2`, east midpoint `E=(P_SE+P_NE)/2`, south
midpoint `S=(P_SW+P_SE)/2`, and north midpoint `N=(P_NW+P_NE)/2`. The normalized
horizontal vector `W→E` is campus east `+X`. Orthogonalize the horizontal `S→N`
vector against `+X` to produce campus north `+Z`; `+Y` remains world up.

For the required 320 m reservation, compute automatically:

- settlement/maypole center M0 = `W + 64 m × +X`;
- Combat Ward/arena center CW0 = `W + 256 m × +X`;
- corridor boundaries = 128 m and 208 m east of `W`;
- settlement resident radius = 64 m around M0;
- arena safety square = 96 × 96 m around CW0;
- arena combat square = 64 × 64 m around CW0.

If the captured site is longer than 320 m, anchor this 320 m frame to the west edge
and leave surplus land beyond the eastern raid approach. If it is wider than 200 m,
center the working frame between the north and south edges. Sample ground height at
M0 and CW0; their markers follow surveyed ground and are never placed at an averaged
floating elevation. The approved maypole and Combat Ward later replace these two
temporary markers exactly.

### A3. Terrain sampling and alteration mask

Sample terrain on a 4 m grid inside proposed building pads, roads, navigation course,
and arena combat floor. Use an 8 m diagnostic grid across the separation corridor,
outer arena safety area, raid approaches, resource plot, shoreline, and release path.
Every sample records original height, local slope, biome, water depth where relevant,
and whether terrain alteration is allowed.

The alteration mask must exclude the separation corridor, four raid approaches,
water-crossing channels, protected locations, important vegetation, and all ground
outside explicitly proposed pads and roads. A sampled point outside the mask is
observation-only even when its height differs from the proposed grade.

### A4. Cut/fill computation and preview

Fit proposed grades per zone rather than flattening the whole campus. Building pads
use a locally selected stable plane; roads use a slope-limited centerline profile;
the arena floor uses one plane derived from the median of its interior samples.
For each allowed grid point compute `delta = proposed height - original height`:

- fill: `delta > +0.10 m`;
- cut: `delta < -0.10 m`;
- retain: `-0.10 m ≤ delta ≤ +0.10 m`.

Report cut area, fill area, maximum cut, maximum fill, estimated cut/fill volume,
maximum post-grade slope, and every threshold violation by zone. The preview must
show original terrain and proposed grade together. It performs no hoe, pickaxe,
terrain-modifier, vegetation removal, or structure placement operation.

### A5. Tagged temporary markers

All preview objects use a unique survey tag such as
`LWC_CAMPUS_SURVEY_<worldUid>_<timestamp>` plus a role tag. Required roles are:

- `CORNER_SW`, `CORNER_NW`, `CORNER_NE`, `CORNER_SE`;
- `SETTLEMENT_CENTER_M0`, `COMBAT_WARD_CENTER_CW0`;
- `SETTLEMENT_BOUNDARY_N/E/S/W`, `RELEASE_BEACON`;
- `CORRIDOR_WEST`, `CORRIDOR_EAST`;
- four arena safety corners and four combat-floor corners;
- `RAID_APPROACH_N/E/S/W` centerlines;
- `CUT`, `FILL`, and `RETAIN` preview samples or decimated visualization markers.

Markers must be nonpersistent where possible, nonblocking, noninteractable, excluded
from base/raid structure counts, and removable by exact survey tag. Cleanup verifies
zero tagged objects remain. Never delete by prefab type, radius alone, or an unscoped
name shared with real world pieces.

### A6. Survey artifact and approval gate

Save a manifest containing the four raw captures, computed frame, formulas/version,
all samples, alteration mask, proposed grades, cut/fill results, marker tag, screenshots,
game/mod versions, and SHA-256 of exported data. A second calculation pass must
reproduce M0, CW0, axes, and deltas within numeric tolerance.

Brian approves the corner frame, automatically computed centers, native-module
survey plan, and cut/fill preview before any terrain-changing operation becomes
available. Rejection or cancellation removes only the tagged temporary markers and
leaves the world unchanged.
