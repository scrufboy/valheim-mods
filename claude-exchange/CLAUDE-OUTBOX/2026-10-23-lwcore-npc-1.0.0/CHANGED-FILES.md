# Changed files

All new; no existing file was modified except `claude-exchange/CURRENT.md`.

| File | Purpose |
|---|---|
| LivingWorldCORE/npc/src/Kernel/Kernel.Pure.cs | Domains, phases, Tuning, PhaseArbiter, CohesionLatch, DomainScheduler, TorchPolicy (no Unity) |
| LivingWorldCORE/npc/src/Kernel/GuardCoverage.Pure.cs | 0.6.0 GuardCoverageV2, renamed |
| LivingWorldCORE/npc/src/Kernel/NativeApi.cs | Every reflected private Valheim member, in one list |
| LivingWorldCORE/npc/src/Kernel/NpcBehavior.cs | Behavior base, frame, senses, blackboard |
| LivingWorldCORE/npc/src/Kernel/NpcBody.cs | Sole ownership-checked native writer (incl. 0.6.3 presentation sync, torch transaction) |
| LivingWorldCORE/npc/src/Kernel/NpcKernel.cs | Per-NPC kernel: sense, arbitrate, grant, exit-before-enter |
| LivingWorldCORE/npc/src/Behaviors/*.cs | combat, combat-regroup, awareness, stealth, sit, settled-guard, traversal, hands |
| LivingWorldCORE/npc/src/Identity/*.cs | 0.6.5 factory/appearance/identity/binding ported (same keys/prefabs) |
| LivingWorldCORE/npc/src/Embodiment/NpcWetStatus.cs | 0.3.45 wet adapter ported |
| LivingWorldCORE/npc/src/CorePlugin.cs | Registration, lwparty / lwparty new / lwstatus |
| LivingWorldCORE/npc/src/Telemetry/CoreTelemetry.cs | Read-only schema-10 recorder (replaces telemetry 0.6.5) |
| LivingWorldCORE/npc/tests/RegressionLedgerTests.cs | 23 tests, each named after a recorded failure |
| LivingWorldCORE/npc/tools/*.py | native gate, IL ownership gate, session analyzer + self-test |
| LivingWorldCORE/npc/build.sh, Build.ps1 | Gated build (Linux verified; Windows script written, not executed) |
| claude-exchange/CURRENT.md | Replaced stale 0.3.34 status with verified 2026-10-23 state |
