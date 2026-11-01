# Sage clothing revision brief — subtle LWC pass

Review scope: read-only comparison of the current LWC PNGs in `valheim-mods-formidable-current/art/sages-vault-original` with the extracted reference maps in `Extracted_SagesVault_1.1.4/Textures`. No artwork, source bundles, game profiles or installed files were changed. The new review files are the only writes. User direction is refinement toward natural worn clothing, not replacement of a decent original appearance with uniform generic material fills.

## Findings

1. **Weave scale is inconsistent.** The LWC brown robe's lower-right wool region has a visibly larger, higher-contrast basket weave than its upper dye/linen regions. The pants reuse the chunky weave on a much smaller UV island. A repeated pattern at the same texture-pixel period does not have the same physical scale across different garment UV layouts. More texture resolution does not fix this.
2. **Pants are too dark relative to the intended outfit.** In the sampled nonblack atlas regions, current sRGB luma medians are approximately 0.214 (brown dye), 0.486 (undyed linen), 0.171 (wool trim), and 0.120 (pants cloth). Pants are about 30% darker than the trim before lighting and material tints. These are image-space diagnostics, not predictions of final game exposure. Crop coordinates, masks, dimensions and source SHA256 values are recorded in `comparison-metrics.json`.
3. **The new surface pattern overwhelms local garment history.** The reference robe has broad folds, restrained tonal variation and less insistently repeated fiber detail. The current pass replaces much of that hierarchy with relatively uniform woven noise. The result can read as a tiled material sample rather than clothing worn by a person.
4. **The chest overlay is part of the outfit.** A robe's visible result spans its mesh `_MainTex` and the body `_ChestTex`; pants use `_LegsTex` plus their mesh map. Reviewing only the robe atlas misses seams, tone discontinuities, and chest/leg material mismatches.
5. **Material channels and tints can exaggerate the problem.** Hood and crown `_Color` tints remain on their neutral maps; selected dark hood tints are already extremely low. Clothing's metallic channel must stay nonmetallic. Normal/smoothness detail must share the albedo weave's scale, otherwise smaller color fibers still shade like coarse rope.

## Pass A — scale and tone, before any added dirt

- Make the visible weave approximately **one third of its current feature size** as the first candidate; apply that scale reduction consistently to the same material's albedo, packed normal and smoothness detail. This is a trial value, not a claim that the current map contains known millimeter dimensions.
- Scale pattern layers **inside unchanged UV-island masks**. Do not resize or shift the atlas, its seam borders, transparency, straps, or garment silhouette.
- Establish one shared cloth master per material. Measure texture density on the actual meshes (pixels per meter along representative UV triangles), then adjust the image-space repeat for each garment so the master has the same physical pitch. Do not use the same pixel-repeat count on every atlas.
- Start with pants cloth in the **same warm value family as robe wool trim**: target median luma within roughly ±10% of the trim in a neutral, equally lit comparison. Keep a deliberate smaller fold/seam range instead of uniformly brightening every pixel. This is a starting art target to approve in game, not a universal rule for every colorway.
- Reduce the micro-weave contrast enough that broad folds dominate at ordinary play distance. At 3–5 m, the garment should read as cloth with a coherent tone; distinct cross-woven threads should mainly appear in close inspection. Reject a pass that shimmers, crawls or forms moire during camera movement.
- Recheck every colorway after its actual material `_Color` multiply. A neutral bright texture can become near-black under the existing black/brown hood tints; do not compensate by randomly changing the tints before the comparison.

Acceptance for A: unchanged UV alignment and silhouettes; visibly finer weave; coherent torso/trim/pants palette; no rope-sized fibers, high-contrast checker pattern or unexpected bronze shine. Keep A as a separate candidate, not an overwrite of Preview 01.

## Pass B — localized use, wear and dirt

- Add wear in response to garment construction and contact: cuffs, elbows, knees, seat, belt friction, folds, hems and boot-adjacent edges.
- Use dirt as localized accumulation with broad irregular transitions, not a uniformly applied grunge layer. Lower hems and knees can carry more ground staining; upper areas should retain readable dye/linen.
- For leather, use edge polish, small crease wear and restrained scratches, preserving thickness and tanning variation. Avoid photographic cracked leather tiled at a size unrelated to the model.
- Keep repairs, fraying and discoloration sparse enough that one garment has a recognizable history rather than every seam receiving identical distress.
- Keep woven relief subtle in the normal map; do not turn dirt into raised coarse fibers. Cloth metal remains zero. Smoothness changes should describe contact wear/moisture/material, not random noise.
- Author color, microstructure, wear, dirt and repair on separate controllable layers. Save matching channel versions together.

Acceptance for B: worn and used at close range, coherent at medium distance, construction-aware wear, no uniformly dirty surface or forced near-black pants.

## Pass C — actual motion and lighting QA

Capture the same named outfit and body variant in both the studio and Valheim. Use fixed camera/light presets and record the candidate ID on every still/video.

1. Neutral daylight: front/back/sides at 0.5–1 m, 3 m and 5 m; compare robe mesh, chest overlay and pants together.
2. A slow turntable and a slow camera move: inspect crawling weave, mip transitions, specular noise and seams.
3. Idle, walk, run and at least one arm-raising emote: inspect stretched weave, cuffs, shoulder seams, chest overlay and knees; frame-step any suspect interval.
4. Warm firelight and low swamp light: ensure pants do not disappear and cloth does not become metallic/plastic.
5. Compare Preview 01, A and B side-by-side with matching framing/exposure. Approve in-motion readability before packaging a replacement.

The standalone studio uses approximate PBR and is excellent for UV, color, scale and pose checks. Final shader/tint behavior must be judged in Valheim. A still on a green background does not prove the moving, shaded garment reads correctly in game.

## Channel implementation notes

- Albedo maps: sRGB. Normal and metallic/smoothness maps: linear.
- Unity packed cloth normal: preserve the established channel swizzle. Do not paint an ordinary blue RGB tangent normal into the packed slot without conversion.
- Metallic/smoothness: metallic in R; smoothness in A. The studio preview converts this to glTF metallic B and roughness G (1−smoothness).
- The studio composites body `_ChestTex` and `_LegsTex` over `_MainTex`/`_SkinTex` for preview, and separately reads an item's `m_armorMaterial`. Review the body overlay and outer garment as one assembled outfit.
- Keep material `_Color` and `_EmissionColor` distinct from the PNG itself. A generic atlas viewer without those values can mislead the art judgment.

This review did not repaint any source. Brian has requested additional refinement passes; author them as separate reviewable candidates and preserve Preview 01.
