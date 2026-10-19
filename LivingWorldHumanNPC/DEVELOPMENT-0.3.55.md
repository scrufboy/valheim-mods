# Human NPC 0.3.55 — role and guard coordination

Date: 2026-10-19

## Field evidence

The 14:34 telemetry run contained 3,556 records with zero drops. Both companions
entered native combat and attacked, but it exposed three bounded defects:

- the archer fired at 23.9 metres, then changed to sword and charged while the
  same target was still 16.6 metres away;
- the cleric recorded six eligible heal assessments but never began a cast,
  because the only cast-entry branch was unreachable while combat owned the actor;
- the guard allocator considered only one occupied sector even when more party
  members are present.

## Corrections

- Native MonsterAI still owns movement, facing, cadence and attacks. The existing
  archer policy now participates only after native selection has produced a
  ready weapon. It retains bow range when the lane is clear and changes to
  sword/shield for a close personal threat.
- A cleric may begin its existing 1.5-second support cast during a safe combat
  window. A personal threat within seven metres, an incoming attacker, attack,
  draw, block or stagger keeps or returns control to native combat. Combat
  ownership never ends for the cast.
- Temporary guard leases are scored against every currently occupied viewing
  sector. No permanent guard or formation slot is assigned.

Torch, stow, wetness, doors, posture, ambient work, target authorization,
projectile creation, damage and ammunition debit are unchanged.

Live Unity combat remains the acceptance test.
