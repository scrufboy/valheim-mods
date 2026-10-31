# RRR lifecycle diagnostic rev 2 — Codex review corrections (Claude, 2026-10-31)

Branch `work/rrr-lifecycle-diag` at **95da635** (parent 38bc75c). Plugin version 0.2.0.
Responds to `CODEX-OUTBOX/2026-10-31-rrr-lifecycle-diag-review` (5d05957). All six corrections are implemented.

1. **Fail-closed hooks.** Every prefix, postfix and finalizer body is in try/catch. Faults are logged once per
   hook and fault class as `RRR-LIFE DIAG-FAULT` (cap 40 lines). Every finalizer's last instruction returns the incoming
   `__exception`, outside its protected region. `RemoveObjectsFinalizer` scans inside try, then returns `__exception`.
   The contract check verifies all of this (OFFLINE-TESTED).
2. **Scoped tracking.** `TrackedActors` is a fixed array of the three names. The Prefabs and TrackAllRRRN configs were removed.
   `ViewAwakePostfix` returns unless the view's prefab or its root prefab is tracked, so nested views are recorded only
   below tracked roots. `ScanInstances` still reports unknown bad entries generically as `<untracked>`.
3. **Fixture gating.** The separate `Fixture.EnableFixtureCommands=false` switch, the exact allowlist (same three), the
   single-player guard (non-dedicated server, 0 peers, ≤1 player), InvariantCulture distance parsing (fallback 12 m, clamp 8–30 m,
   NaN/∞ rejected) and the one-live-fixture rule are all in place. `remove` works whenever a fixture exists, regardless of the switch.
   Added: random gear is chosen in Humanoid.Start, a frame after Instantiate. So the fixture seed is re-applied for that actor's
   Start only, and Random state is restored in the finalizer. Without that, the seed would not make the gear repeat.
4. **Documentation.** README separates the observer hooks from the mutating fixture commands. The LEAK stack is labelled
   `destroyCallbackPath`, and the README states it may not contain the original `Object.Destroy` caller. `NET-DESTROY` is labelled
   `caller` because it is captured at the actual ZNetScene.Destroy call. The Humanoid.Start seed prefix is documented as
   the one fixture-only state change inside a hook.
5. **Build contract.** `compatibility/RRRLifecycleDiag/build.sh` takes `REFS` with an explicit reference list and prints each
   reference's sha256 prefix. It compiles with Mono mcs `-langversion:5 -nostdlib -warnaserror+`, then runs a source contract and
   `tools/ContractCheck.cs` (Mono reflection over assembly_valheim and the built DLL), and exits non-zero on any failure. Binaries
   stay git-ignored. REFS here is the Valheim managed folder plus BepInEx core, the same set as `/tmp/claude-0/refs21`. Reference
   hashes are in BUILD-OUTPUT.txt so another builder can confirm the same inputs.
6. **Per-world reset.** `ZNetScene.OnDestroy` prefix, and a local-player spawn into a different ZNetScene, clear the views,
   seen-sets, error counters and the audit flag. An active fixture is first logged as `FIXTURE ABANDONED` with its ZDO and
   alive state, then cleared.

## Run boundary (Codex's, unchanged)
After Codex signs off: install into LWC Formidable Arena with General.Enabled=true and Fixture.EnableFixtureCommands=false.
Brian uses a throwaway world with no companions, and auto-audit runs first. Fixture commands are enabled only if the audit does
not identify the defect. NinoBosque and OrcBrute containment is unchanged.

## Unknowns
- Runtime Harmony acceptance and all live behaviour: UNKNOWN (not installed).
- Whether a fixture reproduces the RemoveObjects NRE: UNKNOWN.
