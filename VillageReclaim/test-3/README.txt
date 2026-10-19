PoP Village Reclaim 0.1.2 / Test 3

Exit Valheim. Extract the ZIP into the Souls profile BepInEx folder.
Choose Replace all. Resume the same test world.
Clear any remaining Draugr and body piles, then wait near the center for three
minutes. Report whether villagers arrive and supply LogOutput.log.

Changes from Test 2:
- Block non-event SpawnSystem Draugr requests within the registered WoodVillage1
  clearance boundary (80m horizontal / 50m vertical), before instantiation.
- Mark outside world-spawned Draugr in their ZDO, excluding marked newcomers
  from the village's defender count if they wander into the village.
- Explicitly prevent a local encounter spawner from producing more than one
  original defender before reclamation. Civilians retain their respawn behavior.
- Log the active world rule's prefab, allowed biome mask, attempted position,
  and whether it was blocked, once per rule/boundary combination per session.

Runtime root rule remains unresolved: the earlier log identifies SpawnSystem,
but omits the SpawnData biome mask. The default simple Draugr multipliers are
all 1, and do not explain these spawn attempts. This test's boundary safeguard
does not claim to repair a global biome-selection defect.

Previous saved world-spawned Draugr lack the new origin marker and must be
cleared once. No existing creatures, body piles, saves, or NPC definitions
are deleted. Raids are not suppressed. Other world spawns outside the village
remain unchanged. Compile/API and archive checks passed; gameplay test pending.
