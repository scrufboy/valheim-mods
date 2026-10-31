# A1 second live result: MonsterDB-created hostile compatibility

## Intent

Record exactly what Brian's second 1.0.32.26 session proves, and prevent the very short
MonsterDB encounters from being mistaken for long-range tactical evidence.

## Evidence

- **LIVE-PROVEN:** Session `LWC-20261031-000022-LWC_HumanLab_v1-part00.jsonl` ran 965 s with
  two companions and produced 3,790 NPC samples, 44 Initiative records, 210 Contact records
  and 263 Group records.
- **LIVE-PROVEN:** A1 had zero faults, zero domain ownership violations, zero contact-transition
  loss, zero initiative loss, zero capacity eviction/refusal/drop/overflow and a 44 ray/s peak.
- **LIVE-PROVEN:** MonsterDB 0.4.0 loaded and cloned `Greydwarf` into the new prefab
  `LWC_A1_Hostile`; the clone, ragdoll, attack, throw and projectile YML exports were written.
- **LIVE-PROVEN:** Two `LWC_A1_Hostile` instances were discovered by A1, advanced through
  Contact/Group/Initiative state, were selected by both companions and were killed normally.
  The awareness budget's `readerMonsterAI` counter advanced during both encounters. This proves
  the mod-created clone remained compatible with the native `MonsterAI` intent reader.
- **LIVE-PROVEN:** First clone (`id -3085608`): Envelope/Evidence 5.75 m, Noticed 5.48 m,
  Alerted 4.13 m, Committed 3.51 m, MemberTarget 2.99 m, MemberAction 2.18 m; guard killed it.
- **LIVE-PROVEN:** Second clone (`id -3088666`): Envelope/Evidence 3.56 m, Noticed 3.54 m;
  after separation it Alerted/Committed at 11.39 m, native target followed 0.18 s later and
  action followed 0.47 s later; archer killed it from 11.55 m.
- **LIVE-PROVEN:** Whole-session sight noticing was n=38, median 50 m, maximum 64 m. This is
  useful A1 range evidence but must not be attributed specifically to the two close clone spawns.
- **LIVE-PROVEN:** Whole-session A1 cost remained above the intended target: CPU median
  1.53 ms/s, maximum 4.56; heap growth median 0.8 KB/s. No gameplay fault accompanied it.

## Limits and correction

- **FAILED:** The mod-hostile test setup was too close. Brian was told to use a low-health
  manually spawned Greydwarf clone; both encounters ended too quickly to measure distant
  acquisition, group handling, bomb doctrine or sustained tactical response.
- **UNKNOWN:** Actors implemented with custom AI that does not expose native `BaseAI` /
  `MonsterAI` intent were not exercised. `readerVisibleOnly` remained zero.
- **UNKNOWN:** No bomb-worthy modded group was created, so this session says nothing about
  ranged area-control opening decisions.
- **SOURCE-INSPECTED:** The test video is split into `2026-10-31 00-03-54.mp4` and
  `2026-10-31 00-11-51.mp4`; the clone/export and both short encounters are in the second file.

The narrow mod-created/native-MonsterAI compatibility question is answered. Do not ask Brian
to repeat another long general-play session for that same claim.
