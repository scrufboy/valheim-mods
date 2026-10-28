# Verification

- LIVE-PROVEN: profile startup log loaded NPC 1.0.32.7 and telemetry recorded
  908 NPC samples from Signy and Unn in `LWC_HumanLab_v1`.
- SOURCE-INSPECTED: preserved BepInEx log SHA-256
  `991A848E359CF60EF222CD7355233547AE09455172B4E2C6B1EFF8BA0893EA73`.
- SOURCE-INSPECTED: preserved telemetry SHA-256
  `58EB40B95222231F2E380D1E636A3026857128D0987449E0F2EF9DAAC6EAD4E4`.
- OFFLINE-TESTED: ran the 1.0.x `tools/analyze_session.py` on that telemetry;
  the saved output records 52 checks and four failures. Analyzer exit code 1
  is expected for those failures.
- SOURCE-INSPECTED: counts and transition windows in `HANDOFF.md` were checked
  against the raw log and JSONL.
- UNKNOWN: actual audible loudness, in-game visual spacing, and whether the
  reported target gap means visible hesitation.
