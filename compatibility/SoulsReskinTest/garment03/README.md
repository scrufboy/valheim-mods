# Private Garment03 staging

Authority: Brian's recorded action instruction, “Let's do it... get the pack done.” This source only assembles a PRIVATE ART CANDIDATE for review; it does not install, enable, change loadouts, or claim public redistribution clearance.

Baseline: installed Arena LWC.SoulsArtTest.dll 0.1.3, SHA-256 `544a07e998c8dd54411b72b0b5e191dcd049eadf1bb9a92538b97ed19ca043dd`. Its memory-backed loader repair is retained unchanged. Source scope is this folder, not Workbench or LWC NPC code.

`build_bundle.py` embeds exactly the eleven non-comparison PNGs from art/sages-vault-garment-03. Seven exact brown/black robe, chest, tunic, and hood material names are declared in TARGETS; 21 texture references change. Chest and hood maps are independent of the robe maps. Each property must exist in the saved material and its embedded shader declaration. Tints and keywords remain unchanged. Original objects, including prior texture objects, remain byte-identical unless they are one of the seven intended material objects. No meshes are replaced.

Added RGBA32 maps have full mip chains down to 1x1; normal/metal maps are linear, albedo maps sRGB. All eleven base images are compared byte-for-byte after bundle reload. Copying the older runtime retains all its remaining code/asset dependencies. Cursor's art README is preserved unchanged in the candidate.

Build steps, using explicit local paths:

1. Run build_bundle.py --source <0.1.3 DLL> --art <Garment03 directory> --output <new unique bundle output>.
2. Run ../Embed-Bundle.ps1 on the copied DLL and generated bundle. Its method digest verifies unchanged method bodies/variables/exception handlers.
3. Run Label-Garment03.ps1 from the intermediate copy to the final staged LWC.SoulsArtTest.dll; use the existing local Mono.Cecil.dll. It verifies unchanged method instructions while setting visible plugin name/version to LWC Souls Garment03 Art Test 0.1.4. Compatibility GUID stays fantu.sagesvault.
4. Copy final DLL and the three verification JSONs to a short private directory under repository build/, then run package_private.py --profile <read-only Arena source> --build <that short directory> --art <Garment03 directory>. The short path avoids Windows legacy path-length limits. Never point --build at a game profile. The builder rejects output outside repository build/.

Current artifact: build/SoulsG03-0.1.4/PRIVATE-LWC-Souls-Garment03-Art-Test-0.1.4.zip.

OFFLINE-TESTED: eleven exact PNGs, seven materials, 21 slots; complete mips and linear flags; all untouched serialized bytes; unchanged method code; all other embedded resources; all six NPC JSON definitions plus spawn config copied exactly; archive checksums checked after ZIP readback. Active DLL, mods.yml, and seven config files unchanged. In-game rendering UNKNOWN. Not installed, not enabled, not published. Installation would be a separate explicit step after review; this folder contains no installer.

Candidate ZIP SHA-256: fee4648bed71cc517fda1c3ec1a1c82431dfb5396f43d1dfadec847ee0682f15.
Candidate DLL SHA-256: 1146fe5eade8e471b6217ce012b7d42c72d62a11c45310308efb9cbc2f344f68.
Unresolved shader properties: none. Hood _NORMALMAP keyword already existed and is retained.
