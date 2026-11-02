# Workbench to Living World NPC adapter architecture audit

Date: 2026-11-02
Scope: read-only architecture audit plus this note. No runtime source, Workbench source, installed profile, configuration, DLL, or Git history was changed.

## Decision summary

The frozen Workbench can author source-bound appearance packages, but it cannot yet target or create a distinct NPC. The maintained Living World 1.0.x actor line already supplies the safest usable runtime foundation: network prefab construction, owner-gated persistence, durable presentation keys, loadout reapplication, leader binding, and behavior-domain arbitration. It does **not** yet supply the general definition registry, stable person/definition model, authored stats and skills, faction/relationship model, production spawn system, or exact-instance Workbench appearance binding needed to replace RRRCore/Souls.

The next correct seam is therefore an adapter, not a second actor line:

1. Workbench exports an immutable appearance package and source/rig validation data.
2. A framework-owned `NpcDefinition` references that appearance by ID and revision alongside separate gameplay profiles.
3. The maintained 1.0.x factory creates and owns the network actor.
4. An exact-instance appearance binder applies the package only to that actor and returns a restore/dispose handle.
5. The actor persists `DefinitionId`, `PersonId`, `AppearanceId`, and `AppearanceRevision` before promotion into the world.

Appearance must never infer class, role, faction, sex, biome, equipment, stats, or AI. Guard remains Guard, Archer remains Archer, and Scout remains Scout. Warden and Huntress are distinct future authored identities; they are not aliases or migrations.

## Evidence baseline

| Area | Inspected commit | Exact inspected records |
| --- | --- | --- |
| Frozen Workbench production baseline | `LWCModelSkinStudio` `47fa780` (private 0.5.4 packaged/tested baseline recorded by project state); current documentation worktree observed at `ff9acde` | `docs/project/STATE.md`; `docs/project/NPC-APPEARANCE-CONTRACT.md`; `exchange/asset-pipeline/2026-11-01-next-phase-npc-identity-adapter.md`; `mod_export/TargetScope.cs`; `mod_export/SkinRuntime.cs`; `newworkbench/recipes.py` |
| Maintained Living World actor runtime | `valheim-mods-awareness-a1` ref `1ec0f182` (`work/native-hair-attachment-1.0.32.29`) | `LivingWorldCORE/npc/src/CorePlugin.cs`; `Identity/NpcFactory.cs`; `Identity/NpcAppearance.cs`; `Identity/NpcIdentityRecord.cs`; `Identity/NpcLeaderBinding.cs`; `Kernel/NpcBody.cs`; `Kernel/NpcKernel.cs`; `Kernel/NpcBehavior.cs` |
| Shared awareness / Formidable shadow | `valheim-mods-awareness-a1` `7ff725eefc` | `LivingWorldCORE/npc/src/Kernel/AwarenessGroup.cs`; `LivingWorldCompanionSystems/FormidableAI/README.md`; `CombatContracts.cs`; `CombatFrame.cs`; `CharacterCombatProfile.cs` |
| Transitional RRR/Souls compatibility | `valheim-mods-awareness-a1` `15d49d318` | `compatibility/RRRCore-3.1.5-Valheim-1.0.21/Plugin.cs`; corresponding README; `LivingWorldEnemyFamilies` definitions and dependency/readme records |
| Older identity research only | `valheim-mods-awareness-a1` `37d7117` | `LivingWorldHumanNPC/src/HumanProfile.cs`; `HumanIdentity.cs`; `HumanMigrationPolicy.cs` |
| Engine-free encounter contracts | `valheim-mods-vikingai-audit` `dfe52706` | `EncounterPopulation`; `EncounterLoadouts`; `EncounterAllegiance`; `Discovery`; `CampLifecycle/ADAPTER-HANDOFF.md`; `LiveCoreAdapter/README.md`; `RuntimeIntegration/INTEGRATION-CONTRACT-1.0.x.md`; `FormidableNPCFramework/README.md`; `LivingWorldCORE/PROJECT-BOUNDARIES.md` |

No claim below upgrades an engine-free test, source inspection, compile check, or design document into live-game proof.

## Capability and reuse matrix

| Capability | Implemented and runtime-wired now | Implemented/tested offline or design-only | Remaining adapter/replacement blocker |
| --- | --- | --- | --- |
| Workbench appearance authoring | Workbench 0.5.4 authors textures, material parameters, height, bone edits, mesh deltas, equipment slots, and optional animation data for its supported player/shared-material scopes. | The NPC appearance contract specifies `validateAppearance`, `buildAppearance`, and `bindNpcAppearance` separation. | Runtime target scopes are only `local-player`, exact player character, and shared material. There is no exact NPC instance scope, NPC validation context, ownership handle, or reload binding. |
| Runtime actor construction | `NpcFactory.Create` clones the current Player prefab, removes player-only components, adds `MonsterAI`, `NpcAppearance`, leader binding, wet status, and `NpcKernel`, and validates one humanoid/AI/network graph. Prefabs are persistent/prioritized network actors. | `LiveCoreAdapter` has a compile-ready, server-only, allow-listed one-person creation seam against older 1.0.x baselines. | The live factory accepts hard-coded prefab ID, default items, and role rather than a validated data definition. There is no public stable registration API or definition collision/version policy. |
| Stable identity and persistence | `NpcAppearance` stores name, sex, hair, beard, role, and a completion marker in the owned ZDO; `NpcLeaderBinding` persists leader and party generation. Reload repairs the saved presentation instead of rerolling. | Old 0.3.x `HumanProfile` demonstrates strict versioned serialization, stored outcomes, corruption refusal, and trusted-new-spawn gating. Encounter contracts define stable `PersonId` and aggregate IDs with deterministic persistence tests. | 1.0.x has no stable `PersonId`, `DefinitionId`, appearance package ID/revision, origin, history, faction profile, stat profile, or atomic aggregate/CAS store. The old 0.3.x actor line must not be revived or mixed into 1.0.x. |
| Equipment and loadout | `NpcBody.Outfit` validates a named vanilla kit, replaces inventory, equips armor, stows weapons, and persists kit/quality. `ReapplyKitOnce` restores the kit because non-player inventory is not saved. Bomb kind/reserve also persist. | `EncounterLoadouts` has bounded personal gear, finite reserves, progression gates, stable person linkage, serialization, and engine-free tests. `EncounterPopulation` has abstract primary/fallback/special-move packages. | Live kits are only fixed `iron` and `plains` arrays. No data-driven personal inventory, optional asset adapter, exact attachment/animation verification, finite projectile recovery, loot/corpse transaction, or framework loadout API exists. |
| Stats, skills, food, training | Health and several movement/AI numbers are hard-coded in `NpcFactory`; native items supply damage and armor. | Population and loadout policies carry biome/threat/role data, but not live player-style skill execution. | `Skills` is deliberately removed from the actor. There is no authored/persisted health-stamina-eitr-food-skill-training profile or lawful advancement transaction. Appearance cannot be used to fill this gap. |
| Faction and relationship | The current companion factory hard-codes Valheim faction `Players` and group `LivingWorldCompanions`; hostility can be queried through the game's `BaseAI.IsEnemy`. | `EncounterAllegiance` defines explicit Living World allegiance, witnessed relationships, first-contact evidence, and persistence with engine-free tests. Awareness exposes `IAwarenessGroupAdapter` for tactical membership/hostility queries. | Tactical group identity is not a faction system. The live actor has no authored faction/relationship profile, other-mod identity adapter, synchronized hostility table, or relationship persistence wired to actor creation. |
| Combat and behavior | `NpcKernel` supplies one frame/body/selector and priority arbitration across Movement, Hands, Look, and Posture. `NpcBehavior` is a clean module boundary; native writes stay behind `NpcBody`. Voice, movement, and ownership gates protect this line. | FormidableAI contains decision contracts, frames, arbitrator, defense/class proposals, trace, and a read-only shadow observer. Encounter combat-kit routing is engine-free and preserves native ownership. | FormidableAI is not the live full combat owner. Do not transfer domains or change locked combat/voice/movement behavior as part of the appearance adapter. The `7ff725e` profile mapping Guard→Warden and Archer→Huntress is invalid and must not be reused; corrected later work is still WIP, not a live identity system. |
| Awareness and tactical groups | Shared awareness can read one adapter-provided group and delegates hostility to game facts. | Its knowledge book and future camp/hostile adapters are designs. | It is not an identity registry, faction authority, spawn service, or Workbench adapter. Knowledge sources remain unwired. |
| Natural spawn, camps, encounters, raids | Current `lwparty` is a fixed debug Guard+Archer transaction. It can instantiate registered prefabs and safely remove loaded/unloaded records. | Discovery, population, camp lifecycle, activation, and runtime integration define stable sites/people, claims, server authority, atomic transitions, and away-safety requirements with engine-free tests. | No production definition registry, world/camp spawner, spawn caps/conditions, server-resident zone owner, raid/event API, one-person recruitment transaction, or dedicated-server proof is live. `lwparty` and Tooler fixtures must not become campaign acquisition. |
| Transitional hostile content | Six Souls/RRR prefab definitions and repaired compatibility provide a private benchmark. Enemy-family JSON records explicit health, damage modifiers, gear, attacks, AI values, faction, drops, and spawn conditions. Custom Raids/Spawn That currently supply external world-event/spawn services. | Those definition fields are a strong migration checklist and test corpus. | RRR remains authoritative for its definitions; compatibility explicitly does not own stats, equipment, faction, AI, spawning, or drops. External asset IDs and optional packs need validated adapters/fallbacks. |
| Framework replacement | Product boundary and functional release gates are documented in `FormidableNPCFramework/README.md` and `LivingWorldCORE/PROJECT-BOUNDARIES.md`. | The directory currently contains the boundary/readme, not a framework runtime. | RRRCore cannot be removed until a standalone BepInEx/Jotunn runtime registers, persists, equips, factions, animates, attacks, spawns, synchronizes, diagnoses, and migrates equivalent actors with RRR absent. |

## Concrete adapter boundary

### 1. Workbench output stays appearance-only

The Workbench package should remain immutable, source-bound authoring data:

- `AppearanceId` (UUID), schema, and revision;
- base source and hashes/fingerprints;
- supported skeleton/renderer contract;
- body/material/geometry/bone/height edits;
- presentation slots such as hair, beard, clothing, and visible equipment references;
- required and optional asset sources plus fallbacks;
- a deterministic visual hash.

Its existing opaque recipe `identity` must not silently become a gameplay person or class. Workbench must not assign stats, faction, AI, biome, or spawn rules.

### 2. Framework definition owns gameplay composition

A future validated definition should compose independent references, for example:

```text
NpcDefinition
  DefinitionId + SchemaVersion
  ActorKind / authored character type
  AppearanceId + AppearanceRevision
  IdentityPolicyId
  StatProfileId
  LoadoutProfileId
  FactionProfileId
  BehaviorProfileId
  SpawnProfileId
  LootProfileId
  optional Voice/Status/Environment profile IDs
```

`PersonId` is an instance identity created by the world/encounter transaction, not by the appearance recipe and not from prefab, class, role, name, sex, biome, player ID, or scene instance.

### 3. Maintained 1.0.x actor line remains authoritative

Parameterize the existing factory behind a new validated registration service rather than creating another actor implementation. Preserve its one-network-root validation, owner checks, persistent ZNetView, current component graph, kernel arbitration, and safe record cleanup. Registration must reject unknown schema, duplicate IDs, changed definitions under an existing immutable version, missing required assets, and an appearance whose rig/source contract does not match the factory source.

### 4. Bind appearance to one exact actor instance

After the network actor exists and is owned, call an adapter conceptually equivalent to:

```text
ValidateAppearance(package, actorSource) -> report
BuildAppearance(package, assetResolver) -> immutable built appearance
BindNpcAppearance(actorInstance, builtAppearance) -> restore/dispose handle
```

The binder must select renderers beneath that exact actor, clone mutable materials/meshes per required ownership policy, never use player ID, never rely on a global shared-material match, and never scan every scene actor as its primary identity mechanism. Unload/despawn must dispose or restore only resources owned by that binding. Reload must use the persisted IDs/revision and refuse an incompatible or missing package rather than rerolling another look.

### 5. Persist before promotion

On the server/owner, atomically commit at minimum:

- `PersonId`;
- `DefinitionId` and definition version;
- `AppearanceId` and appearance revision;
- immutable generated identity outcomes;
- lifecycle revision and authoritative spawn claim.

Then apply loadout/stats/faction and expose the actor to normal lifecycle. Per-field ZDO writes are insufficient for a multi-record campaign transition unless guarded by the aggregate revision/CAS contract already described by RuntimeIntegration.

## Reuse without violating current locks

1. Reuse `NpcFactory`'s actor graph and validation, but supply validated definition data instead of adding more hard-coded class branches.
2. Reuse `NpcAppearance`'s owner-only create-once and reload-repair pattern, but add an explicit authored appearance input rather than deriving one from role or rerolling.
3. Reuse `NpcBody`'s recorded-kit reapplication pattern for a future personal loadout adapter; do not bypass the single native writer boundary.
4. Preserve `NpcKernel`/`NpcBehavior` domain arbitration unchanged while the appearance/identity vertical slice is proven.
5. Reuse `IAwarenessGroupAdapter` only for tactical membership and hostility facts. Do not turn it into a duplicate faction registry.
6. Reuse the old `HumanProfile` serialization and trusted-spawn ideas only as patterns. Do not restore the superseded 0.3.x prefab/DLL line.
7. Use the six RRR/Souls definitions and Living World enemy-family JSON as field inventories and acceptance fixtures, not as runtime source or identity authority.
8. Reuse engine-free population/loadout/allegiance/camp contracts only after a maintained 1.0.x server adapter passes ownership, persistence, reconnect, and dedicated-server tests.

## Blockers before removing Souls/RRR/MobAI-era dependencies

1. **Exact NPC appearance binding:** per-instance target, source/rig validation, owned material/mesh lifetime, restore/dispose, save/reload proof, and multiplayer asset availability.
2. **Definition registry:** versioned data loading, collision/refusal rules, deterministic network registration, public extension seam, and diagnostics.
3. **Permanent person model:** stable `PersonId`, immutable identity outcomes, origin/history, atomic aggregate persistence, spawn claims, death/unload distinction, and migration policy.
4. **Player-like capability profiles:** explicit health/stamina/eitr/food/skills/training and progression, independently authored from appearance.
5. **Loadout and attack services:** personal inventory, attachment/stowing, optional asset adapters, melee/ranged/projectile/thrown/AOE/status profiles, ammo/debit/recovery, loot, and corpse ownership.
6. **Faction and relationship authority:** authored faction IDs, game-faction mapping, other-mod adapters, witnessed relationship changes, and synchronized hostility without inference from visuals or prefab names.
7. **World spawning:** biome/site conditions, finite caps and budgets, camp lifecycle, raid/event integration, zone residency, server ownership, reconnect/unload, and away-safety proof.
8. **Combat replacement proof:** current Formidable work is shadow/prototype, not a full live combat brain. Any future transfer must preserve the locked ownership, movement, and voice gates and be accepted independently of this adapter.
9. **Asset and multiplayer contract:** required/optional source declaration, fallbacks, hashes, server/client compatibility and refusal behavior, without shipping third-party private assets.
10. **One-at-a-time migration:** migrate and prove one maintained companion and one hostile actor with RRR absent before expanding to all six benchmark identities or deleting the transitional stack.

The only direct MobAI reference found in this audited tree is legacy `PopVillageWorkers/WorkerBridge.cs`; it is not a current framework seam. Its existence reinforces that village-worker migration needs a separately owned adapter rather than carrying the old dependency into the new actor framework.

## Safest next milestone

Build, but do not install by default, one command-only laboratory vertical slice against the maintained 1.0.x actor line:

- one new versioned framework definition using an existing trusted LWC prefab source;
- one Workbench-authored appearance package bound to an exact spawned instance;
- explicit `DefinitionId`, `PersonId`, `AppearanceId`, and revision persistence;
- current hard-coded safe stats/loadout/faction and current behavior left unchanged for this slice;
- default-off command restricted to a local throwaway world;
- no Tooler identity promoted into campaign records.

Acceptance requires:

1. local player plus two actors from the same source coexist, and only the intended actor receives each appearance;
2. hair, beard, materials, body edits, equipment presentation, and height remain actor-local;
3. save/reload and zone unload/reload preserve the same person and exact appearance revision;
4. changing/stowing runtime gear does not corrupt the authored body appearance or another actor;
5. missing, mismatched, duplicate, or altered packages fail closed with useful diagnostics;
6. network ownership, native-write, combat-voice, and movement gates remain unchanged;
7. no Guard/Archer/Scout identity is renamed, aliased, inferred, or migrated;
8. RRR/Souls remain installed as benchmark dependencies until a later standalone hostile migration proves equivalent behavior with them absent.

After that slice passes, add definition-driven loadout, then stats, faction, and server spawn as separate gates. Do not combine appearance binding with a combat rewrite or roster redesign; doing so would make failures impossible to attribute and would repeat the unauthorized class-mapping error this contract now forbids.
