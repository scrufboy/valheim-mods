# Living World C.O.R.E. project boundaries

Living World C.O.R.E. is the independently implemented foundation for
**Characters, Origins, Roles & Encounters**. Its long-term purpose is to provide
the community-facing NPC creation and runtime services that the current lab stack
temporarily obtains from RRRCore, while adding a cleaner capability model,
Formidable NPC behavior, and supported extension points.

## Product map

| Deliverable | Responsibility | Installation boundary |
| --- | --- | --- |
| **Living World Core** | NPC/prefab registration, network-safe identity and persistence, factions, equipment and visual assembly, status/effect services, attack profiles, spawn/encounter contracts, configuration, and extension APIs | Standalone foundation; must not require the Formidable NPC or enemy-content packages |
| **Living World Formidable NPCs** | Human actors, companion and resident states, player-parity presentation, native-combat handoff, recovery, party cohesion, inventory, equipment decisions, and interaction | Installs on Living World Core; currently developed in `LivingWorldHumanNPC/` |
| **Living World Enemy Families** | Optional hostile species, variants, factions, encounter definitions, clothing, glow/VFX, audio, and special attacks | Separate content packages on Living World Core; never required by the core runtime |
| **Living World Village Systems** | Residents, dwellings, work, settlement growth, stewardship, schedules, and community simulation | Optional systems on Living World Core and Formidable NPC contracts |
| **Living World Tooler** | Development, inspection, spawn, encounter, environment, player, and NPC controls | Optional tool; cannot be a runtime dependency of released NPC/content packages |
| **Living World Telemetry** | Evidence collection and scoring for development and acceptance tests | Development-only; removable without changing NPC behavior |
| **Compatibility and migration** | Temporary lab repairs plus future import/migration adapters | Separate from the core; removable when consumers have migrated |

## Current dependency truth

The repaired RRRCore, RunicPack, and Souls of the Land installation is a private
lab and benchmark stack. It supplies working reference behavior and opponents
while Living World Core is incomplete. It is **not** the implementation of Living
World Core and its binaries or private assets are not Living World deliverables.

The replacement target is capability compatibility through independently written
Living World code:

1. Record the observable contract required by existing NPC and content definitions.
2. Implement that contract behind Living World-owned interfaces and tests.
3. Provide optional migration readers/adapters where existing public data formats
   can be supported without retaining the old runtime.
4. Move Living World NPCs and content to the new services one capability at a time.
5. Remove RRRCore from the release dependency graph only after parity gates pass.

Local binary repairs remain under `compatibility/`; they must never become source
dependencies or be packaged as Living World Core.

## Core capability gates

Living World Core is not an RRRCore replacement until all of these work without an
RRRCore assembly loaded:

- deterministic prefab and network registration;
- data-driven NPC definitions and variants;
- stable identity, names, factions, hostility, ownership, and persistence;
- equipment, armor, attachment points, stowing, visual hashes, and material/color
  variation;
- animation, audio, status effects, wetness, environmental presentation, and
  interaction hooks;
- melee, ranged, projectile, thrown, area-of-effect, status-delivery, and cooldown
  attack profiles;
- spawn, encounter, raid, and world-integration contracts;
- synchronized configuration and server-authoritative state;
- documented extension APIs and useful diagnostics;
- migration and regression tests against the definitions used by the lab stack.

Capability parity does not mean copying an old binary. It means that a creator can
build and run the same categories of NPC content through Living World Core, with
clearer contracts and additional supported behavior.

## Formidable NPC boundary

`LivingWorldHumanNPC/` is the current implementation home for **Living World
Formidable NPCs**. Formidable describes the complete actor, not only combat:

- During committed combat, native Valheim `MonsterAI` owns targeting, movement,
  facing, weapon choice, blocking, dodging/circulation, pursuit, and attacks. The
  companion layer may authorize targets, protect cohesion, heal, and recognize
  encounter completion, but it may not run a competing combat controller.
- Outside committed combat, explicit companion, traversal, guard, hold, stealth,
  recovery, town, work, comfort, bed, environmental, and social states arbitrate
  behavior without bleeding into combat.
- Formidable actors must remain capable when telemetry and the Tooler are removed.
- Friendly and hostile humanoids share core actor capabilities; faction, role,
  loadout, initiative, and content profiles produce the differences.

## Enemy-family content track

Enemy variants are optional packages built on Core services, not special cases
inside Core. The planned child-family expansion is:

- **Mountain/North child:** winter-white treatment, larger antlers, fur-layered
  clothing, cold-region glow/VFX, and biome-appropriate attacks.
- **Desert child:** ragged and flowing clothing, heat/desert palette, distinct
  glow/VFX, and biome-appropriate attacks.

MadHatter and RunicPack content may be consumed through optional adapters when the
corresponding pack is installed. Resized vanilla objects should be cloned from the
player's installed game at runtime. New Living World meshes, textures, materials,
sounds, and effects remain separately identifiable LWC assets.

No optional armor or clothing pack may become a hard dependency of Living World
Core. Each content package must declare its own required and optional sources and
provide a defined fallback where appropriate.

## Special-attack track

Thrown bombs and other area attacks are a first-class Core capability, not custom
logic scattered among individual NPC classes. The attack-profile contract must be
able to declare:

- held/stowed item and throw animation;
- projectile or placed-area prefab;
- range, aim, flight, fuse, radius, damage, stagger, status, and terrain rules;
- ally/self filtering and faction ownership;
- telegraph, glow, audio, impact, and lingering effects;
- inventory/ammunition policy and cooldown;
- tactical eligibility, including minimum range and crowd-density requirements.

The first acceptance scenario is a bounded, telegraphed vanilla-derived bomb/AOE
attack that damages legal hostiles without harming its owner or friendly faction.
Only after that common path passes should child variants receive specialized
versions.

## Acceptance sequence

1. Keep the repaired Souls/RRR stack only as the current benchmark and regression
   environment.
2. Finish current Formidable companion combat acceptance without changing the
   future Core boundary.
3. Inventory every RRRCore service actually consumed by our definitions and assign
   it a Living World interface, owner, and test.
4. Implement a minimal standalone Living World Core vertical slice: register one
   networked NPC, equip it, persist it, assign a faction, and execute melee, ranged,
   and thrown/AOE attacks without RRRCore.
5. Migrate one Formidable NPC and one hostile child definition to the new core.
6. Run side-by-side parity and performance tests, then expand migration.
7. Declare replacement status only when the lab profile can disable RRRCore and all
   Living World acceptance scenarios still pass.

