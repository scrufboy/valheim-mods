# Verification

## Inputs

- **LIVE-PROVEN:** `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1\BepInEx\LogOutput.log`, modified 2026-10-23 20:45:25 EDT.
- **LIVE-PROVEN:** `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1\BepInEx\LivingWorldTelemetry\LWC-20261023-202843-LWC_HumanLab_v1-part00.jsonl`, modified 2026-10-23 20:45:24 EDT.
- **SOURCE-INSPECTED:** `C:\Users\scruf\Documents\LivingWorld-CORE-NPC\src` used only to interpret the telemetry owners.

## Session shape

- **LIVE-PROVEN:** `RunStart core=1.0.0 schema=10 world=LWC_HumanLab_v1`.
- **LIVE-PROVEN:** `RunStop t=1001.095`, 5,083 total records: 2,876 NPC, 1,981 Leader, 211 Phase, 14 Torch, one RunStart, and one RunStop.
- **LIVE-PROVEN:** Four combat windows were observed: a Deer target window; a Greydwarf/Greyling/Shaman window; a Greydwarf window; and a Skeleton/Draugr window.

## Analyzer result reproduced

- **LIVE-PROVEN:** PASS — two companions present.
- **LIVE-PROVEN:** PASS — zero domain violations.
- **LIVE-PROVEN:** PASS — zero kernel/behavior faults.
- **LIVE-PROVEN:** PASS — torch absent from combat/readiness under the current analyzer contract.
- **LIVE-PROVEN:** PASS — calm-night torch eligibility 90%.
- **LIVE-PROVEN:** PASS — zero torch toggles shorter than three seconds.
- **LIVE-PROVEN:** PASS — back weapons visible and calm stow stable.
- **LIVE-PROVEN:** PASS — crouch match 100%.
- **UNKNOWN:** Sit was not exercised.
- **FAILED:** 148 Combat/CombatClear flips, 26.1 per minute.
- **FAILED:** Encounter releases reached 28 for one actor.
- **LIVE-PROVEN:** PASS — current party-distance gate.
- **FAILED:** Both actors were absent well before session end.

## Direct calculations

- **LIVE-PROVEN:** Archer bow samples: 356 total, 284 in Combat, 279 with targets.
- **LIVE-PROVEN:** Archer weapon transitions: 120 total, including 55 bow-to-sword and 53 sword-to-bow.
- **LIVE-PROVEN:** Bow episode median 2.01 seconds; 24/55 bow episodes below two seconds.
- **LIVE-PROVEN:** Bow target distance below 8 m in 218/279 target-present bow samples.
- **LIVE-PROVEN:** Guard shield-in-hand combat samples: 0/466.
- **LIVE-PROVEN:** Final encounter: guard 250 to last-observed 60.22 HP; archer 250 to last-observed 6.82 HP.

## Limits

- **UNKNOWN:** No current field proves an arrow was drawn, released, or hit.
- **UNKNOWN:** No current field proves an attack, block, parry, stagger, or dodge.
- **UNKNOWN:** Target names are type/display names, not stable instance identities.
- **UNKNOWN:** No death/despawn/destroy event establishes why each companion stopped sampling.
- **UNKNOWN:** No new video matching this session was found during this review; conclusions are log/telemetry/source based.

## Mutation check

- **OFFLINE-TESTED:** No gameplay source, build output, installed DLL, profile configuration, or asset was modified during this review.
