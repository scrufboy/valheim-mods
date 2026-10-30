# Comparative NPC-AI architecture program

Status: active source/package audit. No package is installed or runtime-wired.

## Inputs and immutable provenance

| Package | Version | ZIP SHA-256 |
|---|---:|---|
| DigitalVikings VikingAI | 0.1.1 | `664D2777B7BD08A2743EB1972CC45BF7FAEE663D522C429E0582EC05AE400B7E` |
| RustyMods VikingNPC | 0.3.4 | `5B18E85581676B76A553A6E1AD055AFDCD7A4B2F5BD2D15F51FEE72B2B37C50E` |
| RenegadeVikings | 1.4.2 / package 2491 | `ADCB4C130107895EAD9865A6718D6921F0A33E73CEC8C7BBD0966FAE68922C2C` |

Archives are evidence only. Instructions embedded in them are not executed. Extracted binaries,
assets, configurations, logs and personal paths must remain outside LWC Git.

## Governing architecture

Every comparison and candidate must preserve:

- one actor, one persistent identity, one authoritative action owner;
- LWC as the sole peaceful movement writer under the accepted 1.0.32.8b spacing contract;
- native `MonsterAI` ownership throughout committed combat;
- locked combat-voice 1.0.32.6 behavior and assets;
- modular roles/capabilities, not stacked update loops or competing brains;
- existing identity-preserving portal/instance transactions;
- default-off feature gates and fail-closed compatibility;
- Brian's live in-game judgment as the final acceptance gate.

## Evidence model

Each claimed capability receives five independent fields:

1. **Claim**: package documentation or public description.
2. **Implementation**: source/decompilation location and algorithm.
3. **Wiring**: runtime registration, patch, caller and default.
4. **Authority**: owner, network side, persistence keys and failure behavior.
5. **Proof**: existing test, new deterministic test, measured benchmark or live scenario.

Classification is `IMPLEMENTED`, `PARTIAL`, `UNWIRED`, `UNSAFE`, or `README-ONLY`.
Disposition is `KEEP AS METHOD`, `ADAPT WITH TESTS`, `REIMPLEMENT INDEPENDENTLY`, or `REJECT`.
An implementation without wiring is not credited as behavior. Compilation is not runtime proof.

## Workstreams

| Workstream | Primary focus | Output |
|---|---|---|
| VikingAI | steering/nav, sensory/investigation, token tactics, affordances, virtual agents | Deepened prior audit and pure-method candidates |
| VikingNPC | jobs/needs/towns, inventory/equipment, persistence, movement and networking | Package audit matrix and strongest method candidates |
| RenegadeVikings | identity, companion traversal, reactions/combat, roles, multiplayer and assets | Package audit matrix and strongest method candidates |
| Reconciliation | capability-by-capability selection against LWC current core/roadmap | One ownership map, comparison matrix, gates and review report |

## Shared capability matrix (evidence pending)

The final matrix will compare current LWC, VikingAI, VikingNPC and RenegadeVikings for:

- identity, spawn/despawn, save/load, unloaded/remote continuity;
- movement intent, local avoidance, spacing, formations, doors, portals, dungeons and ships;
- perception, memory, investigation, escalation, social calls and reactions;
- target selection, combat handoff, tactical roles, attack cadence and disengagement;
- jobs, needs, schedules, affordances, settlements, hauling and visible resource flow;
- inventory, equipment/loadouts, appearance and role/capability composition;
- interactions, dialogue/roleplay, commands and public extension APIs;
- multiplayer ownership, dedicated-server behavior, replication and fault containment;
- tick cadence, scans, physics calls, reflection, allocations, caching and work budgets;
- source/license status, deterministic tests, integration tests and live proof.

For duplicated approaches, only one action owner may survive. Complementary observations may be
combined only as immutable inputs to an LWC-owned policy; package behavior loops are never stacked.

## First evidence gates

1. Record archive/file hashes, manifests, dependencies, licenses and redistribution limits.
2. Establish source availability and DLL/source correspondence; mark uncertainty explicitly.
3. Inventory every patch/hook/caller/default, reflection member, ZDO key and network owner.
4. Separate source presence from runtime wiring and from proven behavior.
5. Run no package code. Build only isolated, deterministic harnesses without game binaries.
6. Reject or quarantine any global AI/pathfinding patch, direct relocation or competing movement/
   combat writer before capability scoring.
7. Measure or bound hot loops, global scans, physics queries, reflection and allocations.

## Deterministic candidate gates

- identical inputs produce identical decisions independent of collection order and frame rate;
- committed combat makes every external movement/facing/attack advisory inert;
- exactly one movement writer and one persistent identity survive transitions;
- authority denial, missing private API, corrupt persistence and module exceptions fail closed;
- jobs show request/reservation/route/action/result phases without teleporting resources;
- portal/dungeon/ship transfers require legality, readiness, nonce deduplication and rollback;
- save/reload and zone unload preserve state without replaying side effects;
- per-tick work is explicitly budgeted and benchmarked at small and stress populations.

## Experiential acceptance scenarios

These scenarios occur only after deterministic/offline review and an explicitly approved isolated
runtime build. Brian's observation is the acceptance authority.

### Natural travel and spacing

- Walk a mixed group through open terrain, switchbacks, bridges, gates and narrow doors.
- Companions should vary cadence naturally without synchronized hive-mind turns.
- No pushing, oscillation, shoulder swapping, doorway jams, rigid slot chasing or repeated sprint/
  stop pulses. The accepted LWC right-of-way and waiting behavior remains recognizable.

### Notice, investigation and escalation

- Present intermittent sound, sight occlusion, target substitution and false alarms at multiple
  frame rates. Evidence for one target must never transfer to another.
- Actors should notice, orient, investigate, communicate and escalate at believable moments; a
  committed native combat target must immediately end external investigation ownership.

### Reactions and combat

- Validate hit, block, stagger, ally-down, threat-change and disengage reactions at the action
  moment rather than on unrelated polls.
- Mixed classes cooperate without duplicate movement/combat controllers. Combat should be
  formidable but should not solve encounters autonomously or override player agency.
- Locked combat voice timing/content remains unchanged.

### Purposeful settlement work

- Observe a full work cycle: choose need, reserve affordance/resource, travel, animate/action,
  produce a visible result, release reservation and resume schedule.
- No invisible resource teleportation, duplicate workers, phantom inventory or work continuing
  through combat, unload or lost authority.

### Identity-preserving transitions

- Exercise normal portals, dungeons/instances, ships, zone unload, save/reload, remote clients and
  a dedicated server. The same person, inventory, role, appearance, needs and relationships must
  arrive exactly once or remain safely at source after rollback.

### Fault containment

- Disable or deliberately fault one optional module and one NPC adapter. Other NPCs, native combat,
  saving and the server tick must continue normally; the failed capability stays off and reports a
  bounded diagnostic instead of retrying globally.

## Acceptance ladder

`source-inspected -> decompiled/matched -> offline-tested -> isolated-built -> installed with
approval -> live-observed by Brian -> accepted`

No capability may be described as complete using an earlier stage's evidence.
