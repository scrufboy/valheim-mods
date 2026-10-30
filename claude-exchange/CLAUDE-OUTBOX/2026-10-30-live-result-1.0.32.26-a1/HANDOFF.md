# Live result: 1.0.32.26 A1 awareness shadow (first session)

Session `LWC-20261030-181021-LWC_HumanLab_v1-part00.jsonl`: 697 s, 2 companions. Mostly dark or misty Black Forest
(Misty, DeepForest Mist, Snow), some clear Meadows. Video: OBS `2026-10-30 18-14-08.mp4` and `18-22-05.mp4`
(telemetry started 18:10:21). Not yet aligned frame by frame. Chat segment markers are not logged by telemetry, so
alignment will be by clock.

## A1 safety (LIVE-PROVEN)
- Awareness faults 0. Ray peak 42/s (budget 48 + 12). Initiative lost 0, contact transitions lost 0.
- Capacity: evicted 0, refused 0, candidates dropped 0, group overflow 0.
- Kernel faults 0, ownership violations 0.
- **Cost above target:** cpu median 1.66 ms/s, p90 2.2, max 4.9 (target < 1, reject > 2 median); heap growth median 0.0 KB/s.
  The service steps every frame (~75/s). Proposed fix for the next revision: run interpretation and groups at 10 Hz;
  sensing is already 4 Hz.

## Initiative (LIVE data; first session, n = 31 targeted contacts)
- Native **target** vs awareness: before Evidence 1, before Noticed 4, before Committed 8 (of 30; 1 Commit absent).
- Native **action** vs awareness: before Noticed 0, before Committed 3.
- Noticed→MemberTarget median +2.41 s (min −1.19). Committed→MemberTarget median +1.35 s (min −8.80, 8 negative).
- Lead that would cover native-first: notice 0.77 m (median lag 0.37 s), commit 0.94 m (median lag 0.99 s).
- Distances (median): envelope 41 m, evidence 27 m, noticed 25 m, committed 21 m, member target 15 m, member action 11 m.
- Envelope→Evidence median 7.3 s: most of the session was dark (35 m notice limit) in forest mist and trees.
- Commitments: 47, all by perceptible targeting; none by sustained approach.
- Readers: MonsterAI 4645, BaseAI 2107 (wildlife), VisibleOnly 0, Formidable 0. **No modded actors were exercised**;
  Brian is adding a hostile mob mod for the next session.

## Gameplay checks (A1 reads nothing; differences are not attributed to it without video)
- FAIL combat churn 13.8/min (vs 6.0/min in .22). Needs video: many short skirmishes in mist.
- FAIL shield in calm 36% (known). FAIL dead time 6% (known; 1.0.6 was 13%). FAIL heavies 0 started (small fights).
- All other checks PASS; no deaths.
