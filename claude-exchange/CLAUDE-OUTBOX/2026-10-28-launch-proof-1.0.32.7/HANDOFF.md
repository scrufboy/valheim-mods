# 1.0.32.7 one-launch proof

Session: Brian, 2026-10-27 23:56 to 2026-10-28 00:01 local, world
`LWC_HumanLab_v1`, character `LWC_Tester`, profile `LWC Human Lab v1`.

| Check | Result | Evidence |
| --- | --- | --- |
| Installed DLL is 1.0.32.7 | PASS | SHA-256 `89b5beaf24550e79…`, 156,160 bytes (LIVE-PROVEN: read from the profile after the session) |
| Plugin loads as 1.0.32.7 | PASS | LogOutput.log line 27: `Loading [Living World C.O.R.E. NPC 1.0.32.7] (local.livingworld.humannpc)`; line 30 `1.0.32.7 loaded` |
| No Living World exceptions | PASS | No `Exception` or `[Error` lines in the log; warnings are Unity/Jotunn only |
| Telemetry 1.0.23 still binds | PASS | `LWC-20261027-235654-…jsonl`: 1,850 records, 0 malformed, RunStart to RunStop `shutdown`, 68 Voice events with NPC names, so the `NpcVoice.Spoke` forwarding name works |
| Companions load with identity | PASS | Signy Thorstensdottir (guard) and Unn Geirsdottir (archer) |
| Combat and voice | PASS | 101 target choices (Skeleton, Draugr, Fuling), 61 strikes, 6 heavy, 4 chains, 4 war cries, 1 fall-back; voice lines alerted, exert, hit, hit_heavy, idle, warcry; all 7 female pools report every clip alive on first use, including death |

## Observation, not a failure

Both companions died to a **Fuling Berserker** (Signy at 224 s: two hits of 62 and
52; Unn at 235 s: hits of 51 and 59). Vanilla death and removal, as designed while
mortality is gated off. This is a balance data point for the future mortality
and encounter-tier work, not a 1.0.32.7 regression.

Male voice pools were not exercised (both companions were female).

## Verdict

1.0.32.7 is **LIVE-PROVEN** for load, telemetry binding, identity, native combat
and female combat voice. Work-order step 3 is complete. Step 4 (install 1.0.32.8b
with `DevSpacingLog = true`) is unblocked.
