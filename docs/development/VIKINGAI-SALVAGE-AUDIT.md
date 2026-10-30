# VikingAI 0.1.1 salvage audit

Status: source-inspected and offline-tested candidate; not installed; not live-proven.

## Provenance and reproducibility

- Supplied ZIP SHA-256: `664D2777B7BD08A2743EB1972CC45BF7FAEE663D522C429E0582EC05AE400B7E`.
- Supplied DLL SHA-256: `31070FC243B263D0E69BFDFE72104F55F285FAEF2D17DC8240F45AB6EE36ACCA`.
- Supplied PDB SHA-256: `180CEA10664346E1AB35EBB2D3A9F4AB813B8FFA9D5C5F14E50FA445F61BF79E`.
- Public source inspected at `0c21c116caff937c508e276189c2b19eb4c00d67` (v0.1.1).
- The DLL is not reproducible from a clean public commit as delivered. Its informational version
  and PDB SourceLink identify `1819713a...`, while the binary contains later sensory-fallback
  members. The best-supported explanation is a dirty-tree build based on that older commit.
  Persistence and portal files are unchanged between the two commits, but whole-assembly source
  equivalence is **not proven**.
- Upstream has no test project or automated test files. Its project expects local game/Unity DLLs
  (`VikingAI.csproj:20-23,31-97`) and contains an optional post-build deployment target
  (`VikingAI.csproj:100-109`). Do not use that target for audit builds.
- Reproducible LWC gates are instead game-assembly-free: `VikingAISalvage/Run-Tests.ps1`.

No ZIP, DLL, PDB, game assembly, third-party binary, log, config, video, or personal path is stored
in this repository.

## Ownership and dependency map

```text
LWC runtime authority
  peaceful movement -> LWC sole movement writer / 1.0.32.8b spacing contract
  committed combat  -> native MonsterAI
  voice              -> locked combat-voice 1.0.32.6
  portal/instance    -> existing LWC identity-preserving transaction gate
        ^
        | requests/advice only; feature flags default off
VikingAI salvage adapter (proposed)
  pure sensory snapshots | affordance intents | loadout plan | virtual schedule projection
        ^
        | no Harmony patches, reflection, ownership claims, transforms, ZDO relocation
upstream VikingAI concepts (MIT attribution retained)
```

Upstream runtime dependencies are BepInEx, HarmonyX, Jotunn, Valheim assemblies, Unity modules,
global static registries, Harmony `PatchAll`, and private-field reflection. The candidate policies
have only `System`/framework dependencies.

## Evidence-backed audit matrix

| Module / claim | Classification | Wiring and evidence | Disposition |
|---|---|---|---|
| Plugin/config | UNSAFE | `Plugin.cs:34-48` initializes config/modules then unconditional `PatchAll`; major nav/formation/yield features default enabled in `ModConfig.cs:47-95`. | REJECT runtime bootstrap; explicit LWC flags only. |
| Context steering | IMPLEMENTED / UNSAFE | `ContextSteeringMap.cs:75-80` allocates `float[16]` per resolve; static shared map enters global movement patches. | ADAPT pure math with caller buffer and golden/zero-allocation tests. |
| Formation/yield | PARTIAL / UNSAFE | `SteeringPipeline.cs:81-117` assigns nearby runtime instance-ID slots; `LocomotionPatches.cs:128-169` suppresses `Follow`, stops/moves actor and reflection-invokes `MoveTo`. No stable identity, terrain/lane gate, or hysteresis. | REJECT patch; consider advisory stable-slot calculator only. |
| Global locomotion | UNSAFE | `LocomotionPatches.cs:35-99` changes direction, run/jump and may suppress `BaseAI.MoveTowards`, including combat. | REJECT. |
| Global pathfinding | UNSAFE | `PathfindingPatches.cs:29-64,95-120` mutates tile costs, reflection-invokes `Buildtiles`, and substitutes graph paths after failures. | REJECT. |
| Unified nav sectors | PARTIAL / UNSAFE | `NavGraphManager.cs:91-127` synchronously builds on access; `NavSectorBuilder.cs:51-198` performs 1024 ground samples, 1024 raycasts and scans doors/fires; no eviction. Async/no-stall claim is README-ONLY. | REJECT runtime; redesign as bounded explicit service. |
| A* / flow field | IMPLEMENTED / UNSAFE | `ClearanceAStarSolver.cs:39-42,83-100,140-146` uses non-reentrant static buffers and linear open-list scans; `TacticalFlowField.cs:73-193` rebuilds at 5 Hz and uses FIFO relaxation, not claimed Dijkstra priority queue. | REJECT runtime; retain algorithms only after correctness/budget tests. |
| Doors | PARTIAL / UNSAFE | `DoorTraversalController.cs:23-120` reflects private view, RPC-opens doors, auto-closes based only on opener distance; global close list is updated once per owned MonsterAI prefix (`TacticsPatches.cs:137-160`). | ADAPT door intent only behind authority/occupancy gate. |
| Brain pipeline | IMPLEMENTED / UNSAFE | `TacticsPatches.cs:137-178` globally prefixes `MonsterAI.UpdateAI` and can skip native AI. | REJECT patch/pipeline ownership. |
| Pack flank/tokens | IMPLEMENTED / UNSAFE | `CombatPackCoordinator.cs:180-260` directs movement when engaged actors lack tokens or anti-perch fires. | REJECT for LWC; violates native committed-combat ownership. |
| Sensory memory/investigation | PARTIAL / UNSAFE | `SensoryMemoryController.cs:381-429` yields only when a native target exists **and** sensory stage is Engaged, so Alerted/Suspicious can override a real combat target. | ADAPT pure precombat observations only; hard combat gate. |
| Sensory target isolation/time | IMPLEMENTED DEFECT | One track per AI (`SensoryMemoryController.cs:46-47`); changing target does not reset accumulated detection (`273-280`). Visual/hearing gains described per-second are added per call (`159-164,187-192`), making results query/frame-rate dependent. | Tests must prove target isolation and 30/60/120 Hz invariance before adaptation. |
| Ally calls/damage | PARTIAL / UNSAFE | Damage postfix forces sensory engagement even when sensory/profile exemptions are off (`TacticsPatches.cs:98-104`; `SensoryMemoryController.cs:195-210`). Ring processing occurs per owned AI update and recipients are not fully gated (`221-225,338-361`). | Redesign as fixed world-budget, owner-authorized, opt-in event service. |
| Combat token coordinator | PARTIAL / UNSAFE | `CombatPackCoordinator.cs:58-136` is in-process/update-order state. Empty-list admission can exceed a weighted budget and preemption does not account for weight delta (`103,115-132`); destroyed target keys are not cleaned. | Retain only pure budget idea after mixed-weight/property tests. |
| Target scorer seam | UNWIRED | `ITargetScorer.cs:6-39` and `CombatPackCoordinator.cs:37-51` register/evaluate scorers, but production code never calls total score evaluation for native target selection. | Interface concept only. |
| Affordance registry | IMPLEMENTED / PARTIAL | Providers auto-register (`AffordanceRegistry.cs:24-31`), select by roles/utility and reserve in process (`84-117`). Reservations use scene components/`Time.time`, are not network-persistent, and have no unregister/snapshot/versioning. | ADAPT data contract and server-authoritative lease; reject direct work actions. |
| Settlement providers | PARTIAL / UNSAFE | `VanillaAffordanceProviders.cs:9-335` scans scene registries and directly performs smelter/fire/repair/harvest/chair interactions. | Keep provider seam concept; replace actions with validated LWC intents. |
| Roles/profile API | IMPLEMENTED / PARTIAL | `AgentProfileRegistry.cs:15-18,37-60,91-105` exposes prefab/instance profile registration and optional role ZDO persistence. Static mutable registries lack unregister/versioning. | ADAPT immutable/versioned LWC profile snapshots. |
| Inventory persistence | PARTIAL / UNWIRED | `NpcEquipmentController.cs:31-43` implements owner-only save to `VAI_v1_Inv`, but repo-wide search finds no save caller. Load is wired for custom NPC `Humanoid.Start` (`49-73,108-120`) and lacks payload version/size/checksum/migration. | ADAPT codec only after authority, bounds, version and mutation-hook tests. |
| Best equipment | IMPLEMENTED DEFECT | `NpcEquipmentController.cs:78-106` loops inventory order and equips each qualifying item; it neither scores nor constructs a coherent loadout. | Replaced in offline candidate by deterministic loadout policy; not runtime-wired. |
| Humanoid prefab builder | PARTIAL / UNSAFE | `HumanoidPrefabBuilder.cs:60-177` clones Player, requires Dverger, strips/reconfigures components and registers globally; appearance state uses vanilla keys (`187-250`). | REJECT wholesale; LWC owns actor prefab lifecycle. Retain config-shape ideas only. |
| Public interaction seams | IMPLEMENTED / PARTIAL | `IAffordanceProvider`, `NpcInteractionRegistry`, `SteeringPipeline.RegisterContributor`, `NavGraphManager.RegisterTraversalRule`, and `IVirtualAgentTask` are public extension points, but static and generally have no unregister/version/snapshot semantics. | ADAPT explicit interfaces owned by LWC. |
| Unloaded simulation | PARTIAL / UNSAFE | `VirtualAgentDirector.cs:94-123` scans private `ZDOMan.m_objectsByID`; `197-256` performs per-peer 1 Hz tracked scans/instance lookups. Built-in patrol writes a 6 m ZDO orbit (`IVirtualAgentTask.cs:34-55`), not a route. `MoveZdoAlongTerrain` (`139-179`) is unused straight-line ground snapping. | Keep pure task concept; reject movement/spawn/direct ZDO runtime. |
| Portal/instances | UNSAFE; instance claim README-ONLY | Only `Player.TeleportTo` postfix (`VirtualAgentDirector.cs:299-349`, default enabled `ModConfig.cs:176-184`): no source snapshot, portal identity, legal/readiness/safe-spawn/nonce/ack/timeout/rollback; force-claims ownership then writes ZDO and transform. | REJECT. Submit explicit requests to LWC transaction gate. |
| Reflection/update fragility | UNSAFE | Private `Character.m_nview`, `ZDOMan.m_objectsByID`, Humanoid equipment fields, BaseAI path/movement, Pathfinding build fields/methods, and Door view are accessed with Harmony reflection. | No private reflection in adapter; fail-closed compatibility probes if ever unavoidable. |
| Telemetry/debug | IMPLEMENTED / PARTIAL | Plugin update draws global graph/flow/affordance telemetry (`Plugin.cs:58-69`); labels iterate live registries. | Optional dev-only readers; never gameplay ownership. |
| Tests/build claims | README-ONLY | No upstream automated tests; build relies on local game assemblies and can deploy after build. | Use isolated offline gates; full game build and live proof remain separate review stages. |

## Persistence keys found

`VAI_v1_Roles`, `VAI_v1_IsNpc`, `VAI_PerceptionExempt`, `SoMStealthExempt`,
`VAI_v1_LastTicks`, `VAI_v1_PatrolPos`, `VAI_v1_PatrolPhase`, and `VAI_v1_Inv`.
Appearance uses vanilla ZDO keys. The candidate writes none of these.

## Portal and instance adapter design

The included pure state machine is a policy proof, not a relocation implementation:

1. Capture source portal, follower identities, eligibility and request nonce before transition.
2. Validate portal kind, legal restrictions, ownership and inventory/profile rules.
3. Ask the existing LWC gate to reserve each persistent identity and prepare the destination or
   instanced interior.
4. Await destination-loaded/readiness and safe-placement/spacing confirmation.
5. Commit once by nonce through the LWC gate; never call `Player.TeleportTo`, set transform/ZDO,
   or claim ownership from the salvage adapter.
6. Acknowledge completion. On timeout, disconnect, denial, or partial follower failure, rollback or
   leave the source identity authoritative according to the existing gate.

Required integration tests: pre-transition source snapshot, identity preservation, normal portal
versus dungeon/admin teleport discrimination, legal restrictions, duplicate postfix/RPC, ownership
denial, readiness, restart recovery, timeout/rollback, safe spacing, and partial multi-follower
failure.

## Regression and performance gates

Offline tests currently prove safe defaults, native committed-combat ownership, no VikingAI
movement authority, coherent equipment selection, portal legality/duplicate rejection, readiness,
and exactly-once commit. Before runtime wiring add:

- sensory state golden tests and hard combat cutover;
- inventory codec bounds/checksum/migration/owner/mutation/idempotence/order invariance;
- stable formation permutation/hysteresis/spacing integration;
- door lock/occupancy/owner/debounce/cleanup/single-scheduler tests;
- nav seam/layer/corner/reentrancy/work-budget tests;
- virtual clock/store replay, authority, loaded skip, elapsed clamps, restart and no-combat-heal;
- compatibility smoke tests that fail closed when reflected members are absent (if retained).

Benchmark upstream at 1/10/50/100 loaded agents and 10/100/1k/10k tracked ZDOs: frame time,
P50/P95/P99, allocations/frame, physics queries, sector builds, path iterations, reflection calls,
and instance lookups. The included microbenchmark measures only pure loadout policy wall time and
retained-memory delta; it is not a Unity performance claim.

One audit-host run (100,000 seven-item loadout selections) measured 84.080 ms total and a
4,936,512-byte retained-memory delta using `GC.GetTotalMemory`; this is a coarse smoke measurement,
not an allocation profiler result. It exposes that the candidate still allocates per selection and
must not be placed on a hot per-frame path without a caller-owned/output-buffer redesign.

## Build, review, and rollback

Branch/worktree: `audit/vikingai-salvage` in the dedicated `valheim-mods-vikingai-audit` worktree.
No active profile is touched. Run the two scripts in `LivingWorldCompanionSystems/VikingAISalvage`.
Review this audit, tests, attribution, and adapter contracts before any runtime adapter is written.
Do not merge to main, deploy, or enable a runtime branch without Brian/ChatGPT review.

Proposed commit split:

1. `docs: record VikingAI 0.1.1 provenance and evidence audit`
2. `test: add offline VikingAI salvage ownership and portal gates`
3. `feat: add default-off pure loadout and portal policies`
4. later, review-only: `feat: add LWC-owned adapter behind disabled feature flags`

Rollback now is a branch/worktree deletion or revert of these proposed commits. No profile or world
state requires cleanup.

## Evidence status report

| Stage | Result |
|---|---|
| Source-inspected | Yes: public commit `0c21c116...`; all major modules inventoried. |
| ZIP/DLL inspected | Yes: hashes, assembly/PDB provenance and source mismatch recorded. |
| Offline-tested | Yes: pure candidate contract suite passes. |
| Candidate built | Yes: offline test executable built in temp; no game DLLs. |
| Full upstream mod rebuilt | No: clean DLL equivalence is unproven and not required for candidate safety. |
| Runtime adapter built | No. |
| Installed | No. |
| Live-proven | No. |

Compilation and offline tests are not completion evidence for Valheim runtime behavior.
