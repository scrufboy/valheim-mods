# Souls embedded reskin test 0.1.1

Private local test repack requested by Brian on 2026-11-01:

> can you have it built into a repacked rekinned version first of souls....removing the dependency and embedding the assets... so we can test our new rekinned version in game in the same test environment?

This package puts LWC's completed clothing, crown, book and staff PNGs directly
inside the equipment bundle used by the existing Souls hostile NPCs. No runtime
texture-swap plugin or loose PNG installation is necessary. The original item
and NPC prefab names stay stable; registration code and NPC definitions are
unchanged. The bundled equipment runtime replaces the separately installed
Sages Vault copy in the eventual test setup. It retains the SagesVault.dll name
and plugin identity deliberately: do not load both copies.

This is a repack of the existing equipment runtime, not a new independent LWC
equipment implementation. RRRCore, Jotunn, BepInEx, Spawn That and the existing
environment's compatibility fixes are still needed. The test does not change
NPC roles, stats, factions, equipment selection, spawning or raids. Previously
equipped items use the repainted textures through their existing item IDs.

## Build

Requires Python with UnityPy, dnfile and Pillow, and Mono.Cecil for the PowerShell
resource-embedding step. Run build_bundle.py with --source pointing at the
diagnostic SagesVault.dll, --art at art/sages-vault-original, and --output at a
staging directory. Run Embed-Bundle.ps1 against the generated bundle and a new
output DLL path. Neither step may overwrite the installed source.

Texture objects are added independently and selected material slots retargeted.
Existing material tints, shader keywords, offsets and scales are preserved.
Pants metal is redirected to sagecloth_m. Existing spell effects, stealth
blades, gems, icon atlas, audio, meshes and all other original objects remain
unchanged. Original texture objects remain embedded so shared references do not
accidentally change excluded effects. New maps use RGBA32 and mipmaps.
Color maps use sRGB; normal and metallic data maps use linear sampling. New
maps use bilinear filtering. Version 0.1.1 corrects the template sampling
settings caught during the Arena installation audit before gameplay.

## Verification

The build reloads the output bundle and verifies all 31 new maps against the
PNG pixels, all 108 changed slots across 42 materials, and every other original
object's bytes. The assembly step checks the embedded bundle hash and unchanged
method instructions. Package verification checks six NPC JSONs and the spawn
configuration byte-for-byte against the test environment.

OFFLINE-TESTED does not establish successful Unity loading or in-game rendering.
Those remain UNKNOWN until an approved installation and game run. The candidate
is staged only; no installation script runs automatically.

## Eventual test installation and rollback

Close Valheim. Back up the existing equipment DLL, Souls config folder and
profile mod list. Disable the existing Sages Vault entry before loading the
repacked DLL: both have the same plugin GUID and must never load together.
Apply only the staged files, keeping the current RRRCore compatibility repair
and raid configs. Confirm all six Souls prefabs register, then inspect a robe,
hood, tunic, pants, crown, book, numbered staff and scholar staff. Confirm tints,
cloth reflectivity, alpha edges and distant mipmaps, plus unchanged excluded
effects. Roll back by removing this candidate's files and restoring the backup.

Only the original LWC texture pack is covered by its included MIT license.
Existing runtime and game assets retain their attribution. This artifact is
for the requested local test; it is not a public release submission.
