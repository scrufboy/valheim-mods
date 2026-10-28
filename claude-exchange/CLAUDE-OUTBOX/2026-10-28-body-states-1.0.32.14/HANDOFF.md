# 1.0.32.14 body states — handoff (Claude, 2026-10-28)

Intent: implement ChatGPT's private body-reactions handoff
(`Living_World_CORE_Body_Reactions_COMPLETE_PRIVATE_2026-10-28`, `06_CLAUDE_HANDOFF/BODY_REACTIONS_REQUIREMENTS.md`)
inside the existing 1.0.32.13 body-reaction addon. Brian: "ok Now you can build...." (2026-10-28 17:09).

Parent: `lwc-core-1.0/1.0.32.13-body-voice` @ ea13706. Candidate: `lwc-core-1.0/1.0.32.14-body-states` @ 3c7e0da.

## Scope
- Same addon (`BodyReactionVoice`), no new controller, no new status writer.
- Unchanged: poison law `BodyVoicePolicy`, its 40 clips (installed hashes = manifest, 40/40), combat voice 1.0.32.6 (gate LOCKED).
- New pools and triggers (SOURCE-INSPECTED, Valheim 1.0.20 decompile):
  - Burning status (`Character.AddFireDamage`, `UpdateLava`) → `burning_onset`, then `burning_repeat` every 6–9 s.
  - Frost status (`Character.AddFrostDamage`) → `frost`, every 8–12 s.
  - Long continuous swim (`Character.IsSwimming` ≥ 20 s), out of water ≥ 1.5 s → `swim`, once, 30 s cooldown.
  - Heavy-attack stamina empty (`NpcBody.Exhaustions`) → `exhaustion`, once, 45 s cooldown.
  - Burning/frost ended after ≥ 3 s → `env_recover`, at most one per 30 s.
- One voice per actor: combat and poison lines take precedence and cut state lines; state lines start only when silent
  and ≥ 2.5 s after the last body line; a line is cut when its state ends. No other-gender fallback.
- Lab audition: `lwbodyaudition list | <pool> [male|female]` plays each clip of a pool on a companion, 3 s apart, logging file names.

## Not wired (needs Brian's decision)
- Cold / Freezing weather: SOURCE-INSPECTED as Player-only (`Player.UpdateEnvStatusEffects`); companions never receive them.
  Making them would need a new status writer like `NpcWetStatus` — the handoff forbids inventing one without approval.
- Smoke (`Smoked`): companions can receive it (`Character.UpdateSmoke`; Player-copied `m_tolerateSmoke`), left silent per handoff.
- Wet/rain: never a trigger, by design. Air: Valheim has no air meter.

## Unknowns
- Everything is UNKNOWN live: clip suitability, trigger frequency, burning actually reaching companions from Surtlings/lava,
  frost from Drakes/Fenrings, exhaustion often landing mid-combat where combat lines win.
- Burning onset and repeat pools have one clip per gender each (as selected), so they repeat the same sound.
