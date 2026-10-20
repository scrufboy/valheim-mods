# Living World C.O.R.E. project boundaries

Living World C.O.R.E. is the story, character, settlement, faction, and encounter
system for **Characters, Origins, Roles & Encounters**. It uses the independently
implemented **Formidable NPC Framework** for the reusable NPC creation/runtime
services that the current lab stack temporarily obtains from RRRCore.

The framework can ship separately for community creators or as a declared Living
World dependency. Living World C.O.R.E. owns the lore and the friendly, resident,
hero, and enemy content built with it; the framework does not require that content.

## Product map

| Deliverable | Responsibility | Installation boundary |
| --- | --- | --- |
| **Formidable NPC Framework** | NPC/prefab registration, network-safe identity and persistence, factions, equipment and visual assembly, status/effect services, attack profiles, spawn/encounter contracts, configuration, and extension APIs | Standalone MIT framework with BepInEx and Jotunn as its intended dependencies; working project home: `FormidableNPCFramework/` |
| **Living World C.O.R.E.** | Lore, origins, roles, factions, friendly and hostile character families, villages, quests, world change, and encounters | Story/game system built on the framework; may distribute its content as several optional packages |
| **Living World Formidable NPCs** | Human, dwarf, elven, and future hero/companion actors; resident states; player-parity presentation; native-combat handoff; recovery; party cohesion; inventory; equipment decisions; and interaction | Living World actor package on the framework; currently developed in `LivingWorldHumanNPC/` |
| **Living World Enemy Families** | Living World hostile species, regional variants, factions, encounter definitions, clothing, glow/VFX, audio, and special attacks | Living World content packages on the framework; their capabilities remain available to friendly/hero actors where a role grants them |
| **Living World Village Systems** | Villagers, dwellings, work, settlement growth, stewardship, schedules, and community simulation | Living World systems on the framework and Formidable NPC contracts |
| **Living World Tooler** | Development, inspection, spawn, encounter, environment, player, and NPC controls | Optional tool; cannot be a runtime dependency of released NPC/content packages |
| **Living World Telemetry** | Evidence collection and scoring for development and acceptance tests | Development-only; removable without changing NPC behavior |
| **Compatibility and migration** | Temporary lab repairs plus future import/migration adapters | Separate from the core; removable when consumers have migrated |

## Current dependency truth

The repaired RRRCore, RunicPack, and Souls of the Land installation is a private
lab and benchmark stack. It supplies working reference behavior and opponents
while the Formidable NPC Framework is incomplete. It is **not** the implementation
of the framework or Living World Core, and its binaries or private assets are not
Living World deliverables.

The replacement target is capability compatibility through independently written
Formidable NPC Framework code:

1. Record the observable contract required by existing NPC and content definitions.
2. Implement that contract behind framework-owned interfaces and tests.
3. Provide optional migration readers/adapters where existing public data formats
   can be supported without retaining the old runtime.
4. Move Living World NPCs and content to the framework one capability at a time.
5. Remove RRRCore from the release dependency graph only after parity gates pass.

Local binary repairs remain under `compatibility/`; they must never become source
dependencies or be packaged as the Formidable NPC Framework or Living World Core.

## Framework capability gates

The Formidable NPC Framework is not an RRRCore replacement until all of these work
without an RRRCore assembly loaded:

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

Capability parity means that a creator can build and run the same categories of
NPC content through the Formidable NPC Framework, with clearer contracts and
additional supported behavior.

## Formidable NPC boundary

`LivingWorldHumanNPC/` is the current implementation home for **Living World
Formidable NPCs**. This package will consume the standalone framework. Formidable
describes the complete actor, not only combat:

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

## Souls benchmark and regional peoples track

Souls of the Land supplies private benchmark opponents, including its established
Forest Child variants and their poison and falling-ice attacks. Those characters
remain part of the benchmark pack. Living World does not rename them, redistribute
them, or extend them with more child enemies.

Living World's regional additions are distinct adult humanoid peoples:

- **Rimebound:** stocky northern/Mountain or Deep North humanoids with wolf and
  fur layers, cold-region glow and effects, heavy melee roles, and a telegraphed
  falling-ice, snow/ice bombardment, or frost-blast special built through the
  common attack-profile contract.
- **Emberworn:** adult Ashlands humanoids with ragged or flowing heat-region
  clothing, ember/fire presentation, melee and ranged roles, and a telegraphed
  fire projectile, burning impact, thrown explosive, or bounded fire-area special.
- **Dwarf folk:** reduced-height, broad, heavily bearded male-presenting humanoids
  with axe, hammer, shield, smithing, mining, settlement, hero, and hostile roles.
  They are a culture and actor family, not a renamed vanilla Dvergr prefab.

Their appearance, factions, names, story roles, encounter placement, decision
profiles, and balance are Living World content. Registration, visual assembly,
equipment, projectile, status, thrown-weapon, and area-effect capabilities belong
in the reusable framework so friendly, hero, resident, pillager, and enemy roles
can all use them without duplicating runtime code.

MadHatter and RunicPack content may be consumed through optional adapters when the
corresponding pack is installed. Resized vanilla objects should be cloned from the
player's installed game at runtime. New Living World meshes, textures, materials,
sounds, and effects remain separately identifiable LWC assets.

No optional armor or clothing pack may become a hard dependency of the framework.
Each Living World content package must declare its required and optional sources
and provide a defined fallback where appropriate.

## Special-attack track

Thrown bombs and other area attacks are a first-class framework capability, not
custom logic scattered among individual NPC classes. The attack-profile contract
must be able to declare:

- held/stowed item and throw animation;
- projectile or placed-area prefab;
- range, aim, flight, fuse, radius, damage, stagger, status, and terrain rules;
- ally/self filtering and faction ownership;
- telegraph, glow, audio, impact, and lingering effects;
- inventory/ammunition policy and cooldown;
- tactical eligibility, including minimum range and crowd-density requirements.

The first acceptance scenario is a bounded, telegraphed vanilla-derived bomb/AOE
attack that damages legal hostiles without harming its owner or friendly faction.
Only after that common path passes should regional enemy or hero profiles receive
specialized versions.

## Acceptance sequence

1. Keep the repaired Souls/RRR stack only as the current benchmark and regression
   environment.
2. Finish current Formidable companion combat acceptance without changing the
   future framework boundary.
3. Inventory every RRRCore service actually consumed by our definitions and assign
   it a Formidable NPC Framework interface, owner, and test.
4. Implement a minimal standalone framework vertical slice: register one networked
   NPC, equip it, persist it, assign a faction, and execute melee, ranged, and
   thrown/AOE attacks without RRRCore.
5. Migrate one Formidable NPC and one hostile regional-person definition to the
   framework.
6. Run side-by-side parity and performance tests, then expand migration.
7. Declare replacement status only when the lab profile can disable RRRCore and all
   Living World acceptance scenarios still pass.
