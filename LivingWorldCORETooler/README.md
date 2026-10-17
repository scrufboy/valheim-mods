# Living World C.O.R.E. Tooler

Unified R2ModMan/Thunderstore-style development and NPC test utility for Living World C.O.R.E.

**Created for and owned by Brian (`scrufboy`), creator of Living World C.O.R.E.** Source and releases belong to [scrufboy/valheim-mods](https://github.com/scrufboy/valheim-mods). Codex and other development tools are not the author or owner.

## Use

1. Install this package beside the current Living World Human NPC Lab build.
2. Launch the disposable Lab profile and enter the approved Lab world/character.
3. Press **Insert** to open or close the panel. The shortcut is ignored while typing in chat, the F5 console, Valheim text entry, or the Tooler's command field.
4. Choose a section and press a button. The panel sends the exact documented command through Valheim's live console dispatcher and leaves all validation to the Human NPC Lab.

The key and scale are in `BepInEx/config/com.scrufboy.livingworldcore.tooler.cfg` after first launch. Rebind `Interface.ToggleKey` there if Insert is not convenient.

## Safety and authority

- The current Human NPC candidate intentionally allows mutation only in its disposable local world and profile, with **Start Server OFF** and no remote peers.
- This Tooler does not bypass that guard and does not claim the underlying candidate is multiplayer-certified.
- Destructive controls use a two-click/five-second confirmation.
- Hot deregistration of network prefabs is not safe. Registration remains owned by the Human NPC plugin at startup.
- The Console tab is forward-compatible: newly added commands can be entered without rebuilding this UI.

## Included command surface

Lifecycle, community/casual tests, scout spawn/migration/status/cleanup, follow/hold/movement/combat, full party prepare/status/locate/recover, specialized Living World waves, every real native Valheim raid, player controls, requested item/prefab spawning, world/time/environment controls, terrain shaping, map/admin tools, and arbitrary console commands.

Unlike the replaced Valheim Tooler fork, this tool does not crawl the game's entire unfinished prefab/item catalog at startup. It resolves only the command or prefab explicitly requested, avoiding the fork's repeated missing-item errors.

## Evidence level

The DLL is offline-compiled against the installed Valheim managed assemblies. Packaging and command-catalog checks are automated. Unity runtime behavior still requires an in-game Lab pass.

