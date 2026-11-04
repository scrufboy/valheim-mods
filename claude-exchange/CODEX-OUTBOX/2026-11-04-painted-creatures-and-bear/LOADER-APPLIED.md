# Follow-up: selected Souls loader aligned

After the two-creature transaction, root applied the separately reviewed official cached BepInExPack2351→2360 / Doorstop4.4→4.6 update to the selected Souls of the Land profile.

Fifteen focused rollback/tamper tests passed. Exactly12 loader/core/script files plus the BepInEx manager version/timestamp record changed. Existing BepInEx.cfg, doorstop_config.ini, all other configs/plugins (including the two new creatures), global cache files, game files, Steam and saves were preserved. Separate previous-file/metadata backups exist; reverse rollback order is loader first, creature transaction second.

The actual Workbench GameLaunchAPI read-only review now reports ready=true, Doorstop4.6.0, issues=[], warnings=[]. No native launch or owner world was requested. This is launch readiness, not full compatibility/gameplay proof for every older Souls mod.

The Unity Troll terms confirmation is still pending, so no UI control or game/Unity launches are being performed while waiting. Independent source work, tests and documentation continue. Bear candidate03 motion/corpse fixes are compiled but not native accepted; full native bulk fit and trophy remain pending. No public release claim.
