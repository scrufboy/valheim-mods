# 1.0.32.14 live session result (Claude, 2026-10-28)

Session: Brian, 21:22–21:52 local, about 30 min, world LWC_HumanLab_v1, game 1.0.21. Brian deliberately tried to
overload the companions with raids: Elder, Bonemass, Moder, forest trolls, Fulings. 16 companions were spawned over the
run, in pairs. Evidence: `BepInEx/LogOutput.log` and telemetry `LWC-20261028-212211-…jsonl` (11,723 records).
**No video:** nothing new in `Y:\OBS OUTPUT\OBS OUTPUT` (newest 13:03) or `Videos\Captures`.

## LIVE-PROVEN
- 1.0.32.14 loaded on 1.0.21. All 18 state clips and 40 poison clips loaded. No exceptions.
- **Bombs:** 27 throws, all native-executed (BOMB-DONE), 1 refused ("native attack did not start").
  - Clusters of 1–3 at 6.7–13.1 m; lead 0–5.5 m when targets were incoming.
  - friendClear ≥ 5.5 m on every throw (minimum 5.5).
  - No Damage record has a companion or the leader as attacker, so no friendly bomb damage was recorded.
  - 368 BOMB-SKIP, mostly "not worth it (single weak)".
- **Frost body voice:** 20 lines across 8 companions, male and female each from their own pool. Frost onset, then
  repeats about 8–10 s apart, then an env_recover exhale after frost ended. Every one was triggered by drake frost hits
  (`hatchling_spit_cold`). No line played over a combat line.
- **Combat voice** (locked): 340 lines, about 23.6 per combat-minute. Unchanged behavior.
- Armor parity, heavies, chains and split-pair defense all pass the analyzer.

## Findings
1. **Bombs barely hurt.** 22 hits recorded while holding the throw item: 17 at 0 damage, the rest 2–10, 0 kills.
   UNKNOWN whether the ooze poison cloud spawned (no video; poison on enemies is not in telemetry). The bombs are
   thrown well but have almost no effect.
2. **Drakes killed 5 companions.** Every death was a drake.
   - Hits were about 90–104 damage while the companion was **Wet**, and about 53–60 when dry, both at armor 29.
   - The leader took about 28 from the same spit.
   - Measured: Wet roughly ×1.8 frost damage on companions. This looks like the native Wet/frost interaction (INFERRED
     from data; the SE definition is not yet inspected).
   - 250 hp companions die in 2–3 wet spits.
3. **Poison body voice: NOT EXERCISED.** The leader was poisoned during the Bonemass raid (hp ticks t609–632), but no
   companion received SE_Poison.
4. **Burning, long swim and exhaustion: NOT EXERCISED.** The heavy pool never emptied (full in 95% of samples). Swimming
   happened only mid-fight.
5. **Spacing:** 4 short leader contacts (0.2–1.05 s) early in the run (t129–298), and one companion with 3 contacts
   (7.6 s total).
6. **Analyzer (104 checks, 9 failed):**
   - 5 failures are the drake deaths.
   - The others: party spread in combat (mean 9.3 m, max 37.5 m); shield in hand in 40% of calm samples; torch
     flicker (2 toggles under 3 s); dead time between targets 7% (earlier builds 13%).
7. **Telemetry recorder** is still the 1.0.23 build. Arrow hit rates above 100% show that its arrow counting is
   unreliable.

## Not changed
No code, install or config changed in response. Decisions are for Brian.
