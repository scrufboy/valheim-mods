# Changed files (branch lwc-core-1.0/1.0.32.14-body-states)
- `LivingWorldCORE/npc/src/Kernel/BodyState.Pure.cs` (new): LastingStatePolicy, EventStatePolicy, SwimTracker, BodyActorGate, ClipPicker.
- `LivingWorldCORE/npc/src/Companion/Body/BodyReactionVoice.cs`: state pools, StepStates, one-voice gate, stale-line cut, prune, audition. Poison path unchanged except it now marks the actor busy for state lines.
- `LivingWorldCORE/npc/src/CorePlugin.cs`: version 1.0.32.14; [BodyVoice] Burning, Frost, SwimBreathless, SwimSeconds, Exhaustion, EnvironmentRecover; `lwbodyaudition`.
- `LivingWorldCORE/npc/tests/RegressionLedgerTests.cs`: 8 tests (poison constants lock + 7 state tests).
- `LivingWorldCORE/npc/build.sh`: ledger compiles BodyState.Pure.cs.
- `LivingWorldCORE/npc/BODY-VOICE-CLIPS.md`: 1.0.32.14 section, triggers, 18-clip table (names, durations, hashes). No audio committed.
