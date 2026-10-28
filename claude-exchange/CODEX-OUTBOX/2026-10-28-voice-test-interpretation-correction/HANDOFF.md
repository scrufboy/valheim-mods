# Correction: 1.0.32.7 was an intentional voice test

Date: 2026-10-28. Supersedes the **interpretation and next-decision wording**
in `../2026-10-28-1.0.32.7-session-analysis/HANDOFF.md`. Preserve the raw log,
telemetry and analyzer output there as evidence; do not relabel the owner's
intent based on the generic analyzer's survival checks.

Brian explicitly says the encounter with superior Fuling Berserkers was run
to exercise companion voices. Both NPC deaths were expected test conditions,
not evidence that their AI or the 1.0.32.7 build failed. They also exercised
the death-voice path.

The installed 1.0.32.7 launch is proven by the BepInEx startup log. The voice
path is evidenced by 68 ordinary/warcry/hit/exert/gloat playback log events,
the two death-voice events, and full available clip counts for the female pools
used. This supports moving past the **launch and voice event** prerequisite.
It does not measure perceived loudness or subjective fit; those are the owner's
observations, not automatic blockers imposed by this record. Male voices were
not exercised in this particular run.

The generic telemetry analyzer marked four checks FAIL: two intentional
deaths, a short initial night-torch transition (15/27 dark-calm samples lit),
and a 45/664 target-gap threshold mostly labeled CombatClear. Those are
diagnostic flags for later focused tests. The telemetry also shows zero domain
ownership violations, zero kernel/behavior faults, 31 arrows spent by each
archer, and active combat through the encounter. None of the four flags by
itself establishes a functional defect in the voice test or requires repeating
the Berserker fight.

The installed 1.0.32.7 DLL remains unchanged. The approved 1.0.32.8b spacing
candidate can follow its own review/install/test process; this report should
not hold it because the test party died. The older 0.3.58 naming DLL still
must not replace the 1.0.x runtime.
