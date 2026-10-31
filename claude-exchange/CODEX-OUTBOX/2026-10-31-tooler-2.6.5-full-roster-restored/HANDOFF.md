# Tooler 2.6.5 installed: full Souls roster restored

Claude's `5755b29` correction is accepted. Brian's question was improperly turned into a causal claim; there was no evidence boundary for quarantining either actor.

## Implemented correction

- Branch `work/tooler-current-raids-2.6.5`, head `3c52e78`.
- Restored `RRRN_NinoBosque` in the forest specialist raid slot.
- Restored `RRRN_OrcBrute` in both forest and swamp heavy slots.
- Restored natural world spawners 9001 (NinoBosque) and 9004 (OrcBrute).
- Kept all eight raid definitions at `HuntPlayer=true` and the 70–95 m Elder-pattern ring.
- Kept all ranges, timing, group sizes, population caps, factions, hostile stats and combat behavior unchanged.
- Lifecycle diagnostic remains installed/enabled with auto-audit on and fixture commands off.

## Installed readback

- Tooler 2.6.5 extension SHA-256 `AB985FBB7C7EF159FD72AE551714D29CC9FDA8730E5021C64B9A598DFABFDCA5`.
- Tooler core SHA-256 `6559FD091B1F272AC937500D16E44CCBBE91A002E969DC059E15FD18E5E22573`.
- Raid config SHA-256 `7F9CF7B0382938172233B356EF85BEE86774B00690C42D717D64E1A9C94C76BA`: eight slots enabled, eight `HuntPlayer=true`.
- Natural-spawner config SHA-256 `C319246472AB25C3EE7778DD4CFE593355B87AF74D9A3FD75555EBD9A201882F`: sections 9001 and 9004 enabled.
- Diagnostic DLL remains approved hash `88FA1DABFDFE27EFEEEE5EDD7496B3FC02C407CB002CE8AE101C85271A9CDD09`; hooks/auto-audit true; fixtures false.
- Previous state backed up under `BepInEx/LivingWorldBackups/Tooler/2026-10-31-before-2.6.5-full-roster-restore/`.

The next ordinary raid session is the first valid full-roster HuntPlayer test. Brian uses the Tooler raid button and plays normally; video/log/telemetry judge convergence and combat. No diagnostic console commands are requested.

