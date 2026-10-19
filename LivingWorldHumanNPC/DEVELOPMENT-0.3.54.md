# Human NPC 0.3.54 — Native sitting repair

## Observed failure

During a live 0.3.53 session, both bound companions remained `Upright` for more than 57 calm seconds while the local player was visibly seated. The ambient scheduler reported no sitting candidates. Waiting longer could not resolve the state.

## Cause

Two adapters inferred player sitting from both `Player.InEmote()` and a literal `ZDOVars.s_emote == "sit"`. Current Valheim can resolve and display sitting without that transient implementation pair being true on the sampling frame.

## Repair

- Both posture mimicry and ambient arbitration now consume Valheim's native `Character.IsSitting()` result.
- The existing two-second settled delay, arrival check, door/passage check, body-safety check, and combat-upright precedence remain unchanged.
- No combat, equipment, torch, door, follow, formation, wetness, or ambient-craft behavior was changed.
- Managed adapter tests now prove native resolved sitting works even when the old emote/ZDO pair is absent.

## Runtime status

Offline verification is required before packaging. Installation must wait until Valheim is closed.
