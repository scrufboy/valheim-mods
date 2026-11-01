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


## Arena embedded Souls reskin 0.1.1 — 2026-11-01

INSTALLED in LWC Formidable Arena on Brian's explicit instruction: “so now in our arena... just install this Souls testpack, enable it, check all of the data, sim test..then I will load and go look”. Equipment DLL SHA256 `6f931d1e1f37c31913462ae0d2bd4244d771d7cadea7a7dbab5533a24cafb021`; ZIP SHA256 `4e62c83dbfc5be95e7850aa9290f0e66450dd4991b3bf81c1c56f10f4b807671`. The existing managed equipment slot remains enabled and is displayed as LWC Souls Embedded Reskin Test 0.1.1. Exactly one active plugin GUID copy. Original equipment DLL remains backed up under BepInEx/LivingWorldBackups/SoulsReskin-20261101T051945Z; the temporary 0.1.0 test is backed up at SoulsReskin-20261101T052307Z.

OFFLINE-TESTED: 31 new texture pixel/mipmap/color-space/filter checks; 108 material assignments; 66 prefab reference walks; six unchanged NPC definitions; no new dangling local references. Registration instructions unchanged. Core stays approved 1.0.32.29 (`d376d63b…`); 417 other profile files unchanged, including raid and NPC config. Color maps use sRGB, data maps linear, all new maps bilinear. This fixes an inherited template sampling error found before gameplay.

In-game loading, rendering and combat are UNKNOWN for this new bundle: no game launched. Brian will inspect the installed art. Existing Pillager and Servant loadouts reference repainted Sage gear; other actor loadouts unchanged. See CODEX-OUTBOX/2026-11-01-souls-reskin-arena-installed/.


## Tracking correction — LWC Souls Art Test 0.1.2 (2026-11-01)

INSTALLED and enabled in Arena. Brian requested a proper visible name and version. The old managed package name and 0.0.0 version have been replaced by `LWC-Souls_Art_Test`, display **LWC Souls Art Test**, author **LWC**, version **0.1.2**, art pass **Preview 01**. Plugin folder is `BepInEx/plugins/LWC-Souls_Art_Test`, DLL `LWC.SoulsArtTest.dll`. BepInPlugin startup display now reads LWC Souls Art Test 0.1.2. Compatibility GUID retained; one active copy. Embedded art bundle and method instructions unchanged from 0.1.1. Added versioned pack icon, README and both manifests.

- DLL SHA256: `21b3dae2eedfa494d50a1d63f5d834068ceb5cae5d0b2ef449e2723f9cb52c74`
- Named archive SHA256: `de2dba3abff8ea21524537982acbcc0470debc5de6ffe196fd1a933c25a19013`
- Archive: `compatibility/SoulsReskinTest/dist/LWC-Souls_Art_Test-0.1.2.zip`
- Naming backup: `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\BepInEx\LivingWorldBackups\SoulsLabels-20261101T052807Z`

OFFLINE-TESTED: installed prefab/material suite passes again (31 textures, 108 slots, 66 prefabs, six unchanged NPC definitions). Manager was closed to prevent cached metadata overwrites. Relaunch was canceled by Windows/user; no security warning bypass attempted. Metadata is verified on disk; visible manager UI not observed. Reopen r2modman to see the new entry. Game remains unlaunched.


## 0.1.3 — stream-lifetime crash repaired, real Unity startup passed (2026-11-01)

FAILED 0.1.2: Unity crashed while loading the first robe from the repacked bundle. The inherited loader called LoadFromStream then immediately disposed the resource stream. The LZ4 repack required later stream reads, producing ManagedStream unreadable, archive position-out-of-bounds and a native Unity crash. Offline parsing had not tested Unity's load behavior.

INSTALLED 0.1.3: copied embedded resource to a byte array and switched to LoadFromMemory, preserving source stream disposal after the copy. Only that loading call, one added helper and the visible version metadata changed; unrelated method instructions, embedded art bundle and all 71 Arena config files unchanged. Backup: `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\BepInEx\LivingWorldBackups\SoulsLoader-20261101T055031Z`.

LIVE-PROVEN startup: launched Arena via the actual r2modman Start modded control; log confirms LWC Souls Art Test 0.1.3, completion of robe/crown/tome/blade/head/scholar/artifact loading, menu music, zero BepInEx error lines and no unreadable-stream/corrupt-archive messages. Visually observed Valheim main menu. Game left running at menu for Brian. No world loaded; NPC appearance and combat remain UNKNOWN for this art pass. No claim of an in-world raid test.

- DLL SHA256: `544a07e998c8dd54411b72b0b5e191dcd049eadf1bb9a92538b97ed19ca043dd`
- ZIP SHA256: `e934f68606b63bbc2fc7f9c9dbb9568617a0d736c80f05096f2f26709ca0cb25`
- Local ZIP: `compatibility/SoulsReskinTest/dist/LWC-Souls_Art_Test-0.1.3.zip`

One enabled Arena manager entry contains the copied six Souls NPC definitions/spawn config plus embedded art equipment runtime. The obsolete Unknown Souls JSON Fix entry was archived outside plugins. Current manager visibly verified v0.1.3 by LWC; original IDs, loadouts and dependencies otherwise unchanged. Source snapshots of logs are local ignored build artifacts; public/source evidence contains summaries only.
