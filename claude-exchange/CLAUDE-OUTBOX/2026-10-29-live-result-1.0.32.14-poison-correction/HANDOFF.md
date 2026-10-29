# Correction to 2026-10-28-live-result-1.0.32.14: the bomb poison works (Claude, 2026-10-29)

The earlier finding "bombs barely hurt" counted impact damage only. Poison ticks (`SE_Poison`, HitData with no
attacker) never reach `StrikeHook`, so telemetry cannot see them.

**LIVE-PROVEN** from the native `Poison damage:` lines in `LogOutput.log` (149 lines, values 20 or 40, ttl about 5.5 s,
5 ticks). They are grouped in time against bomb hits (telemetry t0 = 21:22:11):
- Greyling hits at t498 and t530: 3 and 33 poison applications of 20 (the cloud re-applying to a crowd).
- **Skeleton hit at t630 and Draugr hit at t710: no poison application.** This is consistent with undead poison
  immunity (INFERRED; game data under research).
- Fuling Berserker (GoblinBrute) fight at t987–1001, bomb hit at t996: 13 applications of **40**.
- Fuling, Greyling and Troll raid, t1355–1432: 65 applications of 20/40. Fulings, t1457–1491: 24 applications of 40.
- 40 versus 20 suggests Fulings are weak to poison. INFERRED.
- The poison lines at t465–469 came before any throw (Elder raid). The victim is not named in the log line; no
  companion coughed.

Also not visible in telemetry: the poison victim's identity. Stragglers out of companion range are not recorded.

**Telemetry gap to fix later (Brian decides):** record poison/burning tick damage by bomb source, for example by
remembering the bomb owner per target when the AoE applies the status.

A research agent is compiling per-creature damage modifiers (poison, fire, frost, spirit, and so on) with sources.
