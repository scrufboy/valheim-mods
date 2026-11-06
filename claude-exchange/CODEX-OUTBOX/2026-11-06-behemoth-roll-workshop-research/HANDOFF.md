LIVING WORLD C.O.R.E. — DRAUGR BEHEMOTH ANIMATION AND WORKSHOP RESEARCH
Recorded 6 November 2026 (America/New_York)
Status: SOURCE-INSPECTED feasibility and owner-feedback handoff. No attack, editor control or shaman gameplay was built by this research session.

AUTHORITY AND ROUTING
Brian explicitly requested: “Research if it's possible having that attack attached and those animations attached” and “Update the right chats for this.”
This authorizes research and communicating the full request. The roll, dance, attachment editor and shaman descriptions remain design/exploration requests; this handoff is not an installation or broad implementation order.
Route to the existing Workbench root/creature runtime owner, animation exploration owner, Workshop accessory authoring owner, and Neck faction/lore owner. Preserve current file ownership; do not start competing implementations.

FINDING
The forward human roll is a credible candidate for a new Behemoth attack. Its current source explicitly requires the original valid Humanoid Avatar and assigns the native GoblinBrute (Fuling Berserker) controller. Unity supports sharing Humanoid animation through configured Avatars. That supports a technical route, not a claim that this particular roll looks correct on this body.
The existing Workshop cross-source preview already reads a donor's clip using the target Avatar. Use that path first with the actual Player roll and dance clips from Brian's installed game. Do not invent clip IDs or assume the Berserker controller already contains them.
An AnimatorOverrideController swaps clips in existing states; it does not itself add a new state or independent attack. Do not overwrite bash/rage bash to pretend that a separate special was added. Inspect the existing controller and use a creature-scoped authored extension only if implementation is subsequently ordered.
The roll's visual motion is not damage, forward travel, collision handling or a complete native dodge. Keep normal bash/rage bash working and preserve the approved body rig, geometry and ordinary movement.

LATEST EVIDENCE, WITH LIMITS
SOURCE-INSPECTED: scrufboy/lwc-valheim-workbench work/valheim-workbench at c7e145d6d8f4eda26d7578fc08d5f424314e9211.
- creature_runtime/ColossusPrefabFactory.cs: valid original Humanoid Avatar; native Brute runtimeAnimatorController; native root-motion policy and CharacterAnimEvent integration.
- exchange/animation_capture/2026-11-03-cross-source-preview/HANDOFF.md and animation_capture/README.md: donor/target humanoid preview, frame controls, capture; browser preview is not Unity Animator execution, IK, cloth or shader-exact rendering.
- docs/project/BEHEMOTH-CLOTH-0.1.26-2026-11-06.md: installation receipt reports complete 0.1.26 ClothCandidate01 in Arena, replacing 0.1.22; no game launched for that installation. Do not repeat the older 0.1.22-installed entry as the latest status.
- Native hair/beard components and source normals are retained in the newer source. Magica collision proxies and optional waist flex are candidate work. Actual hair/beard motion, root fit, body collision and corpse behavior remain UNKNOWN until observed.
- docs/project/BEHEMOTH-CONTINUITY-2026-11-06.md: source atlas hair-band restoration remains unfinished. Workshop 0.5.23 preview explicitly does not simulate native cloth. An open 0.1.25 preview can coexist with installed 0.1.26.
- docs/project/BEHEMOTH-SINGLE-BUILD-RULE.txt: standalone and LWC use one canonical creature build; optional ruin guardian remains a separate expansion.
These are inspected source/receipts, not fresh access to installed Windows bytes, the current viewport or gameplay.

OWNER'S SPECIAL ATTACK CONCEPT
This is for the giant Draugr Behemoth, not Brian's Berserker.
Sequence: accumulated qualifying exertion/hits and binding/trinket pressure -> rage tell with raised club -> quick crouch/tuck -> committed forward roll -> recovery.
The huge round body becomes the ram: devastating contact damage, strong push/stagger, and substantial destruction of eligible buildings along its path. Preserve the existing attacks.
The exact meaning of “stamina hits” is unresolved: the source inspected here does not establish the intended stamina/binding counter. Do not silently substitute hit count, health loss or a timer as an approved rule. Record candidate trigger inputs and return the smallest coherent choice for Brian's review.

ENGINEERING SHAPE TO INVESTIGATE
1. Preview the exact Player forward roll on the original Behemoth Avatar through the existing donor selector. Inspect belly compression, hands/club, head, beard, waist, shoulders, ground clearance and rise/recovery. A usable Humanoid rig does not guarantee a convincing ball roll. A human dodge usually depicts one tumble; sustained rolling may need an authored clip or carefully bounded repetition.
2. Treat charge, tell, commitment, active contact and recovery as one bounded authoritative attack transaction. Named phases here describe behavior, not new API/schema requirements. Commit direction after the tell; do not let the roll track the player with unlimited steering.
3. Choose one existing movement authority, with the actual clip's root translation inspected first. Never combine root motion with a second forced displacement. Sweep the body path against terrain and obstacles; do not teleport through a wall.
4. Damage only while the rolling contact is active, using a body-sized sweep/overlap and the existing native damage path. Track each actual damage recipient once per roll, including multiple colliders belonging to the same recipient. Separate building pieces remain separate recipients. Order contacts so intact walls obstruct the roll; continuation through a destroyed piece is bounded and re-evaluated.
5. Tune character blunt impact, stagger and knockback separately from structure damage. Verify the actual native damage modifiers and building rules. Do not delete buildings directly or promise every character can be forcibly stunned regardless of native resistance.
6. Reuse the animation pose without automatically granting the Player dodge's invulnerability, stamina logic or Player-only callbacks. Inspect/strip inappropriate events from an owned copy if needed; never mutate shared vanilla clip assets.
7. Network owner alone commits movement, charge consumption and damage; peers render the same attack identity/phase. Interrupts, death, loss of ground, blocking obstacles, despawn and owner transfer must close the active damage window and restore ordinary combat. Avoid a free full-charge retrigger after reload/transfer.
8. Expose balance only through established authoring/config paths if implementation is ordered. No invented exact damage, range, threshold or cooldown is approved by this research.
Validation should be a complete reviewable candidate followed by Brian's real scenario/video loop: ordinary attacks retained; one target cannot be shredded once per frame; player/NPC push; breakable versus intact structures; slopes; cancellation; hair/club; multiplayer ownership. Do not revive the rejected metric/micro-test delivery framework.

DANCE AND PROMOTIONAL ANIMATION
Brian wants hidden/manual dance animations on this model for Workshop demonstration and advertising. Use the same exact Player donor-clip route. Keep them out of combat and random idle selection.
Workshop-only preview/capture is the first scope. Persisting a dance into the runtime or providing a live-game secret command is separate and not proven necessary for Workshop footage. A menu entry alone does not attach an emote to a controller.
The existing timeline, pause/frame controls and video capture can support the shoot once the clip has been visually checked on the Behemoth. Do not imply that a dance preview proves combat motion or hair physics.

WORKSHOP FITTING REQUEST: “MOVE IT, SAVE IT, THEN LOOK WHERE I PUT IT”
Brian reports that the mustache looks a little low. He wants to select beard/mustache, position them visually, and have the saved placement become the actual fix. He also asks about the brass ring under the nose and possibly the nose itself.
Proposed authoring behavior, not implemented here:
- Select the actual accessory/part; move, rotate and scale it in a head-local frame, with fine numeric controls, reset and undo.
- Save through the existing project/override recipe, retaining exact source model and accessory/style identity. The runtime and corpse must consume the same explicit authored adjustment after automatic fitting; spawning/reopening must not erase it.
- Preserve accessory skinning, cloth root/pins and owned collision references. Move the accessory anchor/appropriate assembly coherently, rather than dragging a simulated tip or an animated body bone that the Animator will overwrite.
- Confirm which parts are independently addressable. A mustache fused into a beard mesh cannot be independently moved with a whole-beard transform. A nose or ring baked into the body needs explicit local mesh authoring, not a fake accessory offset. Do not detach or resculpt the accepted body merely to expose a control.
- Persist actual local transforms/shape data; a screenshot or camera pose cannot supply the correction. Reopen the saved project and inspect the same source-matched placement.
- Inspect idle, raised-club/bash, crouch/roll if added, head turns, and corpse attachment. Browser rest preview does not certify native-game cloth.
Immediate inspection priorities: low mustache/root placement; beard seating; hair/beard movement first; bands restored in the source atlas; effective native normal-map binding and shader lighting. A normal map adds lighting relief, not new silhouette geometry or physics.
Brian's praise of the seam work is feedback, not blanket acceptance of every angle/material/variant.

NECK / “NEX” SHAMAN AND THE SWAMP FACTION
Preserve the established Neckman identity; “Nex Shaman” is Brian's spoken working term here, not authorization to rename internal IDs.
Start the visual exploration from the original Souls of the Land Neckman, with appropriate Fantu/Sages Vault or cleric robes, a staff/control object, and a lead or magical tether terminating at the Behemoth's nose ring. This does not turn a Marauder into a Neckman.
Fantu's 6 November recorded grant permits use/update/adaptation/distribution of his resources for LWC with credit. Preserve original attribution and provenance; no renewed missing-Sages-Vault blocker.
Three separate visual/mechanical meanings:
- Personal protective sphere around the shaman.
- Protection applied to allied creatures, inspired by the Fuling Shaman.
- A distinct binding aura/tether on the Behemoth, with colored glow/smoke showing active control.
Research the installed native Fuling Shaman's actual effects, status behavior, filtering, attachment and lifetime. Do not assume a differently sized sphere is a second independent spell, or that copying VFX creates a functioning shield or control relation.
Reuse suitable native VFX as visual donors without copying native attack/AI side effects. Binding display follows authoritative shaman/Behemoth relation; define death, range, interruption and multiple-shaman behavior before gameplay implementation.
The lead for a staged scene may be visual-only. A live physical rope/joint can drag or destabilize the creatures and is not implied by the promotional shot.
Existing lore already describes enslaved fallen heroes guarding Neck hoards, and abandoned Behemoths returning to old drowned grounds. Preserve that direction. Death of the captor does not automatically mean the lingering binding vanishes; exact new control-break gameplay remains undecided.

ENCOUNTER, VARIANTS AND MEDIA
Brian wants a standalone drowned/sunken ruin guardian with worthwhile treasure and a dangerous poison aftermath, plus the larger shaman-controlled faction encounter.
The 0.1.21 handoff records an optional native-outdoor-ruin guardian expansion; a new custom crypt/temple, live captor group, return-to-hoard AI and new roll are not delivered by that record. Keep the standalone creature independent of Souls/LWC core; shaman/world integration belongs to the optional content layer.
Preserve delayed poison/corpse warning and existing loot behavior. New strength/location/skin variants are requested exploration; appearance changes do not automatically change damage, progression tier or identity. Keep stable saved appearance and corpse agreement. Finite variant sets can repeat.
Suggested owner-requested media sequence: Behemoth portrait with corrected hair/beard/bands; shaman with staff/tether and binding aura; protective spell comparison; club windup into roll if proven; ruin treasure and poison aftermath; a private dance reveal. Capture from the actual supported preview/build and label staged or proposed content accurately.

HANDOFF REQUESTS
Runtime/animation owner: return existing/partial/gap findings for the donor roll, controller path, charge/binding integration and bounded authoritative collision/damage. Do not silently replace accepted attacks or install a hypothetical.
Workshop owner: identify the existing source-matched recipe/export support for per-part transforms; report separable versus fused mustache/ring/nose and the smallest addition needed. Preserve the current source and saved sessions.
Appearance owner: inspect the reported low mustache, beard roots, native motion/collision, bands and effective normals; retain approved seam/skin/club work.
Neck faction/lore owner: retain the shaman/enslavement/hoard story, record the three distinct effects and prepare the requested scene without claiming new gameplay.
This session changed handoff documentation only. Built: none. Installed: none by this session. Valheim/Workshop controlled: no. Gameplay/visual proof for these new requests: UNKNOWN.

PRIMARY TECHNICAL REFERENCES
Unity Humanoid retargeting:
https://docs.unity.com/en-us/engine/6000.6/manual/animation-section/animation-mecanim/avatar-creationand-setup/retargeting
Unity Animator Override Controller:
https://docs.unity.com/en-us/engine/6000.7/manual/animation-section/animation-mecanim/animator-override-controller
Unity animation events:
https://docs.unity3d.com/2022.3/Documentation/Manual/AnimationEventsOnImportedClips.html
Unity swept capsule query:
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Physics.CapsuleCastAll.html
Jotunn creature component/layer contracts:
https://valheim-modding.github.io/Jotunn/tutorials/creatures.html
Use the actual project's Unity/game versions when building; these references describe mechanisms and do not justify upgrading the toolchain.
