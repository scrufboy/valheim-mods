# LWC RRR Lifecycle Diagnostic (0.2.0)

Separate BepInEx plugin. ChatGPT's RRRCore compat DLL, RRRCore, Souls JSON and the NPC mod are untouched.
It has two parts with different safety classes.

## 1. Observer hooks — `General.Enabled` (default **false**)
With `Enabled=false` no Harmony patch is applied and no console command is registered.
When enabled, the hooks read and log; they never skip or replace an original method.
- Every hook body is wrapped. A diagnostic failure logs one bounded `RRR-LIFE DIAG-FAULT` line per hook/fault class
  (at most 40 in total) and never escapes.
- Every finalizer returns the incoming exception unchanged, including when its own inspection fails.
- The single exception to "read only": `Humanoid.Start` re-applies the fixture seed for the **fixture actor only**
  (see part 2) and restores the global `UnityEngine.Random` state in its finalizer. Without an armed fixture it reads only.

Tracking is fixed in code to `RRRN_WoodsSoul` (working reference), `RRRN_NinoBosque`, `RRRN_OrcBrute`.
Registration history is kept only for ZNetViews whose own prefab or root prefab is one of those three.
A `ZNetScene.RemoveObjects` failure still reports **any** bad scene entry generically (`<untracked>`), because the
native failure may point outside that set.

Log lines (prefix `RRR-LIFE`):
- `AUDIT` — once per world on the local player's first spawn (`General.AutoAudit`, default true). Reads prefabs only:
  registration, ZNetView count, nested ZNetViews, missing scripts; each default/random item: null entries,
  ObjectDB presence, child ZNetViews, missing scripts.
- `REGISTER` — ZNetView.Awake of a tracked actor: prefab, view/go ids, ZDOID, whether registered;
  the instantiation stack for the first instance per prefab.
- `NESTED-ZNETVIEW` — a ZNetView below a tracked root (once per path, with stack).
- `STAGE` — Character.Awake, BaseAI.Awake, Humanoid.Start, Humanoid.GiveDefaultItems: `ok` or `THREW`.
- `ATTACH` / `ATTACH THREW` — VisEquipment.AttachItem on tracked actors.
- `NET-DESTROY` (stack = actual caller of ZNetScene.Destroy), `ZDO-DESTROYED`, `CHARACTER-DESTROY`, `UNREGISTERED-DESTROY`.
- `LEAK destroyed-while-registered` — a tracked ZNetView reaching OnDestroy while still in `m_instances`.
  The stack is the **destruction callback path**. Unity can defer destruction, so it does not necessarily contain
  the original caller of `Object.Destroy`.
- `REMOVEOBJECTS-ERRORS`, `BAD-INSTANCE`, `SCAN` — on a RemoveObjects exception (at most one scan per second).
- `RESET` — per-world state cleared at `ZNetScene.OnDestroy` (world teardown) or when the local player spawns into a
  different scene. An active fixture is reported as `FIXTURE ABANDONED` before it is cleared.

Console (registered only when observer hooks are enabled):
`lwc_rrrdiag audit [actor]`, `lwc_rrrdiag scan`.

## 2. Fixture commands — `Fixture.EnableFixtureCommands` (default **false**, separate switch)
**These mutate the world.** `lwc_rrrdiag fixture <actor> [dist] [seed]` instantiates one actor in front of the player;
`lwc_rrrdiag remove` removes it through `ZNetScene.Destroy`.
- Exact allowlist: `RRRN_WoodsSoul`, `RRRN_NinoBosque`, `RRRN_OrcBrute`. Anything else is refused.
- Single-player only: refused unless this process is a non-dedicated server with zero peers and one player.
- Distance: default 12 m; invalid input (non-numeric, empty, NaN, infinite, comma decimal) keeps 12 m and says so;
  valid input is clamped to 8–30 m.
- Seed: default 1337, applied around Instantiate and again for that actor's `Humanoid.Start` (where random gear is chosen),
  then the global Random state is restored.
- One fixture at a time: a second `fixture` is refused until `remove` has run.
- `remove` works whenever a fixture exists, even if the fixture switch has since been turned off.

## Build
`REFS=<dir> ./build.sh` — canonical Mono `mcs -langversion:5 -nostdlib -warnaserror+` against the explicit reference
list in the script (Valheim managed DLLs + BepInEx core). It prints the sha256 prefix of every reference, compiles to
`bin/` (git-ignored), runs the source contract (grep) and `tools/ContractCheck.cs` (Mono reflection over
assembly_valheim and the built DLL: hook targets, parameter binding, try/catch in every handler, finalizers returning
`__exception`, fixed tracked set, distance parsing), and prints the DLL sha256. Non-zero exit on any failure.
