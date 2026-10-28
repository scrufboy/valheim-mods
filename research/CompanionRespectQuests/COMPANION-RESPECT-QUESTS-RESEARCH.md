# Small earned-respect companion requests

Date: 2026-10-28
Scope: research and design only; no game profile, runtime, or module changes

## Executive decision

Living World C.O.R.E. can support small, believable requests that let a future
companion judge the player by a shared deed. The safest first release is **not a
generic quest generator and not a private raid system**. It is a small
server-authoritative request ledger that binds an NPC to a real, currently
validated fact in the nearby world: a live untamed animal, a currently active
vanilla event, a loaded dungeon entrance, or a loaded hostile location.

The MVP should ship in this order:

1. **The Hunter's Measure** — one validated wild boar, or a tiny unbound hunt
   count when no stable individual identity is available.
2. **Hold the Line** — opportunistic credit during a naturally active vanilla
   raid; never start a raid merely to satisfy the request.
3. **The Black Door** — accompany the NPC to a real nearby Burial Chamber
   threshold, then let the player enter alone and return; add Surtling Core proof
   only after item-acquisition provenance is proven.
4. **Embers for the Kiln** — a loaded, progression-appropriate Surtling target
   near an already discovered Swamp/FireHole; never manufacture an infinite core
   farm.
5. **The Last Door of the Village** — a small, bounded action at a real loaded
   `WoodVillage1`, offered only after Draugr/Swamp knowledge. Do not present an
   entire village reclamation as a casual favour.

The first two require no authored enemies. The third is a travel/threshold test.
The last two must remain gated until their world facts and progression boundaries
are demonstrated in Valheim 1.0.x.

## Source and version baseline

This report targets the currently maintained 1.0 line, not the pre-launch
0.21x/0.22x game. Iron Gate released Valheim 1.0 on 2026-09-09 and 1.0.20 on
2026-10-15. The latter is the latest official patch located during this review.
The official 1.0 notes matter because they include new locations, events, and
mechanics, while the 1.0.16 notes specifically fixed excessive spawning,
especially during raids. Any design that assumes an older spawn cadence is
therefore unsafe.

Maintained mod baselines found on 2026-10-28:

| Component | Current evidence | Relevance |
| --- | --- | --- |
| Valheim | Official 1.0.20 patch, 2026-10-15 | Runtime target for proof |
| Jotunn | 2.30.2; 2.30.0 updated most systems for Valheim 1.0.7 | Safe high-level registration/localization/RPC layer, but not a quest engine |
| ASharpPen Spawn That | 1.2.20; source constant and package agree | Spawn configuration, local/world spawner matching, ZDO template tag |
| ASharpPen Custom Raids | 1.8.2; source constant and package agree | Raid definition/conditions/spawns, server config sync, event tagging |

Sources:

- [Iron Gate: Valheim 1.0 has arrived](https://www.valheim.com/news/valheim-1-0-has-arrived-/)
- [Iron Gate: patch 1.0.16](https://www.valheim.com/news/patch-1-0-16/)
- [Iron Gate: patch 1.0.20](https://valheim.com/news/patch-1-0-20/)
- [Jotunn 2.30.x changelog](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/changelog/)
- [Spawn That 1.2.20 package/changelog](https://thunderstore.io/c/valheim/p/ASharpPen/Spawn_That/changelog/)
- [Custom Raids 1.8.2 package](https://thunderstore.io/c/valheim/p/ASharpPen/Custom_Raids/)

### The “asharppen” reference is verified, with a spelling distinction

The author/Thunderstore namespace is **ASharpPen** and the two relevant projects
are separate:

- [ASharpPen/Valheim.SpawnThat](https://github.com/ASharpPen/Valheim.SpawnThat)
  at reviewed commit
  [`dc1e793`](https://github.com/ASharpPen/Valheim.SpawnThat/commit/dc1e7938635ffe19b6c3a889c7d2d268f42f367c),
  version 1.2.20;
- [ASharpPen/Valheim.CustomRaids](https://github.com/ASharpPen/Valheim.CustomRaids)
  at reviewed commit
  [`64f2b0f`](https://github.com/ASharpPen/Valheim.CustomRaids/commit/64f2b0f706a17d006d39edb53bf041e1f7d6c84f),
  version 1.8.2.

`asharppen.valheim.spawn_that` is the lowercase BepInEx plugin identifier used
by Spawn That. It is not the author's display spelling and it is not the Custom
Raids identifier. The local Living World references are internally consistent:
`VillageReclaim.cs` depends on the Spawn That plugin ID, while
`LivingWorldCORETooler/manifest.json` names `ASharpPen-Custom_Raids-1.8.2`.

Custom Raids also declares Spawn That as a *soft* dependency in
[`CustomRaidPlugin.cs`](https://github.com/ASharpPen/Valheim.CustomRaids/blob/64f2b0f706a17d006d39edb53bf041e1f7d6c84f/Valheim.CustomRaids/Valheim.CustomRaids/CustomRaidPlugin.cs).
That relationship does not make the two products interchangeable.

## What vanilla already provides

### 1. Seeded world locations and loaded world truth

Valheim divides the world into 64 by 64 metre zones. A zone can have one
location and multiple vegetation instances. Jotunn exposes the vanilla timing
through `ZoneManager.OnVanillaLocationsAvailable` and documents that locations
must be handled through the post-SoftReference location model rather than by
directly inserting old prefab references. See the maintained
[Jotunn zones/location documentation](https://valheim-modding.github.io/Jotunn/tutorials/zones.html).

Vanilla `ZoneSystem` supplies location instances and location lookup such as
`FindClosestLocation`. Living World's own `VillageReclaim.cs` already proves
that `FindClosestLocation("WoodVillage1", position, out instance)` works in the
current project stack. It also proves an important limitation: the code follows
the lookup with distance, live-character, `SpawnArea`, player-presence, owner,
and quiet-time checks. The lookup by itself does not establish that a target is
loaded, uncleared, reachable, or safe.

**Design rule:** a seeded location is a candidate fact. A request may become
offerable only after a loaded-zone validator confirms the relevant entrance,
spawner, creature, or location proxy and derives a reachable interaction point.
Do not reveal an unexplored dungeon or village merely because seed data knows it
exists.

### 2. World, local, location, and dungeon spawners

Vanilla has materially different spawn sources:

- `SpawnSystem.SpawnData` world spawns around eligible players;
- `CreatureSpawner` local spawns authored into locations/rooms;
- `SpawnArea` sources such as nests/piles that select from a list;
- event spawns supplied by the active `RandomEvent`.

This distinction matters to quest identity. “Kill a boar” found through the
world spawner is not the same lifecycle as a boar attached to
`Runestone_Boars`, and neither behaves like a raid spawn. A request must record
what kind of target it accepted and never infer completion just because an
entity of the same prefab died elsewhere.

Spawn That's public documentation confirms these categories in maintained
source. Its
[local-spawner configuration](https://github.com/ASharpPen/Valheim.SpawnThat/blob/dc1e7938635ffe19b6c3a889c7d2d268f42f367c/src/SpawnThat.Docs/configs/config-types/local-spawner-config.md)
matches a location or dungeon room plus prefab, can override only an existing
local spawner, and cannot distinguish multiple same-prefab spawners in the same
named room/location. Its
[world-spawner configuration](https://github.com/ASharpPen/Valheim.SpawnThat/blob/dc1e7938635ffe19b6c3a889c7d2d268f42f367c/src/SpawnThat.Docs/configs/config-types/world-spawner-config.md)
supports biome, global keys, time, environment, distance, nearby prefab, and
location conditions.

### 3. Boars and ordinary hunting

Vanilla already supplies wild Boars in the Meadows, local boar runestone
encounters, faction/AI, damage ownership, death, drops, taming, and networked
ZDO identity. No custom boar is needed for a respect test.

The quest layer must nevertheless distinguish:

- alive from dead;
- wild from tamed;
- ordinary world/local animal from a mod-authored quest entity;
- a specific ZDO target from an unbound “one nearby boar” objective;
- player/party contribution from an environmental or third-party death.

Do not count a tamed animal. Do not direct the companion to attack passive
wildlife before the player accepts and deliberately engages. Do not grant
respect if the chosen animal dies unloaded, by terrain, by another biome's mob,
or before the party reaches it. If exact ZDO binding proves brittle across zone
unload, downgrade explicitly to a short bounded hunt contract: after acceptance,
one eligible wild boar death within the request radius, while requester and
player are both present, with direct player/party damage evidence.

### 4. Raids/events

Vanilla owns the event clock, eligibility, location, synchronization, duration,
and event spawn list through `RandEventSystem`, `RandomEvent`, and
`SpawnSystem`. Boss/creature progression keys influence which events are
eligible. A raid is therefore a poor thing for a small request to summon, but an
excellent world fact to recognize opportunistically.

The inspectable Custom Raids source patches
[`RandEventSystem.GetPossibleRandomEvents`](https://github.com/ASharpPen/Valheim.CustomRaids/blob/64f2b0f706a17d006d39edb53bf041e1f7d6c84f/Valheim.CustomRaids/Valheim.CustomRaids/Patches/RaidConditionsPatch.cs)
and builds ordinary `RandomEvent`/`SpawnSystem.SpawnData` records in
[`RaidConfigManager.cs`](https://github.com/ASharpPen/Valheim.CustomRaids/blob/64f2b0f706a17d006d39edb53bf041e1f7d6c84f/Valheim.CustomRaids/Valheim.CustomRaids/Raids/Managers/RaidConfigManager.cs).
Its author documentation states that the host/dedicated server decides which
raid applies, broadcasts the raid name, and the client responsible for the zone
manages spawning. This is strong evidence that completion cannot safely be a
single local-client boolean.

**Design rule:** observe the active event; do not take ownership of it. Credit
requires the requester to be in the active event area, alive, and loaded; the
player must be present and contribute to at least one eligible event-spawned
hostile. Complete when the event naturally ends or a bounded “stand together”
timer succeeds. Never mark every creature with the raid prefab as a quest mob.

### 5. Dungeon access and Surtling Cores

Burial Chambers are generated interior locations entered through a teleport
transition. The important companion limitation is architectural: dungeon doors
call the character teleport path, but vanilla's useful implementation is on
`Player`; ordinary `Character` roots do not get player portal/dungeon travel
for free. An inspectable 1.0 mechanics audit documents this flow and the
`Character.TeleportTo`/`Player.TeleportTo` split in its
[portal and teleport analysis](https://github.com/Blaakan/MC_Valheim/blob/main/docs/game/farming-cooking.md#11-portals-teleports-and-creatures).

Therefore an NPC must not be ordered through a Burial Chamber door until Living
World has its separate identity-preserving instanced-interior travel gate. The
safe small request is “escort me to the black door; enter and return,” with the
NPC waiting at an exterior anchor. This is lore-coherent and technically honest.

Surtling Cores are normal inventory items, not unique quest objects. Merely
checking `Inventory.CountItems` permits stockpiled cores, server handoffs, chest
loops, and repeated acceptance. A core request requires a baseline inventory
snapshot plus acquisition evidence after acceptance. Even that proves only a
new core entered inventory, not that it came from the nominated chamber, unless
the runtime can correlate the pickup/drop ZDO or the target interior session.

**MVP rule:** do not require cores. For a later revision, require no more than
one newly acquired core, do not consume it, complete once per NPC/world, and
bind credit to the nominated chamber session. Never reset or refill the dungeon.

### 6. Portal constraints

Vanilla portals teleport the local player and check world portal rules plus
inventory teleportability. They do not transport following creatures. The same
inspectable 1.0 audit shows `TeleportWorldTrigger` resolving a `Player`, player
inventory restrictions, server-side portal pairing, and no creature movement.

Requests must consequently use one of three explicit policies:

1. **No portal while active** for target-bound shared hunts and defence;
2. **Suspend and resume** when both NPC and target remain safely persistent;
3. **Fail without penalty** if travel invalidates the world fact.

Never teleport the NPC silently to the player, never clone it at the destination,
and never count an offscreen target death while separated by a portal. The
request ledger must bind the persistent NPC identity, not a transient Unity
instance.

### 7. Draugr villages

The project's own current `VillageReclaim.cs` provides direct runtime evidence
for vanilla `WoodVillage1`, local `CreatureSpawner` Draugr, nearby
`Spawner_DraugrPile` `SpawnArea` components, world Draugr filtering, ZDO owner
checks, player-presence checks, and a 180-second quiet proof. It also marks
world-spawned Draugr separately so they cannot be confused with village
occupants.

This is exactly why a generic “kill three Draugr near the village” request is
unsafe. Nearby world Draugr, local village Draugr, pile spawns, Custom Raids
spawns, and other mods may all share a prefab name. A later request must use
location membership plus source tag/known spawner relationship, and must not
compete with Village Reclaim's own clear-state authority.

Do not offer the village request before the player has legitimate Draugr/Swamp
knowledge. A Meadows coordinate does not make Draugr a Meadows-tier spoiler.

## What existing mods demonstrate

### Spawn That: configuration and source tagging, not quest ownership

Spawn That 1.2.20 demonstrates:

- maintained Valheim 1.0.x compilation;
- code API and file-config merging;
- server-to-client in-memory configuration sync;
- world, local, `SpawnArea`, dungeon-room, and location-aware configuration;
- spawn conditions including biome, keys, time, distance, items, and nearby
  prefabs;
- `TemplateId`, written as `spawn_template_id` on the spawned entity ZDO.

The implementation of that last feature is inspectable in
[`ModifierSetTemplateId.cs`](https://github.com/ASharpPen/Valheim.SpawnThat/blob/dc1e7938635ffe19b6c3a889c7d2d268f42f367c/src/SpawnThat/Options/Modifiers/ModifierSetTemplateId.cs).
It is useful for provenance when Living World intentionally authors a fallback
micro-encounter.

It does **not** demonstrate a durable per-NPC request, participant contribution,
fair cancellation, NPC mortality policy, or exact seeded target discovery. Its
world templates are recurring spawner rules by default, which is the wrong
primitive for a one-shot favour unless Living World supplies strict activation,
identity, count, cleanup, and completion state.

### Custom Raids: event construction and synchronization, not a small errand

Custom Raids 1.8.2 demonstrates:

- adding and overriding `RandomEvent` records;
- configuring event spawns and conditions;
- supplemental config files;
- host/server event selection and client zone spawning;
- `TemplateId` support for event-spawn ZDO provenance;
- explicit client-and-server installation for general use.

Its
[`RaidEventConfigurationFile.cs`](https://github.com/ASharpPen/Valheim.CustomRaids/blob/64f2b0f706a17d006d39edb53bf041e1f7d6c84f/Valheim.CustomRaids/Valheim.CustomRaids/Configuration/ConfigTypes/RaidEventConfigurationFile.cs)
describes `TemplateId` and the event conditions. Its README warns that spawning
is ultimately managed by the client in charge of the zone.

Use Custom Raids for authored public events and development trials, not as the
default mechanism behind a private “earn my respect” conversation. A forced
raid changes the world for all nearby players, can damage bases and NPCs, may
interact with the vanilla raid clock, and is disproportionate to a small favour.

### Jotunn: maintained lifecycle helpers

Jotunn 2.30.2 is a suitable dependency for registration, localization,
network-compatible RPCs, prefab access, and location lifecycle callbacks. Its
1.0 releases also show why this system needs a narrow adapter layer: Valheim 1.0
changed systems such as build categories and SoftReference assets. Directly
holding old location prefab references across loads is not a stable contract.

### Randy Knapp's Haldor quests: Epic Loot Adventure Mode, verified

The remembered Haldor feature is **Epic Loot's Adventure Mode**, not an
ASharpPen raid feature. The reviewed current author source is
[RandyKnapp/ValheimMods](https://github.com/RandyKnapp/ValheimMods) commit
[`79498d0`](https://github.com/RandyKnapp/ValheimMods/commit/79498d09d56e6d14be3c20ac8d2083edf34bd76d)
(Epic Loot 0.15.4, 2026-10-27). Epic Loot's configuration describes Adventure
Mode as the bundle of secret stash, gambling, treasure maps, and bounties. The
older author wiki gives the same player-facing summary:
[“What is Adventure mode?”](https://github.com/RandyKnapp/ValheimMods/wiki/What-is-Adventure-mode%3F).

The exact current behavior matters:

- Epic Loot patches `StoreGui.Show` and opens its extra merchant panel **only**
  when the trader's localized identity is Haldor (`$npc_haldor`); Hildir receives
  a different temper panel. See
  [`StoreGui_Patch.cs`](https://github.com/RandyKnapp/ValheimMods/blob/79498d09d56e6d14be3c20ac8d2083edf34bd76d/EpicLoot/src/Adventure/StoreGui_Patch.cs).
- Available bounty targets come from configured creature prefabs. The current
  code filters out missing prefabs, undiscovered biomes, and optionally biomes
  not unlocked by boss keys, then selects a target per eligible biome on a
  refresh interval. See
  [`Bounties.cs`](https://github.com/RandyKnapp/ValheimMods/blob/79498d09d56e6d14be3c20ac8d2083edf34bd76d/EpicLoot/src/Adventure/feature/Bounties.cs).
- On acceptance, Epic Loot does **not** choose an existing troll, boss, camp,
  dungeon, or other real structure. `BountyLocationEarlyCache` samples a
  seed-derived `WorldBiomeIndex`, chooses a biome point in a configured radial
  band, and explicitly operates with “nothing instantiated.” It calls the point
  the centre of a map circle. See
  [`BountyLocationEarlyCache.cs`](https://github.com/RandyKnapp/ValheimMods/blob/79498d09d56e6d14be3c20ac8d2083edf34bd76d/EpicLoot/src/Adventure/feature/BountyLocationEarlyCache.cs).
- Epic Loot then creates a persistent `AdventureSpawnController`. When a zone
  owner loads the area, the controller samples actual terrain inside the map
  circle, checking loaded zone, biome, floor, obstruction, water/lava, and ward
  conditions, then **spawns a configured main bounty creature and adds**. This
  is an authored micro-encounter, not reuse of a naturally seeded monster. See
  [`AdventureSpawnController.cs`](https://github.com/RandyKnapp/ValheimMods/blob/79498d09d56e6d14be3c20ac8d2083edf34bd76d/EpicLoot/src/Adventure/bounties/AdventureSpawnController.cs).
- Treasure maps use the same seed-biome point selection and persistent
  controller, but create an authored treasure chest rather than selecting an
  existing chest in a real structure. See
  [`TreasureMaps.cs`](https://github.com/RandyKnapp/ValheimMods/blob/79498d09d56e6d14be3c20ac8d2083edf34bd76d/EpicLoot/src/Adventure/feature/TreasureMaps.cs).
- The accepted bounty/treasure records and area pins are saved per player. The
  current API exposes active bounty centres, radius, main target/add counts,
  bounty-completed and target-slain events, and pin types. See the author-maintained
  [Epic Loot integration API](https://github.com/RandyKnapp/ValheimMods/blob/79498d09d56e6d14be3c20ac8d2083edf34bd76d/EpicLoot/docs/API.md#bounties).

So the user's recollection is substantively right—Haldor offers randomized maps
and bounty jobs, including named stronger targets with escorts—but the world
model is different from the recommended natural-fact respect request. Epic Loot
chooses a valid seed-derived biome area and authors the chest/encounter when the
area loads. It does not normally discover a real pre-existing structure and put
a boss into it.

#### What Living World may adapt conceptually

The following patterns are strong and generic enough to adapt independently:

- filter offers by positive biome discovery and configured progression;
- deterministic refresh windows instead of rerolling on every conversation;
- save the accepted request before creating any encounter, so failure cannot
  leave an orphan spawn;
- use an imprecise search area rather than a pin directly on the target;
- defer final ground placement until the responsible zone is loaded;
- persist a one-shot controller/ledger and make spawn placement idempotent;
- expose explicit completion events and inspectable active-request state;
- log why no offer/location/spawn was available instead of silently failing.

Living World should not copy Epic Loot code, JSON data, names, target-name
fragments, icons, map-circle assets, Haldor UI, bounty tokens, rewards, or
encounter text. More importantly, it should not silently adopt the same product
shape. Epic Loot is a repeatable merchant economy; companion respect is a
personal relationship beat. Natural nearby facts should remain the default, and
an authored micro-encounter should remain a disclosed, tightly bounded fallback.

#### Map knowledge implications

Epic Loot correctly filters offers to biomes the player has discovered, but its
seed index can choose an **unexplored point** within that known biome and then
show an area on the map. That is appropriate for a purchased treasure map; it is
not automatically appropriate for a nearby companion's observation.

Living World should separate three knowledge levels:

1. **Known kind:** the player knows the biome/creature exists, so dialogue may
   name it.
2. **Known region:** the player's explored-map or recent physical travel supports
   “near here.” A vague bearing/area cue is allowed.
3. **Validated local fact:** the target is loaded or the location is already
   discovered and validated. A personal respect request may bind it.

Default companion requests should require level 3. A jarl or notice-board
expedition may intentionally operate at level 1 or 2 and reveal a broad search
area as the value of the intelligence. No-map worlds need a diegetic alternative
(bearing, landmark description, raven message, map-table entry, or companion
guidance); the backend must not assume a visible `Minimap` pin.

#### Haldor, message board, jarl, and companion are different quest surfaces

| Surface | Best use | Offer model | Reward/relationship | World targeting |
| --- | --- | --- | --- | --- |
| Haldor-style merchant | Repeatable economy and exploration purchase | Refreshed catalogue | Coins/tokens/items | May reveal a new broad region and author content |
| Settlement message board | Public civic work anyone may take | Shared posted jobs, expiry/reservation | Settlement standing/material reward | Existing settlement needs or public hazards |
| Jarl | Regional authority, campaign, law, war | Deliberate commissioned mission | Faction rank/access/consequence | Known strategic sites; larger authored encounters allowed |
| Individual future companion | Personal judgment and trust | One contextual spoken request | Respect/recruitment memory | Nearby validated fact; smallest possible intervention |

A notice board is a good **view** over the shared request ledger, but it must not
turn a companion's private test into a fungible public contract. Likewise a jarl
can use the same durable state machine, contribution records, and progression
policy, yet should have separate issuer type, scope, map-intelligence permission,
reward transaction, multiplayer reservation, and failure consequences.

The clean architecture is one request kernel with four policy/front-end
profiles—not one Haldor bounty system reskinned for every social role.

### What Living World itself must implement

Neither vanilla nor the reviewed mods supplies the required semantic layer.
Living World must own:

1. **Request definition** — ID, lore text, biome/progression policy, radius,
   timeout, repeat policy, and failure policy.
2. **Offer validation** — requester state, active player, loaded target fact,
   safe reachable anchor, and no conflicting request.
3. **Persistent binding** — world UID, NPC persistent ID, accepting player ID,
   target kind and stable target identity when available.
4. **Authority** — server/owner validates transitions and writes state; clients
   request and display but do not award respect.
5. **Contribution ledger** — eligible damage, presence, target source, event
   membership, and requester survival.
6. **Respect ledger** — idempotent award transaction, once-only key, and audit
   reason.
7. **Unload/portal/restart behavior** — suspend, resume, or fair failure without
   offscreen simulation.
8. **Safe cleanup** — only Living World tagged fallback entities, never vanilla
   wildlife or event mobs.
9. **Compatibility** — detect Spawn That/Custom Raids tags when present but do
   not hard-depend on them for vanilla requests.

## Proposed runtime contract

### Candidate → validated fact → bound request

Use a three-stage pipeline:

1. **Candidate scan (cheap):** query nearby loaded characters and known location
   instances; do not mutate anything.
2. **Fact validation (strict):** confirm same zone context, prefab/type, alive,
   faction/tame state, progression permission, distance, ground/entrance anchor,
   requester path test where applicable, and current network ownership.
3. **Acceptance binding (authoritative):** after the player accepts, write one
   request record and only then attach observation hooks/tags.

Suggested durable record:

```text
schemaVersion
worldUid
requestId
requestInstanceId
npcPersistentId
acceptingPlayerId
state = offered|active|suspended|readyToTurnIn|completed|failed|expired
targetKind = characterZdo|location|randomEvent|authoredEncounter
targetZdoid / locationHash+zone / eventInstanceFingerprint
acceptedWorldTime
lastValidatedWorldTime
contributionFlags
respectTransactionId
```

Do not store a Unity instance ID or `GameObject` reference as durable identity.
Do not use prefab name alone as target identity.

### Safe mission targeting rules

A target is valid only when all applicable rules pass:

- player and requester are loaded and within the request's presence envelope;
- target is loaded, alive, not tamed, not player faction, and not a Living World
  companion/resident;
- target is not already reserved by another request;
- target source is known enough for the mission (ZDO, event tag, or location
  membership);
- the request does not reveal a biome, creature, material, or location the
  progression policy says the player cannot know;
- an exterior anchor is on stable ground, outside water/fire/base exclusion,
  outside an entrance trigger, and reachable by the requester;
- owner/server revalidates at acceptance and completion;
- absence or ambiguity means “no offer,” not “guess and spawn.”

For a character request, subscribe to damage/death while loaded and retain a
small authoritative contribution summary. On unload, suspend. Do not run remote
damage, remote AI, or an elapsed-time death roll.

### Authored micro-encounter fallback

Fallback is justified only when the fiction calls for a particular immediate
test and no valid natural fact exists. It is not justified merely to make an
offer appear every time.

If eventually enabled:

- spawn only after explicit acceptance and while accepting player and requester
  are present;
- use one to three progression-appropriate vanilla creatures at most;
- choose a sampled ground point in a visible annulus, validate biome, altitude,
  slope, water, base exclusion, collision clearance, and navigation reachability;
- attach Living World request ID and origin tags to the ZDO; optionally also use
  Spawn That's/Custom Raids' `spawn_template_id` convention for interoperability;
- set no recurring world-spawner rule and no loot multiplier;
- permit normal drops only once; record completion so reload cannot recreate the
  encounter;
- pause or cleanly cancel before combat if requester/player leaves; once combat
  begins, never delete a living hostile in view;
- never resolve combat or death offscreen;
- never place enemies inside a player base, dungeon, portal exit, occupied NPC
  settlement, or another active event;
- never use higher-biome enemies or names as a surprise.

The MVP should ship with this facility disabled.

## Five mission specifications

### 1. The Hunter's Measure — ranked MVP #1

**Fiction.** A hunter or wary wanderer asks the player to show patience and a
clean hand: bring down one wild boar nearby, together, without slaughtering a
pen.

**Offer gate.** Meadows knowledge is implicit; requester is exterior and calm;
at least one loaded, live, untamed wild `Boar` exists within a conservative
radius (for example 40–120 m); no player-base/tame context surrounds it; the
requester has an exterior reachable follow anchor.

**Binding.** Prefer the boar's ZDOID. If live proof shows ordinary wildlife
identity cannot survive the required unload boundary reliably, bind a hunt
envelope instead: one eligible boar after acceptance, within radius, while both
participants remain present.

**Credit.** Player or bound party deals meaningful direct damage; target dies
while loaded; requester remains alive and nearby. Environmental death alone does
not count. Companion-only killing without player participation does not count.

**Failure.** Target wanders/unloads or another actor kills it: release without
respect loss and offer a new natural target later. Portal: suspend/fail fairly,
never remote-complete.

**Anti-exploit.** Tamed boars excluded; one award per NPC; no spawned replacement;
no drop changes; no count from a boar killed before acceptance.

**Why first.** It exercises target validation, contribution, persistent NPC
binding, and idempotent reward with the smallest world impact.

### 2. Hold the Line — ranked MVP #2

**Fiction.** When a genuine raid starts near the party, the NPC does not issue a
menu quest. It makes an in-world challenge: stand your ground with me.

**Offer gate.** A vanilla/current registered `RandomEvent` is already active;
player and requester are in its actual area; event is progression-valid because
vanilla already selected it; requester is combat-capable. No event is forced.

**Binding.** Record event name plus start/world time, centre, and a local
instance fingerprint. Where a spawn tag exists, use it for attribution; do not
assume every same-prefab creature is an event spawn.

**Credit.** Both remain in the area for a bounded period or until natural event
end; player contributes to at least one eligible hostile; requester survives.
The completion moment is server/owner validated.

**Failure.** Leaving, requester death, event cancellation, or restart ambiguity
ends the attempt without reputation penalty. Do not keep the raid alive for the
quest.

**Anti-exploit.** Once per NPC and optionally once per event family; no forced
Custom Raid; no extra enemies or loot; no credit for joining at the final second.

**Why second.** It reuses a real dramatic world event and proves multiplayer
authority, while adding no spawn pressure after the 1.0.16 raid-spawn fix.

### 3. The Black Door — ranked MVP #3

**Fiction.** A cautious NPC has found an old Black Forest burial door but will
not cross it. The player earns respect by reaching it together, entering, and
returning alive.

**Offer gate.** Black Forest knowledge is established by an explicit player
fact (visited biome, known item, or configured progression key); a nearby
`Crypt2`/current Burial Chamber location candidate is already discovered or in
a loaded explored zone; exterior entrance and wait anchor validate; no active
event/combat.

**Binding.** Location stable identity (location hash/name + zone coordinate) and
exterior anchor. Never bind only the nearest map coordinate.

**Credit.** Requester reaches and waits at exterior anchor; accepting player
uses that dungeon entrance, spends a minimum bounded time inside, and returns to
the same exterior/requester alive. This proves nerve without pretending the NPC
can use player-only dungeon teleport.

**Optional later proof.** One newly acquired Surtling Core during the bound
interior session, only after pickup provenance is proven. Do not consume it.

**Failure.** If the location is already invalid/cleared or the wait anchor is
unsafe, do not offer. Portal or different exit cannot complete it. Requester does
not enter, clone, or teleport.

**Anti-exploit.** Once per location/NPC; minimum interior time; same entrance
return; no dungeon reset, refill, or core spawn.

### 4. Embers for the Kiln — ranked MVP #4

**Fiction.** A smith/survivor wants proof that the player can face living flame,
not a shopping-list delivery.

**Offer gate.** Swamp and Surtling knowledge are established; a live hostile
Surtling is loaded nearby or an already discovered, loaded `FireHole` context is
validated; request is exterior; no base or portal exit overlaps the fight.

**Binding.** One live Surtling ZDO, never the FireHole as a recurring kill
contract.

**Credit.** Same direct contribution and presence rules as the boar, with combat
appropriate to the known progression tier.

**Failure.** Water/environment kills without contribution do not count. Target
unload is a fair retry, not an offscreen resolution.

**Anti-exploit.** One target and one award; no “kill N” loop; no Surtling Core
requirement; no water-farm construction incentive; never add or accelerate a
FireHole spawner.

**Why late.** Surtling environmental deaths and repeatable FireHole farming make
provenance and exploit resistance more important than for boars.

### 5. The Last Door of the Village — ranked MVP #5

**Fiction.** At a known Draugr village, the NPC asks for one bounded act—clear a
specific outer house/approach and return—rather than “reclaim the village.”

**Offer gate.** Player has explicit Draugr/Swamp knowledge; a `WoodVillage1` is
loaded; the chosen sub-area has live village-associated hostiles or a pile;
Village Reclaim has not marked the whole location cleared; requester can reach a
safe perimeter anchor; no raid is active.

**Binding.** Village location identity plus a bounded sub-area and exact loaded
target ZDO(s) whose source/location membership is proven. World Draugr marked by
the existing `pop_reclaim_world_draugr` convention are excluded.

**Credit.** Player and requester remain present; player contributes to the exact
bound hostiles; sub-area stays quiet for a short loaded observation window.
Completion must not write Village Reclaim's `pop_reclaim_v1_cleared` key.

**Failure.** Any source ambiguity, unload, or competing village-state transition
cancels safely. Never delete Draugr, disable piles, or pronounce the settlement
reclaimed.

**Anti-exploit.** Once per NPC/village; exact targets, not prefab counts; no
respawner alteration; no Custom Raid overlay; no reward for unrelated world or
raid Draugr.

**Why last.** It crosses another Living World module's state boundary and has
the most spawn-source ambiguity.

## Progression and spoiler policy

The offer system must use positive knowledge, not a guessed boss order. A server
may use world-based or player-based progression, and modpacks can alter keys.

Recommended positive evidence, strongest first:

1. explicit player discovery/known-text/biome record;
2. player-specific keys when player-based progression is active;
3. world global keys only when the server actually uses world progression;
4. configured compatibility adapter for progression mods;
5. never infer knowledge solely from geographic proximity.

Mission text and UI must remain generic until the evidence is present. A nearby
Meadows Draugr village does not authorize the words Draugr, Swamp, entrails, or
Surtling. A seed-known Burial Chamber outside explored space must not place a
marker or compass hint.

## API and runtime feasibility for 1.0.x

### Recommended implementation boundary

Create a dedicated request module behind the existing Living World actor and
companion interfaces. It should depend on the Living World stable identity and
combat observation contracts, plus Jotunn 2.30.2 for supported lifecycle,
localization, and networking helpers. Use direct Valheim/Harmony adapters only
where no stable Jotunn event exists, and isolate every patch behind a versioned
adapter.

Likely read/observe surfaces:

- `ZoneSystem` location instances / `FindClosestLocation`;
- `Character.GetAllCharacters()` for loaded candidates only;
- `ZNetView`, ZDOID, owner, and durable request fields;
- `Humanoid`/`Character` damage and death observation;
- `RandEventSystem` current event and event area;
- `Teleport`/dungeon transition observation for the accepting player;
- inventory add/pickup observation only for the post-MVP core proof;
- existing Living World companion persistent ID, party, combat authorization,
  death, and telemetry events.

Avoid patches that replace vanilla spawn iteration, event selection, AI update,
or dungeon generation. Spawn That and Custom Raids already patch these hot
surfaces; observation and post-validation reduce compatibility risk.

### Optional mod integration

- **Spawn That absent:** natural requests still work. Authored fallback stays
  disabled or uses Living World's own strictly bounded spawn adapter.
- **Spawn That present:** read `spawn_template_id` for provenance; optionally
  register code configuration only for explicitly enabled fallback content.
- **Custom Raids absent:** natural event request still watches vanilla
  `RandEventSystem`.
- **Custom Raids present:** recognize its configured events and template IDs;
  never assume custom events are progression appropriate. Require an allowlist
  or event metadata policy.

Do not hard-depend on either ASharpPen mod merely to implement boar hunting or
event observation.

### Multiplayer authority

Custom Raids' documented host-selection/client-zone-spawn split and vanilla ZDO
ownership mean the module must tolerate different machines owning the NPC,
target, and event spawn.

Minimum protocol:

1. client asks to accept/turn in;
2. server/request authority revalidates and allocates an instance ID;
3. target owner reports signed-by-context observations (damage/death/event
   presence) through a bounded RPC;
4. server validates request state and writes the idempotent respect transaction;
5. all peers receive display state.

Never trust a client-supplied “target died” or item count without current request
context. Never award twice after RPC retry or reconnect.

### Performance

Do not scan the whole ZDO world. Candidate scans should operate on loaded
characters and cached nearby location instances, staggered per NPC with a low
frequency and hard radius. Subscribe to events only while a request is offered
or active. Cache negative offer results briefly. The village validator may use
the same bounded location membership techniques already proven in
`VillageReclaim.cs` rather than global `FindObjectsOfType` polling in the final
implementation.

## Ranked MVP and acceptance gates

| Rank | Slice | Runtime mutation | Main risk | Ship gate |
| ---: | --- | --- | --- | --- |
| 1 | Hunter's Measure | None | wildlife identity/unload | exact eligible death and idempotent respect proven |
| 2 | Hold the Line | None | event attribution/authority | natural raid only; no extra spawn; multiplayer proof |
| 3 | Black Door | None | location/entrance identity | same entrance out-and-back; NPC safely waits |
| 4 | Embers for the Kiln | None | environmental kill/farming | direct contribution and no farm acceleration |
| 5 | Last Door of the Village | None | competing module/source ambiguity | Village Reclaim state unchanged; exact source proof |

Do not ship authored micro-encounters in the first MVP. If natural availability
is too low, improve offer timing and NPC dialogue variety before adding spawns.

## What must be proven in game

### Shared foundation

- [ ] Build and load against Valheim 1.0.20, Jotunn 2.30.2, current Living World
  1.0.x actor line, with and without Spawn That 1.2.20 and Custom Raids 1.8.2.
- [ ] Dedicated server and listen-server both agree on offer, acceptance,
  progress, completion, and one respect award.
- [ ] Save/restart while offered, active, ready-to-turn-in, and completed.
- [ ] NPC persistent identity survives zone unload; no fresh NPC inherits the
  request.
- [ ] Portal use follows the declared suspend/fail policy; no clone, snap, remote
  completion, or lost companion.
- [ ] Requester permanent death can never be undone or replaced by quest logic.
- [ ] RPC retries, reconnects, and owner migration cannot duplicate respect or
  rewards.
- [ ] No target outside the loaded simulation dies or progresses because of the
  request.
- [ ] No protected player, tame, companion, resident, or neutral is targetable.
- [ ] No request reveals an undiscovered location/biome/enemy/material.

### Hunter's Measure

- [ ] Natural world boar, runestone boar, tamed boar, already-dead boar, and boar
  killed by another mob are correctly distinguished.
- [ ] Player damage, companion damage, environmental damage, and unrelated
  player damage produce the intended credit outcomes.
- [ ] Target wandering beyond radius, unloading, or being killed before
  acceptance fails fairly and leaves no stale reservation/tag.
- [ ] Normal drops and spawn cadence are unchanged.

### Hold the Line

- [ ] At least one natural vanilla raid from start through natural stop on host
  and dedicated server.
- [ ] Non-event creatures of the same prefab do not count.
- [ ] Joining late, leaving the event circle, requester incapacitation/death,
  server restart, and event replacement/cancellation are handled deterministically.
- [ ] Custom Raids installed and absent produce the same core safety properties.
- [ ] The request never calls `SetRandomEvent`, `SetForcedEvent`, or changes the
  raid pool/timer.

### The Black Door

- [ ] Loaded/explored and seed-known-but-unexplored Burial Chambers are not
  confused.
- [ ] Entrance and exit correlation works when multiple chambers are nearby.
- [ ] NPC reaches a safe exterior anchor, never enters the trigger, never calls
  player teleport, and resumes after player return.
- [ ] Different exit/portal/logout does not falsely complete.
- [ ] If core proof is later enabled: stockpiled, chest-moved, player-dropped,
  traded, dungeon-picked, and duplicated/reconnected cases are distinguished.

### Embers for the Kiln

- [ ] Environmental water death alone yields no credit.
- [ ] A world Surtling, FireHole Surtling, and event Surtling are distinguishable.
- [ ] No repeated award or altered FireHole respawn/drop behavior.

### The Last Door of the Village

- [ ] `WoodVillage1` local spawner Draugr, pile Draugr, world Draugr, and raid
  Draugr are classified correctly.
- [ ] Existing `pop_reclaim_v1_encounter`, `pop_reclaim_v1_cleared`, and
  `pop_reclaim_world_draugr` behavior remains unchanged.
- [ ] Completion of the small request cannot unlock, block, or reset full village
  reclamation.
- [ ] Quiet proof pauses on unload and requires player/requester presence.

### Authored fallback, only if later approved

- [ ] Terrain, slope, water, collision, navigation, player-base, portal,
  settlement, dungeon, and active-event exclusions all work.
- [ ] Exactly one tagged encounter is created per accepted instance across save,
  restart, reconnect, and zone owner migration.
- [ ] Cancellation before combat cleans up only Living World-owned entities;
  cancellation during combat does not visibly erase living enemies.
- [ ] Normal loot once, no respawn, no farming loop, no offscreen resolution.

## Final recommendation

Implement the request ledger and **The Hunter's Measure** first, entirely on
natural loaded world facts. Then add **Hold the Line** as an observer of real
events. These two slices prove nearly every hard foundation—identity,
authority, contribution, presence, persistence, fair failure, and idempotent
respect—without changing world generation or spawn balance.

Treat dungeon cores, Surtling farms, and Draugr villages as later compatibility
gates, not attractive shortcuts. Custom Raids and Spawn That are valuable,
maintained examples and optional provenance providers, but Living World must
remain the sole owner of the relationship transaction: who asked, what real
fact was witnessed, who took the risk, and why respect was earned exactly once.
