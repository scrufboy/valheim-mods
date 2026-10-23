# Verification

## OFFLINE-TESTED (Linux/Mono 6.8, against Brian's installed assemblies, staged read-only)

- Regression ledger: 23 passed, 0 failed.
- Native gate: 16 private members verified as live instance members. Negative test: the gate rejects
  `MonsterAI.m_updateTargetIntervalNear` (the 0.6.4 failure).
- Compiled at C# 5 against assembly_valheim (mtime 2026-10-17), BepInEx 5.4.23.5 core, and Jotunn from the profile.
- Ownership gate: 42 native write sites, all in NpcBody, NpcFactory, NpcAppearance, NpcLeaderBinding and
  NpcWetStatus. Negative test: a planted behavior that calls HideHandItems and SetFollowTarget fails the gate.
- Analyzer self-test: a clean synthetic session passes; a planted-regression session fails 4 checks.
- Reproducible: two clean builds produced identical hashes.
  - LivingWorld.HumanNpc.dll `71d0f72db02e5614c9bec3bc899d49a6db045dda308755b8fcb44b4ab8bab37d`
  - LivingWorld.Telemetry.dll `68a9a5e202dbf4b1cdc87004aa1241881b051eaf6e506599d9fe6e018520e90f`

## NOT DONE

- Build.ps1 has not been executed on Windows.
- No Unity or live run. LIVE-PROVEN: nothing.

## Pre-install state (read from the profile 2026-10-23 ~03:05 EDT)

- Installed 0.6.5: HumanNpc `F8F51AB7…4417525` and Telemetry `E89AEFBC…510BBA`. These match the receipt.
- The last log (02:27) was 0.6.4 with REGISTRATION FAILED. 0.6.5 was never launched.
