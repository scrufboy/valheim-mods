# Comparative audit: VikingAI, VikingNPC and RenegadeVikings

Status: statically inspected and clean-room policies offline-tested. Nothing installed or live-proven.

The governing plan and experiential acceptance ladder are in
`NPC-AI-COMPARATIVE-PROGRAM.md`. The detailed VikingAI evidence is in
`VIKINGAI-SALVAGE-AUDIT.md`.

## Package and legal boundary

| Package | Package / DLL evidence | Source correspondence | License result |
|---|---|---|---|
| VikingAI 0.1.1 | ZIP `664D277...`; DLL `31070FC...` | Public `0c21c116...`; DLL/PDB names older `1819713a...` but contains later code, implying dirty-tree build. Whole-DLL clean correspondence unproven. | MIT, DigitalVikings 2026. May adapt with notice. |
| VikingNPC/Norsemen 0.3.4 | ZIP `5B18E855...`; DLL `C236467A...` | Public `c57706b9...`; package docs match and unique current symbols are in DLL. No PDB and no clean rebuild, so exact match unproven. | **No license grant.** Embedded custom asset bundle and merged ServerSync. Do not copy code/assets; clean-room methods only. |
| RenegadeVikings 1.4.2 | ZIP `ADCB4C13...`; DLL `F95638FF...` | Binary-only; no source/PDB/repository URL. Embedded ServerSync, LocalizationManager, YamlDotNet, translations and asset bundle. | **No license grant or notices.** Do not copy code/assets/config/translations; behavioral evidence only. |

All three packages lack automated tests. VikingNPC's build contains absolute developer/profile paths
and a post-build profile copy; it must not be built as checked in. RenegadeVikings has no buildable
source. No package binary, asset or configuration belongs in LWC Git.

## Architecture and action ownership

```text
                         observations / data only
   spatial index   staged perception   role/loadout data   work/interaction intent
          \              |                    |                    /
           +-------------+--------- LWC policy core --------------+
                                     |
          peaceful movement ----------+---------- committed combat
             LWC sole writer                        native MonsterAI
                                     |
                     persistence / transition authority
                 one identity + LWC transactional gates
```

- VikingAI globally patches `BaseAI`, `Pathfinding`, sensing and `MonsterAI.UpdateAI`; its brain may
  skip vanilla and write movement in combat. Reject its live controller.
- VikingNPC replaces `MonsterAI.UpdateAI` with a monolithic `VikingAI : MonsterAI` loop controlling
  peaceful activity and combat. Reject the controller despite owner checks.
- RenegadeVikings mostly configures ordinary `Humanoid`/`MonsterAI`. This is the safest combat
  method of the three: varied data-driven actors while native combat remains native. Its auxiliary
  patches still contain unsafe authority and relocation behavior.

## Cross-package capability matrix

| Capability | Current LWC contract / need | VikingAI | VikingNPC 0.3.4 | RenegadeVikings 1.4.2 | Best method and disposition |
|---|---|---|---|---|---|
| Actor/combat brain | Native `MonsterAI` owns committed combat | Global prefix/brain can skip native | Full custom ordered combat/movement loop | Tunes native MonsterAI; no replacement brain | **KEEP AS METHOD:** Renegade's data-driven native-AI tuning, independently implemented. Reject both replacement brains. |
| Peaceful movement | One LWC writer; accepted 1.0.32.8b spacing | Global MoveTowards/Follow/path rewrites | Direct MoveTo plus >15 m teleport | Vanilla movement plus direct portal/horn relocation | **REJECT** all movement loops/relocation. Preserve LWC. Context vectors may be advisory only. |
| Spatial queries | Shared bounded registry needed | Pooled 8 m character grid; piece lifecycle registry, but some full-set scans | Per-worker full `ZNetScene.m_instances` scan | Global character/collider scans | **ADAPT WITH TESTS:** VikingAI grid/lifecycle method under LWC scheduler and caller buffers. |
| Formations/spacing | Stable identity, hysteresis, lanes/right-of-way | Runtime instance-ID neighborhood slots; separation/yield override Follow | No formation; random circling | No formation; direct offset/stack placement | **REIMPLEMENT.** None satisfy LWC spacing or natural cadence. |
| Doors | Occupancy-aware request under LWC authority | Reflection/RPC open; unsafe auto-close; repeated global scheduler | Absent | Absent | **REIMPLEMENT.** At most keep VikingAI's door-intent concept. |
| Perception/memory | Believable target-isolated staged evidence | Strong four-stage vocabulary and near-miss location idea; target leakage and query-rate defect | Simple last-known target; work can suppress enemy sensing | Vanilla fields; extreme hear range `9999` on actors | **REIMPLEMENT INDEPENDENTLY:** VikingAI vocabulary + time-based isolated evidence; offline clean-room policy added. |
| Reactions | Correct action moment, exact once | Damage/projectile hooks, but participation gates/cadence unsafe | Random block every 2 s; dodge polling every 0.2 s | Mostly vanilla | **KEEP NATIVE / ADAPT EVENTS.** Use native combat action windows; no polling controller. |
| Combat coordination | Formidable, not autonomous | Token/flank design but mixed weights exceed budget and denied actors reposition | None | None | **REIMPLEMENT only if proven.** Pure budget/fairness algorithm, never movement/attack owner. |
| Capability/role composition | Modular, one action owner | Rich `AgentBehaviorProfile` flags and role-filtered providers | Tools imply work roles inside monolith | Tiered prefab/loadout classes | **KEEP AS METHOD:** immutable LWC capability snapshots combining VikingAI vocabulary with data-driven native kits. |
| Work/affordances | Reserved, visible travel/action/result; real resources | Best provider/utility/lease architecture; direct mutations and local reservations | Visible tool animation but synthesizes mining/lumber/fish items; full-world scans | Visible walk to chest, then unsafe async reflection and unreserved removal | **ADAPT/REIMPLEMENT:** VikingAI candidate+lease, VikingNPC presentation, Renegade walk-to-source, LWC authoritative transaction. Offline work state machine added. |
| Settlements/towns | Purposeful schedules/material provenance | Five prototype providers; no hauling chain | Towns removed in 0.3.0; raids currently unwired | Jobs/settlements absent | **REIMPLEMENT.** None supplies a real town economy. |
| Needs | Modular bounded needs/affinity | Absent | Fixed last-fed hunger; consumes inventory/world food | Vanilla hunger plus chest food search | **KEEP AS METHOD:** timestamp hunger and visible source approach; LWC NeedsAndAffinity remains owner. |
| Social/roleplay | Relationships, memory, dialogue/trade/quests | Only empty interaction registry | Random greet/farewell/global cooldown | Trader inventory and shallow random talk | **REIMPLEMENT.** Keep ordered interaction seam and data-driven trader/dialogue surfaces. None has social memory. |
| Inventory persistence | Versioned, bounded, owner-authoritative | Save method unwired; load wired; unversioned base64 | Mutation/destroy saves plus 10 s Load/Save; unsafe RPC ownership transfer | No NPC inventory codec | **ADAPT WITH TESTS:** explicit mutation hooks, but independently implement version/bounds/checksum/migration. Reject ownership transfer. |
| Equipment/loadout | Coherent deterministic kit | Sequentially equips every qualifying item | Per-slot better-item selection, still side-effect/order based; random chance inversion bug | Tiered random kits; `VikingEquipped`/VisEquipment state | **ADAPT:** data-driven kit method plus clean-room coherent loadout policy already offline-tested. |
| Identity/death/revival | Same person exactly once | Roles/appearance partial; no complete identity lifecycle | Tomb recreates a new ZDO, breaking identity; follow stored by player display name | Vanilla ZDO/prefab only; no identity abstraction | **REIMPLEMENT through existing LWC identity.** Reject VikingNPC revival semantics. |
| Portals/dungeons | Identity-preserving legal/readiness transaction | Post-TeleportTo force claim + ZDO/transform move | Post-TeleportTo direct ZDO/transform move; global dungeon flag | Post-TeleportTo transform move; README “except dungeons” contradicted | **REJECT all three.** Existing LWC transaction gate only; offline nonce/readiness policy added. |
| Ships | Reserved seat, authority migration, natural board/disembark | Absent | Scans chairs, pins transform/velocity, teleports on disembark | README horn convenience only; no ship logic | **REIMPLEMENT.** VikingNPC supplies experiential evidence only. |
| Unloaded simulation | Pure capped schedule projection, never route snapping | Private world scan; direct 6 m ZDO orbit and passive heal | Wall-time item manufacture from last target, uncapped/invalid grants | Absent beyond vanilla persistence | **REIMPLEMENT.** Keep only role-filtered task/schedule seam. |
| Multiplayer authority | Server/owner validation, deterministic handoff | Mixed owner checks; every peer scans; force ownership in portal | Owner tick, but inventory RPC transfers ownership and insufficient authorization | Several mutations lack owner gate; death spawn depends on local killer | **REJECT authority shortcuts.** LWC server-authoritative adapters only. |
| Fault containment | One module/NPC cannot destabilize all | Global patches/static registries/private reflection | Monolithic update/global enum/faction patches | Global patches, async void, reflection, event leaks | **REIMPLEMENT:** per-module failure boundary, disabled-on-fault state and bounded diagnostics. |
| Debug/acceptance tooling | Brian-visible reasons/ownership/timing | Strong capped selectable overlays | Debug inventory save logs only | Boss pins/presentation only | **ADAPT WITH TESTS:** VikingAI overlay method as read-only LWC diagnostics. |

## High-confidence defects and contradictions

### VikingAI

- Synchronous sector access can perform 1,024 ground samples, 1,024 raycasts and door/fire scans;
  async/no-stall documentation is contradicted.
- `ContextSteeringMap` allocates per resolve; reflection invocations allocate in hot paths.
- Sensory evidence transfers across target switches and “per-second” gain is applied per query.
- Investigation may suppress native combat until sensory reaches Engaged.
- Inventory save has no internal caller; portal and unloaded movement are direct relocation.

### VikingNPC

- Work “mining/lumbering” uses zero-damage presentation and synthesizes inventory output without
  consuming the rock/tree. Fishing copies fish item without consuming fish/bait.
- Random item chance is inverted (`roll <= chance` skips), making 0.8 behave near 0.2.
- Work scans all loaded instances per worker every 30 seconds; inventory runs Load+Save every 10
  seconds per Viking.
- Client inventory RPCs transfer ZDO ownership and do not enforce authority independently.
- Tomb revival creates a new ZDO; portals/ships directly relocate actors.
- Current version has no towns or raider ships despite historical repository documentation.

### RenegadeVikings

- README's portal exception for dungeons is contradicted: the postfix has no dungeon check.
- War horn has six offsets but accepts an unbounded follower list; seven or more can index past it.
- Chest feeding is `async void`, reflects private movement/container methods and removes items without
  a container lease/access/rollback transaction.
- Spawn records are cleared and rebuilt on every `SpawnSystem.UpdateSpawnList` call.
- Per-equipment config handlers subscribe to static events without teardown, risking retained actors.
- Several taming, name/appearance, portal and death-spawn mutations lack adequate owner/server gates.

## Complementary combination selected

No package behavior loop is accepted. The strongest combined architecture is:

1. **Actor construction:** Renegade's method—data-driven humanoid/role/loadout profiles using native
   combat—implemented in LWC without its binary/assets.
2. **Capabilities:** VikingAI's declarative vocabulary, rewritten as immutable, versioned LWC
   snapshots with all external features default off.
3. **Perception:** VikingAI's stage vocabulary and near-miss location concept, rewritten as
   target-isolated, delta-time evidence that becomes inert at native combat handoff.
4. **World indexing:** VikingAI's explicit lifecycle registries/caller-owned query buffers, with one
   shared LWC scheduler and measured work budgets.
5. **Work:** VikingAI affordance candidates/leases + VikingNPC visible tool presentation +
   Renegade's visible source approach, all feeding one LWC transaction state machine. Real authorized
   resources change only at the action moment.
6. **Equipment:** data-driven kits plus LWC's coherent deterministic loadout scorer.
7. **Diagnostics:** VikingAI-style read-only overlays showing owner, intent, block reason, target
   evidence, lease and transition nonce for Brian's live review.

This combines observations and presentation, never controllers.

## Capabilities none of the packages solves adequately

- stable identity across death/revival, portals, instances, ships and ownership migration;
- natural heterogeneous group travel through narrow spaces without rigid slots or teleport recovery;
- authoritative, material-preserving settlement work and hauling chains;
- restart-safe distributed reservations and believable unloaded schedules/routes;
- relationship/social memory, quests and sustained roleplay;
- dedicated-server tested authority migration and malicious-client resistance;
- native-combat-safe coordinated roles/reactions;
- per-NPC/module fault isolation;
- deterministic tests, load benchmarks and Brian-accepted experiential cadence.

## Performance benchmark plan

Measure 1/10/50/100 loaded actors and 100/1k/10k/50k world records. Record median/P95/P99 frame
time, bytes/frame, physics queries, registry scans, path calls, ZDO reads/writes, RPCs, reflection
calls, async continuations and config-event subscriptions. Scenarios: open travel, doorway pressure,
combat handoff, sensory occlusion, full work cycle, inventory mutation, portal/ship transition,
unload/reload and module fault injection.

Specific regression limits must be set from the accepted LWC baseline, not from these packages.
Any candidate that adds global per-actor world scans, hot reflection, direct relocation or per-frame
allocations fails before live review.

## Candidate and proof status

Clean-room offline policies now cover:

- default-off feature and ownership gates;
- coherent deterministic equipment loadout selection;
- identity/legality/readiness/nonce portal transaction states;
- target-isolated, time-based precombat perception;
- reserved travel/arrival/action/exactly-once work transactions with combat cancellation.

These policies use framework libraries only and contain no package code or assets. Passing their
tests proves only deterministic policy behavior—not Valheim integration.

## Final disposition

| Package | Keep | Reject |
|---|---|---|
| VikingAI | MIT architectural vocabulary, spatial indexing method, staged perception vocabulary, affordance/interaction seams, diagnostics | Global patches, brain ownership, movement/pathfinding runtime, tactical controller, portal/unloaded relocation, current persistence |
| VikingNPC | Behavioral evidence for ordered actor priorities, visible work/tool presentation, mutation-save hooks, simple hunger/talk methods | All copied code/assets (unlicensed), replacement combat brain, global faction patches, synthetic resources, ownership transfer, portals/ships, new-ZDO revival |
| RenegadeVikings | Behavioral evidence for native-brain data-driven actors, kits, trader surfaces and visible source approach | All copied code/assets (unlicensed), direct relocation, unsafe chest/taming/death authority, spawn rebuilds, event leaks |

Brian's live scenarios in `NPC-AI-COMPARATIVE-PROGRAM.md` remain mandatory before any isolated
runtime adapter can move from installed-with-approval to accepted.
