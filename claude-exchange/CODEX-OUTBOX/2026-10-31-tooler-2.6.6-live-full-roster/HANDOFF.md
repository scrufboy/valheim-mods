# Tooler 2.6.6 live result: source spawning and pursuit restored

Brian completed an ordinary Tooler session after the 5,000-second interval defect was corrected. This run answers the immediate question.

## Raid result

- Tooler 2.6.6 loaded.
- Four deliberate Souls raids ran: two `lwc_formidable_arena` and two `lwc_formidable_souls_swamp`.
- All six source actors spawned:
  - `RRRN_Pillager`: 51 across 35 batches
  - `RRRN_WoodsSoul`: 17 across 12 batches
  - `RRRN_NinoBosque`: 2 across 2 batches
  - `RRRN_Neckman`: 13 across 9 batches
  - `RRRN_Servant`: 5 across 5 batches
  - `RRRN_OrcBrute`: 4 across 4 batches
- The first custom raid spawned Pillagers immediately, WoodsSoul after six seconds, NinoBosque at 23 seconds, and OrcBrute at 42 seconds.
- The source actors did advance. Awareness saw the contact envelope at median 69.46 m, evidence at 54.74 m, commitment at 50.94 m, native companion target acquisition at 9.04 m, and native action at 7.89 m. Group samples contain closing speeds up to roughly 5–6 m/s from 50–65 m. `HuntPlayer` therefore had live actors and brought them into the fight.

## Safety/lifecycle result

- No `ZNetScene.RemoveObjects` recurrence.
- No `DIAG-FAULT`.
- No still-registered destruction finding.
- All tracked WoodsSoul/NinoBosque/OrcBrute deaths used the normal `ZNetScene.Destroy <- Character.OnDeath` route and were unregistered before object destruction.
- The only logged error was an unrelated companion spawn refusal on unsuitable ground.

## Formidable/companion evidence

- Core 1.0.32.29, 1,353-second session, six identities used with at most two active observers.
- Zero ownership violations, kernel faults, or awareness faults.
- Awareness cost median 0.75 ms/s; peak 4.15 ms/s; ray budget held.
- Sight notices: 178, median 54 m, maximum 80 m. Hearing notices: 27, median 27 m.
- 264 commitments; notice-to-commit median 1.4 seconds; median commitment distance 36 m.
- Native-only contacts: zero. True awareness-late targets: three, median lag 0.76 seconds.
- Two iron-q1 companions died in the first swamp fight, both to `RRRN_Neckman`; the plains-q3 pair recorded no deaths.
- Existing analyzer failures remain: combat cohesion (mean 10.2 m, max 33.7 m), calm shield handling, 10% dead time between targets, and 20% melee held against distant/flying targets.

## Bomb evidence

- 61 throws, median 13.6 m, mean 13.8 m, maximum 19.8 m.
- 19 throws were at least 15 m; 18 were under 12 m; none were below 9 m.
- This confirms the 9–20 m execution envelope is active. It does not establish visually whether each throw was tactically early enough; no new video file was found on disk.

## Preserved evidence

- Log archive: `BepInEx/LivingWorldBackups/sessions/2026-10-31-tooler-2.6.6-full-souls-raids/LogOutput.log`
- Log SHA-256: `E2C627707205996B80004DBF748A79860F075F52550BC53675C59F3EA8E3EFFF`
- Raw telemetry: `BepInEx/LivingWorldTelemetry/LWC-20261031-172019-LWCThrowaway-part00.jsonl`
- Telemetry SHA-256: `417CDC0F86A80D0309154057607C2DD9279B00D5ACC6C6399A4413DFC5BDAD40`

The spawn-interval repair is live-proven. Do not reopen prefab containment or manual fixture testing from this result.
