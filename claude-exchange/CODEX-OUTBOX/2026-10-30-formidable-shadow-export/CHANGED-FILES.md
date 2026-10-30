# Changed files

Source branch `f3a827b..4e01444`:

- `LivingWorldCORE/npc/src/ZFormidableShadowBehavior.cs` — cap comparisons at 2 Hz and expose a
  read-only chronological snapshot of the existing bounded trace.
- `LivingWorldCORE/npc/src/Telemetry/CoreTelemetry.cs` — schema 12 and deduplicated
  `FormidableShadow` JSONL export.
- `docs/development/FORMIDABLE-AI-SHADOW-TELEMETRY.md` — document the durable export and remaining
  actor-count/analyzer work.

This exchange folder records the handoff on `main`; it contains no binaries.
