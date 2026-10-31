# Request to Claude: stage A1 rev 5 in the bounded Souls arena

Brian wants to enter the game as soon as the candidate is genuinely ready. The live test is not another manually
spawned clone and not a defend-the-leader scenario. It is the same bounded mixed Souls raid, arriving naturally at
range, run once in his elevated/open position and once in the nearby town/MOUT position while he remains
fixed/crouched, torch-only, and in god mode.

I created the isolated r2modman profile:

`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena`

It contains Custom Raids 1.8.2, RRRCore 3.1.5, RunicPack, SagesVault, MonsterDB, and all six Souls enemy JSONs.
Spawn That is deliberately disabled so ambient spawning cannot contaminate the two-run comparison. The event file is:

`BepInEx\config\custom_raids.supplemental.lwc_formidable_arena.cfg`

Event `lwc_formidable_arena` spawns `RRRN_Pillager` (2-3) and `RRRN_OrcBrute` (1-2) at 50-65 m. Both entries use
`HuntPlayer = true`; the raid forces `Faction = PlainsMonsters` so the two Souls types remain one hostile training
horde. This does not give the companions a defend objective. They simply fight the raid.

Please do the following now:

1. Rebuild the approved A1 revision 5 head `7c22507` canonically in the Linux/Mono environment that produced the
   published candidate hashes.
2. Place the canonical DLLs in the Arena profile only, after backing up the installed 1.0.32.26 DLLs and config.
3. Verify the installed hashes are exactly:
   - core `389a2b746e54bd0f991d6d4c77c37359991df86ce0c728b324bce3267e7032d6`
   - telemetry `3c3c08e5b25ad5578d393735ba37788f6805cdec040a98d00196f04d1f35e165`
4. Keep `AwarenessShadow = true` and `ShadowObserver = true`. A2 is not in this build and nothing from awareness may
   control gameplay.
5. Do not launch Brian into the test yet. Report the installed paths and hashes so I can independently run the full
   launch gate for Custom Raids, RRRCore, the Souls prefabs, and the event on the current Valheim build.

My local Windows compiler passed 157/157 tests plus native and ownership gates, but emitted different short-branch IL
for the unchanged locked combat-voice module. The lock correctly rejected that output. I did not weaken the lock and
did not install the Windows-built DLL.
