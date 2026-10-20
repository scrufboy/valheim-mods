# scrufboy Valheim Mods

The GitHub home for Brian's **Living World C.O.R.E.** Valheim work.

**C.O.R.E.** means **Characters, Origins, Roles & Encounters**.

## Claude/Codex collaboration

- [Shared Claude exchange](./claude-exchange)
- [Current handoff](./claude-exchange/CURRENT.md)
- [Exchange protocol](./claude-exchange/PROTOCOL.md)

The exchange is append-only: both agents preserve prior evidence, distinguish offline results from live proof, and may not erase working behavior to promote a new candidate.

## Active projects

- [FormidableNPCFramework](./FormidableNPCFramework) — standalone community NPC framework and staged replacement for current RRRCore capabilities
- [LivingWorldCORE](./LivingWorldCORE) — Characters, Origins, Roles & Encounters story, faction, settlement, and world architecture
- [LivingWorldHumanNPC](./LivingWorldHumanNPC) — Living World Formidable NPC runtime: human actors, community behavior, parties, combat, recovery, and raid testing
- [LivingWorldCORETooler](./LivingWorldCORETooler) — unified in-game development and NPC test panel
- [LivingWorldTelemetry](./LivingWorldTelemetry) — focused runtime telemetry
- [LivingVillageFoundation](./LivingVillageFoundation) — resident, building, work, and settlement foundations
- [LivingWorldCompanionSystems](./LivingWorldCompanionSystems) — companion activities, classes, population, and tactics

## Utilities and compatibility work

- [CameraMode](./CameraMode)
- [ControllerFix](./ControllerFix)
- [AIStartupFix](./AIStartupFix)
- [VillageDiagnostics](./VillageDiagnostics)
- [VillageReclaim](./VillageReclaim)
- [PopVillageWorkers](./PopVillageWorkers)

The repaired RRRCore/Runic/Souls stack is a temporary private benchmark and
migration environment. It is not the Formidable NPC Framework and is not part of
the released Living World Core. See the
[project boundaries](./LivingWorldCORE/PROJECT-BOUNDARIES.md) for the standalone
Core, Formidable NPC, optional enemy-family, asset, and special-attack plan.

Shared material is organized under [docs](./docs), [research](./research), [tests](./tests), and [archive](./archive).

Generated binaries, game/runtime files, downloaded third-party mods, videos, raw logs, and private test telemetry are intentionally excluded.

Copyright © 2026 [scrufboy](https://github.com/scrufboy). Released under the repository MIT license.
