# Changed files

Source commit: `af31a58`

- `LivingWorldCORE/npc/src/Identity/NpcAppearance.cs` — restores fresh native selection/attachment, immutable saved-presentation repair, and strict native LOD renderer readiness.
- `LivingWorldCORE/npc/src/Kernel/NativeApi.cs` — centralizes the five request-cache fields and five native visual methods.
- `LivingWorldCORE/npc/tools/appearance_gate.py` — locks the fresh transaction order, saved repair, canonical ZDO commit, native attach calls, and visible-LOD acceptance.
- `LivingWorldCORE/npc/Build.ps1` — runs the new appearance gate.
- `LivingWorldCORE/npc/build.sh` — runs the same gate in the canonical Mono build.
- `LivingWorldCORE/npc/src/CorePlugin.cs` — version `1.0.32.28`.

There are no changes beneath the combat, voice, movement, awareness, raid, equipment, or telemetry implementation folders.

