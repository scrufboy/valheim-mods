# ORDER 15:40 — Colossus club spikes in black metal + wet gashes. Build and install; do NOT launch or test (Brian tests).

Claude verified from the installed 000-andeancolossus.bundle (AndeanColossus_Club mesh, 324 verts / 396 tris):
- The club has **9 spikes** (pyramids, 3 faces each = **27 triangles**). Apex = vertex whose radius from the club axis exceeds its neighbours' mean by > 0.12 units.
- **All 27 spike triangles share ONE UV triangle** on the skin texture: pixels x 378–805, y 39–404 of the 1254² map (top-centre, above the eye). That area is also used by the **body** (61k of 73k pixels overlap) and other club faces. **Painting it black would blacken body skin too.**

1. **Black metal spikes:** split the 27 spike triangles into their own submesh on the club (same method as ColossusBronzeBinding: triangle partition, original topology/UVs kept). Give that submesh the vanilla **Black Metal** look — clone the material of a native black-metal item (e.g. SwordBlackmetal / AxeBlackMetal) and use its metal tint/smoothness. Living club and corpse club both. No mesh shape change.
2. **Wet gashes, moist skin:** Brian will paint the gouges/face fixes. Check whether the Colossus body shader exposes smoothness/gloss (a map or channel). If yes: accept a greyscale **wet mask** from Brian in the skin UV layout (white = wet gash, mid-grey = moist skin, dark = dry) and bind it. If no: say in ONE line what the shader supports and the closest option. Regenerate the normal map from Brian's new paint when he delivers it (existing skin-normal-unity-ag pipeline).
3. Install into Arena (game closed; r2modman open is not a blocker). One line: "Installed." Do not launch.
