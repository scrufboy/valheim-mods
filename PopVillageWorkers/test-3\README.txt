PoP Village Workers - Test 3 (0.1.2)

Exit Valheim. Extract into the Souls profile BepInEx folder. Replace all.
Use the existing reclaimed test village; no new world is needed.

First check normal player movement and controller follow/stay interaction.
Then leave one fed commoner in Stay beside a visible damaged wooden wall
within 25m. Wall health should be below 95%. No workbench or repair materials
are required. Provide the log and whether it moved/worked. Successful repairs
and activity changes are logged.

Test 2 was withdrawn after the real-world log showed a per-update exception
flood. Its DLLs were moved out of the active profile into a recoverable backup.
This package reintroduces the worker addon with the following corrections:

- Both idle and hungry RandomMovement reflection calls supply the game's current
  three parameters (dt, centerPoint, snapToGround=false), not the obsolete two.
- Removed Harmony discovery for all three unsupported experimental off-screen
  roaming hooks; they caused a startup patch failure on the full profile.
- A worker-specific update exception unregisters that worker for this session,
  logs the failure once, and permits native AI on subsequent updates. The bridge
  does not repeatedly re-register a failed worker during that session.
- Native controller interaction and hover remain; AI status is appended.
- Previous reverse-update, current effect API, workstation permissions, stone
  category, and Piece registry repairs are retained.

Reference and signature checks pass against Valheim 1.0.17. Isolated Unity startup
and archive validation are checked before delivery. These checks do not execute
in-world NPC movement, pathfinding, repair or workstation work. Test 3 still
requires this gameplay test and is not a finished simulation release.

No PoP NPC definitions, spawn rules, reclamation DLLs, game saves, or Souls enemy
content are changed. The addon includes four DLLs under plugins/PoPVillageWorkers.
All three PoP commoner types are eligible; guards keep their original controller.

Training remains required for workstations: have a commoner follow within 5m,
demonstrate fuel/ore loading repeatedly, then Stay near the station and supply
coal/ore in a nearby wooden chest. Test repairs and player movement first.

Original AI authors: sonofmusk/Horem; MobAILib: RagnarsRokare.
Private project adaptation; preserve upstream licenses/distribution terms.
