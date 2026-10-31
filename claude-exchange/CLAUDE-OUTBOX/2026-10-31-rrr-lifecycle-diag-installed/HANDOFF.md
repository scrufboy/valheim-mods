# RRR lifecycle diagnostic 0.2.0 — installed for the audit-only gate (Claude, 2026-10-31)

Follows ebe632d.

## Installed (INSTALLED; read back from the device and re-hashed)
| path | sha256 | bytes |
|---|---|---|
| `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\BepInEx\plugins\LivingWorldRRRLifecycleDiag\LivingWorld.RRRLifecycleDiag.dll` | 88fa1dabfdfe27efeeee5edd7496b3fc02c407cb002ce8ae101c85271a9cdd09 | 29696 |
| `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\BepInEx\config\livingworld.core.rrrlifecyclediag.cfg` | 57ec6ef5e04bdebb5be0d27f0b59ca1d7182f4db6cd102de25148cc42a561074 | 759 |

Config: General.Enabled=true, General.AutoAudit=true, General.StackDepth=14, Fixture.EnableFixtureCommands=false.
Pre-check: there was no prior diagnostic folder or config in the Arena profile, so there was nothing to back up.
BepInEx may rewrite the .cfg on first load (same values; whitespace/comments may differ, so its hash may change).
Unchanged: Tooler 2.6.4, NinoBosque/OrcBrute raid and natural-spawner containment, RRRCore compat, and every other plugin and config.

## Brian's first test
Load a throwaway world in LWC Formidable Arena. No companions, combat, raids or commands. Exit after the world finishes loading. Return LogOutput.log.

## Pass criteria (Codex)
Plugin version 0.2.0; `RRR-LIFE armed 14/14 hooks`; AUDIT lines for RRRN_WoodsSoul, RRRN_NinoBosque and RRRN_OrcBrute; zero `RRR-LIFE DIAG-FAULT`.
If any criterion fails, stop. Fixtures stay off until the audit is reviewed.
