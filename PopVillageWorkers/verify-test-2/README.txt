PoP Village Workers - Test 2 (0.1.1)

Exit Valheim. Extract into the Souls profile's BepInEx folder. Replace all.
Load the existing reclaimed village; no new world is needed.

FIRST CHECK: walk up to a commoner using the controller. Native game interaction
prompts and follow/stay should work. Worker status is appended to the native
hover text without replacing game controls.

REPAIR CHECK: leave a commoner in Stay mode near a damaged wooden wall, with
food available. Work search radius is approximately 25m, with line-of-sight
required. Supply CookedMeat on the ground or in a nearby wooden chest.
Damage the wall below 95% health. A workbench is not required by this inherited
repair routine. Observe movement/status and send LogOutput.log. The bridge records worker
activity changes and successful repairs, making results observable.

WORKSTATION CHECK: have a commoner follow within 5m of a smelter. Demonstrate
feeding fuel/ore until training completes, then set Stay. Supply coal and ore
in a nearby wooden chest. Check that it fetches supplies and loads the station.
Training and food routines are inherited from Horem's worker AI. New job
schedules, crop routines, extra guards and a leader are later work.

CHANGES FROM TEST 1:
- Corrected MobAILib's reverse BaseAI update signature to bool; preserved the
  real game update and consumed its return at the original caller.
- Restored native Tameable hover and interaction instead of the legacy
  keyboard-only overrides.
- Migrated four old EffectList.Create calls to the current six-argument API.
- Initialized the allowed workstation HashSet after AI config deserialization,
  because the old serializer dropped that collection and prevented training.
- Included current stone-building category in repair searches.
- Added worker activity and successful repair logging and native hover status.

Retained civilian-only recognition, current Piece registry lookups, delayed
registration, and stripped companion dependencies from Test 1. Guard, elf,
dwarf, enemy and legacy Friendlies NPCs do not receive this worker controller.
No spawn configs, NPC definitions, reclaim code or game saves are replaced.

VALIDATION:
- Original MobAILib reproduces exact reverse-patch startup InvalidProgramException
  in an isolated actual-Harmony/current-game harness; repaired DLL passes.
- Runtime recognition tests accept M01/F01/M02 and reject guard/Friendlies/Draugr.
- Runtime workstation initialization yields 10 known stations, rejects unrelated
  prefab names. Managed references resolve against current game and dependencies.
- Isolated real Valheim chainloader test passes. Gameplay controller, worker
  movement, repairs, station training and resource delivery need this test run.

Original AI: sonofmusk/Horem. MobAILib: RagnarsRokare. Project adaptation for
the repaired RRRCore baseline. Preserve original licenses/distribution terms.
