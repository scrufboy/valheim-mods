# Current implementation map

## Published and installed states

| State | Ref | Status |
| --- | --- | --- |
| Combat voice 1.0.32.6 | branch `lwc-core-1.0/baseline-combat-voice-1.0.32.6`, tag `combat-voice-approved-1.0.32.6`, commit `26fd295` | APPROVED, LOCKED, LIVE-PROVEN |
| Rename boundary 1.0.32.7 | branch `lwc-core-1.0/installed-1.0.32.7`, commit `9933763` | INSTALLED; launch proof still required |
| Party spacing 1.0.32.8b | branch `lwc-core-1.0/1.0.32.8b-party-spacing-test`, commit `33addad`, DLL hash `7cc46473…` | SOURCE-INSPECTED, 96/96 OFFLINE-TESTED, BUILT; not installed |
| Hot-path experiment 1.0.33 | branch `lwc-core-1.0/parked-1.0.33`, commit `965a2a0` | PARKED, not installed |

The local v4 bundle is the complete offline preservation record. GitHub now
contains the corresponding source refs. The annotated GitHub tag has a different
tag-object hash from the local bundle tag, but both peel to the same approved
commit `26fd295b87d0c1057f7503b64012e165621b3d2c`.

## Proven 1.0.x modules

- Identity: stable name, sex, hair, beard and role fields; broader identity is OPEN.
- Persistence: identity, leader binding and kit tier only; inventory, durability,
  relationship, home, order, skills and history are OPEN.
- Traversal: Follow, formation leases, catch-up, regroup and settled guard are
  live-proven before spacing. Portals/instances have no implementation.
- Spacing: soft standing regions, leader back-off, sidestep, blocked wait and
  deterministic right-of-way are offline-tested in 1.0.32.8b. Live doors, slopes,
  physical contacts and formation feel remain required.
- Awareness/combat: phase rules, authorized threats, native `MonsterAI` combat,
  chained melee, heavy openings, armor parity, fallback and war cry are live-proven.
- Gear: fixed iron/plains kits, ammunition top-up, shield pairing and stowing.
- Combat voice: semantic gendered pools and detached death playback are locked.
- Investigation, full needs, progression, worker/resident classes, portal transfer,
  memorial mortality and inventory persistence remain OPEN.

## Known ownership debt

`NpcBody.cs` mixes engine body services with content kits. `Kernel.Pure.cs`
contains phase rules plus many policies that need module boundaries. Guard and
spacing terrain checks overlap. Identity keys remain spread across classes instead
of one versioned character record.

The full source-level audit is
[11-IMPLEMENTATION-AUDIT-2026-10-27.md](11-IMPLEMENTATION-AUDIT-2026-10-27.md).

Status vocabulary: `SOURCE-INSPECTED`, `OFFLINE-TESTED`, `BUILT`,
`INSTALLED`, `LIVE-PROVEN`, `DESIGN-AUTHORITY`, `OPEN`.
