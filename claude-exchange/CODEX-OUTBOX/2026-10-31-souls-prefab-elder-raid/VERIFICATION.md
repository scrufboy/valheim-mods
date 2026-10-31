# Verification

- **OFFLINE-TESTED:** compatibility source compiles against Valheim 1.0.21,
  BepInEx 5.4.23.6 and Harmony from `LWC Formidable Arena`.
- **OFFLINE-TESTED:** installed compatibility DLL matches the build at SHA-256
  `dd3073faeee77948b35522e93386e6087a3b820b694abe5917280fb199c45686`.
- **OFFLINE-TESTED:** `Verify.ps1` passes the Elder pacing, group caps,
  suppression, 70-95 m ring, no-forced-player-target, Legionnaire heavy slot,
  installed hash, Spawn That presence and six-prefab natural roster checks.
- **INSTALLED:** active raid config SHA-256
  `824042f3fe0cb8fbe26e47be86e582eb7d90765ee9f695da6a5f332f10cc2b49`.
- **PRESERVED:** replaced raid files and BepInEx loader caches are under
  `BepInEx\LivingWorldBackups\2026-10-31-before-souls-prefab-and-elder-raids`.
- **LIVE-PROVEN 2026-10-31:** isolated Arena world load on Valheim 1.0.21 and
  C.O.R.E. 1.0.32.27 loaded 15 expected plug-ins. Spawn That 1.2.20, Custom
  Raids 1.8.2, RRRCore 3.1.5 and the compatibility plug-in were all present.
- **LIVE-PROVEN:** `RRR_NPC base rebuilt from the current Player prefab graph`
  executed and all six named Souls prefabs reported `humanoid=True
  monsterAI=True`, followed by `SOULS-PREFAB SUMMARY 6/6 ready`.
- **LIVE-PROVEN:** complete launch/world-load log: zero error lines, zero
  `DesignNpcBase` exceptions, zero missing/invalid Souls prefab records and zero
  `Unable to find spawn RRRN_*` warnings.
- **NOT YET LIVE-PROVEN:** natural-spawn frequency, raid composition/pacing in
  play, and combat behavior under those encounters. Those are the next video
  and telemetry evidence.

