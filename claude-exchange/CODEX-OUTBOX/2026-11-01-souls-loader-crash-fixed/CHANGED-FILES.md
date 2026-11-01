# Changed files

- compatibility/SoulsReskinTest/consolidate_pack.py: one full managed pack/archive with six NPC definitions and the equipment runtime; archive obsolete metadata outside plugins.
- compatibility/SoulsReskinTest/Fix-BundleLoader.ps1: memory-backed resource loader and 0.1.3 startup label; verify all unrelated methods unchanged.
- compatibility/SoulsReskinTest/install_loader_fix.py: backup crash evidence and 0.1.2, install 0.1.3, verify unchanged embedded bundle/configs, version manifests/icon and full package.
- compatibility/SoulsReskinTest/README.md and VERIFICATION.md: failure and live startup proof.
- docs/CURRENT-PROJECT-STATE-2026-10-27.md: latest installed candidate and its actual validation boundary.
- This outbox: repair and results.

Installed changes limited to the named pack's loader DLL and metadata/icon/readme; manager version 0.1.3. No NPC config, raid, LWC core, telemetry or world changed. Both logs backed up locally; no raw telemetry uploaded.
