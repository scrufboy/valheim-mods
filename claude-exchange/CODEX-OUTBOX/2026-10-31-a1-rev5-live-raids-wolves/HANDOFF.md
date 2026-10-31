# A1 revision 5 live result: ordinary raids, ranged bombs, wolves, and failed Souls setup

## What was actually live

- **INSTALLED:** `work/awareness-a1` at `7c22507`, Living World C.O.R.E. NPC
  `1.0.32.27`, built reproducibly against Valheim 1.0.21 and installed only in
  `LWC Formidable Arena`.
- **INSTALLED:** actual DLL hashes are core
  `979972ebe0a617ddb7189295c9932b4c988c615d05fc6e2f260aa1acd190edfa` and
  telemetry `42298a29ef4b5cc0951f4c81907df0b0dedfa827a561d0c3b47a5d096879a270`.
- **LIVE-PROVEN:** A1 remained shadow-only. It did not target, move, ready,
  attack, block or throw for the companions.
- **LIVE-PROVEN:** only Ingegerd Arnesdottir and Gudfrid Thorstensdottir
  completed appearance and joined the party. Four later requested actors never
  completed initialization and must not be counted as a functioning six-person
  party.

## A1 revision 5 result

- **LIVE-PROVEN:** the session ran 1,442 s with zero awareness faults, zero
  domain violations, zero kernel/behavior faults, zero initiative/contact loss,
  and a peak of 50 rays/s inside the 48/s plus burst budget.
- **LIVE-PROVEN:** revision 5 reduced measured awareness cost from the earlier
  1.53-1.66 ms/s medians to **0.55 ms/s median** in this session. Maximum was
  3.99 ms/s; median heap growth was 0 KB/s; two observers were active.
- **LIVE-PROVEN:** sight notices were n=131, median 49 m, maximum 71 m; hearing
  notices were n=50, median 29 m, maximum 37 m. There were 196 commitments;
  median notice-to-commit was 0.3 s and median commitment distance was 36 m.
- **LIVE-PROVEN:** the initiative ledger was complete: 178 records, zero
  native-only contacts and zero ring loss. Only three cases were classified as
  true awareness-late, with median lag 0.42 s. The other five native-first cases
  were companions initiating rather than awareness missing a committed threat.
- **LIVE-PROVEN:** awareness was materially earlier than the unchanged native
  combat response. Median distances were Envelope 57.97 m, Evidence 46.44 m,
  Noticed 42.97 m, Committed 38.60 m, native member target 14.24 m, and native
  member action 11.70 m. Median Committed-to-native-target delay was 6.28 s.

This is sufficient live evidence to accept the revision-5 scheduling/cost change
and use A1 as the factual input to A2. It is not evidence that A1 controlled any
combat action.

## Bomb-range result

- **LIVE-PROVEN:** 25 bombs were thrown in the ordinary raid and mountain
  encounters. Release distance was 9.1-20.0 m, median 15.7 m. Fourteen throws
  were at least 15 m, three were at least 18 m, and none violated the 9 m floor.
- **LIVE-PROVEN:** friend clearance was 6.2-23.8 m, median 12.6 m. Sixteen throws
  saw two incoming enemies and one saw three.
- **SOURCE-INSPECTED:** the three OBS recordings show poison being established
  ahead of the companions rather than repeatedly at their feet. Brian's live
  judgment was “much, much better.”
- **LIMIT:** the current bomb behavior still consumes native combat acquisition;
  A1 was not its authority. This proves the 9-20 m release law is a successful
  improvement, not that FormidableAI already owns ranged opening initiative.

## Souls raid failure is not a failed player test

- **FAILED:** RRRCore 3.1.5 threw `NullReferenceException` in
  `RRRCore.npc.Npc.DesignNpcBase`, called by `MakePrefab` during ObjectDB
  initialization.
- **FAILED:** Custom Raids consequently could not find any requested Souls
  prefab: `RRRN_Pillager`, `RRRN_OrcBrute`, `RRRN_WoodsSoul`, `RRRN_Neckman`,
  `RRRN_NinoBosque`, or `RRRN_Servant`.
- **LIVE-PROVEN:** `lwc_formidable_arena` could start as an event, but it had no
  valid Souls spawn prefab and therefore produced no Souls actors on any run.
- **CORRECTION:** the earlier launch gate stopped too early. It proved the
  three-instruction `ZRoutedRpc.Everybody` patch allowed RRRCore to load at the
  plugin stage, but it did not wait for the world/ObjectDB stage where this
  second incompatibility occurs. The prior statement that patched RRRCore had
  loaded “without the exception” was incomplete and is superseded by this world
  load evidence.

Do not ask Brian to repeat the Souls raid. RRRCore/prefab construction must be
repaired and all six prefab names confirmed registered offline/in a launch gate
before another Souls live session has any purpose.

## Wolves and the four failed replacement actors

- **LIVE-PROVEN:** the wolf raid killed the two active companions: Ingegerd at
  t=1340 and Gudfrid at t=1361. Wolves repeatedly landed 14-41 damage at roughly
  1.1-2.5 m. Blocking was disabled in the test configuration, so this is useful
  pressure/defense evidence but not a complete defense verdict.
- **LIVE-PROVEN:** after those deaths, two requested pairs existed only in
  `Initializing` for about 9.6 s each, then the party transactions failed:
  Ingeborg/Hallvard on missing rendered Hair17/Hair16 and Aslak/Bjorn on
  Hair3/Hair38. The log says `No partial party was kept` for both transactions.
- **CORRECTION:** the analyzer's headline “Companions present 6” is misleading.
  It counts any `Npc` sample, including actors that never leave `Initializing`.
  Operationally this session had two companions, followed by four failed spawn
  attempts.

## Other unchanged combat evidence

- **LIVE-PROVEN:** the two active companions dealt 10,461 damage and 152 kills
  between them. Six of eight heavy attacks on staggered targets landed; median
  heavy damage was 292 versus 49 for ordinary melee.
- **LIVE-PROVEN:** party spread still needs work (mean 8.4 m, maximum 29.8 m;
  11% of combat samples over 16 m), combat handoff churn was 10.2/min, and 8%
  of fighting samples had no target despite a nearby hostile.

