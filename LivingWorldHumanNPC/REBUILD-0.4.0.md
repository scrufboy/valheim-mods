# Living World Human NPC 0.4.0 foundation

This is a clean compiled source boundary, not another 0.3.x patch.

The retained 0.3.x source and development notes remain in Git as evidence and
are not compiled by `rebuild/Build-Rebuild.ps1`.

## First live acceptance boundary

One `lwparty` command must:

1. remove every saved `LWC_HumanScout_v1`, `LWC_HumanGuard_v2`, and
   `LWC_HumanArcher_v2` record across the current world;
2. spawn exactly two actors beside the player;
3. produce an independently randomized male or female native human for each;
4. require visible native hair on both, visible native beard on every male, and
   no beard on a female;
5. bind both actors to the player only after both appearance checks pass;
6. let native `MonsterAI` exclusively own target selection, pursuit, facing,
   weapon selection, and attacks during combat.

If either actor fails, both newly created actors are removed and the command
reports failure. It cannot report or retain a partial party.

## Modules compiled into 0.4.0

- `NativeHumanFactoryV2`: Player clone, complete serialized Humanoid/Character
  copy, removal of Player-only components, native network/visual/AI setup.
- `HumanAppearanceV2`: immutable persisted identity and native appearance
  selection plus visible hair/beard verification.
- `HumanCompanionV2`: one peaceful follow/formation adapter that yields fully
  whenever native AI is alerted or owns a target. A transient exception,
  missing lookup, or single clear scan retains the current state and retries;
  combat requires 25 consecutive clear scans before peaceful formation resumes.
- `HumanNpcPlugin`: registration, global stale-record replacement, exact
  two-person transaction, commands and status.

`LivingWorldTelemetry/rebuild` is the paired read-only 0.3.0 observer. It does
not patch gameplay methods. It records identity, verified appearance, durable
companion state, native AI target/follow state, hands and stowed items, health,
wetness, position, velocity, and separation every half second.

Runic Pack, MadHatter, and SagesVault may remain installed, but none is a hard
dependency of this human foundation. RRRCore is neither referenced nor loaded
by it.
