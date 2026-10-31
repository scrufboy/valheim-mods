# 1.0.32.29 live result: native hair gate passed; ordinary encounters captured

Brian exited the game after a short ordinary-play session. He did not remember the custom raid commands, so no configured raid was started. He recovered his gear, spawned one party, and fought naturally spawned RRR encounters along the road.

## Appearance and party gate

- Loaded core and telemetry `1.0.32.29`.
- Installed hashes remain core `d376d63b…`, telemetry `f127b7f4…`.
- `APPEARANCE READY Aud Arnesdottir: female, hair=Hair28, beard=none, role=guard`.
- `APPEARANCE READY Vestein Alfsson: male, hair=Hair31, beard=Beard12, role=archer`.
- `PARTY READY: two companions bound to you`.
- No appearance failure and no partial party.

This live-proves the .29 complete-native-visual-root correction for the selections exercised here. It does not yet exercise rigid Hair1/Hair2 or Beard1 specifically.

## Session evidence

- Telemetry: schema/current core session, 619 s, 2 active companions, 2,040 companion samples.
- Spawn That naturally produced `RRRN_Pillager x3` and `RRRN_WoodsSoul x3` groups. No raid event was active.
- Zero domain ownership violations; zero kernel/behavior faults; zero companion deaths.
- Damage: Aud 15 hits / 903 damage / 6 kills; Vestein 25 hits / 1,427 damage / 11 kills.
- Awareness: 26 initiative records, no native-only invisible contacts, no ring overwrite, peak 23 rays/s, median CPU 0.54 ms/s, zero capacity drops.
- Notice distances in this short route: hearing median 26 m/max 33 m; sight median 28 m/max 34 m. The environment was not the long-range raid arena, so do not treat these as range limits.
- Awareness-late classification: 4 cases, median lag 1.17 s; modeled perception lead to cover them is 0.43 m. Keep this evidence for A2/core initiative review.

Existing non-appearance analyzer failures remain: combat handoff churn, cohesion return frequency, calm shield state, dead time between targets, and some melee held against flying/distant targets. These are not .29 regressions.

## Separate scene error burst

At 11:09:22, immediately before the first logged `Spawned RRRN_WoodsSoul x 3`, Valheim emitted 264 `NullReferenceException` lines over about 11 seconds from native `ZNetScene.RemoveObjects -> CreateDestroyObjects -> Update`. There was no LWC kernel fault, behavior fault, ownership violation, or appearance failure. Do not attribute this to .29 hair attachment without evidence. Preserve it for the RRR/Souls spawn-path investigation.

## Preserved raw evidence

Profile backup:

`BepInEx/LivingWorldBackups/sessions/2026-10-31-1.0.32.29-hair-and-road-encounters/`

- `LogOutput.log`, SHA-256 `1D0A818BE5CF636F5CD7DCC2D387EF4EBDE5F5E34EAA52480977D9DBCAB17AEA`
- `LWC-20261031-110335-LWC_HumanLab_v1-part00.jsonl`, SHA-256 `903285120D85E3708FA346DEC10B7B7DA98533F4603E4BC56803D42A85EF4FE8`

## Correct commands for the next deliberate raid session

In the F5 console after `devcommands`:

- `event lwc_formidable_arena`
- `event lwc_formidable_souls_swamp`

Use `stopevent` before switching to the other raid if an event is still active.

No immediate replay is requested. Analyze this preserved session first, and keep raid work separate from the proven .29 appearance correction.
