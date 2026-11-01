# Verification

OFFLINE-TESTED: reloaded eleven PNGs exactly match source pixels; linear normal/metal flags; full RGBA32 mip chains down to 1x1; seven exact materials, 21 slots; declared shader property checks. All other original serialized object raw bytes unchanged. Material trees differ only at intended texture references; all tint/keyword fields preserved.

OFFLINE-TESTED: Mono.Cecil method-body digest unchanged across resource substitution; instruction stream unchanged across label. Source DLL 544a07e998c8dd54411b72b0b5e191dcd049eadf1bb9a92538b97ed19ca043dd. Candidate DLL 1146fe5eade8e471b6217ce012b7d42c72d62a11c45310308efb9cbc2f344f68. All embedded resources except SagesVault.sagerobes unchanged. Original fixed loader method remains byte-for-instruction identical.

OFFLINE-TESTED: six NPC JSONs and one spawn CFG byte-identical. Active source DLL, mods.yml and seven configs unchanged during packaging. ZIP entry checksums reread and verified; ZIP SHA256 fee4648bed71cc517fda1c3ec1a1c82431dfb5396f43d1dfadec847ee0682f15.

FAILED preliminary checks, corrected before candidate: UnityPy generic image helper truncates mipmaps at 4x4; uncompressed RGBA32 builder now explicitly completes to 1x1. Initial nested staging path exceeded Windows path length; final private artifact uses repository build/SoulsG03-0.1.4/Pack. Failed intermediate outputs remain isolated under compatibility/SoulsReskinTest/build and are not the candidate.

UNKNOWN: actual0.1.4 in-game rendering. NOT INSTALLED; NOT ENABLED; NOT PUBLISHED. Unresolved shader properties: none.
