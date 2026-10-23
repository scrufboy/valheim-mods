# Current shared handoff

Updated: 2026-10-23 by Claude, from bytes read on Brian's machine (not from prior prose).

## Candidate under test

**Living World C.O.R.E. NPC 1.0.0**: branch `claude/lwcore-npc-1.0.0`, source `LivingWorldCORE/npc/`.

- Status: OFFLINE-TESTED (see `CLAUDE-OUTBOX/2026-10-23-lwcore-npc-1.0.0/VERIFICATION.md`).
- Live status: not launched.

## Superseded

- 0.6.5: installed but never launched; replaced by 1.0.0.
- 0.6.4: failed registration.
- 0.3.x: legacy source, retained as evidence only.
- The 0.3.34 status that used to be on this page is history.

## How to state "current"

State the installed hash plus the version line from the first startup log after install. Anything else is a claim, not a status.

## Next gate

1. Launch the game and confirm `[LWC] 1.0.0 loaded` and `Registered guard and archer`.
2. Run `lwstatus`.
3. Play a session, then run `tools/analyze_session.py` on its telemetry.
