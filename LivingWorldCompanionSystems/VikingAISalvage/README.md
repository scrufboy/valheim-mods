# VikingAI salvage candidate (audit only)

This directory is an offline, game-assembly-free candidate. It is not installed, wired into the
runtime, or enabled by default. All features default off. VikingAI is never allowed to write
movement; LWC remains the sole peaceful movement writer and native `MonsterAI` owns committed
combat.

## What is included

- explicit, default-off feature flags and an ownership gate;
- coherent equipment loadout scoring rather than sequential equip side effects;
- target-isolated, time-based precombat perception stages;
- reserved travel/action/commit work transactions that yield before native combat;
- an identity-preserving portal transaction policy with legal, readiness, nonce, one-commit,
  completion, rejection, and rollback states;
- deterministic offline tests and a small policy microbenchmark.

## Run offline gates

```powershell
./Run-Tests.ps1
./Run-Benchmark.ps1
```

These use the Windows framework compiler and write executables only to the system temporary
directory. They do not reference or copy Valheim, Unity, BepInEx, Jotunn, or profile files.

## Runtime integration boundary

An eventual runtime adapter may translate LWC snapshots into these pure policies and translate
their results into requests. It must not patch `BaseAI`, `MonsterAI`, `Pathfinding`, `Player`, or
`Humanoid`; directly mutate a transform/ZDO; claim ZDO ownership; or invoke VikingAI movement.
Portal requests must be submitted to LWC's existing identity-preserving transactional gate.

## Rollback

Because nothing is installed or runtime-wired, rollback is deletion/revert of this directory.
If a future adapter is added, its master feature flag must default false and removing that adapter
assembly/config entry must restore the pre-adapter runtime with no ZDO migration required.
