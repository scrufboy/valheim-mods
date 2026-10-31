# RRR lifecycle diagnostic 0.2.0: source accepted, binary rebuild required

Codex reviewed the complete `work/rrr-lifecycle-diag` source at `95da635`, its README, `build.sh`, `ContractCheck.cs`, submitted 139/139 output and negative-test description.

## Source review

- **SOURCE-INSPECTED / ACCEPTED:** all six requested corrections are implemented.
- Every installed Harmony handler is exception-contained; finalizers preserve the incoming exception.
- Registration history is fixed to WoodsSoul, NinoBosque and OrcBrute.
- Fixture creation is separately disabled by default, exact-allowlisted, single-player guarded, bounded and one-at-a-time; removal remains available.
- Documentation correctly separates observer hooks from fixture mutation and limits the destruction-stack claim.
- Per-world state reset and active-fixture abandonment reporting are present.
- The fixture-only `Humanoid.Start` seed application is scoped to the current fixture actor, restores global Random state in the finalizer and is explicitly documented. It is acceptable for the deterministic fixture boundary.
- Build and metadata contracts are materially stronger than revision 1.

## Binary reference mismatch

Codex independently hashed the files currently installed for Brian's Arena profile and Valheim installation. Ten of fourteen reference prefixes match `BUILD-OUTPUT.txt`, including `assembly_valheim.dll`. Four do not:

| Reference | Submitted build prefix | Current installed prefix |
| --- | --- | --- |
| `assembly_utils.dll` | `771d59cbf3ef7040` | `843946b3df81fb0a` |
| `SoftReferenceableAssets.dll` | `0c529a84c1ab011a` | `d390b7110d3343f0` |
| `Splatform.dll` | `ab1753f05f5fae8c` | `f98fd1ceff64c1d7` |
| `BepInEx.dll` | `36bdb9af2b8f2eda` | `18caffd2c09dc4e6` |

Current sources checked:

- game managed files: `D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed/`;
- loader files: active `LWC Formidable Arena/BepInEx/core/`.

Because the candidate DLL hash `a63938ac...9253` came from a different four-file reference set, that binary is **not approved for installation**. This is a build-input correction only; no source redesign is requested.

## Required next action

Stage the exact twelve current game-managed references and the exact two current Arena BepInEx-core references into a clean `REFS` directory. Run `build.sh` twice. Return the complete reference table, 139/139 contract output, identical two-build DLL hash and size. Keep the binary out of Git and do not install it yet.

After those four prefixes match, Codex approval is limited to the first runtime gate: install with `General.Enabled=true`, fixture commands false, load the throwaway world, and require `RRR-LIFE armed 14/14 hooks`. Auto-audit runs before any fixture is enabled.

