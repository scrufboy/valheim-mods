# Living World C.O.R.E. NPC 1.0 Solidification Specification

Evidence scope: SOURCE-INSPECTED user requirements and project sources define the requested behavior and boundaries. Proposed structures and delivery sequencing remain UNKNOWN until implemented. The installed 1.0.0 candidate is INSTALLED but its live behavior is UNKNOWN until the current test.

Date: 2026-10-23  
Authorial direction: Brian / scrufboy  
Recipient: Claude  
Current candidate: Living World C.O.R.E. NPC 1.0.0 on `claude/lwcore-npc-1.0.0`

## Purpose and governing decision

Living World C.O.R.E. must contain the NPC's understanding of companion duty,
holding, release, town context, usable work and social affordances, citizenship,
returning home, and migration. These concepts cannot live only in the Village
Reclamation project. Reclamation may construct, restore, populate, govern, and
operate villages, but a Living World person must recognize and respond to a town
wherever the required world evidence exists.

This is a 1.0 solidification scope, not permission for another sequence of small
runtime patches. Build the complete state and ownership foundation on the branch,
exercise it through offline gates, and install a single coherent candidate after
the currently installed 1.0.0 baseline has produced its first live evidence.

The objective is one believable person whose combat, travel, companion life,
settlement life, and migration all use the same identity and the same exclusive
state/ownership system. Town behavior is not a different NPC species and should
not require swapping to an unrelated controller.

## Current 1.0.0 facts

Claude's 1.0.0 establishes the correct structural direction:

- `NpcKernel` senses once, arbitrates one top-level phase, and grants exclusive
  Movement, Hands, Look, and Posture domains.
- Behaviors enter, tick, and exit under the domain scheduler.
- `NpcBody` is the guarded native-state writer.
- Native combat has the highest priority and owns every domain while committed.
- Identity, randomized sex, hair, male beard, equipment presentation, wetness,
  formation, traversal, guard coverage, crouch, sit, and torch policy are present.
- The current context enum contains only `FieldCompanion`.
- Hold, town/resident context, doors, workstation activity, beds, citizenship,
  dismissal, returning home, and migration are not implemented.
- No 1.0.0 behavior has yet been proven in a live Unity run.

The existing 1.0.0 DLL and telemetry DLL must remain untouched while that baseline
is tested. Development belongs on the branch/source, not as an unrecorded installed
hotfix.

## Canonical person model

Do not turn every new requirement into another mutually exclusive top-level phase.
The person needs independent durable dimensions that the arbiter combines into one
authorized action.

### Identity

- Stable NPC identity, name, appearance, sex, hair, beard, personality/aptitude
  seed, equipment, inventory, learned facts, and history.
- Never reroll identity because the person follows, holds, changes town, unloads,
  migrates, or resumes village duty.

### Citizenship

- Optional `HomeSettlementId`, normally the durable identity of a Living World
  maypole/settlement anchor.
- Home citizenship persists while the NPC accompanies the player.
- Entering another maypole's area creates guest context; it does not silently
  transfer citizenship.
- A home transfer is an explicit admission/resettlement transaction.

### Engagement or order

- `Follow`: active travelling companion.
- `Hold`: remain in a bounded local area while retaining the current leader and
  citizenship. Hold is not dismissal and not permanent immobility.
- `GoHome`: retain identity, equipment, citizenship, and leader relationship while
  travelling back to the assigned home settlement.
- `ReleaseFree`: end companion engagement without deleting the person. The person
  becomes free-roaming unless an explicit settlement admission occurs.
- `SettleHere` or equivalent explicit admission: establish or transfer home only
  after settlement capacity, permissions, and identity checks pass.

### Spatial context

- Wilderness or road.
- Own home settlement.
- Guest/visited settlement.
- Outpost linked to a settlement.
- Migration journey.

### Duty and temporary action

- Companion traversal, companion readiness, native combat, regroup, hold-area
  activity, resident duty, resident leisure, sleep, social activity, migration,
  and dormant/unloaded representation.
- One committed action and one movement owner at a time.
- Combat and immediate danger preempt every optional town activity.

These dimensions must be persisted independently. Do not compress home,
companionship, town presence, and the current animation into one enum or one flag.

## Settlement anchor belongs in the core

The Living World maypole is the canonical settlement anchor. Its core contract is:

- Stable networked settlement identity stored in versioned ZDO fields.
- Explicit bounds or bounded linked areas; do not determine ownership only by the
  nearest maypole when town areas overlap.
- Authority/ownership metadata and an optional linked-outpost set.
- A bounded roster of citizens, guests, held companions, assigned homes/beds, and
  active reservations.
- Capability and permission facts consumable by NPC behavior without granting
  ownership, recipes, items, or resources by proximity alone.
- Load/unload reconstruction without duplicating people or native geometry.

Placing a maypole should make a location recognizable as a possible Living World
settlement. It must not automatically absorb every nearby NPC. A companion brought
there can hold and participate respectfully. Permanent citizenship begins only
through an explicit player or settlement admission decision.

The same anchor semantics must work for a manually built town, an existing village,
a reclaimed village, and a future generated settlement. Reclamation may create or
restore the maypole and its records; the NPC core interprets them.

## Core affordance and totem registry

Every NPC-usable workstation/social/location totem must be understood by the core.
This does not mean duplicating full production code inside the NPC or replacing
every vanilla object. It means a single core registry describes what exists, where
an NPC stands, how it faces, which animation channel is safe, who may reserve it,
and whether the use is ambient or economically real.

Use two acquisition paths:

1. Discover supported ordinary vanilla or mod-added pieces directly from their
   native components and configuration.
2. Support private Living World marker/totem prefabs for locations that need an
   explicit NPC anchor, role, facing, capacity, or animation override.

Never mutate the global vanilla prefab. Any Living World variant must be a private
clone with its own prefab identity and versioned ZDO metadata. A marker can be
invisible or use an approved private visual, but its semantic record must not depend
on recognizing a decorative mesh by name.

### Required affordance catalogue

| Group | Native or Living World evidence | Core meaning |
|---|---|---|
| Settlement | Maypole / Living World settlement anchor | Settlement identity, bounds, citizenship and permissions |
| Workshop | Workbench, stonecutter, artisan table, galdr table | Ambient crafting class 1; real work only with a job transaction |
| Smithy | Forge, black forge | Ambient crafting class 2; hammer pose and inspected attachment contract |
| Kitchen | Cauldron | Ambient crafting class 3; do not claim unverified stirring behavior beyond the native channel |
| Cooking | Wood and iron cooking stations | Interaction/observation point; no fabricated food or dedicated animation claim |
| Industry | Spinning wheel, kiln, smelter, blast furnace, windmill, refinery | Job/observation affordance only where safe; hazardous assets require exclusions |
| Social | Chair, bench, throne, table, fire gathering point | Reserved seat/rest/social arrangement with authored facing |
| Housing | Bed and dwelling marker | NPC-specific rest occupancy; never Player bed ownership, spawn point, or sleep voting |
| Storage | Chest/container marker | Permission and reservation boundary; ambient use never mutates inventory |
| Passage | Supported ordinary door/gate passage | Traversal affordance, never a destination; close only a door actually crossed when clear |
| Defense | Guard point, sentry point, outpost marker, archery position | Coverage/role affordance without creating hostility or targets |
| Building role | Kitchen, Smithy, Workshop, Barracks, Healing, Great Hall, Housing | Semantic building membership supplied by marker or village survey |

Known native evidence from the existing project research must be reused:

- `CraftingStation.m_useAnimation == 1`: workbench-class pose.
- `CraftingStation.m_useAnimation == 2`: forge-class pose.
- `CraftingStation.m_useAnimation == 3`: cauldron-class pose.
- Chair attachment channels include `attach_chair` and `attach_throne`.
- The native human controller contains sitting, chair, sleep, interaction, bow, and
  hammer-related clips, but a clip name alone is not permission to replay arbitrary
  animation events.
- A composed non-Player `Humanoid` cannot safely call unmodified Player chair/bed
  interaction logic. It needs an NPC-specific occupancy/attachment adapter.

## Affordance record and reservation contract

Each discovered or marked affordance should resolve to a compact record resembling:

- Stable affordance ID and network object identity.
- Settlement/building identity, if any.
- Affordance kind and activity class.
- World position, use position, authored facing, safe approach point, and radius.
- Capacity and current reservation token.
- Home/guest/free permissions.
- Ambient-only versus real-job capability.
- Required animation parameter/channel and a vetted event policy.
- Hazard, shelter, roof, fire, and accessibility facts where relevant.
- Last validation time and invalidation reason.

The registry must be bounded and shared. Do not give every NPC an independent
whole-scene scan. Survey nearby loaded pieces at a lower cadence, cache stable
records, invalidate them on destruction/unload/change, and stagger path checks.

An optional activity transaction is:

1. Context permits activity.
2. Select from a bounded prevalidated snapshot.
3. Reserve with an owner-authoritative token.
4. Commit one destination.
5. Approach through the sole Movement owner and native pathfinding.
6. Confirm arrival, continued permission, safety, and ownership.
7. Enter the vetted animation/pose through the required domains.
8. Dwell or complete.
9. Exit, detach, remove temporary props, release the reservation, and return to the
   correct companion/resident/hold behavior.

Danger, combat, leader departure, order changes, route failure, object destruction,
authority loss, or unloading must cancel this transaction cleanly. Marginally better
activities must not cause destination churn.

## Ambient action versus real work

The core must keep appearance separate from economy.

Ambient use may:

- Walk to a permitted station.
- Face it and play a vetted workstation pose.
- Sit, socialize, observe, eat/drink as a gesture, or stand watch.
- Make the town and companion feel alive.

Ambient use may not:

- Create an item, food, fuel, ammo, or skill gain.
- Consume or move inventory.
- Unlock recipes or progression.
- Claim a profession.
- Repair a structure.
- Access another player's locked storage.

Real work is a separate idempotent job transaction supplied by the settlement or
Reclamation layer. It must authorize resources, destination, inventory mutations,
output, completion, and accounting. Both ambient and real work may use the same
core affordance and animation adapter without conflating their side effects.

## Behavior in each governing state

### Follow in wilderness

Current traversal, formation, awareness, readiness, torch, environmental status,
and native combat behavior apply. Nearby permitted assets can support brief ambient
activity only when the leader is clearly occupied or resting and the NPC can cancel
promptly to follow.

### Follow inside own town

The person remains a companion but recognizes home permissions. When the leader is
moving, follow respectfully and avoid crowding. When the leader is busy, seated, or
stationary long enough, the NPC may reserve a nearby permitted home activity. It
does not resume a long resident shift while still under Follow.

### Follow inside a guest town

Use guest permissions. Sit, socialize, guard, observe, or perform explicitly allowed
help. Do not claim beds, jobs, chests, or citizenship and do not transfer home.

### Hold

Hold creates a bounded local anchor, not a frozen statue. The NPC may guard, sit,
use permitted nearby ambient affordances, shelter, sleep in its assigned home bed,
and defend itself/settlement. It must not follow the departing player unless Hold
is revoked. The same identity and leader relationship remain.

### Resident at home

Companion traversal is dormant. The person follows resident duty/rest/social cycles
using home permissions, assigned dwelling/bed, profession capabilities, and the
settlement workboard. Native combat still preempts resident life.

### Go Home

Keep the person loaded and use native navigation while practical. The destination
is the assigned home settlement, not merely the player's last location. Arrival
transitions to resident mode. Off-screen journey records are a later capability and
must never coexist with another live copy.

### Release Free

End companion engagement but keep the person. Free roaming must be bounded and
persisted. If released within an accepting settlement, admission is still explicit;
otherwise the NPC is not silently adopted.

### Migration and Exodus

Moving villagers is a first-class journey, not despawn-and-clone.

- Select an explicit source and destination settlement/maypole.
- Preserve each person's identity, inventory, equipment, skills, history, and
  relationships.
- Reserve destination capacity before departure.
- Record origin, destination, departure, route/progress, roster, and status.
- Loaded travel uses actual navigation and party/group cohesion.
- Arrival commits the home transfer exactly once.
- Cancellation or failure retains one authoritative person and a recoverable state.
- Unloaded, absent, delayed, or unseen never means dead.
- Do not render a traveller at the source and destination simultaneously.

This journey foundation is what later allows the Exodus story and the physical
movement of villagers between reclaimed towns.

## Integration into the current 1.0 kernel

Preserve the current exclusive-domain architecture. Extend it rather than adding
parallel Update/LateUpdate writers.

Recommended core additions:

- `NpcEngagement`, `NpcSpatialContext`, `NpcCitizenship`, and `NpcActivityState` as
  independent durable records.
- `SettlementAnchor` and a shared `SettlementRegistry`.
- `AffordanceRecord`, `AffordanceRegistry`, and owner-authoritative reservations.
- `ContextSense` that supplies cached settlement/home/guest/hold facts to the
  existing single kernel sense pass.
- `AmbientActivityBehavior` claiming the exact domains required by its current
  action.
- `ResidentActivityBehavior` for home duty/leisure using the same activity engine.
- `DoorPassageBehavior` or a passage sub-transaction owned by the current Movement
  behavior; never a competing path writer.
- `BedRestBehavior` using an NPC-specific reservation and attachment adapter.
- `MigrationBehavior` only after the durable journey record exists.

Do not make every facility a separate component with its own scan and movement
loop. The affordance/activity engine selects one action; specialized adapters
execute it.

Native combat remains the highest-priority all-domain behavior. No town, torch,
workstation, door, chair, bed, hold, or migration code may tick native writes while
combat owns those domains.

## Required corrections and clarifications before calling this 1.0 complete

1. The current `TorchPolicy` disables the torch in `Readiness`. Brian's intended
   rule is: a torch may remain during curiosity/attention/readiness at night and is
   doused on full combat commitment, stealth, swimming, or a stable light condition.
   Do not patch this independently; correct it within the consolidated ownership
   work and add a regression test.
2. `NpcContext` cannot remain a one-value `FieldCompanion` enum.
3. `lwparty` currently reasons only about loaded kernels. A durable party/order
   service must not create duplicates because an existing companion is unloaded or
   far away. Test-only replacement and production recruitment must be distinct.
4. `lwparty new` currently removes records by prefab rather than a fully scoped
   leader/party identity. It must never delete another player's people on a server.
5. Local UI facts such as inventory/map visibility are valid only for the locally
   controlled bound player. They must not be generalized to remote leaders.
6. Town/affordance discovery must be shared and bounded; it cannot multiply full
   collider, character, or path scans by population.

## Core and Reclamation boundary

### Living World C.O.R.E. owns

- Person identity and durable state dimensions.
- Companion, hold, return, release, resident, guest, and migration semantics.
- Recognition of maypoles, settlements, buildings, and usable affordances.
- Reservations and safe native animation/attachment adapters.
- Exclusive behavior/domain arbitration.
- Native combat preemption and clean activity interruption.
- Public provider contracts that allow Reclamation or another compatible town mod
  to register settlements, buildings, jobs, and permissions.

### Village Reclamation owns

- Discovering/restoring village structures and blueprints.
- Settlement creation story, steward/governance, population admission policy, and
  workboard/job generation.
- Real resource movement, production, repair, construction, food economy, and
  building maintenance.
- Supplying building roles and real-job permissions to core interfaces.
- Higher-level Exodus events and destination selection.

Reclamation consumes the core; it must not replace the person's controller. The
same core NPC must remain recognizable before, during, and after village work.

## One-branch 1.0 delivery sequence

Do not install intermediate feature fragments.

1. Preserve and review the first live 1.0.0 telemetry before modifying behavior.
2. Add the durable context/citizenship/order schema and migration from existing
   saved keys, with no visible behavior change.
3. Add shared settlement and affordance registries plus maypole recognition.
4. Integrate the existing offline companion activity policy into the kernel/domain
   scheduler; do not ship the policy DLL as a stacked plugin.
5. Add verified workbench/forge/cauldron ambient activity and chair/bench rest.
6. Add Hold and home/guest behavior.
7. Add NPC-specific bed rest and supported door passage.
8. Add explicit Go Home, Release Free, and Settle Here transactions.
9. Add the durable loaded migration foundation. Off-screen simulation may remain
   gated if it cannot be made identity-safe within this release.
10. Run all offline gates, install one candidate, and execute the acceptance matrix.

If a capability cannot meet ownership, persistence, and cleanup requirements, keep
its interface and state reserved but report it as gated. Do not insert a partial
writer into the live controller merely to show the animation once.

## Regression and build gates

Extend Claude's regression ledger and IL ownership gate. Required new cases include:

- Combat interrupts every activity and no activity writes during combat.
- Readiness does not douse a valid torch; committed combat does.
- Hold permits bounded life but does not follow or dismiss.
- Follow leaving town cancels optional activity and rejoins promptly.
- Home and guest contexts never silently exchange citizenship.
- Overlapping settlement bounds do not select by nearest center alone.
- One affordance cannot be reserved by two NPCs.
- Destroyed/unloaded affordance releases or expires safely.
- Failed routes do not create rapid reselection loops.
- Workstation animation cannot mutate inventory or create output.
- Bed use cannot change player ownership, respawn, or sleep voting.
- Door logic acts only on a door actually used and never closes through an actor.
- Missing/unloaded NPC does not trigger replacement or duplication.
- `lwparty` and replacement operations are scoped to the correct leader.
- Migration preserves one identity and one authoritative representation.
- Save/load during every transaction either resumes or safely cancels without a
  duplicate person, reservation, or item transfer.
- Shared scan/path budgets remain bounded as population increases.

The analyzer must report unexercised scenarios as unexercised, never passed.

## Live acceptance matrix

Run each state distinctly and preserve telemetry/video commentary:

1. Wilderness Follow: walking, sprinting, stopping, formation, stow/draw, darkness,
   crouch, rain, swimming, and natural combat.
2. Companion in town: leader walks, uses menus, crafts, sits, sleeps, and leaves.
3. Guest town: respectful ambient behavior without home/job/bed theft.
4. Hold: player departs and returns while the NPC guards, rests, shelters, and
   defends within the hold area.
5. Resident home: maypole recognition, assigned activity, leisure, bed, combat
   interruption, and resumption.
6. Doors: approach, open, cross, clear, optional close, failed/dead-end path, and
   player manual door changes.
7. Explicit Go Home, Release Free, and Settle Here transitions.
8. Loaded group relocation between two connected test areas while preserving every
   person's identity and equipment.
9. Save/reload and unload/reload during hold, activity, return, and migration.
10. Multiplayer/server authority before any claim of Xbox-compatible behavior.

## Accessible sources and evidence

Claude is authorized to inspect and use the following local project material for
this work. Synced project files under the project mirror's `sources` directory are
read-only reference material.

- Current Claude source/package:
  `C:\Users\scruf\Documents\LivingWorld-CORE-NPC`
- Current branch bundle:
  `C:\Users\scruf\Documents\LivingWorld-CORE-NPC\lwcore-npc-1.0.0.bundle`
- Main repository checkout and history:
  `C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\github-sync-forest-children`
- Existing behavior, activity, population, building-marker, door, animation, lab,
  and village research:
  `C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage`
- Read-only ChatGPT project mirror:
  `C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\sources`
- Active test profile:
  `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1`
- Active logs:
  `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1\BepInEx\LogOutput.log`
- Active telemetry:
  `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1\BepInEx\LivingWorldTelemetry`
- Souls reference profile:
  `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Souls of the Land\BepInEx`
- OBS evidence:
  `Y:\OBS OUTPUT\OBS OUTPUT`

Priority references inside `work\LivingVillage` include:

- `COMPANION-BEHAVIOR-CONTRACT-20261014.txt`
- `COMPANION-ROADMAP-AND-COMPARISON-20261014.txt`
- `companion-activities-v1\INTEGRATION.txt`
- `companion-activities-v1\src\CompanionActivities.cs`
- `companion-population-v1\README.md`
- `core\BUILDING-MARKERS.txt`
- `companion-lab-expansion-v1\START-HERE.txt`
- the inspected legacy ambient/workstation adapters and their tests under
  `human-npc-0.3.13-follow-recovery` and `human-npc-0.3.16-companion-tactics`

## Required response from Claude

Before changing the installed profile, return:

1. The proposed final state dimensions and persistence keys.
2. The exact core interfaces for settlements, affordances, reservations, jobs, and
   Reclamation providers.
3. The behavior priority/domain table including town, hold, doors, beds, and
   migration.
4. The definitive 1.0 inclusion list and any explicitly gated capability.
5. The migration plan from 1.0.0 and older `lwc_human_v2_*` records.
6. The expanded regression ledger and live acceptance plan.
7. A statement of which code is reused unchanged, which is rewritten, and which
   older code is discarded.

The implementation must be judged by coherent state transitions, native ownership,
identity safety, bounded work, and live evidence—not by the number of features
claimed or animations briefly observed.

