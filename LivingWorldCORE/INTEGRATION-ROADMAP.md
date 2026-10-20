# Living World C.O.R.E. integration roadmap

This roadmap records the order of work. It does not declare an untested feature
complete and does not authorize a new candidate to overwrite an accepted build.

## Product shape

Living World C.O.R.E. is one product family with explicit modules rather than one
monolithic actor script:

| Module | Owns | Must not own |
| --- | --- | --- |
| Formidable NPC Framework | Registration, identity, networking, factions, equipment, animation/effect hooks, attacks, spawn and extension contracts | Living World story, companion orders, village schedules |
| Formidable Actors | Friendly, hero, resident and hostile actor bodies; native-combat handoff; recovery and player-parity embodiment | Settlement construction, raid tooling, telemetry scoring |
| Companion Systems | Follow, Hold, formation leases, traversal, guard/readiness, regroup and explicit party directives | Native targeting, attack cadence, resident jobs |
| Combat | Authorized-hostile boundary plus native `MonsterAI` ownership of movement, facing, blocking, weapon execution and attacks | Torches, formation correction, leader gaze, work, comfort or town schedules |
| Settlement Systems | Maypole/charter authority, territory, dwellings, beds, jobs, storage, schedules and resident navigation | Party formation or combat decisions |
| Enemy Families | Living World factions, roles, loadouts, encounter profiles, regional presentation and special attacks | Framework internals or third-party binary ownership |
| Reclamation | Location discovery, clearance, settlement chartering, restoration plans and population arrival | Core combat behavior or global world editing |
| Tooler and Telemetry | Explicit development controls, evidence collection and acceptance reporting | Released NPC behavior or hidden runtime dependencies |

Every actor has one persistent identity and exactly one exclusive action owner.
Shared identity, inventory, equipment, health and status survive state changes;
movement, facing, hands, attacks and animation commands do not have competing
owners.

## Gate 1 — current companion combat acceptance

Do not begin combat rewrites while this gate is being evaluated. Record the exact
installed build and preserve its binary before each session.

Required scenarios, in order:

1. Natural travel and ambient companion observation.
2. One hostile, then a small controlled Greydwarf group.
3. A full vanilla Greydwarf raid/spawn cycle in open terrain.
4. An Elder-class encounter only after the Greydwarf gate passes.

For the raid gate, two companions must remain present, acquire genuine threats,
use their prepared roles, avoid idle/torch/formation bleed-through, survive, clear
the remaining authorized encounter, verify the area, and regroup. Telemetry must
attribute damage and state ownership; survival alone is not a pass.

The Elder-class test measures survivability, target execution, pursuit bounds,
recovery and regroup. It must not be used to patch boss-specific decisions into
the generic combat controller.

Passing behavior is frozen with a versioned binary, source commit, configuration,
log, telemetry schema and concise acceptance record before later modules advance.

## Gate 2 — live site survey and physical laboratory

The altered live terrain is authoritative. The world seed identifies the world and
original generated locations, but it cannot reconstruct player terrain edits.

The Tooler survey records four walked corners, ground and water elevations, then
computes the site center, axes, dimensions and elevation range. A bounded grid
survey produces slope and cut/fill information. Preview markers identify:

- the maypole-centered settlement laboratory at the west side of the campus;
- the resident boundary and transition ring;
- the separation corridor and open raid approaches;
- the Combat Ward and arena center at the east side;
- arena bounds, team starts and observation positions.

Survey and preview are non-destructive. Terrain application is a separately
confirmed operation performed only against a recorded world backup while offline
and without peers.

## Gate 3 — proven settlement geometry

Use vanilla `WoodVillage1` / Draugr-village modules as navigation references:
great hall, longhouse, houses, tower, doors, roads and local-spawner rooms.
Populated Villages populates these native locations; it does not provide an
independent house-blueprint library.

Living World records measured footprints, thresholds, lanes, roof clearances,
origins and pathing hazards, then recreates modular Living World construction
blueprints. Reference geometry may guide compatibility; third-party packages,
compiled assets and identity are not redistributed.

The first physical campus contains covered beds, seating, hearths, workbench,
forge, cauldron, cooking, storage, doors, dead ends, ramps, water crossings, a
safe resource plot, observation points and a separately governed combat arena.

## Gate 4 — common actor consolidation

After companion combat is accepted, move capabilities behind stable framework and
actor interfaces rather than copying the temporary lab stack:

1. identity, persistence and network registration;
2. factions and target authorization;
3. equipment, stowing and armor-pack catalogs;
4. animation, audio, status and environmental presentation;
5. melee, ranged, thrown and area-effect attack profiles;
6. spawn, encounter and raid definitions;
7. diagnostics and migration readers.

The repaired RRR/Runic/Souls installation remains a benchmark until the same
categories of friendly and hostile actor work with RRRCore absent. Observable
numbers and behavior can be measured; third-party code, assets and package
identity do not become Living World source.

## Gate 5 — settlement residence and work

`Release` is a transition request, not a persistent state. It succeeds only when
the actor can adopt a settlement charter, assigned post, faction camp or other
explicit authority. Otherwise the actor remains on Hold.

Resident work begins with a bounded Woodcutter vertical slice: authorized resource
area, real tool use, finite production, inventory capacity, container delivery,
structure/tree protection, combat suspension, resume/return-home behavior,
persistence and telemetry. Repair, building, cooking, smithing, farming, hauling,
comfort and social schedules follow the same job contract.

Combat always cancels the work lease and receives exclusive actor control. Work
cannot poll, steer or decorate an actor while combat owns it.

## Gate 6 — regional enemies and faction encounters

Friendly, hero and hostile NPCs share the common actor foundation. Faction, role,
equipment, initiative, encounter placement and special-attack profiles create the
difference.

Migrate one friendly actor and one hostile regional adult to the framework first.
Then add pillager camps, Rimebound, Emberworn, dwarf folk and renamed original
Living World factions. Controlled two-on-two and wave trials precede world spawns.
Pillagers, regional enemies, vanilla creatures and other hostile factions may
oppose one another according to explicit faction rules.

## Gate 7 — village reclamation project

Reclamation is a side project built on accepted settlement contracts:

1. discover and survey a complete village location;
2. record its native structures and ownership boundary;
3. clear the hostile encounter through game evidence;
4. charter the location without destroying retained geometry;
5. plan and visibly perform bounded restoration;
6. assign dwellings, beds, facilities, storage and jobs;
7. introduce population, steward and defenses;
8. persist growth and recover cleanly after unload/reload.

The maypole or Living World charter identifies settlement authority. A Combat Ward
identifies a test arena or defended encounter area and is never interchangeable
with settlement ownership.

## Non-regression evidence

Every gate records its source commit, build identity, configuration, dependencies,
tests and live evidence. A later module may consume an accepted interface but may
not silently edit an earlier module's behavior contract. Tooler and telemetry can
be removed without changing the result.
