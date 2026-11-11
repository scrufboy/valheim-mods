# Swamp faction locations: bounded research

Research date: 2026-11-07. Research only: no packages imported, no game/app launched, no world altered, no native or licensed asset copied here. Bonemass and its native encounter are outside this change. Large temple/dungeon work remains deferred until the faction raid is working.

## First location to build

Build an **additional small inhabited swamp ruin**, with an open combat yard, a shaman retreat, several entrances, and clear navigation around the beast. Use it as the same faction's home and a repeatable combat/telemetry setting. This is a design recommendation, not an existing working location.

| Route | Purpose | Implementation boundary |
| --- | --- | --- |
| New ruin/camp | First inhabited faction location | Separate location identifier and spawners; preserve vanilla ruins, crypts, iron access, boss sites |
| Alternative crypt | Later small underground faction encounter | Separate entrance and dungeon generator first; verify entry/exit, navigation, resources and persistence before considering replacement |
| Replace Sunken Crypts | Optional later world-generation policy | Explicitly target crypt locations; replacement affects generation and requires separate handling for already generated instances |

Existing game data provides useful templates: `SwampRuin1`, `SwampRuin2`, `SwampHut1` through `SwampHut5`, and `SunkenCrypt4`. Jotunn's generated location list identifies their swamp placement, creature spawns and the crypt interior environment. This establishes reusable game patterns, not approval to replace every location. [S1]

## Actual purchased asset evidence

Found locally, read-only:

`C:\Users\scruf\AppData\Roaming\Unity\Asset Store-5.x\KumaBeer\3D ModelsEnvironments\Stylized ancient modular ruins.unitypackage`

Observed size: 92,192,511 bytes; modification time reported locally: 2026-11-05 07:51:38. Its presence confirms a cached named package, not its license terms, mesh suitability, imported state or runtime compatibility. No separate package with temple/crypt in its filename was found in the same filename scan. This does not prove no temple package exists elsewhere. Inspect modular dimensions, materials and colliders through the existing tools before selecting pieces; keep source package and extracted licensed assets outside Git.

## Supported location and dungeon mechanisms

Expand World Data's `expand_locations.yaml` supports location clones, blueprint-backed locations and a per-location custom dungeon generator. It can change objects and add relative objects. Exterior footprints above the suggested 32-meter radius can cross zone boundaries and need care. Location definitions are pregenerated: changes require redistribution in unexplored areas; already explored areas require separate Upgrade World handling. These are different migration paths, not a live rewrite of existing scenery. [S3]

`expand_rooms.yaml` supports cloned rooms and blueprint rooms, with themes, entrance connections, endcaps and connection rules. These provide a real assembly mechanism for authored modular pieces; AI still needs valid room geometry and tested connections. [S4]

`expand_dungeons.yaml` supports dungeon and camp algorithms, room counts/requirements, room themes, bounds and object swaps. A generator can combine the `SunkenCrypt` and `ForestCrypt` room sets. This makes an alternative crypt technically plausible; it does not demonstrate a new playable crypt or arena has been built. [S5]

Custom purchased mesh prefabs still require the established Unity registration/bundle pipeline; a blueprint configuration is not a substitute for packaging and registering those meshes. This is an implementation inference from the distinction between prefab definitions and location assembly.

## Fresh and existing worlds

Prefer a fresh disposable test world with the new additive location enabled from generation. For an existing world, first use a backed-up copy and distinguish ungenerated land from generated land; unrevealed minimap land can already be generated. Upgrade World's author describes generated coverage extending hundreds of meters beyond player visits. Its zone-reset operation removes objects including player structures. Therefore no biome-wide reset, blanket replacement or automatic destructive migration is proposed or executed here. [S6]

The first raid and creature behavior tests do not depend on changing world generation. Prove the encounter on open swamp terrain before measuring navigation in custom ruins.

## Fuling role benchmark: version-qualified

The published Jotunn dataset declares **Valheim 1.0.7**. Its camp/location records list:

| Location | Listed actor prefabs |
| --- | --- |
| `GoblinHut01` | `Goblin` |
| `GoblinHut02` | `Goblin`, `GoblinBrute`, `GoblinShaman` |
| `GoblinHut03` | `Goblin`, `GoblinArcher`, `GoblinBrute`, `GoblinShaman` |

Thus the current published dataset has **four camp actor prefab roles**, with three in the smaller listed composition. Do not give a blanket answer of three without qualifying game version and camp. This is not a verified count from Brian's running installation. [S1]

The character table confirms these distinct prefabs. Base `Goblin` has four unique weapon choices: club, spear, sword and torch; repeated entries weight choices. `GoblinArcher` lists `GoblinSpear` in its random weapon list, so its name alone does not establish bow use. Equipment variants should not inflate the actor-role count. [S2]

For Formidable, the useful coverage is line fighter (Neckman/Marauder), evasive ranged fighter (tiny blowgun creature), support/controller (Neckman Shaman), and optional heavy (existing Behemoth). This is a proposed encounter-role mapping, not a new lore naming decision or a claim of native implementation.

## Primary sources

- S1 — Jotunn generated location data: https://valheim-modding.github.io/Jotunn/data/zones/location-list.html — checked the declared version, `GoblinHut01/02/03`, swamp huts/ruins and `SunkenCrypt4`.
- S2 — Jotunn generated character data: https://valheim-modding.github.io/Jotunn/data/prefabs/character-list.html — checked `Goblin`, `GoblinArcher`, `GoblinBrute`, `GoblinShaman` and equipment lists.
- S3 — Expand World Data author location docs: https://github.com/JereKuusela/valheim-expand_world_data/blob/main/docs/locations.md
- S4 — Expand World Data author room docs: https://github.com/JereKuusela/valheim-expand_world_data/blob/main/docs/rooms.md
- S5 — Expand World Data author dungeon docs: https://github.com/JereKuusela/valheim-expand_world_data/blob/main/docs/dungeons.md
- S6 — Upgrade World author repository and operational explanations: https://github.com/JereKuusela/valheim-upgrade_world

## Still unverified

Actual local game version and local camp composition; imported ruin prefab inventory; the separate temple package identity; correct colliders/materials; location registration and network behavior; movement around ruins; and all proposed alternative crypt behavior. Nothing in this note constitutes runtime test evidence.
