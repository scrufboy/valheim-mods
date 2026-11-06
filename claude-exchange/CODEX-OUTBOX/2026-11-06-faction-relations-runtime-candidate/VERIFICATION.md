# Verification

- OFFLINE-TESTED: C# 5 library compilation with warnings as errors against:
  - Valheim assembly SHA-256 `9f959c0396ec4240d47fcbe5d0d062c428cc627244506db5ea567f9d77b2b769`
  - BepInEx 5.4.23.6 SHA-256 `18caffd2c09dc4e6a51fc13b4af7fd2389251f2ec8ecdf8bf076b84a14fb991e`
  - Harmony 2.9.0 SHA-256 `1a21cc03424fc82c3dd1346905d16494536b9595ae4162228d99fb7c285c1031`
  - Jotunn 2.30.2 SHA-256 `9509221046842a70f94b98bab4cb41005dee026823a7a79345829ed46a30f796`
- OFFLINE-TESTED: `RuntimeRegistryTests PASS: 13 checks`.
- OFFLINE-TESTED: engine-free encounter allegiance suite remains
  `EncounterAllegiancePolicyTests PASS: 55 checks`.
- OFFLINE-TESTED: reviewed compatibility config hashes packaged unchanged:
  - natural spawner `eaa7cc22412a37444f1e6dbd13e3b108cd661a1a38b4fc528e70286a9929366d`
  - mixed raid `56484b17c9add9b6822f1bbb27f9f4a114ff39e632c0c98b28c8643dc4c3c8f9`
- OFFLINE-TESTED: final DLL SHA-256
  `b251e2d5a561aa48f7127e7b9e0468896bfc9ac8efe6324f8d2138c5e238b45c`.
- SOURCE-INSPECTED: Candidate04 removes the incorrect server-only `IsEnemy`
  evaluation gate. Synchronized deterministic relations execute on every peer;
  only valid ZNetView owners write managed identity ZDO state.
- INSTALLED: no.
- LIVE-PROVEN: no.
- FAILED: first build correctly exposed missing current-game
  `SoftReferenceableAssets.dll`; the deterministic reference list was corrected
  and the final build passed.
