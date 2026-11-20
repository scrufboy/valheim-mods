# Studio API and compiler audit

2026-10-20. SOURCE-INSPECTED / OFFLINE-TESTED. No game launched or installation performed.

## Method

Read actual cached managed metadata and relevant IL through Mono.Cecil 0.11.x;
compiled and ran the audit with Roslyn under .NET 8.0.21. These are actual
reference signatures, not stub approximations. Cached game assemblies do not
identify their game release through assembly version (assembly_valheim is 0.0.0.0).
They must be compared to the intended installation before calling it compatible.

| Reference | SHA-256 |
| --- | --- |
| assembly_valheim.dll | 25a0a107dce4d834c44c2b72d0eafd5cb7793933bda81816accfa1ea9543dace |
| assembly_utils.dll | 6be2afeb97b5b26fd3176e1b8584c07e57ad48ddcd6e8e5e79e32bee943827c1 |
| Jotunn.dll 2.30.2.0 | 1181212861a8cab6618b9ddd6257b7aaeac7f6b6be476ea7bb4b1721b45e4ef7 |
| BepInEx.dll from BepInExPack_Valheim 5.4.2202 | 2674d3aecf3097bee817abe7e8bbcc42bf583df51402069d5fcd4fbed55017ce |
| 0Harmony.dll from same pack | 1a21cc03424fc82c3dd1346905d16494536b9595ae4162228d99fb7c285c1031 |

BepInExPack and Microsoft.NETFramework.ReferenceAssemblies.net462 1.0.3 were
retrieved as build-only references. No dependency binaries belong in studio source
or the studio distributable; users install declared dependencies normally.

## Verified signatures

- `bool Player.TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)`.
- `bool Player.IsTeleporting()`; teleport request returning true is not arrival proof.
- `long Player.GetPlayerID()`.
- `Dictionary<string,string> Player.m_customData` is public and included by player serialization.
- `void Game.SavePlayerProfile(bool setLogoutPoint, bool isFromRpc = false)`.
- `bool ZNet.IsSinglePlayer` is a **static** property. IL tests isServer and !openServer.
- `int ZNet.GetPeerConnections()`; `bool ZNet.IsServer()`; `long ZNet.GetWorldUID()`.
- `void ZNet.Save(bool sync, bool saveOtherPlayerProfiles = false, bool waitForNextFrame = false)`.
- `Hoverable`: `string GetHoverText()`, `string GetHoverName()`, **`float GetHoverOffset()`**.
- `Interactable`: `bool Interact(Humanoid user, bool hold, bool alt)` and
  `bool UseItem(Humanoid user, ItemDrop.ItemData item)`.
- `bool ZNetView.IsOwner()`, `ZDO ZNetView.GetZDO()`.
- `GameObject ZNetScene.GetPrefab(string name)` and the int-hash overload.

`SavePlayerProfile` is synchronous in this reference, but it ignores the bool
returned by `PlayerProfile.Save()` and may early-return on save-system blocks.
It cannot certify durable storage. A flushed studio-owned atomic journal is the
primary return record; customData/profile save are supplementary recovery data.
Do not erase recovery data merely because SavePlayerProfile returned normally.

## Jötunn registration

Actual Jötunn 2.30.2 signatures:

- `PrefabManager.OnVanillaPrefabsAvailable`: Action event.
- `GameObject PrefabManager.CreateClonedPrefab(string name, string baseName)`.
- `GameObject PrefabManager.CreateClonedPrefab(string name, GameObject prefab)`.
- `CustomPiece(string name, string baseName, PieceConfig config)`.
- `CustomPiece(GameObject prefab, bool fixReference, PieceConfig config)`.
- `bool PieceManager.AddPiece(CustomPiece piece)`.
- `PieceConfig`: public Name, Description, PieceTable, Category, Usage,
  CraftingStation, AllowedInDungeons, Icon and Requirements properties.
- `void PieceConfig.AddRequirement(string item, int amount, bool recover)`.
- `void LocalizationManager.AddToken(string key, string value, bool override)`.

Clone after vanilla resources exist, register once, unsubscribe the callback.
Original native door behavior must be removed only from the studio clone before
its first active instance. Retain its native build metadata/icon or supply an
original icon. Never mutate the original prefab.

## Input

`ZInput` lives in **assembly_utils.dll**, not assembly_valheim.dll.
Verified public static methods include GetButtonDown(string), GetButton(string),
GetJoyLeftStickY(), GetJoyRightStickX(), GetBoundKeyString(string,bool=false),
ResetButtonStatus(string) and ResetAllButtonStates(). Input action names still
need binding-level verification; API existence does not prove a named mapping.
Avoid broad ResetAllButtonStates because it mutates gameplay input globally.

## Environment

Native `EnvZone` exists; there is no inspected EnvironmentVolume type.
Public fields: string m_environment, bool m_force, MeshRenderer m_exteriorMesh.
Its OnTriggerStay only selects the local Player. With m_force=false it publishes
a zone-specific environment via static EnvZone.GetEnvironment(). With m_force=true
it calls EnvMan.SetForceEnvironment and clears to empty on trigger exit, rather
than restoring a prior override. Avoid forced zones for a clean restoration contract.

`EnvMan.AppendEnvironment(EnvSetup)`, GetCurrentEnvironment(),
ForceInstantEnvironmentSwitch(), and SetForceEnvironment(string) are public.
EnvSetup is ordinary mutable data (including ambient/fog/sun colors, densities,
light intensities, weather flags, VFX arrays, audio and wind). Clone privately;
never modify an existing setup in m_environments. `SetForceEnvironment` changes
the client-global EnvMan and refreshes reflections; it is not camera-isolated.
Camera-scoped RenderSettings snapshots must restore on every render end, exception,
exit and teardown, and need live proof against postprocessing/weather scripts.

## Reproducible offline build

`build_studio.py` uses an explicit original-source allowlist and genuine managed
references. It accepts paths rather than hard-coding Brian's machine. Example:

```sh
python3 LivingWorldStudio/build_studio.py \
  --managed /path/to/Valheim/valheim_Data/Managed \
  --bepinex /path/to/BepInEx/core \
  --jotunn /path/to/Jotunn.dll \
  --framework /path/to/Microsoft.NETFramework.ReferenceAssemblies.net462/build/.NETFramework/v4.6.2 \
  --compiler /path/to/microsoft.net.compilers.toolset/tasks/netcore/bincore/csc.dll \
  --dotnet /path/to/dotnet
```

Compiler runtime: cached patch-runtime/dotnet-home/dotnet; Roslyn csc.dll from
microsoft.net.compilers.toolset. Build emits DLL, compiler log, input hashes and
verification JSON beneath LivingWorldStudio/build. It never installs to a game or
profile. A successful compile proves type compatibility only, not visuals,
teleport completion, controller usability or live recovery.


## Native teleport behavior (IL inspected)

Player.TeleportTo stores the supplied Vector3 and Quaternion without an added
portal/center offset. UpdateTeleport waits two seconds, assigns requested position
and full rotation each frame, zeroes Rigidbody.linearVelocity and resets max-air
altitude. It then waits for ZNetScene.IsAreaReady(target). For distantTeleport=true
it additionally waits at least eight seconds before checking floor. A successful
ZoneSystem.FindFloor ends teleport without replacing the requested coordinates.
If no floor exists, distant teleport eventually substitutes GetSolidHeight+0.5;
non-distant teleport instead reverts to the pre-transfer position and rotation.
Non-distant native teleport is therefore the safer fail-closed studio primitive.
Verify actual arrival and retain the journal on every mismatch.

FindFloor raycasts from target+up*1 downward 1000 m. Its native mask includes
Default, static_solid, Default_small, piece and terrain. Studio default-layer floor
colliders are included. This is not a capsule-obstruction check. A pre-transfer
destination collision test cannot prove safety while the target zone is unloaded.
ZNet exposes SetReferencePosition(Vector3) and GetReferencePosition(), but their
streaming ownership interactions have not been audited; do not add a competing
reference-position writer for an unsupported preflight.

## Studio compilation result

OFFLINE-TESTED: four original studio sources compiled against the genuine cached
game, Jötunn, BepInEx and framework references. No game/Unity/BepInEx stubs were used.
The successful run had two FindObjectsOfType deprecation warnings and zero errors.
The actual game netstandard 2.1 facade is required alongside the .NET Framework
4.6.2 references. Input hashes and compiler output are recorded in
build/build-verification.json and compile.log; rerun after source changes.
No installation or live test was performed.
