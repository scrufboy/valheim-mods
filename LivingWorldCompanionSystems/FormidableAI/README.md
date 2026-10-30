# FormidableAI

FormidableAI is the clean-room foundation for LWC's eventual full replacement combat AI.

Current status: **offline policy prototype only**. It is not referenced by the Human NPC runtime,
not built into its DLL, not installed and not live-proven.

## What exists

- `Kernel`: immutable combat frames, explicit decision domains, deterministic arbitration, atomic
  multi-domain leases, native fallback, cancellation/handback and per-module fault isolation.
- `Defense`: target-isolated time-based perception and action-moment block/dodge decisions using
  observed strike identity, impact window, range, facing, stamina, equipment and ownership facts.
- `Classes`: capability compositions for Warden, Huntress, Mystic, Skirmisher, Commander and
  Pathfinder.
- `Integration`: the first shared mapping from class/defense decisions into one kernel. The exact
  strike policy is the only current defense-domain claimant; a generic class request cannot become
  a second defense brain.

## Ownership model

The kernel arbitrates these domains independently but atomically where an action requires several:

`Perception | Targeting | Movement | Facing | Defense | Equipment | Attack | Support`

During migration, an unclaimed domain explicitly falls back to native combat. The final destination
is FormidableAI ownership of all required domains, but no domain transfers without a tested runtime
adapter, rollback and Brian's live acceptance.

## Run the offline suites

```powershell
./Run-All-Tests.ps1
```

The tests compile into the system temporary directory using framework libraries only. They do not
load Valheim, Unity, BepInEx or a profile.

## Next vertical slice

The next implementation step is not another policy. It is a read-only runtime observation adapter
that constructs a `CombatFrame` and emits telemetry while native combat remains the owner. That
shadow-mode trace must prove stable identities, frame facts, cost and correspondence with actual
native actions before any runtime domain lease is allowed to execute.
