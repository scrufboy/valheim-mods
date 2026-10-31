# LWC RRR Lifecycle Diagnostic (0.1.0)

Separate, off-by-default BepInEx plugin. With `Enabled=false` (default) it installs no hooks.
It observes only; every finalizer rethrows the original exception. ChatGPT's compat DLL is untouched.

Config: `BepInEx/config/livingworld.core.rrrlifecyclediag.cfg`
- `Enabled` (false), `Prefabs` (RRRN_NinoBosque,RRRN_OrcBrute,RRRN_WoodsSoul), `TrackAllRRRN` (false),
  `StackDepth` (14), `AutoAudit` (true — static prefab audit on first local player spawn; no spawning).

Log lines (prefix `RRR-LIFE`):
- `AUDIT` — prefab registered?, ZNetView count, nested ZNetViews, missing scripts; every default/random item:
  null entries, ObjectDB presence, child ZNetViews, missing scripts.
- `REGISTER` — tracked ZNetView.Awake: prefab, view/go instance id, ZDOID, first spawn stack per prefab.
- `NESTED-ZNETVIEW` — any ZNetView registering below a root (once per root+path, with stack).
- `STAGE` — Character.Awake, BaseAI.Awake, Humanoid.Start, Humanoid.GiveDefaultItems: ok / THREW.
- `ATTACH` — VisEquipment.AttachItem on tracked actors: item, result, child ZNetViews; THREW.
- `NET-DESTROY`, `ZDO-DESTROYED`, `CHARACTER-DESTROY`, `UNREGISTERED-DESTROY`.
- `LEAK destroyed-while-registered` — a ZNetView destroyed outside ZNetScene while still in m_instances (stack).
- `REMOVEOBJECTS-NRE` / `BAD-INSTANCE` — on a RemoveObjects exception (max 1 scan/s): every registered entry
  that is destroyed, has a null ZDO, or a mismatched ZDO, named from the registration map.

Console (isolated diagnostic world only): `lwc_rrrdiag audit [prefab]`, `lwc_rrrdiag fixture <prefab> [dist=12] [seed=1337]`,
`lwc_rrrdiag remove` (ZNetScene.Destroy on fixtures), `lwc_rrrdiag scan`.

Build (canonical Mono): see VERIFICATION in claude-exchange/CLAUDE-OUTBOX/2026-10-31-rrr-lifecycle-diag.
