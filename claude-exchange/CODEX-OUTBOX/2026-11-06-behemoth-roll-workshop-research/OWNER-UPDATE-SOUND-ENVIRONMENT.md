# Active implementation update — Behemoth surprise roll, sound and environment
Recorded 6 November 2026 (America/New_York)

This update supersedes the earlier research-only status and any conflicting proposal in HANDOFF.md. Brian reports explicit implementation authorization in the Workbench execution chat. Existing root owns implementation. This documentation session must not start competing source edits, builds or installs.

## Owner-supplied current progress
- Show Player roll on the current Behemoth first; Brian reports he is watching the animation on screen and calls it fantastic.
- Exact source: Player_animator, Base Layer.Dodge, clip Dive, pathID -8031980558117531977; duration 1.5 seconds; native state speed 1.7.
- Workshop 0.5.25 plays the clip on the actual 0.1.28 Behemoth with 47 matched tracks, as reported by the execution-chat update.
- No game launched. This is owner/execution-chat-reported Workshop preview evidence, not independently inspected Valheim gameplay proof.
- All three existing native Brute attacks are retained.
- Proposed additive sequence uses a creature-local temporary PlayableGraph. This is the implementation owner's proposal, not a completed or gameplay-proven controller change.
- Existing acquired zombie audio pack has an explicit recovery role using an exact owner-local one-shot; candidate suitability still awaits Brian's audition. Do not invent a filename or substitute a different pack.
- Faction correction was sent to the existing encounter-allegiance chat. Reported installed findings: Pillager forced Undead and Krauguk Plains. Preserve that owner's task; do not create a parallel faction fix.

## Authorized attack behavior and surprise timing
Implement additively: rage/adrenaline charge, cooldown, near-bash damage, building/tree collision, knockback/stagger, brief recovery and fast reacquisition. The current near-bash damage direction supersedes speculative inflated damage from the earlier brainstorm; structure/tree response is deliberate, not a license to bypass native damage rules.
It is a serious, frightening, occasional special. The familiar club-raise/bash preparation should sometimes become a fast forward tuck/roll, surprising the player who expects a swing. NO big pre-attack roar and no long announcement that gives the surprise away. Rage/adrenaline describes its internal readiness, not a mandatory prolonged roaring tell.
Preserve bounded duration/distance and real collision handling. Brian's comment about rolling until it hits something was immediately qualified; it is not an instruction for endless rolling. Keep a short, readable recovery and prompt target reacquisition.

## Required three-part sound sequence
Brian: “It has to have a special exertion sound when it does it ... an impact sound when it hits ... an exhaustion sound or a relief sound from the zombie pack when it's done ... and it recovers.”

1. LAUNCH / EXERTION: a distinct heavy exertion grunt/growl at the actual roll commitment, synchronized to the tuck/launch. It must not become a big advance roar that spoils the surprise.
2. REAL CONTACT / IMPACT: a powerful impact/crash/thud at an actual damaging collision, positioned at the collision. Use suitable native game timber/stone/body responses where available. No phantom impact when he rolls through empty space; avoid repeatedly firing a full crash on every physics frame or every collider of the same recipient.
3. END / RECOVERY: one appropriate exhausted breath, spent groan or relief sound from the existing acquired zombie pack as rolling ends and the brief recovery begins. Use the execution owner's exact local recovery one-shot after audition; do not make the whole clip length a mandatory long recovery or delay fast reacquisition.

Layer low rumbling/thundering native movement sound under ground traversal to sell the giant's mass, keeping launch, collision and recovery recognizable. Stop movement layers on roll completion, interruption, death or despawn; prevent duplicate peer playback. The concrete audio implementation stays with root and the existing sound ownership/arbitration path. This is a required presentation specification; no particular uninspected sound asset is claimed suitable.

## Required environmental response
- Grounded travel raises contact debris and smoke/dust appropriate to the surface as it happens. Effects follow the actual ground-contact path rather than a static cloud at the starting position.
- Use the game's appropriate existing effects where available. Snow should produce snow response and tracks/marks along the route; dirt, stone, wood and shallow water should use suitable contact responses rather than universal smoke.
- Snow tracks are an explicit owner requirement, not a claim that the current effect path already creates them. Root must inspect the native snow/ground-mark mechanism and report any unsupported surface/renderer behavior rather than fake a successful response.
- Trees must be real collision/damage recipients and eligible trees must be capable of being knocked down, with native break/fall response. Include both standing trees and resulting fallen logs in path obstruction handling; not a cosmetic tree animation or direct object deletion.
- Buildings likewise receive real, bounded native damage/impact and debris response; surviving structures obstruct travel. Preserve existing bash and other attacks.
- Keep character knockback/stagger, world collision, audio and visual effects aligned with the same authoritative roll/contact phase. Presentation does not independently decide damage, destruction or movement.
- Surface effects, sounds and tracks do not establish terrain deformation unless the native mechanism actually supports it. Do not add unapproved terrain writers.

## Handoff and evidence status
ACTIVE IMPLEMENTATION: existing Workbench root owns the additive attack and sound/environment integration.
OWNER/EXECUTION-CHAT REPORTED: actual Workshop preview, exact clip metadata, current package/app versions, attack preservation, faction findings.
UNKNOWN / NOT CLAIMED: in-game roll movement/damage, tree/building destruction, snow marks, actual audio audition, network ownership and full environmental response.
This pass updates shared documentation only. No competing source edits, game launch, build, install, desktop control or new gameplay-proof claim.

Documentation changes in this update: adds this owner-update file, replaces REQUEST.md with current authorization/status, and prepends a superseding notice to HANDOFF.md. Runtime code and assets unchanged by this session.
