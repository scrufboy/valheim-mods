# Review: RRR lifecycle diagnostic requires corrections before install

Codex reviewed `work/rrr-lifecycle-diag` at `38bc75c`, including the complete `compatibility/RRRLifecycleDiag/Plugin.cs` and README. The diagnostic target and three-actor comparison are appropriate. The current build is **not approved for installation** because its observer-safety and fixture boundaries are not yet enforced by code.

## Blocking findings

### 1. Diagnostic exceptions can alter the failure being observed

**SOURCE-INSPECTED:** Harmony prefixes, postfixes and finalizers call reflection, Unity object access, dictionary scans and logging without an outer diagnostic `try/catch`. In particular, `RemoveObjectsFinalizer` calls `ScanInstances` before returning the original exception. If `ScanInstances`, `DescribeView`, `zdoRef`, `instancesRef`, `RootTracked`, `Stage` or logging throws, the diagnostic can replace/add an exception instead of preserving the original game path.

Required: every hook must be fail-closed. No diagnostic exception may escape. `RemoveObjectsFinalizer` and every stage/attach finalizer must always return the incoming `__exception`, even if diagnostic inspection fails. Emit at most one bounded `RRR-LIFE DIAG-FAULT` line per hook/failure class.

### 2. Nested-view tracking is global, not scoped to the three actors

**SOURCE-INSPECTED:** `ViewAwakePostfix` returns only when `!isTracked && !nested`. Therefore every nested `ZNetView` in the game is inserted into `views`, and every unique root/path may be logged, even when its root is unrelated to WoodsSoul, NinoBosque or OrcBrute. This is broader and potentially noisier than the stated diagnostic boundary.

Required: retain historical registration details only when the view or its root belongs to the explicit tracked set. `ScanInstances` may still report an unknown bad scene entry generically if the native failure points outside that set.

### 3. The fixture mutation is insufficiently gated

**SOURCE-INSPECTED:** `lwc_rrrdiag fixture` accepts any registered prefab name. A malformed distance parses to zero and can instantiate on the player. Documentation says “throwaway world only,” but code cannot enforce that statement.

Required:

- separate `EnableFixtureCommands=false` configuration, independent from observation;
- exact allowlist: `RRRN_WoodsSoul`, `RRRN_NinoBosque`, `RRRN_OrcBrute` only;
- refuse fixture commands with more than one player/peer;
- preserve the 12 m default on parse failure and clamp to a safe documented range;
- allow only one live fixture at a time and require network-safe removal before the next;
- keep `remove` available whenever a diagnostic fixture exists.

### 4. “Observer only” and destruction-stack claims overstate the code

**SOURCE-INSPECTED:** the hooks intend to observe, but the console fixture explicitly instantiates and destroys actors. Describe the plugin as observer hooks plus an explicitly armed isolated fixture. Also, Unity destruction can be deferred; an `OnDestroy` prefix stack is not guaranteed to contain the original caller of `Object.Destroy`. Label that stack as the destruction callback path unless a hook at the actual destroy call proves the caller.

### 5. The branch lacks a reproducible build contract

**SOURCE-INSPECTED:** the branch contains `Plugin.cs`, README and `.gitignore`, but no build script or executable contract test. The exchange refers to a temporary `/tmp/claude-0/refs21` path that does not exist in the repository.

Required: commit the canonical Mono build script with explicit reference inputs and a small source/metadata contract check. Keep binaries out of Git. Report the rebuilt hash after the safety changes.

### 6. Static state needs a world/session reset

**SOURCE-INSPECTED:** `audited`, `views`, `fixtures`, error-window counters and “seen” sets remain static for the process. A world reload can retain stale Unity wrappers and suppress the next audit.

Required: clear per-world collections and counters on a safe world/scene teardown or next local-player spawn before the new audit, without discarding an active fixture before its removal is reported.

## Accepted run boundary after correction

After the corrected branch passes its build/contract checks, installation into the already-isolated **LWC Formidable Arena** profile is acceptable. NinoBosque and OrcBrute remain disabled in raids and natural spawners. Brian loads a throwaway world with no companions; auto-audit runs first. Only if auto-audit does not identify the defect, enable fixture commands and run WoodsSoul, NinoBosque and OrcBrute one at a time, removing each through `ZNetScene.Destroy` before the next. No combat video is required; the deliverable is `LogOutput.log`.

