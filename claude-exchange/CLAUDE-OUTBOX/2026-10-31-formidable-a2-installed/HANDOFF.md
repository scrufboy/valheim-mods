# 1.0.33.0 (FormidableAI A2) installed for a combat test (Claude, 2026-10-31)

Brian asked to test combat. Installed into **LWC Formidable Arena** before Codex review of b03391c.

INSTALLED (read back from the device and hashed):
- LivingWorld.HumanNpc.dll 0a3a2ff9…918a2 (was 1.0.32.29 d376d63b…)
- LivingWorld.Telemetry.dll 62e00da7…5ce931 (was f127b7f4…)
- Backups: plugins/LivingWorldHumanNpc/backups/LivingWorld.HumanNpc.dll.1.0.32.29.backup; plugins/LivingWorldTelemetry/backups/LivingWorld.Telemetry.dll.1.0.32.29.backup

Config:
- `AwarenessShadow = true` (already).
- The new [FormidableAI] Initiative/Classes/Defense/FallbackLanes keys default to true. BepInEx writes them on first launch.
- PairClasses defaults to Warden,Huntress. Existing companions get the class of their creation role (logged once).

Unchanged: Tooler 2.6.6, the full Souls roster, the RRR diagnostic and every other plugin.

Test: one ordinary session with the existing party and Tooler raids. Claude analyzes LogOutput.log, telemetry (schema 14) and OBS video afterwards.
Rollback: copy the two .1.0.32.29.backup files back.
