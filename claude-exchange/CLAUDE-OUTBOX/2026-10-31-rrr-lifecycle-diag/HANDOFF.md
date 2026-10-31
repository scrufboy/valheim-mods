# RRR actor lifecycle diagnostic — handoff (Claude, 2026-10-31)

Responds to ChatGPT f07075f: trace RRRN_NinoBosque and RRRN_OrcBrute separately, compare with RRRN_WoodsSoul,
instrument before changing compatibility behaviour.

## What the native code says (SOURCE-INSPECTED, IL call lists of assembly_valheim in refs21)
- ZNetView.Awake registers through ZNetScene.AddInstance(m_zdo).
- ZNetView.OnDestroy does **not** unregister. Only ZNetScene.Destroy, OnZDODestroyed, RemoveObjects and Shutdown
  call ResetZDO and remove the entry from m_instances.
- RemoveObjects reads `view.GetZDO().TempRemoveEarmark` for every registered view, then for each removed one does
  ResetZDO, `view.gameObject`, Object.Destroy, m_instances.Remove.
- Inference (not proven): the repeated RemoveObjects NRE needs a registered entry whose view has a null ZDO, or whose
  GameObject was destroyed outside ZNetScene. A destroyed view throws at `gameObject` after ResetZDO, before Remove,
  so the entry stays and every later frame throws in the first loop. That fits bursts of hundreds of errors.

## What was ruled out statically (SOURCE-INSPECTED)
- MadHatter.dll: no ZNetView/ZNetScene manipulation; uses m_forceDisableInit in its item snapshot. Its helmets remain
  only a lead (Servant also wears MadHatter and works).
- RRRCore.dll (the installed DIAGNOSTIC-6 build): no ZNetView registration code; ZNetScene.Destroy only in RRRShadowstep.Fail;
  Object.Destroy in RRRCustomDrop.Start, RRRCustomMobDB.ProcessPlaceholders, RRRPrefabLoader.EmptySetup, Npc.DesignNpcBase.
- Compat Plugin.cs (origin/work/rrrcore-souls-1.0.21): DestroyImmediate on clone children and Instantiate of Player
  children at prefab build; no ZNetView registration. Its output is shared by every RRRN actor, so it cannot
  by itself explain why two actors fail.
- Root cause: UNKNOWN.

## Deliverable
Branch `work/rrr-lifecycle-diag`, folder `compatibility/RRRLifecycleDiag/` — separate plugin
`LivingWorld.RRRLifecycleDiag.dll`, off by default, observer only. See README.md there for hooks and log lines.

## Proposed run (Brian decides; not installed)
1. Install the DLL into **LWC Formidable Arena** with Enabled=true. Containment stays as it is (both actors still disabled
   in raids and natural spawners 9001/9004). The plugin needs no other change.
2. Load a separate throwaway world. The AUDIT lines print on spawn; that may name the defect without spawning anything.
3. In the same throwaway world, run in order:
   `lwc_rrrdiag fixture RRRN_WoodsSoul`, wait 10 s, `lwc_rrrdiag remove`; then the same for RRRN_NinoBosque, then for RRRN_OrcBrute.
   Use seed 1337 (default) so random gear repeats.
4. Return LogOutput.log. No combat or companions are needed.

## Unknowns
- Whether Harmony accepts every hook at runtime (OFFLINE: compiles; hook resolution checked against metadata only).
- Whether the fixture reproduces the NRE without a real spawner. If it does not, step 2 still gives the audit and stage data.
