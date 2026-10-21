# Claude access requirements

Claude must be given actual access to the following. Paths are evidence
locators, not proof that a remote Claude session can read them.

## Project conversations

- Export or share the complete ChatGPT project:
  `Living World C.O.R.E. Custom NPC CREATION and AI`
- Project ID: `g-p-6acbfeacfc1c81918b6bf81f40fe6336`
- Include the current `Explore Xbox-compatible server mods` conversation,
  `Valheim Mod Story Research`, `Souls of the LAND REDUX`, the tooler task,
  RRR/Pop Villages tasks, camera task, arena task, and the previous
  `Prepare full Claude project handoff` task.
- A dedicated project chat named `Full Accountability Handoff to Claude` was
  created on 2026-10-21 to produce the conversation-level audit.

## Repository and source access

- GitHub: `https://github.com/scrufboy/valheim-mods.git`
- Required branch: `handoff-rimebound-emberworn`
- Required current commit: `06b7255`
- Local checkout:
  `C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\github-sync-forest-children`
- Read-only project mirror material:
  `C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\sources`

The repository modules that must be included in the review are:

- `LivingWorldHumanNPC`
- `LivingWorldCORE`
- `LivingWorldCORETooler`
- `LivingWorldCompanionSystems`
- `FormidableNPCFramework`
- `LivingWorldTelemetry`
- `LivingWorldEnemyFamilies`
- `LivingVillageFoundation`
- `VillageDiagnostics`
- `VillageReclaim`
- `PopVillageWorkers`
- `compatibility`
- `research`
- `docs`
- `claude-exchange`
- `archive`
- `CameraMode`
- `ControllerFix`
- `AIStartupFix`

Do not omit Git history, non-current branches, retained packages, manifests,
build verification files, development notes, regression audits, or previous
Claude/Codex exchange material.

## Active and comparison profiles

Root:

`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles`

Required profiles:

- `LWC Human Lab v1`
- `Souls of the Land`
- `Eledyn`
- `Default` when it contains comparison configuration

For each profile provide:

- `BepInEx/plugins`
- `BepInEx/config`
- `BepInEx/LogOutput.log` and rotated logs
- `BepInEx/doorstop_config.ini` and loader/config metadata when relevant
- `BepInEx/LivingWorldTelemetry`
- manual backup folders and installed build information
- r2modman profile metadata and enabled/disabled mod state

The active installed human DLL is:

`LWC Human Lab v1\BepInEx\plugins\LivingWorldHumanNpc\LivingWorld.HumanNpc.dll`

## Video and commentary evidence

- OBS root: `Y:\OBS OUTPUT\OBS OUTPUT`
- Include the specifically referenced video:
  `2026-10-17 04-58-40.mp4`
- Include every later Human Lab recording and preserve creation timestamps.
- Video is commentary evidence; align it to the matching startup log and
  telemetry session before drawing conclusions.

## Downloaded comparison mods and assets

Root: `D:\Users\scruf\Downloads`

Known required archives include:

- `EggyTeam-ValheimToolerCompatibilityFork-1.0.1.zip`
- `Worobeyko-VikingsForHire-1.6.3.zip` and retained 1.6.0
- `neurodr0me-RRRCore-3.0.0.zip`
- `neurodr0me-RRRCore-3.1.5.zip`
- all retained RRRCore diagnostic packages
- `OdinPlus-MadHatter-2.2.4.zip`
- `OdinPlus-RunicPack-1.0.4.zip`
- `OdinPlus-RunicPack-1.1.6.zip`
- retained RunicPack diagnostic packages
- any Souls of the Land, Pop Villages, Friendlies, Outsiders, raids, camera,
  armor, clothing, and asset archives present in the project/profile history

These are comparison/reference artifacts. Review licenses before redistributing
their binaries or source. A hash manifest can be shared even when an archive
cannot legally or practically be bundled.

## Game and build dependencies

- Valheim install:
  `D:\SteamLibrary\steamapps\common\Valheim`
- Managed assemblies:
  `D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed`
- Native bundles:
  `D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles`
- BepInEx, Jotunn, Harmony, Unity, Mono.Cecil, compiler, PowerShell, Python, and
  UnityPy dependency paths recorded by `LivingWorldHumanNPC/Build-HumanNpc.ps1`

Do not redistribute Iron Gate or third-party copyrighted assets as if they are
Living World property. Give Claude local read access or hashes/metadata when
the files cannot be included.

## World/save evidence

Provide the disposable Human Lab world and tester profile only through a safe
backup/export. Preserve the original. Include the world seed, character save,
map/terrain state, lab coordinates, and any arena/site survey documents. Never
let an audit tool mutate the sole copy of a save.

## Minimum verification after transfer

Claude should independently confirm:

1. repository remote, branch, commit, and clean/dirty state;
2. installed DLL version and SHA-256;
3. log/telemetry/video session alignment;
4. archive and build-manifest hashes;
5. which evidence is live, offline, installed-only, superseded, or unknown;
6. which materials remain inaccessible.

Until those checks pass, Claude has a handoff description—not complete access.
