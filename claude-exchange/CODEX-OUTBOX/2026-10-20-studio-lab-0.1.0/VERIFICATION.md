# Verification

SOURCE-INSPECTED: native game/Jotunn/BepInEx API metadata/IL; cached game Version.CurrentVersion=1.0.17. Jotunn 2.30.2. Game.SavePlayerProfile does not expose disk success. Native non-distant teleport preserves requested pose and reverts without floor; distant fallback may alter Y and is not used.

OFFLINE-TESTED: five explicit original runtime source files compiled against real net462/game/Jotunn/BepInEx references; zero errors, two obsolete API warnings. StudioJournal PASS 28 assertions. Source diff whitespace check passed. Packaging verifies source/DLL hashes, ZIP integrity and that the only DLL is the studio's own assembly.

DLL SHA256: a09d3b411ff0e88fa305694bfdf7ef936726484a666e1bf07055ef544531e989
Installable lab ZIP SHA256: ff606dbad7a216e3e8cbcdcf2a32a64f06bc7d31b47f1216e47cad78fca6845b

NOT INSTALLED. NOT LIVE-PROVEN. UNKNOWN: current installed-game ABI, rendered material quality/mesh seams/cove feasibility, native controller hover/control experience, environment settling/third-party overrides, live reload/teleport/obstruction recovery. No claim of complete companion or multiplayer support.

Review fixes before packaging: retry initial failed recovery; initiate return despite optional room-rebuild failure; retain journal until grounded/clear arrival; throttle duplicate errors; retain completed-session fence against stale player-save mirrors; native floor-safe teleport mode; background/farclip capture scope prevents distant world through mesh gaps.
