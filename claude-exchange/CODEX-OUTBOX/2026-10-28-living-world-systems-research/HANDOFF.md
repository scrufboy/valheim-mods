# Living World C.O.R.E. living systems research

Date: 2026-10-28. Branch: `work/companion-encounter-world`. Draft PR:
https://github.com/scrufboy/valheim-mods/pull/4.

## What the owner wants from this comparison

The owner supplied a video transcript and the local
`RustyMods-VikingNPC-0.3.4.zip` package. He wants the living-world capabilities
examined and built into our own modular encounter system: biome-shaped people
and kit, independent survival/work routines, contextual conversation, ship and
structure use, persisted lives and inventory, and capable combat. Our distinct
consent, aid, affinity, and alliance arc remains the authority for relationships.
The owner explicitly rejects taming and hiring modules for these people; no
relationship state from the reference should be transplanted. These world
capabilities attach to an independently living, server-owned person.
Do not treat this audit as authorization to install the reference package into the maintained
1.0.x profile.

The [package page](https://thunderstore.io/c/valheim/p/RustyMods/VikingNPC/)
and [source repository](https://github.com/RustyMods/Norsemen) identify the
reference and its author. These links are source attribution, never Living
World C.O.R.E. names or package identity.

## Evidence and scope

- SOURCE-INSPECTED: Supplied archive at
  `D:\Users\scruf\Downloads\RustyMods-VikingNPC-0.3.4.zip`, SHA-256
  `5B18E85581676B76A553A6E1AD055AFDCD7A4B2F5BD2D15F51FEE72B2B37C50E`.
  It contains `Norsemen.dll`, README, changelog, and manifest. The manifest
  requires BepInExPack 5.4.2333 and YamlDotNet 16.3.1. The DLL was not run.
- SOURCE-INSPECTED: Public `RustyMods/Norsemen` default branch, package version
  0.3.4. GitHub reports no repository license. This is a design and API
  reference; implement our own code and data, with attribution when describing
  the influence. Do not import its source, assets, names, or dialogue wholesale.
- SOURCE-INSPECTED: Thunderstore marks the package deprecated. The 0.3.0
  overhaul removed towns and raider ships. The `Raid` implementation remains
  in source but event construction is commented out in
  [`Plugin.cs`](https://github.com/RustyMods/Norsemen/blob/master/Plugin.cs),
  so it is not evidence of a current playable raid system.
- UNKNOWN: Compatibility of this 0.3.4 DLL with the maintained Valheim 1.0.x
  profile. No side-by-side game test or runtime performance measurement was
  performed.

## What the source actually offers

| Capability | Source observation | Living World application |
| --- | --- | --- |
| Biome spawning | [`SpawnManager.cs`](https://github.com/RustyMods/Norsemen/blob/master/Managers/SpawnManager.cs) registers seven biome prefabs with Valheim `SpawnSystem`. Frequency, cap, day/night, altitude, forest, progression key, and level are configurable. | Keep our server-claimed camp/person IDs and discovery rules. Use biome spawn constraints as input to *where* a claimed scene can become active, not as an ambient duplicate generator. |
| Visible person and loadout | [`Customization.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/Viking/Customization.cs) persists model, hair, beard, skin, and name in ZDO. [`Equipment.yml`](https://github.com/RustyMods/Norsemen/blob/master/Files/Equipment.yml) describes biome-specific weighted gear sets, optional weapons, items, and boss keys. [`Armor.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/Viking/Armor.cs) derives protection from worn armor. | Bind our existing `EncounterPopulation`/`EncounterLoadouts` cast to verified native prefabs and actual armor. Commit the generated look and kit once, before showing the actor. Validate keys and prefab names, then persist person-owned gear and supplies. |
| Tool-driven work | [`WorkTargetSearch.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/WorkTargetSearch.cs) searches loaded objects for mineable rock, trees, or fish; it checks pickaxe tier, axe, fishing rod, and bait. [`Working.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/Working.cs) equips the tool, moves to a target, performs an action, and adds inventory resource. | Add camp/work task policy and a Valheim adapter. A person can hunt, cut, mine, fish, guard, repair, or rest according to role, need, inventory, site, and current threats. Use bounded local target queries and actual world objects. Record products once; never claim a witnessed task happened during zone unload. |
| Inventory and persistence | [`Container.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/Viking/Container.cs) saves inventory into the actor ZDO and reloads on data revision. [`Work.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/Viking/Work.cs) has elapsed-time resource accrual on actor setup. | Persist inventory, equipped slots, finite ammunition, resources, and last work state under one server-owned person. Decide explicitly whether any abstract background production is allowed; do not silently turn unloaded time into unlimited loot. Test unload/reload and ownership transfer. |
| Speech and presentation | [`Talk.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/Viking/Talk.cs) selects greeting/farewell or context lines by sight, distance, alertness, and player-base presence; it pairs text with emote triggers and sound effects. [`TalkManager.cs`](https://github.com/RustyMods/Norsemen/blob/master/Managers/TalkManager.cs) syncs dialogue data from the server. | Feed our authored encounter dialogue and voice cue IDs from durable social facts. Allow look, pause, warning, hail, point, emote, then choice. Prioritize combat over speech and rate-limit both text and voice. Client presents only approved lines and animation. |
| Ship passage | [`Sitting.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/Sitting.cs) checks a followed player's ship and attaches to a free ship `Chair`; it detaches and returns near the player when the ship situation ends. | Add an owned seat reservation/boarding/disembark module after the no-push gate. Test two companions, seat contention, ship destruction, player disconnection, save/reload, and ownership migration. This source does not establish independent house chair or bench behavior. |
| Combat | [`Attacking.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/Attacking.cs) uses vanilla item attack metadata, target visibility, range, movement, and friend-target types. [`Blocking.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/Blocking.cs) and [`Dodging.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/Dodging.cs) provide simple timed defensive decisions. | Reuse the *question set* (target, range, angle, attack metadata, threat, defense), not these random decisions. Our weapon profiles need explicit spear throw/recovery/fallback, knife lunge positioning, atgeir ally safety, bomb reserve, and finite magic with observed native events. |
| Faction response | [`FactionManager.cs`](https://github.com/RustyMods/Norsemen/blob/master/Managers/FactionManager/FactionManager.cs) changes vanilla enemy relations for its Vikings, including theft/aggravation behavior. | Map our server-owned allegiance, witnessed offense, and affinity state to native hostility. Do not infer friendship from biome, gear, or Valheim's faction enum alone. |
| Network contract | [`VersionHandshake.cs`](https://github.com/RustyMods/Norsemen/blob/master/VersionHandshake.cs) checks matching mod versions; [`CustomizationManager.cs`](https://github.com/RustyMods/Norsemen/blob/master/Managers/CustomizationManager/CustomizationManager.cs), [`NameGenerator.cs`](https://github.com/RustyMods/Norsemen/blob/master/Managers/NameGenerator.cs), and dialogue manager synchronize YAML data. | Version and schema-gate our server-issued cast/config/presentation data. The server owns rolls, identity, work outcomes, relationship, combat, and inventory; clients render authorized state. |

## What this source does not solve for us

1. [`Following.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/Following.cs) chases the target beyond 3 m, runs beyond 5 m, and teleports beyond 15 m. It does not match a deliberately slow leader pace or demonstrate a no-push solution. Our 1.0.32.8 OBS/session review remains the movement evidence.
2. [`Attack.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/Viking/Attack.cs) forces spear users away from the secondary attack, randomly selects other secondary attacks, and adds arrows/bolts when none are found. This cannot fulfill our finite spear, tactical lunge/atgeir, and ammunition contracts.
3. [`VikingAI.cs`](https://github.com/RustyMods/Norsemen/blob/master/Behaviors/VikingAI/VikingAI.cs) is a single priority chain of partial methods. Its sleep call is commented out. It does not prove living house routines, doors, bench choice, workstations, persistent camp schedules, earned quests, or server-resident actors in unloaded zones.
4. The 0.3.4 name list is short and first-name only; see [`Names.yml`](https://github.com/RustyMods/Norsemen/blob/master/Files/Names.yml). Our Norse naming and lineage catalog should remain independent and deeper.
5. The talk file is generic flavor text, not a persistent person-specific introduction or a quest conversation. We should keep our authored situation/line IDs and approved voice bindings.

## Build order for Claude on the maintained 1.0.x core

1. Keep the existing no-push gate open. Use the exact installed 1.0.32.8
   recording, log, and telemetry in
   `../2026-10-28-installed-1.0.32.8-session/` to repair contact and movement
   authority. Then add leader slow-walk pace as a separate polish gate.
2. Port `EncounterPopulation` and `EncounterLoadouts` through a native prefab
   registry. Resolve each saved cast into an actual person model, armor set,
   primary/fallback weapon, tools, finite supplies, and level within biome and
   world-key rules. Record exact prefab IDs and rejection reasons in telemetry.
3. Add a camp/work module with server-owned task claims, bounded target search,
   real action completion, inventory debit/credit, threat interruption, and
   deterministic return to a home routine. Distinguish visible work from any
   abstract background schedule. Never mint work rewards twice across unload.
4. Add contextual talk/emote/voice presentation over the existing encounter
   state and `EncounterVoice` assets. The first hail, caution, help request,
   response, invitation, and farewell each require their own situation and
   approved cue, not a random generic line.
5. Add ship seat reservation and disembark; then separately add door use,
   household seat choice, and workstation choice. Each is an independent
   interaction contract with resource reservation and fallback.
6. Continue specialist combat integration. Use Valheim attack metadata but
   select moves through our role, safety, finite-resource, and observed-event
   policies. Do not inherit unlimited ammunition or random special-attack
   selection from the reference.
7. Verify dedicated-server ownership, two clients, zone unload/reload, save
   restart, inventory recovery, and one/two-person balance. Record actual live
   observations before changing the active game profile.

## Acceptance for this reference work

This audit is complete as a source comparison. It is not an implementation or
compatibility claim. Its conclusions should inform the modular backlog and
Claude's 1.0.x adapter work; the current installed profile remains 1.0.32.8.
