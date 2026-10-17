Test 3 optional roaming repair
=============================

Test 2 world log proves MobAILib.Awake aborts PatchAll with Undefined target
ZDOMan_SendZDOToPeers_Patch. Merely observing all plugins listed as loaded
does not mean each Awake completed. The previous startup warning was a
real failure indication and must not be treated as harmless.

Removed the HarmonyPatch discovery attributes from all three optional
unloaded-zone roaming hooks: ZoneSystem_Update_Patch,
ZDOMan_SendZDOToPeers_Patch and ZDOMan_FindSectorObjects_Patch. This
disables the complete experimental capability instead of leaving partial
network ownership behavior active. These hooks already do nothing when
RoamingAI=false; loaded-area workers use separate active patches.

No restored alias for SendZDOToPeers is asserted. No game method or
ordinary world update is replaced. Other MobAILib patches remain intact.

BaseAI reverse bool handling audited: before that call the prefix checks
valid nview and ownership. Native BaseAI false returns correspond exactly
to invalid view or nonownership. The normal worker owner path returns true.
The game MonoUpdatersExtra loop itself discards bool results, so existing
prefix default result does not alter scheduling in this version.

Input: work/PopVillageWorkers/disabled-test-2/MobAILib.dll
Output: work/ai-startup-fix/MobAILib-Test3.dll
Tests: zero unresolved unique references; actual Harmony/current BaseAI
reverse compilation passes. Real Unity/world verification remains with
the combined repair package; this DLL does not include the concurrent
Common.Invoke reflection repair.

PatchMobAI.exe is now compatible with original void or Test2 bool input,
so the worker-reflection patched library can be fed through it to combine
the repairs reproducibly. It expects all three original roaming attrs.
