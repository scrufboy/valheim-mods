# 1.0.32.7 gameplay and voice session analysis

Date: 2026-10-28. This analysis follows Brian's completed play session. The
raw `LogOutput.log`, schema-11 telemetry JSONL, and analyzer output are preserved
in `../2026-10-28-installed-1.0.32.7-session/`. No profile file was changed.

## Result

- LIVE-PROVEN: BepInEx loaded `Living World C.O.R.E. NPC 1.0.32.7`, both named
  companions were present, and gameplay produced 908 companion telemetry samples
  over 255 seconds. The telemetry recorder version was 1.0.23.
- OFFLINE-TESTED (analysis of live data): `tools/analyze_session.py` reported
  **52 checks, 4 failed**. This run is not an overall pass or promotion proof.
- LIVE-PROVEN: the female voice pools were registered and every used category
  reported its full clip count alive on first use: alerted 2/2, exert 9/9,
  ordinary hit 9/9, heavy hit 9/9, gloat/idle 4/4, war cry 3/3, death 4/4.
  The log recorded 68 non-death voice lines: 12 alerted, 27 exert, 12 hit,
  2 heavy hit, 11 idle/gloat, and 4 war cries. Each companion also triggered
  one separate death voice. Heavy exertion is logged at the attack frame.
  These events establish clip selection/playback calls; Brian's audible
  experience is not measured by telemetry. Male pools were registered but no
  male companion exercised them in this run.
- LIVE-PROVEN: combat and party behavior produced no domain ownership
  violations or kernel/behavior faults. Both archers fired 31 arrows. Melee
  chains, heavy attacks, partner defense and native combat handoff were
  exercised; their analyzer checks passed.

## Four failed checks, with scope

1. **Two deaths.** Signy died at telemetry t=224 s, Unn at t=235 s, both in
   Combat with Fuling Berserker as last attacker. The last hits included
   Berserker blows of roughly 51–62 damage. The log independently records one
   death voice for each. This is a real survival failure in the tested Plains
   q3 fight. The config had `ShieldBlocking = false`, so shield-block behavior
   was not exercised. One fall-back ended `caught`. Do not infer a code cause
   or silently count the 49 enemy kills as a successful survival test.
2. **Night torch threshold.** The checker required 60% of dark Calm samples
   lit; this run had 15/27 (56%). The 12 unlit samples were the first six
   consecutive samples for each companion, t=45.05–47.58, with `torchWanted`
   false; later dark Calm samples were lit. This is a short transition window,
   not evidence of repeated torch flicker (that check passed). More sustained
   night-Calm observation is needed before treating it as a persistent defect.
3. **Target gap threshold.** The checker found 45/664 fighting samples
   (7%) with a nearby hostile but no recorded target, above its 5% ceiling.
   Thirty-nine of those samples were labeled `CombatClear`, six `Combat`.
   Four of the six `Combat` samples still had `attacking=true`, so this metric
   alone cannot establish that the companions stood idle. Review the six
   Combat timestamps and target handoff before assigning a fix.

The stock analyzer lists the two deaths as separate failures, making four
failed checks total. It reported 0 startup `[Error` or `Exception` log lines,
which does not cancel these gameplay failures.

## Next decision

Treat the 1.0.32.7 launch prerequisite as met, but this session is not a broad
behavioral acceptance pass. Review Brian's visual/audible report and the
death/target evidence before deciding whether to install 1.0.32.8b for a
separate spacing test. The Norse naming port remains separate and uninstalled.
