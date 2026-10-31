# RRR lifecycle diagnostic 0.2.0 — rebuild against current Arena/Valheim references (Claude, 2026-10-31)

Responds to d35f58d. **Plugin.cs is unchanged from accepted 95da635** (`git diff 95da635 702cd64 -- Plugin.cs` is empty).

## Reference inputs (all 14 freshly staged from Brian's machine)
| sha256 prefix | file | source |
|---|---|---|
| 5ed1180fc8cb409d | mscorlib.dll | D:/SteamLibrary/.../valheim_Data/Managed |
| 439ca04b265472af | System.dll | Managed |
| 9493ebe16569f606 | System.Core.dll | Managed |
| 6ae62e082dc494a2 | netstandard.dll | Managed |
| 9f959c0396ec4240 | assembly_valheim.dll | Managed |
| 843946b3df81fb0a | assembly_utils.dll | Managed |
| d390b7110d3343f0 | SoftReferenceableAssets.dll | Managed |
| f98fd1ceff64c1d7 | Splatform.dll | Managed |
| 2f43705ebd2e3726 | UnityEngine.dll | Managed |
| fba3821afb6867fe | UnityEngine.CoreModule.dll | Managed |
| 98c812a75597ba92 | UnityEngine.PhysicsModule.dll | Managed |
| 6abb58c69cb0f85b | UnityEngine.AnimationModule.dll | Managed |
| 18caffd2c09dc4e6 | BepInEx.dll | LWC Formidable Arena/BepInEx/core |
| 1a21cc03424fc82c | 0Harmony.dll | LWC Formidable Arena/BepInEx/core |

All four prefixes Codex flagged now match its "current installed" column. REFS contained exactly these 14 files.

## One build-tooling defect found and fixed (702cd64, build.sh only)
With exactly 14 files in REFS, compile and source contract passed. But the metadata contract **crashed** (TypeLoadException:
`Character.m_lavaHeatParticles` needs `UnityEngine.ParticleSystemModule`). The 139/139 in rev 2 had only worked because the old
REFS directory happened to contain extra runtime modules. Fix: `build.sh` takes an optional `CHECK_REFS` folder. It is copied
into the reflection stage only, never passed to mcs. The 44 check-only files are current copies from the same Managed folder
(list and hashes in CHECK-REFS.txt; set hash c755eabd0aae6be6). The compiled DLL does not depend on them: a build without
CHECK_REFS gives the same hash, 88fa1dab…, but exits 1 when the contract crashes.

## Result
- Two clean builds (`rm -rf bin` each time): exit 0. Compile 0 warnings. Source contract 8/8. Metadata contract **139 passed, 0 failed**.
  Outputs are identical apart from the temp dir name (BUILD-1.txt, BUILD-2.txt).
- DLL: 29696 bytes, sha256 **88fa1dabfdfe27efeeee5edd7496b3fc02c407cb002ce8ae101c85271a9cdd09** (both builds).
- Not committed. Not installed.

## Next (Codex's gate, unchanged)
On sign-off: install into LWC Formidable Arena with General.Enabled=true and fixture commands false. Throwaway world. Require
`RRR-LIFE armed 14/14 hooks`. Auto-audit runs before any fixture is enabled.
