# 1.0.32.15 live: guard/pacing regression found and fixed as 1.0.32.16 (Claude, 2026-10-29)

**Brian, live, 14:32 session:**
- With his weapon drawn, the companions stand still behind him and do not move into guard positions.
- With it sheathed, the guard routine became "walk to a location, look, run back to the same location".
- Walking behind him while he stands is "a little slow".
- "Before the pacing and spacing got installed" they behaved properly.

**LIVE-PROVEN (telemetry, five sessions). Companion moving speed while the leader stands:**

| Session | Build | Calm | Readiness (weapon drawn) |
| --- | --- | --- | --- |
| 10-27 02:44 | 1.0.32.7 | 3.42 m/s | 3.9 m/s |
| 10-28 12:52 | 1.0.32.8b (spacing) | 2.71 m/s | 3.8 m/s |
| 10-28 21:22 | 1.0.32.14 (with 1.0.32.10 gait) | 0.71 m/s | 0.66 m/s |
| 10-29 14:32 | 1.0.32.15 | 0.76 m/s | 0.64 m/s |

With the weapon drawn and the leader standing, companions were still 79% of the time under 1.0.32.15, against 41% under
1.0.32.7 and 14% under 8b. **Spacing (8b) did not cause it. The 1.0.32.10 gait law did.**

**Cause (SOURCE-INSPECTED):** `GaitPolicy.Decide` matched a *standing* leader's speed (0) + 0.15, floored at 0.8 m/s
and in walk mode. Every walk to a formation spot or after a guard sweep therefore crawled.
- A ledger test had enshrined that ("a standing leader never below 0.8 m/s").
- Settled-guard approaches themselves are excluded from gait, but the returns to formation (which gate the next sweep)
  were not.
- Spacing back-off loops also lasted far longer at that speed: one companion repeated SPACE-BACKOFF for about 12 s.

**Fix, 1.0.32.16 (`lwc-core-1.0/1.0.32.16-gait-standing` @ 42fb9cb):** gait matching applies only while the leader moves
(at or above 0.35 m/s, the kernel's LeaderMoving threshold). A standing leader means native pace. Walking-pace matching is
unchanged.
- OFFLINE-TESTED: ledger 125/125 (new test, plus the old assertion corrected); all gates pass against 1.0.21.
- INSTALLED: hash `53de430c…`. 1.0.32.15 backed up. Live test pending.

**Video:** the 14:34 recording has about 80 s of play (the game closed at 14:35:39) and shows the weapon-drawn
standstill. A config fallback exists: `[Movement] MatchGait=false` restores the 1.0.32.9 pace.
