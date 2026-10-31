# A1 rev 5 (1.0.32.27) from both live sessions, and the proposed A2 boundary

Second session ingested: MonsterDB-created / native-`MonsterAI` compatibility is **LIVE-PROVEN** (narrow claim only). No
long-range mod-hostile, custom-AI fallback, group or bomb claims are made from the two close clone spawns.

## Candidate
`work/awareness-a1` **`7c22507`**, version 1.0.32.27, parent `eb28f1c` (approved/installed .26). Not installed.
Changed: `CorePlugin.cs` (version), `Awareness.Pure.cs`, `AwarenessGroup.cs`, `CoreTelemetry.cs`,
`RegressionLedgerTests.cs`, `analyze_session.py`.
Gates: ledger **157/157**; native 46; ownership 76 (unchanged); voice LOCKED unchanged; movement-writer OK; analyzer OK.
Hashes: core `389a2b746e54bd0f991d6d4c77c37359991df86ce0c728b324bce3267e7032d6`,
telemetry `3c3c08e5b25ad5578d393735ba37788f6805cdec040a98d00196f04d1f35e165`.

## What changed, and the evidence from both sessions

| Change | Evidence (session 1 / session 2) |
|---|---|
| **10 Hz step** for interpretation, rays, groups, posture, member edges (candidate pass stays 4 Hz) | the service stepped every frame: 78.5 / 79 steps/s; CPU median 1.66 / 1.53 ms/s, max 4.9 / 4.56. Expected about 8x less step work; rays per second are unchanged by design. **To be measured** in the next session; milestone times now quantized to 0.1 s |
| **Urgent 0.25 s refresh** for a contact awaiting its confirming sample (State None, 1 sample) and for **Alerted** contacts | every native-target-before-notice case had the contact in **State None** (s1: 4 cases, lag 0.09–1.19 s; s2: 1 case, 1.40 s). The confirming sample waited for the 1 s far refresh: s2 Evidence→Noticed median is **exactly 1.00 s**. Alerted contacts beyond 20 m committed on the next 1 s sample (Troll s1: lag 1.5 s at 26 m). **Range unchanged.** |
| **Analyzer:** native target before commitment split into *companion engaged first* (companion struck before commitment, or the contact never committed) and *awareness late*; the commitment lead uses late cases only. Timelines carry a `wildlife` flag | s1: 9 = 4 engaged first + 5 late (median lag 0.64 s). s2: 10 = 7 engaged first (boars, Necks, skeleton; 4 never committed) + 3 late (1.40 s). Counting provoked or passive creatures as "late" overstated the gap |

Corrections **not** made, because the evidence doesn't support them: notice range (sight median 28 m in s1 at night, 50 m in s2
by day, max 64 m, under the 60 m ceiling with salience); the commit rules (every live commitment was perceptible targeting
or harm, and none was wrong in the video review).

## Proposed A2 boundary (for review, not built)

A2 = **posture only**. The shadow posture (`Calm / Observe / Readiness / HoldStealth / Combat`) starts to drive only what is
already posture-level today, through the existing owners, with no new writer:
1. **Readiness from credible threat.** `PhaseArbiter` gets `Readiness` from an `Alerted` non-wildlife contact (as well as
   the leader armed / native alert). `Observe` maps to the existing `AwarenessLook` (look at the contact), not to
   weapons. Wildlife never readies.
2. **HoldStealth.** While the leader sneaks, `Alerted` contacts do not raise Readiness. `Committed` still does.
3. **Not in A2:** targeting (A3), commitment → Combat (A4), bombs and bows (A5). `TargetSelector`, `NativeCombatBehavior`
   and `StepBomb` are untouched.
4. **Gates:** ownership and movement-writer unchanged (the phase arbiter and `AwarenessLook` are existing owners); voice
   lock unchanged; a feature flag (`AwarenessPosture`, default off) with immediate fallback to today's phase rules;
   ledger tests for the mapping; one live session compared against the A1 shadow posture already recorded.

## Deterministic fixture for the VisibleOnly reader (design; needs a go)

A dev-only console command `lwfixture visibleonly <distance>`, in a **separate lab DLL** so the runtime ownership gate stays
untouched:
- spawns one durable Greydwarf-bodied actor at the given distance in front of the party, with its `MonsterAI`/`BaseAI`
  component removed (so no reader exists) and high health;
- drives it with a fixed script: stand 3 s, face the nearest companion 2 s, charge at 4 m/s, strike once in reach, retreat;
- the session record must show reader `VisibleOnly`, then Noticed, then Alerted (running), then Committed (facing and charging),
  with milestone times that match the script within 0.25 s.

Brian's whole part would be one command and about 30 seconds, not a general play session.
