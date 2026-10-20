# Living World settlement and combat lab build specification

Status: design only. This document authorizes no world edits, mod installation, or
companion/combat-code changes.

## Charter and coordinate system

The maypole is the settlement authority marker and local origin `(0,0,0)`. Face
east from the maypole to define `+X`; north is `+Z`. Record the world seed, world
name, maypole map coordinates, ground height, build version, and screenshots before
placing or removing anything.

- Resident boundary: 64 m horizontal radius from the maypole.
- Working settlement: 0–48 m. Residents may own beds, work, eat, socialize, store
  materials, and patrol here.
- Transition ring: 48–64 m. Gates, muster points, delivery handoff, and release
  preparation belong here; no resident bed or irreplaceable storage does.
- Release destination: east gate at `(64,0,0)`, followed by a marked road to a
  release beacon at `(96,0,0)`. A released resident is outside settlement authority
  only after reaching the beacon. The route must not pass through the arena.
- Contamination rule: settlement tests use only actors assigned to the settlement.
  Arena actors never cross the 96 m release beacon or settlement perimeter.

Use numbered signs or noninteractive markers for every test station. Measure from
the maypole rather than relying on visual estimates.

## Preferred site and alternatives

### Purpose-built settlement — preferred

Choose level Meadow or cleared Black Forest terrain with a shallow shoreline or
stream along one edge. Reserve a minimum rectangle of 240 × 180 m: settlement on
the west, separation corridor in the middle, arena on the east. Avoid boss sites,
important forests, farms, portals, and permanent player storage.

This option gives repeatable geometry, clean ownership, measurable path widths,
and predictable reset behavior.

### Complete large Draugr village conversion

Use only a complete village whose occupied footprint fits inside the 48 m working
radius. Before conversion:

1. Duplicate the world save and record its hash/date; never experiment on the only copy.
2. Photograph every building and mark the untouched village perimeter.
3. Inventory spawners, body piles, loot, doors, beds, fires, workstations, terrain,
   and vegetation before clearing anything.
4. Establish a new maypole origin near the village center and survey every retained
   structure into the same coordinate plan.
5. Put new test fixtures in reversible freestanding annexes. Do not demolish unique
   buildings merely to make the grid exact.

If the village cannot provide the required 2 m and 1 m navigation lanes, dead end,
ramp, water route, and safe resource plot, reject it and use the purpose-built plan.

## Measured settlement layout

All dimensions are clear interior/path dimensions, not outside wall dimensions.

| Zone | Center from maypole | Size | Required contents |
|---|---:|---:|---|
| Charter green | `(0,0,0)` | 16 m diameter | Maypole, four approach paths, no clutter within 4 m |
| Dwelling A | `(-18,0,+14)` | 8 × 10 m | Two owned covered beds, two chairs, table, fire-safe hearth |
| Dwelling B | `(-18,0,-14)` | 8 × 10 m | Two owned covered beds, bench, table, campfire |
| Long hall | `(+10,0,+18)` | 12 × 20 m | Paired 2 m hall doors, table, six seats, covered fire |
| Craft court | `(+12,0,-18)` | 18 × 18 m | Covered workbench and forge; roof clearance and fire separation |
| Kitchen | `(+28,0,-8)` | 10 × 12 m | Cauldron, cooking spit, food racks, table, covered fire |
| Storehouse | `(-32,0,0)` | 10 × 12 m | Labeled storage, inbound/outbound material points |
| Navigation course | `(+30,0,+18)` | 18 × 22 m | Doors, paired doors, lanes, dead end, ramp, incline |
| Guard circuit | radius 38–46 m | 250–290 m loop | Four guard posts at cardinal points and clear sight breaks |
| Resource plot | `(-42,0,-32)` | 30 × 30 m | Sacrificial trees, log drop, stump area, material delivery |
| East gate/muster | `(+58,0,0)` | 10 × 12 m | Gate, guard positions, release queue, no beds |

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
`(-58,0,0)`. Release verification begins at the east muster point and ends at the
96 m beacon. The settlement reset point is a protected sign/console at `(0,0,-6)`;
it is a procedural checkpoint, not a place to store valuable items.

For every run record: world copy ID, actor roster, time/weather, door states, fire
states, container counts, assigned beds, starting positions, and enabled telemetry
schema. Reset actors, loose items, doors, workstation queues, fires, containers,
felled trees, and damaged test pieces. Restore the world copy if terrain or permanent
structures changed; do not attempt an improvised partial reset.

## Adjacent combat arena

Place the arena center 160 m east of the maypole at local `(160,0,0)`. Maintain an
empty 48 m-wide separation corridor between the 64 m settlement boundary and the
arena's west safety perimeter. No settlement bed, workstation, tame, storage, or
patrol route may enter that corridor or arena.

- Outer safety perimeter: 96 × 96 m, clearly marked.
- Combat floor: 64 × 64 m, flat, replaceable surface.
- Central objective pad: 8 m diameter for defend/destroy tests.
- Team A and B starts: `(-24,0,0)` and `(+24,0,0)` relative to arena center.
- Neutral reset/spawn vestibules: 8 × 12 m behind each team start, with closed gates.
- Ranged lines: marks at 8, 16, 24, and 32 m from the center.
- Cover: four symmetric 3 × 6 m barriers; no settlement-like furniture.
- Terrain strip: one 3 m-wide incline, one shallow trench, one short bridge, all
  mirrored so faction comparisons remain fair.
- Observation towers: west and south, outside the 64 m combat floor.

Controlled faction fights use only explicit team spawns and a written cleanup pass.
Natural raid tests are separate runs: use a minimal, arena-only raid trigger outpost
near the arena perimeter, verify the event circle does not intersect the 64 m
settlement boundary, and start from a clean world copy. Never run a natural raid and
a settlement-behavior trial simultaneously. After each fight confirm zero surviving
combatants, projectiles, drops, temporary effects, or alerted settlement residents
before returning to settlement testing.

## Staged construction checklist

### Stage 0 — preserve and survey

- [ ] Valheim closed; source world duplicated; backup path and hash recorded.
- [ ] Seed, map coordinates, terrain, nearby locations, and baseline screenshots recorded.
- [ ] Purpose-built or Draugr-conversion option explicitly selected.
- [ ] Maypole origin, north/east axes, 64 m boundary, release route, and arena center staked.

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

- [ ] 96 m perimeter and 64 m floor measured from the arena center.
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
