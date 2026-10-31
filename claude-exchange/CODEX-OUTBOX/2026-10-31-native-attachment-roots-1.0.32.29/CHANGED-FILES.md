# Changed files

Commit `1ec0f18`:

- `LivingWorldCORE/npc/src/Identity/NpcAppearance.cs` — uses Valheim's complete `m_visual` root for native attachment containment; preserves all other visibility checks; adds exact beard failure details.
- `LivingWorldCORE/npc/src/Kernel/NativeApi.cs` — centralizes `VisEquipment.m_visual` for the native gate.
- `LivingWorldCORE/npc/tools/appearance_gate.py` — locks the complete visual-root contract and rejects the skinned-only containment regression.
- `LivingWorldCORE/npc/src/CorePlugin.cs` — version `1.0.32.29`.

No combat, awareness, movement, voice, equipment, raid, or telemetry behavior changed.

