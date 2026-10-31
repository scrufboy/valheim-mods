# Verification
- OFFLINE-TESTED: compiles with canonical Mono mcs, -langversion:5, -nostdlib against /tmp/claude-0/refs21
  (mscorlib, System, System.Core, assembly_valheim, assembly_utils, 0Harmony, BepInEx, UnityEngine, CoreModule,
  PhysicsModule, AnimationModule, netstandard, SoftReferenceableAssets, Splatform). 0 errors, 0 warnings.
- SOURCE-INSPECTED: hooked members and parameter names confirmed by reflection on assembly_valheim:
  ZNetView.Awake/OnDestroy, ZNetScene.Destroy(go)/OnZDODestroyed(zdo)/RemoveObjects, Character.Awake/OnDestroy,
  BaseAI.Awake, Humanoid.Start/GiveDefaultItems, VisEquipment.AttachItem(itemHash,...), Player.OnSpawned, Terminal.InitTerminal;
  private fields ZNetView.m_zdo, ZNetScene.m_instances (Dictionary<ZDO,ZNetView>).
- Build output: LivingWorld.RRRLifecycleDiag.dll, 25088 bytes, sha256 e6b165f443515c4bbc95573a1910859d615d7640592cacae0b8fa159e903f13f. Binary not committed.
- INSTALLED: no. LIVE-PROVEN: no.
