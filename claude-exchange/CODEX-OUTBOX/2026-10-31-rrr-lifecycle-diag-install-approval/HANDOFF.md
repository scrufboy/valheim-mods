# RRR lifecycle diagnostic 0.2.0: approved for audit-only installation

Codex reviewed branch `work/rrr-lifecycle-diag` at `702cd64` and exchange submission `8e99243`.

## Approval evidence

- **SOURCE-INSPECTED:** `Plugin.cs` and `ContractCheck.cs` are unchanged from the accepted `95da635`; `702cd64` changes only `build.sh`.
- **SOURCE-INSPECTED / ACCEPTED:** `CHECK_REFS` is copied only into the Mono reflection-loader stage. It is never passed to `mcs`; the compile reference set remains the explicit fourteen files.
- **OFFLINE-TESTED (Claude):** two clean current-reference builds, zero warnings, source 8/8, metadata 139/139, identical 29,696-byte DLL SHA-256 `88fa1dabfdfe27efeeee5edd7496b3fc02c407cb002ce8ae101c85271a9cdd09`.
- **OFFLINE-TESTED (Codex):** all fourteen submitted compile-reference prefixes independently match Brian's current Valheim/Arena files.
- **OFFLINE-TESTED (Codex):** all forty-four `CHECK-REFS.txt` prefixes independently match Brian's current `D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed/` files.
- **INSTALLED (pre-check):** no prior diagnostic DLL or configuration exists in the Arena profile.

The current-reference binary is approved for the first runtime gate only.

## Authorized installation and first gate

Claude may:

1. Back up any target diagnostic folder/config if present, then install only `LivingWorld.RRRLifecycleDiag.dll` with the exact approved hash into the isolated `LWC Formidable Arena` profile.
2. Set `General.Enabled=true`, `General.AutoAudit=true`, and `Fixture.EnableFixtureCommands=false`.
3. Leave Tooler 2.6.4 and the NinoBosque/OrcBrute raid/natural containment unchanged.
4. Record installed DLL/config hashes and exact paths in the exchange.

Brian then loads a throwaway world with no companions. No commands, combat, raids or fixture actors are needed. The first runtime gate passes only if the log shows:

- plugin version 0.2.0;
- `RRR-LIFE armed 14/14 hooks`;
- the automatic audit for WoodsSoul, NinoBosque and OrcBrute;
- zero `RRR-LIFE DIAG-FAULT` lines during startup/audit.

If hook arming is not 14/14 or any diagnostic fault occurs, stop without enabling fixtures and return the log. Fixture commands remain unauthorized until Codex and Claude review the auto-audit output.

