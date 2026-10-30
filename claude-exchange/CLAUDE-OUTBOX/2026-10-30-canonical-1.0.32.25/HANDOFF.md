# Canonical build: 1.0.32.25 bomb opening arbitration

Branch `work/bomb-ranged-acquisition`, head `9b895d2` = ChatGPT `7919a34` + one Claude commit.

## Voice-gate discrepancy: environment, not code (SOURCE-INSPECTED)

`combat_voice_gate.py` fingerprints the module's **compiled IL**, instruction by instruction. The lock
was written from the Linux/Mono (mcs) build. Windows csc emits different IL for identical source, so the
gate fails on Windows even for the unchanged parent. On the canonical Mono build both `5f54db8` (earlier
today) and this head report **LOCKED, unchanged**. The lock was not refreshed. Canonical = Mono build here.

## Gates at `9b895d2` (OFFLINE-TESTED)

Ledger 131/131; native 46; ownership 76 sites, all allowed; combat voice LOCKED, unchanged; movement-writer OK;
analyzer OK. Core `fd09dd0d…`, telemetry `dbe9f2d8…`.

## Review of the opening path (SOURCE-INSPECTED)

- No valid throw means no ownership: `Wants` in Readiness returns true only when cadence is ready, not busy,
  and `ThrowPlanner` approves a plan with a visible hostile near the impact point. An ordinary alert with no
  valid throw does not take combat.
- Ownership is held through the throw: `Wants` returns true while `Body.Throwing`, `StepBomb` calls
  `FinishBomb` (restores the real loadout), and `Exit` also finishes a throw if ownership is lost. After the
  throw the cooldown makes `Wants` false in Readiness, so ownership is released unless real combat began.
- Added (Claude): `BOMB-THROW ... mode=opening-area-control | in-combat-area-control` (request #4).
- Fixed (Claude): 7919a34 dropped the `Body.Ai != null` guard before `CanSeeTarget` in both scans.
- **Cost note (not changed):** in Readiness, `Wants` runs a full character scan with up to 24 `CanSeeTarget`
  raycasts every kernel tick (10 Hz) per companion whenever the cooldown is ready, and `StepBomb` repeats it
  when granted. Suggest caching the Wants scan at the 0.2 s cadence StepBomb already uses. Not a blocker
  for the test.
- Learned-knowledge decision noted as a core-memory requirement; no runtime change.

Ready for install on the new go.
