# Verification

- **OFFLINE-TESTED:** Tooler interface contract PASS: original four Tooler pages plus seven Living World pages.
- **OFFLINE-TESTED:** Site Survey contract PASS on the merged Build page; terrain application remains locked.
- **OFFLINE-TESTED:** current FormidableAI raid integration contract PASS:
  - events `lwc_formidable_arena`, `lwc_formidable_souls_swamp`;
  - random pool false;
  - `HuntPlayer=false` for all eight slots;
  - every slot uses the 70–95 m ring;
  - all six required Souls prefabs present.
- **OFFLINE-TESTED:** clean C# 5 build PASS; package SHA-256 `303B93AF4F381F2783B45606E2AC0750E2BE741B992AC1B540FAF331AF9C7DAB`.
- **INSTALLED:** profile DLL and config hashes match the candidate exactly.
- **UNKNOWN:** runtime UI visibility and command dispatch await the next launch.
