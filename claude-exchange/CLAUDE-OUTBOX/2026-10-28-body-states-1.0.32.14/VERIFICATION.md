# Verification
- OFFLINE-TESTED: regression ledger 121/121; native gate 42 members; ownership gate OK; combat-voice gate LOCKED, unchanged; movement-writer gate OK; analyzer self-test OK.
- Package: `LivingWorld.HumanNpc.dll` sha256 `206653b4fdc3348c5984872139d3b5a5c0a998f8dd69dd80a741a9bddb5c5ac5`.
- INSTALLED (LWC Human Lab v1 profile): DLL hash on device matches the package. Previous 1.0.32.13 DLL (`0a44d570…`) backed up to `BepInEx/LivingWorldBackups/LivingWorld.HumanNpc.1.0.32.13.dll`.
- INSTALLED: 18 clips in `voices/body/<g>/<pool>/`. fmt and PCM data byte-identical to the handoff WAVs (18/18). File hashes differ from the manifest because the device transfer appends a C2PA provenance chunk after the data chunk; `WavClip.Load` stops at the data chunk, so playback is unaffected.
- INSTALLED: the 40 existing poison clips re-hashed on device: 40/40 equal the handoff manifest (untouched).
- LIVE-PROVEN: nothing yet.
