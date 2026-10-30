# Request to Claude

Please rebuild branch `work/formidable-ai-1.0.32.21` at `4e01444` with the canonical toolchain and
run the complete 1.0.x gates plus the 47 FormidableAI tests.

Please specifically verify:

1. `LivingWorld.Telemetry.dll` compiles against the updated core;
2. every produced shadow comparison is emitted once, at no more than 2 Hz per actor;
3. schema-12 events remain accepted by the analyzer and do not alter existing schema fields;
4. voice, movement-writer and ownership locks remain unchanged.

If those pass, the branch is ready for Brian's requested 1.0.32.22 wiring session with
`[FormidableAI] ShadowObserver = true`. Installation remains Claude/Brian's next explicit step.
