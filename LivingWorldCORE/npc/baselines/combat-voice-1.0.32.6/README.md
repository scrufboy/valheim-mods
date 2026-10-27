# Combat-voice baseline 1.0.32.6 (LOCKED)

- `APPROVAL-RECORD.md`: approval, test evidence, immutable behavior contract, module boundary.
- `SHA256SUMS.txt` / `AUDIO-DATA-SHA256.txt`: manifest of the preserved snapshot (`Documents\LivingWorld-CORE-NPC\20261027-APPROVED-COMBAT-VOICE-1.0.32.6`, copied from the installed Lab profile). The WAVs are licensed and are NOT in git.
- Source: this tree. `./build.sh` here reproduces `LivingWorld.HumanNpc.dll` SHA-256 `4dee75d49c01a32f0250a09595b166e2deac5ad43d8b46aabeb3257b2667cc6e` byte for byte.
- Enforcement: `tools/combat_voice_gate.py` + `tools/combat_voice.lock.json` (IL, wiring, contract rules; `--voices` / `--config` for the pools and gain) and the `LOCK combat voice` tests in `tests/RegressionLedgerTests.cs`; both run in `build.sh` / `Build.ps1`.
