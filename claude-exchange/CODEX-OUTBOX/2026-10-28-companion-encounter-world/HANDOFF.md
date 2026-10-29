# Living World companion encounter world — Claude handoff

Date: 2026-10-28. Owner: Brian (`scrufboy`). Follow
`claude-exchange/PROTOCOL.md`. The exact owner statements are in `REQUEST.md`.
This submission is the reviewed engine-free design/policy layer and research for
the maintained 1.0.x game line. It is **not an installed or playable campaign
encounter build**.

## Read this first

- SOURCE-INSPECTED: `LivingWorldCORE/COMPANION-ENCOUNTER-RESOLUTION-ARC.md`
  joins the discovery → lived camp → need/respect → danger alert → rescue →
  recruitment → safe release → return arc.
- SOURCE-INSPECTED: `LivingWorldCompanionSystems/README.md` indexes the module
  handoffs and the authored Split-Ash scene.
- SOURCE-INSPECTED: `LivingWorldCompanionSystems/EncounterLore/` has a Meadows
  two-person pilot, a gated Plains example, 58 dialogue lines, request beats,
  and both final-choice presentations: player **“Will you come with me?”** or
  person **“Will you have me?”**. Both need the same home, world-key, roster,
  and consent facts.
- SOURCE-INSPECTED: `LivingWorldCompanionSystems/RuntimeIntegration/` describes
  the stable IDs and one server-authoritative encounter transaction. QuestBroker
  alone owns durable request status; its private/public visibility is explicit.
- SOURCE-INSPECTED: `research/CompanionRespectQuests/COMPANION-RESPECT-QUESTS-RESEARCH.md`
  compares vanilla, ASharpPen Spawn That/Custom Raids, and Randy Knapp Epic Loot
  Haldor Adventure Mode with linked author sources. Epic Loot selects a
  seed-derived area and places an authored bounty when it loads; it does not
  normally bind an existing local monster to the bounty.

## What the offline modules actually do

| Module | Implemented policy boundary | Live boundary still missing |
| --- | --- | --- |
| `Discovery` | Stable template/encounter/site IDs, one lead, safe candidate facts, five-day cadence, deterministic clues | World/zone candidate queries, camp prefabs, networked actor creation, clue UI |
| `CampLifecycle` | Whole-camp watcher and station-aware routines, away-state freeze, no offscreen death, stable needs fact seam | Unity movement/animation/stations, ZDO serialization, network claims |
| `FriendInNeed` | Material-danger alert, approved surprise voice token, one bounded follow-up, native-combat yield | Live witness evidence, voice resolver, HUD, network replay rules |
| `NeedsAndAffinity` | Concrete need/aid facts and qualitative trust without grindable points | Actual world target checks, aid interactions, persistence adapter |
| `QuestBroker` | One durable request across authorized camp/board/jarl views, private by default, exact-site single claim | Board/jarl actors and UI, map disclosure, miniboss spawner, reward transaction |
| `RescueRelease` | Factual invitation, two active/six bonded, safe handoff, gear ownership, terminal death | World-key/home proof, physical transfer, corpse/gear, actual mortality gate |
| `EncounterVoice` | Text/emote/cue scheduler, cooldowns, combat suppression, approved shock pool | Human listening/approval for new conversational candidates; audio/game adapter |
| `RuntimeIntegration` | Engine-free aggregate, source seam map, identity and transaction tests | Port into authoritative maintained 1.0.x source and live world/network proof |

## Latest owner direction to integrate next

- SOURCE-INSPECTED: `REQUEST.md` adds a further first-contact requirement that
  emerged after the modules were built. A distant one- or two-person group may
  be friendly, predatory pillagers, or Eitrherjar Legionaries. The player and
  the people notice each other without instant omniscient labels or instant
  hostility. Camps can be encountered while hunting, camping, or patrolling.
  Friendly introductions need cautious, varied, lore-grounded signals and
  boundaries; a warning or ongoing normal combat may interrupt them.
- SOURCE-INSPECTED: The existing faction/behavior authorities are
  `LivingWorldCORE/FACTION-AND-PEOPLES-BIBLE.md`,
  `LivingWorldCORE/WORLD-COMPANION-DISCOVERY-AND-BIOME-ROLES.md`, and
  `LivingWorldHumanNPC/BEHAVIOR-STATE-CONTRACT.md`. Faction relationships must
  be authored by person/group; do not infer morality from body, biome, prefab,
  or a temporary native `Character.Faction` enum. Native combat owns movement,
  hands, facing, and attacks once authorized.
- SOURCE-INSPECTED: The owner wants traveled, equipped survivors who know
  actual nearby places from world generation and their own histories. Bind a
  person's knowledge to stable site/location IDs and a believable source
  (travel, hunting, testimony) before dialogue or quest presentation. A seed
  lookup is a candidate; loaded-world validity and progression still require
  proof. Gear and level must fit origin, role, biome, world keys, and lived
  history. If a later tier is earned, preserve `PersonId`, relationship,
  inventory ownership, injuries, and death record; improve the same person
  rather than delete and replace them with a new NPC.
- UNKNOWN: Exact current Creature Level and Loot Control integration API and
  compatibility with the maintained 1.0.x branch have not been source-proven
  here. Treat it as a researched compatibility option, not a dependency or a
  promise that levels can be changed by replacing a prefab.
- UNKNOWN: Hugin/Munin participation in introductions is a story idea, not an
  implemented feature or a verified asset/trigger. Literature-inspired lines
  need source attribution and should be identified as Living World fiction,
  never presented as an attested saga episode.

## Source and build reality

- SOURCE-INSPECTED: This submission is on a new GitHub branch created from
  `work/norse-human-names`; the exact commit and URL are recorded after push in
  the final chat reply. The temporary local `lwc-core-norse-port` checkout is
  at `claude/voice-1.0.32.1` and was read only for the integration seam. It is
  not the delivery artifact.
- SOURCE-INSPECTED: `C:\Users\scruf\Documents\LivingWorld-CORE-NPC\src` is a
  separate older 1.0.1 source tree, so it is not proof of the current 1.0.32.7
  runtime. Determine the authoritative maintained source revision before
  applying the adapter.
- OFFLINE-TESTED: The pure policy and cross-module test runners listed in
  `VERIFICATION.md` pass. Lore/voice JSON and voice line bindings validate.
- UNKNOWN: No 1.0.x campaign DLL was built from these modules. No game-profile
  file was installed or changed. No dedicated-server or in-game encounter,
  faction, combat, voice, inventory, or save/reload proof exists for this arc.

## Recommended live sequence

1. Locate and name the authoritative maintained 1.0.x source commit and current
   installed profile hash. Do not port into the 0.3.x Human NPC lab or the older
   1.0.1 source tree by accident.
2. Port stable encounter, site, camp, incident, request, and person IDs plus one
   world-owned transaction store into that source. Preserve existing combat and
   voice owners; use a single actor spawn claim and no offscreen death inference.
3. Make one real Meadows Split-Ash camp appear safely, persist through unload,
   and resume with the same two people and their gear. Prove ordinary routines,
   cautious mutual discovery, and native third-party combat without forced
   targeting or a friendly-to-hostile misclassification.
4. Add one verified nearby request (the boar/meal need is the smallest pilot),
   one witnessed aid outcome, and both invitation presentations. Board/jarl
   views remain unavailable until real public issuer and UI adapters exist.
5. Add authored miniboss encounters only after site validation, single-claim
   spawn/cleanup, progression, reward, and network ownership gates pass.
6. Build with the maintained branch's actual compiler/native/ownership gates,
   then test in a disposable world. Record exact DLL, installed hashes, log and
   telemetry, screenshots, save/reload, two-player arrival, and failure cases.

The current work is a reviewable modular foundation. It should be merged into a
playable claim only after those live gates provide evidence.
