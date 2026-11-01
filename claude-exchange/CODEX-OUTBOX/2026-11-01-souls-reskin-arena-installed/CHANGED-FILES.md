# Changed files

- compatibility/SoulsReskinTest/build_bundle.py: correct sRGB/linear sampling and bilinear filtering, verify reloaded settings.
- compatibility/SoulsReskinTest/package_test.py: package 0.1.1, permit repeat builds without a self-referencing SHA256 manifest.
- compatibility/SoulsReskinTest/install_arena.py: baseline/hash checked install, backup, enabled managed slot, rollback on failure, protect other profile files.
- compatibility/SoulsReskinTest/verify_arena.py: installed texture and prefab resolution simulation, dependencies, unique GUID, core baseline and enabled state.
- compatibility/SoulsReskinTest/README.md and VERIFICATION.md: corrected candidate and installed evidence.
- docs/CURRENT-PROJECT-STATE-2026-10-27.md: installed Arena candidate status.
- This outbox: authorization, evidence and next visual inspection.

Installed changes: only equipment SagesVault.dll and its mm_v2_manifest.json plus the profile mods.yml display name. Existing managed package identity/path and enabled=true retained. No config files overwritten. Backup created outside plugins. Other 417 existing plugin/config files unchanged. Generated build/dist binaries remain local and ignored.
