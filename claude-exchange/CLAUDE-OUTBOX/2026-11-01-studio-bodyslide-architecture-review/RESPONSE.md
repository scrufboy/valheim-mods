# LWC Model & Skin Studio: BodySlide/Outfit Studio architecture review (Claude, 2026-11-01)

Authority: Codex relaying Brian's explicit work order (2026-11-01 09:11 ET, Claude chat): "BodySlide and Outfit Studio for Valheim, using Claude, Cursor and Codex. Please start your independent architecture review now; no further confirmation is needed for this review."
Scope: review only. No source, art, NPC identity, profile or installed file was changed.

## Evidence basis

- **SOURCE-INSPECTED:** the local studio was read from Brian's machine, read-only, at ~09:15 ET:
  - `LWCModelSkinStudio`: README, `newworkbench/DESIGN.md`, `morphs.py`, `recipes.py`;
  - `asset_pipeline/importer.py`;
  - `mod_export/SkinRuntime.cs`, `exporter.py`, tests;
  - `backend/server.py`;
  - `viewer/studio.js`, by sections only.
  - **The tree was being edited during the read** (`studio.js` grew from 56,504 to 61,105 bytes), so line numbers refer to that copy. Local Git `64706e9` was not checked out separately.
- **Not read:** the owner's manual (skimmed only), the remaining viewer tests, `index.html`/CSS, the UniView vendor files and the binaries.
- **SOURCE-INSPECTED, game metadata:** Valheim 1.0.21 `assembly_valheim` member list for `VisEquipment` (reflection over member names only; no decompiled code).
  - The body: `m_bodyModel` (SkinnedMeshRenderer), `m_models` (`PlayerModel{m_mesh, m_baseMaterial}`), `m_lodGroup`, `m_boneMap`.
  - Body texture slots: `s_chestTex`/`s_chestBumpMap`/`s_chestMetal`, `s_legsTex`/`s_legsBumpMap`/`s_legsMetal`, `s_skinBumpMap`.
  - Equipment instances: `m_chestItemInstances`, `m_legItemInstances`, …
  - Methods: `SetModel`, `UpdateEquipmentVisuals`, `AttachArmor`, `SetupCloth`, `m_clothColliders`; the assembly hard-references `MagicaClothV2`.
  - Behaviour behind these names is **inferred**, not verified.
- Nothing below is LIVE-PROVEN. The 0.2.0 skin runtime is offline-tested only (its RELEASE-0.2.0.md says the same).

## 1. Rig and morph risks (ranked)

1. **Runtime mesh access is the gating unknown.**
   - Game meshes loaded from asset bundles are usually not CPU-readable at runtime. If they aren't readable, a runtime "mesh-delta loader" cannot read vertices to apply a field, and cannot add blend shapes to them either.
   - This decides the whole runtime design, and it is UNKNOWN.
   - Before building any morph runtime, run a one-hour spike in an isolated profile: log `Mesh.isReadable` for the Player body (both `m_models`) and two armor `attach_skin` meshes.
   - The fallback options differ in licensing and design:
     - shipping a deformed copy (game-derived geometry, see §4);
     - per-vertex deltas (derived data, still keyed to game geometry);
     - bone-scale morphing (the cheap, rig-touching approach the brief rules out).
2. **"Same world-space field" for clothes is wrong as stated; the code does a Kabsch fit instead of the exact mapping.**
   - Garments and body share a skeleton by bone name (`m_boneMap`; viewer rebinding at `studio.js:263-269`), but each mesh has its own bind poses.
   - The correct shared space is the **body bind-pose (rig) space**: map each garment vertex through its own bind pose into the skeleton's bind space, apply the body field there, and map back. That is exact, and no fit is needed.
   - `morphs.py:354-370` instead aligns garments by rigid SVD on shared bones and accepts up to 3.5% of body height of residual (about 6–7 cm on a human).
   - That tolerance hides real misfits. The deformation is evaluated at the wrong place exactly where clothing hugs the body (waist, shoulders), which produces clipping.
   - Replace the fit with the bind-pose mapping. Make any residual above ~2 mm a hard reject that names the bone.
3. **Normals and tangents.**
   - `morphs.py:373-383` recomputes area-weighted normals from scratch. This is a problem in two ways:
     - UV seams duplicate vertices, so the copies of one point get different normals: visible seams on shoulders and sleeves.
     - Authored hard or soft normals are lost.
   - Instead, transform the original normals by the field's inverse-transpose Jacobian. Weld by position when you need to recompute. Leave vertices with zero displacement untouched.
   - The code **refuses meshes that carry TANGENT** (`:457-458`). Valheim cloth uses normal maps, so tangents matter. A morph that can't carry tangents would leave normal-mapped cloth lit wrongly after deformation.
   - The pipeline needs to recompute tangents after deformation (offline MikkTSpace, or `RecalculateTangents` at runtime) and verify them, not reject such meshes.
4. **Coordinate mirroring is a silent-corruption risk.**
   - The importer mirrors X, flips V, reverses winding (`importer.py:259, 409-413, 433`) and rewrites bind poses as `mirror@m@mirror` (`:419-424`). Morphs are computed in that mirrored glTF space.
   - A runtime delta must be converted back exactly. Get one sign wrong and it widens the wrong side or turns faces inside out.
   - Add a round-trip test: import, morph at 0, export the delta, and require an identical Unity-space mesh. Also test one ±15% case on an asymmetric probe vertex.
5. **Per-instance, per-event re-application.**
   - Writing the shared mesh would morph every Player, companion and Souls NPC that uses it. Each character needs its own mesh copy, which costs memory per body.
   - Every `SetModel` (base-body switch) and `UpdateEquipmentVisuals` rebuilds or attaches item instances, so the morph must be re-applied after each of them.
   - It must also cover every LOD renderer under `m_lodGroup`.
   - Expect the ragdoll on death to show the default body unless it is handled too (UNKNOWN; check the Player ragdoll prefab).
6. **Cloth simulation and colliders.**
   - The game uses MagicaCloth V2 (`SetupCloth`, `m_clothColliders`). Capes and robes simulate against colliders sized for the default body.
   - A ±15% wider torso will push into cloth that still collides with the old volume. Either scale the cloth colliders with the same field, or cap the sliders where cloth is equipped.
   - A morph that changes the mesh a cloth proxy was built from may also break the proxy (UNKNOWN until tested).
7. **Landmarks and weights.**
   - The 16 required bones (`morphs.py:33`) exist on Player-derived rigs: LWC companions are Player clones, and RRR Souls NPCs are Player-based. Monsters are correctly rejected.
   - Skin weights are truncated to 4 influences and renormalised at import (`importer.py:416-418`). Fine for Unity's default quality, but compute morph extents from the **untruncated** weights when the source has more than 4. Otherwise the 95th-percentile radii (`:268-284`) drift.
8. **Collision and gameplay.**
   - Vertex morphs change nothing physical: the capsule collider, hit detection and attach points (hand, back weapons, `m_helmet`) are unchanged.
   - That is correct for fairness. But wide shoulders will clip back-slot weapons (`m_backMelee`, `m_backShield`) unless those attach transforms are offset by the field too.
   - That offset is a *transform* change and must not touch the rig itself.

## 2. Material and channel risks

1. **Author per target shader's real slots, not one universal channel set.**
   - The body overlay path (`Custom/Player`) exposes chest/legs **albedo, bump and metal** only (`s_chestTex/BumpMap/Metal`, `s_legsTex/BumpMap/Metal`). There is no emission, occlusion or opacity slot for overlays.
   - So "explicitly manage albedo, normal/height, metallic/smoothness, emission, occlusion, opacity" must be a **per-shader capability table**. The studio must not let an author paint emission or occlusion that the target shader ignores.
2. **Tint colour space.**
   - `_Color` goes into glTF `baseColorFactor` unconverted (`importer.py:312-313`). glTF factors are linear, while Unity colour properties are authored in gamma.
   - So the preview tints are off, worst for the very dark hood tints already flagged in the texture brief.
   - Convert sRGB→linear for colour properties (not for alpha). Re-judge the tint findings after that.
3. **Normal packing at runtime.**
   - The importer unpacks DXT5nm-style normals (`R*A`, G) correctly.
   - But a runtime `Texture2D(RGBA32, linear)` is not an imported "normal map". The shader's `UnpackNormal` on DXT5nm platforms reads A/G.
   - The exporter must **repack edited normals into the swizzle the shader expects** and verify the packing (a test with a known tilted normal).
   - The exporter currently decides linear from slot names (`exporter.py:202`). That is fine for flags, but nothing checks the packing.
4. **Preview fidelity.**
   - Chest and legs overlays are NEAREST-resized when composited (`importer.py:353-370`), which aliases where the game samples bilinear/trilinear.
   - Occlusion is not wired into the GLB.
   - A metal map is also reused as the roughness map (`studio.js:458`). That is correct only for the packed G/B layout, so keep it but assert it.
   - "PBR shading is approximate" should list these specific differences in the manual.
5. **"A painted shadow is not a dent."** Agreed. Concretely:
   - keep PSD masters with separate albedo-shading, height/normal-detail and wear/dirt layers;
   - derive the normal only from height layers;
   - **forbid deriving normals from albedo** in the pipeline;
   - store PSDs outside the export folder;
   - export one flattened PNG per channel with its hash recorded in the recipe (content-addressed `Textures/<sha256>.png` already exists, `recipes.py:316-344`).

## 3. Runtime risks (SkinRuntime 0.2.0)

1. `Resources.FindObjectsOfTypeAll<Material>()` runs **every 5 s forever** (`SkinRuntime.cs:27-32, 70`). It allocates over every loaded material, which causes periodic hitches in a heavily modded profile like the Arena.
   - Make it event-driven: after ZNetScene/ObjectDB are ready, plus a post-hook on `VisEquipment.UpdateEquipmentVisuals`. Or stop scanning once every binding has resolved.
2. It **writes the shared material asset** (`:86-88`). That is right for a global reskin pack. It is wrong for per-character recipes: two people wearing the same robe with different edits is impossible this way.
   - Recipes need per-renderer `MaterialPropertyBlock`s or instanced materials, bound to the character.
3. Matching by material name + shader + original texture name/size (`:79-85`) is fragile across game and mod updates, and can match an unintended material with the same name and size.
   - Record the original texture's content hash at export time and verify it at runtime before swapping (one-time cost per texture).
4. Multiplayer: textures swap locally only. A recipe (and later a morph) is invisible to other players unless it is carried on the character, e.g. in ZDO, and every client has the same pack.
   - Decide this before adding recipe fields. It affects identity storage (§5).

## 4. Packaging and licensing (a blocking question, not a code fix)

- The export ZIP contains **edited copies of game- or mod-derived textures** (`importer.py:328-329` → `exporter.py:232-245`). The notice saying the app bundles no Valheim textures covers the app itself, not its exports.
- A personal-use pack is one thing. A Thunderstore-ready ZIP with a manifest and dependency (`exporter.py:225-241`) is set up for redistribution of Iron Gate and third-party (Sages Vault / Souls) derived art.
- **Boundary:** each exported PNG records its provenance (original LWC art / derived from <source> / unknown). Export stays "personal" unless every pixel source is cleared, and a public build refuses derived pixels.
- The studio is GPL-3.0 and the runtime DLL ships under it. Whether GPL-3.0 code linked against Valheim's proprietary assemblies can be distributed is a **legal question for Brian**. It is flagged here, not decided.
- Hard-coded personal paths in `server.py` (the Arena profile path, the `LWC.SoulsArtTest.dll` path, `K:/Photoshop.exe`) must become settings before any packaged build leaves this machine.

## 5. Recipe and identity

- Two recipe formats exist:
  - `recipes.py` (schema 1, strict keys, opaque `identity`, dependency resolution that never substitutes);
  - the viewer's `workbench` object saved into `project.json` (`studio.js:337-339, 578`), which has **no schemaVersion and no identity**.
  - Make the viewer read and write only the `recipes.py` schema, so one format exists.
- `identity` is checked only for JSON-serialisability (`recipes.py:200-205`).
  - Require it to round-trip **byte-identical** through load/save/edit (a test).
  - Forbid recipe code from reading any key inside it.
  - Forbid any LWC runtime from deriving class, role, faction or loadout from a recipe, consistent with `docs/development/CHARACTER-IDENTITY-CONTRACT.md`.
  - A recipe is appearance; it is never applied to an existing companion automatically.
- Slots `chest, legs, head, back, rightHand, leftHand` are missing Valheim's shoulder (cape) and utility, which `VisEquipment` treats separately (`m_shoulderItem`, `m_utilityItem`, `SetShoulderEquipped`). Add them now or state that they are excluded.
  - Note the cape is the MagicaCloth case in §1.6.
- `newworkbench` has **no tests** and no viewer or server wiring yet. Recipe and morph modules should not ship before both exist.

## 6. Proposed production-ready boundary

| Stage | In scope | Gate to pass |
|---|---|---|
| 0.2.x texture skins | Global reskin packs, personal use | One isolated-profile game launch shows the bound textures and zero unmatched bindings. Event-driven scan; hash-verified originals; provenance in the ZIP; settings instead of hard-coded paths |
| 0.3 recipes (studio only) | One schema; slots incl. shoulder/utility; opaque identity; content-addressed textures; per-shader capability table; PSD masters kept out of exports | Tests: identity byte round-trip; unresolved dependency never substituted; capability table rejects unsupported channels; sRGB tint conversion; normal repack verified |
| 0.4 morphs (studio preview only) | ±15% sliders; bind-space field; garments via their own bind poses; Jacobian normals + tangents; mirror round-trip | Tests: zero morph = identical mesh; asymmetric probe; seam normals continuous; garment residual ≤ 2 mm or named reject; animated clips show no flips/collapses (frame-step) |
| 0.5 morph runtime spike (isolated profile, no release) | Read-only probe: `isReadable`, LODs, `SetModel`/`UpdateEquipmentVisuals` timing, ragdoll, MagicaCloth | A written result decides between deltas, copies and bone-scale before any runtime morph code exists |
| Per-character runtime (later) | `MaterialPropertyBlock`/instanced materials per character; ZDO-carried recipe reference; multiplayer rule | Only after the 0.5 result, Brian's explicit decision and the licensing answer |

**Not acceptable at any stage:**
- writing shared meshes;
- inferring identity from appearance;
- installing into the Arena baseline without Brian's instruction;
- publishing derived pixels.
