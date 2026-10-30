# Source evidence

- `4c09a79:LivingWorldHumanNPC/REBUILD-0.6.5.md`: invalid native target-scan constant rewrite removed;
  "No replacement scan loop is added."
- `cbc0f0f:LivingWorldHumanNPC/rebuild/HumanBehaviorStateMachine.cs`: `NativeAlert` -> readiness;
  `CommittedThreat` -> native combat.
- `cbc0f0f:LivingWorldHumanNPC/rebuild/HumanBehaviorController.cs`: commitment came from
  `ai.GetTargetCreature()`; native alert was a separate fact.
- `78bc786`: 1.0.4 restored explicit engagement rules rather than attacking every sensed creature.
- `7919a34:LivingWorldCORE/npc/src/Behaviors/TargetSelection.cs`: current target candidate build is bounded
  by `PartnerReach`/`MaxRange` and native `CanSenseTarget`/`CanSeeTarget`.
- `7919a34:LivingWorldCORE/npc/src/Kernel/NpcKernel.cs`: `ScanHostiles` uses `HostileScanRadius` and ignores
  enemies unless their own AI is already alerted.

Conclusion: extended noticing was intended but was not successfully implemented in 0.6.5. The prior state
semantics were real and should have prevented a weapon module from becoming its own perception system.
