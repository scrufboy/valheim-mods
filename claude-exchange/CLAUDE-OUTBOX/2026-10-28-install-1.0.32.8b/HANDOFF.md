# 1.0.32.8b installed for the live spacing test

- BUILT (Claude workspace, Mono mcs 6.8, refs from Brian's Valheim 1.0.20 install,
  BepInEx core and Jotunn): source `lwc-core-1.0/1.0.32.8b-party-spacing-test` @ `33addad`.
  All gates pass: 96/96 regression ledger, native gate (39 members), ownership gate,
  analyzer self-test, combat-voice lock unchanged, movement-writer gate OK.
- Reproducible: SHA-256 `7cc4647364281c878a2c74cdcb209268a7da53b0b4c5822be0b311b28ec8d039`,
  identical to the recorded 8b hash.
- INSTALLED 2026-10-28 12:2x to `LWC Human Lab v1\BepInEx\plugins\LivingWorldHumanNpc\`;
  read-back hash matches. Version text in the DLL reads `1.0.32.8` (8b keeps the 1.0.32.8
  string): use the hash to tell builds apart.
- Telemetry recorder 1.0.23 left as installed.
- Config: added `[Movement] PartySpacing = true`, `DevSpacingLog = true`. Existing
  voice/combat settings untouched.
- Rollback copies (outside the plugin scan folder):
  `BepInEx\LivingWorldBackups\LivingWorld.HumanNpc.1.0.32.7.dll.backup` (`89b5beaf…`) and
  `local.livingworld.humannpc.cfg.1.0.32.7.backup`.
- NOT LIVE-PROVEN. Live test per the 8b review: doors, slopes, corridors, menus, physical
  contact, formation feel; `SPACE-*` log lines.
