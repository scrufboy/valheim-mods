# Human NPC 0.3.52 — Combat continuity and defense

## Measured failure

The 0.3.51 session loaded the expected DLL but showed combat authorization
churning while companions were already under attack. The telemetry recorded 59
incoming damage events and no block attempts or results. During the first melee,
18 of 28 incoming hits landed while the companion reported no authorized target.
The awareness pass repeatedly replaced or cleared an authorized target at roughly
half-second intervals.

## Repair

- Authorized combat now permits Valheim's native `MonsterAI.UpdateTarget` to run.
- Passive companion scanning remains closed outside legal combat.
- The awareness scan can select a target only when no committed target exists.
- A living committed target uses a bounded encounter leash rather than a tight
  follow-position leash.
- Attributed damage renews encounter ownership without abandoning a living target.
- `HumanCombatDefense` is a narrow shield adapter. It can raise an already equipped
  shield for a close, visible attacker actively swinging at that companion. It
  cannot move, face, retarget, equip, attack, or force a riposte.

## Preserved systems

Follow formation, utility torches, visible-back weapon stowing, wetness, doors,
ambient guard behavior, awareness HUD, cleric behavior, party creation, names and
resource handling were not redesigned for this repair.

## Acceptance requirement

Offline gates must pass and the installed DLL hash must equal the packaged build.
Live acceptance then requires continuous engagement under pressure, no repeated
authorization blanking while taking hits, and `BlockAttempt`/`BlockResult` events
when a shield user faces a close visible swing.
