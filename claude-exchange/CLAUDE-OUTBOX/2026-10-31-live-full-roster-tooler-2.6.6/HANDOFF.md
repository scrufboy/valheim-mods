# Live session 17:20–17:43 — full Souls roster, Tooler 2.6.6 (Claude, 2026-10-31)

Sources: LogOutput.log (17:20 launch) and telemetry LWC-20261031-172019-LWCThrowaway. The video (Y:\OBS OUTPUT, 17-07-41 to 17-35-50) has not been reviewed yet.

## Error question (LIVE-PROVEN for this session)
- **0 NullReferenceException** lines. The game logged one Error: PARTY FAILED "stand on open loaded ground", from our mod's own guard.
- RRR-LIFE: armed 14/14; 0 DIAG-FAULT, 0 LEAK, 0 SCAN/BAD-INSTANCE, 0 STAGE THREW.
- Instances: NinoBosque registered 5×, OrcBrute 4×, WoodsSoul 20×. Every one passed Character.Awake, BaseAI.Awake, Humanoid.Start and GiveDefaultItems, and attached its gear
  (EphemeralHelm, GrimAgeHornedHelm, GrimAgeChaosWarrior, ShieldBoneTower, Torch, etc.).
- Removal: all 21 kills went through Character.OnDeath → ZNetScene.Destroy. That includes **all 4 OrcBrutes** and 1 NinoBosque. Every other removal was a clean unregistered destroy (zone unload or quit).
- Conclusion: in this session neither NinoBosque nor OrcBrute produced the error and both behaved normally through spawn, gear, AI start, death and removal. The source of the earlier bursts is still UNKNOWN.

## Raids (LIVE-PROVEN that they ran)
army_theelder 17:22:26, lwc_formidable_arena 17:24:38 and 17:35:47, lwc_formidable_souls_swamp 17:28:38 and 17:38:16, army_goblin 17:31:56.
Awareness telemetry: first evidence median 69 m, Noticed 53 m, native engagement median 9 m, i.e. raiders closed from the ring into melee range. 21 tracked Souls kills.

## Anomaly worth checking (LIVE-PROVEN position; cause UNKNOWN)
One NinoBosque spawned at t=370 at (27.3, 50.1, -485.0). It re-registered at exactly (27.2, 49.1, -480.9) at t=623, 667 and 1247, i.e. each time its zone reloaded, and lived 490 s.
It did not move for ~15 minutes, despite HuntPlayer. Possible stuck spawn (terrain) or non-hunting AI. Video around 17:26 should show it.

## Companion outcome (telemetry)
Bjorn and Ragnfrid died to Swamp Neckman in the swamp raid (t=547/553). Analyzer: 94 checks, 6 fail (party spread in combat, shield on back in calm 55%, 10% dead time, 20% melee for distant targets, two deaths).
