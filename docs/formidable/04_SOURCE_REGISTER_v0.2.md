# FORMIDABLE Source Register (17 October 2026)
**Classification:** external public reference / historical game-mod provenance / internal conversation decision. URLs are reference targets; pages and versions can change, and the code teams must verify compatibility against their actual current build.

| ID | Source | Claim supported / what to check | Limit |
|---|---|---|---|
| SRC-01 | https://www.nexusmods.com/valheim/mods/1672 | Salpati Souls of the Land: Forest Children, Neckman, Common Folk Pillagers, Krauguk Legionnaires, Dark Servants; biome placements; dependencies and contributor credits. | Historical author description; *not* rights clearance for derivative distribution. |
| SRC-02 | https://github.com/ASharpPen/Valheim.CustomRaids | Custom Raids supports custom events, replacing/disabling vanilla raids and config extensions. | Compatibility under current Valheim must be tested. |
| SRC-03 | https://thunderstore.io/c/valheim/p/ASharpPen/Custom_Raids/ | Published distribution, current reported version. | Avoid assuming all users have same version. |
| SRC-04 | https://asharppen.github.io/Valheim.SpawnThat/ | World and local spawner configuration, including villages/dungeons. | Distinct concern from scheduled raids / world locations. |
| SRC-05 | https://github.com/JereKuusela/valheim-expand_world_data | Data-driven locations, encounters, weather, spawns, objects and prefabs; documents risks. | Important existing-world vs new-seed distinction. |
| SRC-06 | https://github.com/JereKuusela/valheim-expand_world_data/blob/main/docs/locations.md | World-generated locations, distributions, placement constraints, `genloc` after changes. | Locations usually pregenerated; not a live town-building engine. |
| SRC-07 | https://github.com/JereKuusela/valheim-expand_world_data/blob/main/docs/blueprints.md | `.blueprint` / `.vbuild`, center-piece, nested objects, terrain snapshots. | Investigate ownership, terrain and collision safety. |
| SRC-08 | https://github.com/JereKuusela/valheim-expand_world_events | Configurable random events and frequency; alternative/complement subject to ownership review. | Do not deploy second competing raid writer blindly. |
| SRC-09 | https://github.com/sirskunkalot/PlanBuild | Building blueprint capture reference. | Dependency optional, extract research only. |
| SRC-10 | https://valheim.fandom.com/wiki/Obliterator | `incinerator` invokes lightning and damage; candidate *visual effect* source. | AVOID invoking vanilla obliteration/damage in celebration. Verify actual prefab API. |
| SRC-11 | https://valheim.fandom.com/wiki/Hugin | Hugin's established tutorial role in Valheim. | Hugin/Munin scripted reclamation dialogue is our original proposal. |
| SRC-12 | https://upload.wikimedia.org/wikipedia/commons/f/fd/The_Prose_Edda_%281916_translation_by_Arthur_Gilchrist_Brodeur%29.pdf | Old Norse ravens Huginn/Thought, Muninn/Memory in mythic reference. | Norse mythology ≠ guaranteed in-game dialogue mechanics. |
| SRC-13 | https://www.valheimians.com/article/how-to-create-custom-runestones-on-a-valheim-server-with-wec/ | Community demonstration of setting `RuneStone.m_text` via world-edit data. | Shows text variation; does not prove unique physical carved rune geometry or current compatibility. |
| SRC-14 | https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/locations_blueprints.md | Sample workflow using PlanBuild/hammer_save and placed blueprint location. | Lab must confirm current game version. |
| SRC-15 | https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/locations_variants.md | Examples of custom object swaps and location variants. | Do not mutate existing world without backup and controlled test. |

## Project facts and provenance not externally verified by this packet
- October 12 historical logs showed a MobAILib reverse-patch exception, legacy Friendlies type mismatch, and an 11-plugin scene-cleanup error storm. Later engineering work can supersede all of these. **Do not classify them as current defects until rechecked.**
- The user explicitly stated the other chats have progressed further; request their latest success/failure ledger.
- The user specified original faction IDs/aggression/combat-stat baselines should remain intact while characters receive original lore and display names.
- The prior assistant's name **Myrkherjar** is rejected for the hostile legion. The E-initial name in another chat has not been recovered from accessible context; research must not invent a false recollection.
- User asked that this remain a research/governance discussion; no gameplay code modification was authorized by this lore handoff.