# Live 1.0.32.29 (LWC Formidable Arena): appearance gate PASSED; Souls actors fought; one foreign NRE burst

Telemetry `LWC-20261031-110335-LWC_HumanLab_v1-part00.jsonl` (619 s); `LogOutput.log` 11:02–11:13.

- **LIVE-PROVEN, appearance gate passed:**
  - `APPEARANCE READY Aud Arnesdottir: female, hair=Hair28, beard=none, role=guard`;
  - `APPEARANCE READY Vestein Alfsson: male, hair=Hair31, beard=Beard12, role=archer`;
  - `PARTY READY: two companions bound to you.`
  - Brian also saw companions spawned with hair and beards.
  - Beard1 and rigid styles weren't rolled this run, so the rigid path is covered offline only.
- **LIVE-PROVEN, Souls actors spawned naturally and were fought:** Spawn That spawned `RRRN_Pillager x3` (11:07:11) and
  `RRRN_WoodsSoul x3` (11:09:34). Companions took hits from WoodsSoul (median 4) and Pillager (median 10); no deaths.
  No raid event was started; Brian didn't recall the command.
- **Gameplay checks:** ownership violations 0, kernel faults 0, no deaths. Heavies: 5 started (all on staggered targets),
  3 landed (median 161).
  - FAIL churn 18.1/min (known).
  - FAIL cohesion returns 6.1/combat-minute (known).
  - FAIL shield in calm 37% (known).
  - FAIL dead time 6% (known).
  - FAIL bow-for-distant 16% melee.
  - FAIL **torch lit at night in calm 37%** (was 94% in the 10-30 session; needs a look, not attributed).
- **A1:** faults 0, ray peak 23/s, CPU median **0.54 ms/s**, initiative/contact loss 0, 26 records. Native target before
  commitment: 8 (4 companion engaged first, 4 late, median 1.17 s).
- **FOREIGN ERROR, unattributed:** 263 `NullReferenceException` in `ZNetScene.RemoveObjects`
  (`Component.get_gameObject`) between 11:09:22 and 11:09:32, while the party crossed a Meadows/BlackForest border
  (`GetBiome error` warnings at the same time). It then stopped. This pattern means a `ZNetView` was destroyed without
  `ZNetScene.Destroy` (dead entry in the scene's instance list). Living World logged no fault in that window. Candidates
  are the Souls/RRRCore compatibility path or another spawning mod (inference only). Worth a source check before a raid
  session.
