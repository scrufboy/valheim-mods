# Souls prefab repair and Elder-pattern competitive telemetry raids

## Brian's boundary

- **OWNER-DIRECTED:** Souls actors must exist in their original biome-based
  natural spawns and in a controlled raid. They are competing combatants whose
  pressure and decisions help train the universal FormidableAI core that will
  eventually live beneath friendly and hostile Living World actors.
- **OWNER-DIRECTED:** the controlled raid clones `army_theelder` spawn law. It
  must not become a custom packed barrage. The Krauguk Legionnaire/OrcBrute is
  the sporadic heavy in the Elder brute slot.

## Source candidate

- **OFFLINE-TESTED:** branch `work/rrrcore-souls-1.0.21`, head `15d49d3`.
- **SOURCE-INSPECTED:** RRRCore 3.1.5 deletes a Goblin clone's visual children,
  copies current Player children, then dereferences ten hard-coded 2023 transform
  paths. Valheim 1.0.21 no longer satisfies that graph; `Npc.DesignNpcBase`
  null-dereferences before any Souls prefab is registered.
- **OFFLINE-TESTED:** `LivingWorld.RRRCoreCompat.dll` replaces only that private
  base-construction method. It maps the current Player prefab's serialized body,
  attachment, eye and foot references into the clone, then returns authority to
  the unchanged RRRCore configuration pipeline.
- **OFFLINE-TESTED:** the plug-in compiles against current Valheim 1.0.21 and the
  Arena profile. SHA-256:
  `dd3073faeee77948b35522e93386e6087a3b820b694abe5917280fb199c45686`.
- **INSTALLED:** that exact DLL is staged only in `LWC Formidable Arena`. The
  prior RRRCore `ZRoutedRpc.Everybody` literal repair remains installed and is
  still required before this later prefab stage.

## Spawn design

- **SOURCE-INSPECTED:** current `army_theelder` law is 120 s; ordinary group
  slots capped at 6 and 4 with group size 1-3 and radius 3; specialist and brute
  capped at one with 10 m same-prefab suppression; all check every 5 s.
- **OFFLINE-TESTED:** two Souls events use that exact pacing/population law:
  - `lwc_formidable_arena`: Pillager common line, WoodsSoul secondary group,
    NinoBosque specialist, OrcBrute/Legionnaire heavy;
  - `lwc_formidable_souls_swamp`: Pillager common line, Neckman secondary group,
    Servant specialist, OrcBrute/Legionnaire heavy.
- **OWNER-DIRECTED:** the Legionnaire occupies only the one-at-a-time Elder
  brute slot. It is not a group spawn.
- **OFFLINE-TESTED:** deliberate arena-boundary differences are 70-95 m random
  spawn radius, `HuntPlayer=false`, one hostile faction, `NearBaseOnly=false`.
  These preserve Brian as an ordinary observer while making approach and
  initiative visible.
- **INSTALLED:** the old ten-minute six-entry arena and the close-range trial
  events were moved to the dated backup. Only the two Elder-pattern events are
  active.
- **INSTALLED:** the original Souls Spawn That file remains active in config and
  contains all six prefabs with their original biome, day/night, chance,
  distance and group rules. The stale BepInEx type-loader cache was moved to the
  backup so Spawn That is rediscovered on the next launch.

## Live registration gate — PASS 2026-10-31

- **LIVE-PROVEN:** the isolated `LWC Formidable Arena` profile loaded the
  `LWC_HumanLab_v1` world with Valheim 1.0.21 and C.O.R.E. 1.0.32.27.
- **LIVE-PROVEN:** BepInEx loaded 15 expected plug-ins, including Spawn That
  1.2.20, Custom Raids 1.8.2, RRRCore 3.1.5 and the compatibility plug-in.
- **LIVE-PROVEN:** the compatibility plug-in rebuilt the RRR_NPC base from the
  current Player prefab graph. RRRCore then registered all six Souls actors as
  humanoids with MonsterAI and emitted `SOULS-PREFAB SUMMARY 6/6 ready`.
- **LIVE-PROVEN:** the complete launch/world-load log contains zero error lines,
  no `DesignNpcBase` exception and no `Unable to find spawn RRRN_*` warning.
- **DIAGNOSTIC-CORRECTED:** the first launch attempt was not an Arena result.
  Steam had a stale Valheim `LaunchOptions` entry for `LWC MonsterDB A1`, which
  was appended after r2modman's Arena arguments and won. That obsolete entry was
  cleared (with a localconfig backup), and the passing launch command contained
  only the Arena preloader path.

Registration is now proven. Natural-spawn frequency and the two raid
compositions remain the next live combat/video evidence, not a prerequisite for
this prefab gate.

