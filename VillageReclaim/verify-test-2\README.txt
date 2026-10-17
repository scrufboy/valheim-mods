PoP Village Reclaim 0.1.1 - gameplay test build

Test 2 corrects village identification using ZoneSystem's registered
WoodVillage1 coordinates. It replaces the failed live Location lookup from
Test 1. No NPC definitions, stats, assets, or Souls dependencies are changed.
The untouched Test 1 world can be revisited: blocked local Draugr spawn points
should now initialize. A fresh world remains preferable for a full first-arrival
test. Do not use a world containing civilians saved before the reclaim add-on.

Requires the installed PopVillages Restored NPC definitions, repaired RRRCore,
and Spawn That 1.2.20 from the completed Souls profile.

Exit Valheim. Extract this ZIP into the profile's BepInEx folder, allowing
replacement of config/popvillages/spawn_that.local_spawners.popvillages.cfg.
The plugin belongs at plugins/PoPVillageReclaim/PoPVillageReclaim.dll.
Retain your Discovery diagnostic plugin for the test.

TEST IN A FRESH DISPOSABLE WORLD using your known seed. Do not use the previous
world with already-saved villagers to evaluate first-arrival behavior.
1. Visit the village. Its human spawn points should produce vanilla Draugr,
   not villagers. The plugin does not delete or add body piles.
2. Destroy the village's body piles and kill its remaining Draugr manually.
3. Remain within 50 metres of the village center. After three continuously
   clear minutes, the log should say Village reclaimed, then civilians spawn.
4. Save/reload. Villagers should remain unlocked and respawn when killed.
5. Visit another uncleared village; it must still have the Draugr encounter.
Provide the complete LogOutput.log and observations after these steps.

This is a compiled test build, not a runtime-verified final repair.
Clearance checks use an 80-metre horizontal radius and 50-metre vertical range
around the containing Location. Nearby unrelated Draugr or body piles can delay
clearance. Player presence is required, and an original local Draugr spawn must
have been recorded. Completion flags are saved per affected spawner, not globally.
Existing villagers are not deleted. Previously explored worlds without recorded
encounter state are not migrated automatically.
Only human PoP encounters are gated; dwarf and elf encounters are out of scope.
No worker/farming AI is included in this encounter repair.

Rollback: exit the game, remove this plugin, restore the previous spawner config.
Keep the pre-test world backup; rollback does not undo saved entities.
