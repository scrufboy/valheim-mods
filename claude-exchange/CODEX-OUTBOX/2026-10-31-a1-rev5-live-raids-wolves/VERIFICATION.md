# Verification

## Raw artifacts

- **LIVE-PROVEN:** telemetry
  `LWC-20261031-073603-LWC_HumanLab_v1-part00.jsonl`, 10,702,538 bytes,
  SHA-256 `721373a9697de8c611cb59c726195c40d8d625701573000653acd0ee592d6400`.
- **LIVE-PROVEN:** log `LogOutput.log`, 441,396 bytes, SHA-256
  `e6be226c035014657d0f9037c3d6869174e3b881af48b9c0995fe033f1846a0d`.
- **SOURCE-INSPECTED:** OBS files:
  - `Y:\OBS OUTPUT\OBS OUTPUT\2026-10-31 07-41-44.mp4` (476.971 s);
  - `Y:\OBS OUTPUT\OBS OUTPUT\2026-10-31 07-49-42.mp4` (479.533 s);
  - `Y:\OBS OUTPUT\OBS OUTPUT\2026-10-31 07-57-41.mp4` (154.217 s).
- **PRESERVED:** telemetry and log copied to
  `BepInEx\LivingWorldBackups\sessions\2026-10-31-a1-rev5-raids-wolves`.

## Analyzer and log checks

- **OFFLINE-TESTED:** ran the revision-5 `tools/analyze_session.py` against the
  complete telemetry file. It read a clean RunStop at 1,442 s and reported zero
  awareness, ownership, kernel and behavior faults.
- **SOURCE-INSPECTED:** all 25 `BOMB-THROW` records were parsed directly from the
  log for distance, incoming count and friend clearance.
- **SOURCE-INSPECTED:** log lines 1281-1288 identify the RRRCore
  `DesignNpcBase` exception. Lines 1309-1316 identify every missing Souls prefab.
- **SOURCE-INSPECTED:** log lines 3925-3936 record four hair-render appearance
  failures and two failed party transactions. Telemetry confirms each actor
  remained in `Initializing` for only 20 samples and was removed at full health.

## Limits

- **UNKNOWN:** exact obsolete RRRCore prefab/component assumption causing the
  `DesignNpcBase` null dereference. It requires isolated compatibility work, not
  another gameplay session.
- **UNKNOWN:** native frame-time/save-hitch cost. Brian observed no new play
  stutter; the only reported hitch remains game saving.
- **NOT EXERCISED:** blocking; it was disabled in the test configuration.

