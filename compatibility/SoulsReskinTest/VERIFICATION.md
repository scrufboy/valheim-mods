# Embedded Souls reskin candidate 0.1.0

Authorization: Brian requested a repacked reskinned Souls build with embedded assets for testing in the same environment on 2026-11-01.

OFFLINE-TESTED: 31 new textures, 42 materials, 108 texture references. PNG pixels match after bundle reload, with mipmaps. Every original object outside the selected materials is byte-identical. Material colors and non-reference properties match. All assembly method instructions match the source. Six NPC JSON definitions and the spawn config match the Arena source byte-for-byte; they also match the Souls of the Land profile. No NPC roster, role, faction or loadout changes.

- Source DLL SHA256: `D2D9EA86325CC516C90D86512270CD940AFF01600A42A4A55D3B3EC7C8B82B3D`
- Candidate DLL SHA256: `7083D5843A9A492B4B91652230C2CD8F91873322EFB99B7BC402BC760442F5E2`
- Embedded bundle SHA256: `4A99F8C7D9041F1D9EAA3FC1ABD237DBFB6609021C5A32FB4EE358C381454FDD`
- Package SHA256: `668559b83cd663af58d0bcf91fd0eb82def1c5f6dfcf3ee3d4777e7f6d4440b6`
- Package size: 80874147 bytes

NOT INSTALLED. In-game load, UV alignment, shader sampling and appearance remain UNKNOWN. The installed profiles are untouched. The repack includes existing Sages equipment registration code and prefabs; the separate mod installation is replaced by the packaged copy, not by an independently authored registration system. The next visual test checks the embedded art before independent LWC item construction. Existing external dependencies other than the separate equipment installation are unchanged.

Build inputs are the diagnostic DLL and completed art/sages-vault-original PNGs. The art directory existed as untracked work before this task; it was not staged into the source commit. Exact input texture hashes are included in the packaged bundle-verification.json. Build/dist artifacts are local and ignored by Git.
