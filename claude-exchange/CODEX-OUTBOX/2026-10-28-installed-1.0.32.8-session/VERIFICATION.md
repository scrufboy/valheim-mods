# Verification: installed 1.0.32.8 session

| Evidence | Result |
| --- | --- |
| Game exit | LIVE-PROVEN: BepInEx log ends in ordinary Valheim shutdown at 13:06:38; `RunStop` at t=832.699 |
| Installed plugin | INSTALLED: 1.0.32.8 log text; DLL SHA-256 `7CC4647364281C878A2C74CDCB209268A7DA53B0B4C5822BE0B311B28EC8D039` |
| OBS first clip | LIVE-PROVEN: `Y:\OBS OUTPUT\OBS OUTPUT\2026-10-28 12-55-49.mp4`; 476.309 s; SHA-256 `B37B2B2A0BC220BEE6D3E1C34C1AF1EB5518DAE721CEA0EA9EF7513AFCB4B10C` |
| OBS second clip | LIVE-PROVEN: `Y:\OBS OUTPUT\OBS OUTPUT\2026-10-28 13-03-46.mp4`; 175.017 s; SHA-256 `F0FF7F817DFFE0CE21127E38B4D04BF95A4FB8BD8F9D0C61FB97C9481232FFF0` |
| Source log | LIVE-PROVEN: SHA-256 `D596C6CF9C08C7EE40914F5BB0F50EC5404FA2A599B1A3A30D1862F44FCDDA42`; Git copy is redacted, SHA-256 `FCADA25989E26B1E2CEC10118CF9BDA702B0B1E48B5872DB31B0AA4141073E7C` |
| Telemetry | LIVE-PROVEN: 5,025 JSONL lines including `RunStop` (which reports 5,024 preceding records), SHA-256 `422D1A22D6657893BEC9CF9B574C48E1B099CF08AEAFEE266B70AD83E925B758` |
| Pushing | FAILED: owner still felt pushing; 10 logged leader contacts (6.4 s total), contact tracker measures below 1 m only |
| Slow walking | LIVE-PROVEN: owner reports jitter; t=460–480 player had 0/39 near-stops, NPCs 2/39 and 4/39 with 3.84 m/s peaks |
| Structure traversal | LIVE-PROVEN: owner judged the traversal nice; independent door/seat/workstation use was not tested or expected |
| Voice and raid | LIVE-PROVEN: 38 voice events, 2 warcries, 49 killing strikes (46 after warcry); subjective loudness/fit UNKNOWN |
| Core errors | SOURCE-INSPECTED: no Living World `Error`/`Fatal` or kernel/domain fault line found in this session log |
| New code/package/install | UNKNOWN: no repair written or installed by this review; existing 1.0.32.8 profile unchanged |

The video remains in the user's OBS folder. The two local clips were inspected
at 30/15-second overview intervals and 10/2-second movement intervals. Frame
sampling supports scene/timing review; the owner's direct report is the
acceptance result for felt pushing and visual gait.
