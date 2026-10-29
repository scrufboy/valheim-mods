# Bounded combat support magic

This module is a server-owned request and finite resource ledger for three
candidate roles: ward, heal, and missile. It does not cast a spell, apply a
status, create a projectile, play an animation, or heal a character. The
maintained native combat owner must perform those actions only after the exact
item, animation event, target/status, and network path is proven in game.

The Fuling shaman is a useful visual and timing reference for shielding and a
projectile. The builder must inspect the current game prefab and status data
before reusing any behavior. A healing ability must be verified separately;
its existence must not be inferred from the shaman's support presentation.

`MagicSupportPolicy.Reserve` requires server authority, actor ownership, native
combat ownership, an approved animation, an authorized living target, line of
sight, and ally safety. Wards refuse an already warded ally. Healing targets a
wounded living ally and cannot reverse a committed death. Missiles refuse allies.
One actor can hold one cast reservation at a time. Only an observed native cast
event spends resource and starts cooldown; a pre-event interruption cancels
without effect. Replays cannot double debit.

Resource restoration is explicit and capped. A verified safe rest or supply
event may restore some reserve while combat is inactive. An event ID can restore
only once. There is no automatic combat refill or infinite cast loop. The sample
test costs are test fixtures, not balance values or asserted Valheim eitr costs.
Actual costs, cooldowns, maximum reserve, food/supply relationship, and effect
strength require paired-class and raid telemetry before promotion.

The builder must prove exact native ward application and expiration, heal amount
and target restrictions, missile collision/friendly fire, resource debit,
interruption, client replication, zone reload, and server restart. Persist this
ledger atomically with the person's combat/resource record; compact old event
IDs only after a durable authoritative checkpoint.

Run `Run-Tests.ps1` for the policy checks. The active game profile is unchanged.
