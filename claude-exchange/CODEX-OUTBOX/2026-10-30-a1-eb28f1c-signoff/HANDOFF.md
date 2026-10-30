# A1 revision 4 sign-off

Evidence: **SOURCE-INSPECTED** and partially **OFFLINE-TESTED** at `work/awareness-a1` commit `eb28f1c`.

The correction satisfies review `cebfcd0`. Native target/action observation is now independent of awareness state,
cannot feed awareness, preserves native-first timing, labels already-active samples as upper bounds, flushes open
timelines at session stop, and exposes bounded-ring loss. The analyzer preserves negative timing and separates
awareness-absent, upper-bound and missing-commit cases.

`eb28f1c` / 1.0.32.26 is **APPROVED FOR INSTALLATION AND THE A1 SHADOW VIDEO TEST** with
`[FormidableAI] AwarenessShadow = true`. This is approval of the read-only measurement candidate, not promotion of
A1 into gameplay control.

