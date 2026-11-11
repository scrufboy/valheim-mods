# Installation receipt — Living World C.O.R.E. startup presentation

Installed 2026-11-11 into `LWC Formidable Arena` only. Valheim was not launched.

## Evidenced sources

- Exact supplied music attachment: `C:\Users\scruf\AppData\Local\Temp\codex-file-preview-Pic62s\001 — A World Worth Defending.mp3`
  - SHA-256: `43AEC8E1741C7D4B92DF269DBF93D7BA1D283EBD9E8FD0826334A687D29EE33B`
  - MP3, 48 kHz stereo, 200.04 seconds, approximately -14.9 LUFS / -1.2 dBTP.
- Approved graphic: `LWC-Floating-Core-Stone\OriginalStoneWordmark.png`
  - SHA-256: `D90C2DE1D57CB5F8F3E5957148DE03CF7FCAE5334A69EE4EB1544A698D6603D7`
- Azumatt ChangeOfScenery 1.0.6 package:
  - Package SHA-256: `FC2AABB1368A28464F5ABC7E76DC2441D5C06BE5A723A4DB198E717699022F12`
  - DLL SHA-256: `9B01EF6C19E942FD540316F0D3B0BD5A0E4A9EB5025D5F1202285909AD893591`
- Azumatt Logo Changer 1.0.12 package:
  - Package SHA-256: `DE64EEC46D9C1177BA56ED4C8974DEF7B7D2670B557EA60028E7B09391EE0692`
  - DLL SHA-256: `F3034ECF24DF01EF80F1CB28156D3D48A8DD81CDC387BAA9DFD57E4467F3880C`

## Generated title assets

- `LogoChanger_LOGO.png`: 2000x788, SHA-256 `BA079BB1BA20ED6CB6E876513E9360E2B083D419419A4E69012A56B0B4D09B92`
- `LogoChanger_AshlandsLogo.png`: 2048x448, SHA-256 `5C9918562B23721648A96D09633F313BC2181FBB3520C6A46060E11621D23953`

Both use the approved cracked-stone primary wordmark and the secondary line `Valheim — Deep North 1.0`, with transparent backgrounds and framework-native aspect ratios.

## Backup

Exact pre-install state and hashes:

`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\_LWC_Backups\StartupPresentation-20261111-122319`

All seven target files were absent before installation; this is recorded in `before-files.csv`. `installed-files.csv` records the installed hashes.

## Installed paths

- `BepInEx\plugins\Azumatt-ChangeOfScenery\ChangeOfScenery.dll`
- `BepInEx\plugins\Azumatt-Logo_Changer\LogoChanger.dll`
- `BepInEx\config\Azumatt.ChangeOfScenery.cfg`
- `BepInEx\config\Azumatt.LogoChanger.cfg`
- `BepInEx\config\Azumatt.LogoChanger_Images\LogoChanger_LOGO.png`
- `BepInEx\config\Azumatt.LogoChanger_Images\LogoChanger_AshlandsLogo.png`
- `BepInEx\config\Azumatt.ChangeOfScenery_Audio\A World Worth Defending.mp3`

All paths above are relative to the `LWC Formidable Arena` profile.

## Validation state

- Source: verified.
- Compiled: upstream Azumatt binaries verified by version and SHA-256; no local code compilation was required.
- Offline-tested: passed. All managed assembly references resolve against the installed Valheim 1.0.21 / BepInEx 5.4.23.6 files; required current menu types and patch targets exist; image sizes, transparency and music URI/hash were validated.
- Packaged: yes. Standalone package SHA-256 `3763EA87A55389F9342283094458AC523EC145B4090E49436C4E32314E346612`.
- Installed: yes, profile-local only.
- Launched: no.
- Game-tested: no; requires separate launch authorization.
