# FormidableAI kernel candidate

Game-independent ownership/arbitration prototype only. It is not wired into Valheim and does not
patch or replace `MonsterAI`.

The kernel accepts an immutable observation frame, gathers class/module proposals, deterministically
awards non-overlapping decision-domain leases, and assigns every unclaimed domain to the native
fallback. A module exception disables only that module and is returned as bounded fault evidence.

Run `Run-Tests.ps1`. The executable is created only in the system temporary directory.

Runtime-oriented callers should allocate one `ArbitrationBuffer` per actor and use `EvaluateInto`.
Modules on a steady-state path should implement `IBufferedFormidableCombatModule`, which collects
proposals into the arbitrator's reused list. The older `Evaluate` API remains as an allocating
compatibility path. `Run-Benchmark.ps1` measures the reused path after warmup.
