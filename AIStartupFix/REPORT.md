MobAILib startup repair
======================

The current Valheim 1.0.17 BaseAI.UpdateAI(float) returns bool. MobAILib's
BaseAI_UpdateAI_ReversePatch.UpdateAI stand-in returns void. Harmony copies
the original method's bool-returning IL into the void stand-in. The runtime
rejects this at ret, producing the observed InvalidProgramException.

PatchMobAI.cs changes the stand-in return type to bool and consumes its
return value in its one existing caller. It retains the genuine BaseAI
reverse patch, preserving ownership checks, takeoff/landing, jump timers,
random movement timers, regeneration and time-since-hurt updates. This
does not disable base AI or replace its logic with an approximation.

Input: work/PopVillageWorkers/verify/plugins/PoPVillageWorkers/MobAILib.dll
Output: work/ai-startup-fix/MobAILib.dll
SHA256: 1FB438F49BA642D92A897F3F113BA124458F2D315CA7E8893324F5E1328D9DEC
The input's previous building registry corrections remain present.

Validation:
- ResolveAssemblyRefs: zero unresolved unique references.
- TestReverse harness with original DLL: reproduces Harmony IL Compile
  Error / InvalidProgramException using actual current game assembly and
  installed BepInEx Harmony DLL.
- Same harness with repaired DLL: reverse patch compiles successfully.
- Optional all-patches test cannot run on Windows .NET Framework host:
  current MonsterAI includes default interface methods unsupported by
  that host. All-patch/startup validation therefore requires Unity Mono.
- No live profile, saves, or game configuration modified by this task.

Reproduction commands (PowerShell, repository root):

    & .\work\ai-startup-fix\PatchMobAI.exe .\work\PopVillageWorkers\verify\plugins\PoPVillageWorkers\MobAILib.dll .\work\ai-startup-fix\MobAILib.dll
    & .\work\ai-startup-fix\TestReverse.exe .\work\ai-startup-fix\MobAILib.dll

Compilation uses the Windows Framework64 v4.0.30319 csc compiler, with
Mono.Cecil.dll for patch utility and BepInEx/core/0Harmony.dll for harness.
