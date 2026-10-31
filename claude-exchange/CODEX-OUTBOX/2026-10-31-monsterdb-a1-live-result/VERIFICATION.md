# Verification

## Installed/live artifacts

- **INSTALLED:** Living World C.O.R.E. 1.0.32.26 with `AwarenessShadow = true` remained the
  tested runtime.
- **INSTALLED:** MonsterDB 0.4.0 was isolated in the `LWC MonsterDB A1` profile; the original
  `LWC Human Lab v1` profile did not receive `MonsterDB.dll`.
- **LIVE-PROVEN:** `BepInEx/LogOutput.log` reports `Loading [MonsterDB 0.4.0]`, successful
  `LWC_A1_Hostile` clone exports, and no A1/kernel/ownership fault.
- **LIVE-PROVEN:** Telemetry file
  `LWC-20261031-000022-LWC_HumanLab_v1-part00.jsonl`, 6,877,434 bytes, clean RunStop.
- **SOURCE-INSPECTED:** OBS files `2026-10-31 00-03-54.mp4` (476.2 s) and
  `2026-10-31 00-11-51.mp4` (288.5 s). The latter contains the clone and encounters.

## Analyzer

Ran the `work/awareness-a1` analyzer against the complete session.

- **LIVE-PROVEN:** 965 s, two companions, 3,790 NPC samples.
- **LIVE-PROVEN:** Awareness faults 0; capacity loss 0; initiative/contact loss 0;
  ownership violations 0; kernel/behavior faults 0.
- **LIVE-PROVEN:** 44 Initiative records; native-only 0; 38 sight notices with median 50 m
  and maximum 64 m; 9 hearing notices with median 18 m and maximum 27 m.
- **LIVE-PROVEN:** Cost CPU median 1.53 ms/s, maximum 4.56; heap median 0.8 KB/s.
- **LIVE-PROVEN:** Both clone ids appear in Contact, Group, Initiative, TargetChoice and Strike
  evidence. Their `targetPrefab` is exactly `LWC_A1_Hostile`.

## Existing unrelated failures

- **FAILED:** combat handoff churn 29.4/min;
- **FAILED:** cohesion returns 5.7/combat-minute;
- **FAILED:** shield on back in calm 35%;
- **FAILED:** two torch toggles under three seconds.

A1 is shadow-only; these gameplay failures are not attributed to A1 by this submission.
