# Engineering Handoff — FORMIDABLE Story Interface
**Purpose:** Reconcile new story behaviors to the **latest** existing builds. Do not implement from this document alone.

## Authority and stop line
This governing chat writes narrative requirements, not code. Engineering chats may already have better, newer solutions. **Before any edit**, inspect current source, packages, test logs, configured prefab names, authoritative services, and accepted behaviors. Report EXISTING / PARTIAL / GAP / DEFERRED for each item. No backport from old diagnostic packages solely because they appear in old research. Do not duplicate core identity, allegiance, faction, work, or economy systems. No gameplay files are modified by this handoff.

## Functional contracts to evaluate, in dependency order
| ID | Request / reason | Ownership / tests | Status |
|---|---|---|---|
| V-01 | Verify cleared Draugr village eligibility from true remaining hostiles / spawn sources | Village integration; no ghost trigger, repeat-safe | RESEARCH |
| V-02 | Hugin→Munin (optional) opt-in prompt with Yes/Not yet | Village scripted presentation uses existing core event mechanism; no forced activation | RESEARCH |
| V-03 | No branch persists activation marker for returning later | Village, stable ID and idempotency; no duplicate markers | RESEARCH |
| V-04 | Yes branch picks safe visual center from village footprint/hall/terrain | Village planner; don't overwrite built structures, support poor layouts | RESEARCH |
| V-05 | Thor/Obliterator-style lightning+smoke+sound without destruction or area damage | Effects/presentation; screen/nearby actor safety, no item deletion | RESEARCH |
| V-06 | Slightly larger maypole, restrained wood/greenery materials | Assets/presentation; material and prefab rights; no vanilla replacement | RESEARCH |
| V-07 | Small groups of common folk arrive from trees with different locomotion animations | Spawn/presentation uses single authoritative roster; no double population | RESEARCH |
| V-08 | Restore original buildings then modular blueprint expansion | Village construction; blueprint terrain/collision/ownership; test existing & fresh seeds | RESEARCH |
| V-09 | One authoritative village economy (wood/meat/food/shelter) independent of worker animation | Core/village contract: animation must not double count; save/load/off-screen bounds | RESEARCH |
| V-10 | Jarl/steward/matron roles and settlement milestones | Reuse existing roles; not a new political simulation | RESEARCH |
| F-01 | Preserve existing hostile/friendly faction IDs, stats, prefabs, AI aggression | Core + FORMIDABLE content; regression compare on current stable package | REQUIRED COMPATIBILITY |
| F-02 | Show renamed characters in lore/UI without breaking internal identifiers | Content/data migration proposal; old values unchanged unless explicitly approved | RESEARCH |
| E-01 | Sparse seed-consistent pillager camps with campfires and stand/sit/watch/chase | World placement / existing AI; no pile of hostile spawns next to stone | RESEARCH |
| E-02 | Original-style runestone encounter text in selected remote areas | Verify runestone prefab, texture, UI rune conversion and RuneStone text first | RESEARCH |
| R-01 | Modest pillager raid events for settlements; no troll / overwhelming village attack | Compare ASharpPen Custom Raids vs existing raid service vs Jere event config | RESEARCH |
| R-02 | Later rare corrupted-legion raid, only if balance/progression approved | No swarm, no wholesale faction redesign | DEFERRED |
| P-01 | Photo mode to capture vanilla structures, camp examples, source prefab material | Lab research only; no screenshot capture claimed | RESEARCH |

## Candidate external tools: evaluate first, never adopt blindly
1. **ASharpPen Custom Raids**: supports added/overridden/disabled raids and supplemental config (`custom_raids.supplemental.*.cfg`); current mod and dependencies require compatibility check with Valheim 1.0 and our installed profile. See https://github.com/ASharpPen/Valheim.CustomRaids and https://thunderstore.io/c/valheim/p/ASharpPen/Custom_Raids/.
2. **ASharpPen Spawn That**: world and local spawner configurations, including village/camp/dungeon spawners; compare against existing integrated Spawn That. https://asharppen.github.io/Valheim.SpawnThat/.
3. **JereKuusela Expand World Data**: seed-location distribution, biome, custom objects, blueprints and location grouping. Locations are generally set at world generation, with separate treatment for explored worlds; this is not itself proof that live village repair is covered. https://github.com/JereKuusela/valheim-expand_world_data and docs locations / blueprints.
4. **JereKuusela Expand World Events**: event/raid configuration; **alternative** to Custom Raids, not automatically a second concurrent event writer. https://github.com/JereKuusela/valheim-expand_world_events.
5. **PlanBuild** or **Infinity Hammer**: reference capture/blueprint format; not automatically required in shipped mod. https://github.com/sirskunkalot/PlanBuild.
6. **Valheim Obliterator**: internal prefab `incinerator`; its lightning also causes damage. Use a standalone safe VFX/audio approach only after investigating the existing implementation. https://valheim.fandom.com/wiki/Obliterator.

## Open balance decisions that MUST be based on real tests
Do not hardcode unapproved wave sizes, frequencies, spawn min/max or class levels. Capture actual current Souls/FORMIDABLE per-creature aggression, stats, hitboxes, faction mappings and biome distributions. Test camp visibility/approach with bows and occlusion. Record impact of raid composition on a village's actual defense and food progress. Require a single authoritative spawn budget for concurrent camps, village residents, raids, and companions.

## Rights & compatibility safeguard
Original Souls of the Land author: Salpati. The original package includes third-party assets and credit to RRRCore, Sage's Vault, MadHatter, RunicPack and Spawn That. Changing lore/display names does not license the original assets or code for redistribution. Track license/permission separately and retain creator attribution. New C.O.R.E. source must be independently authored; do not simply strip signatures from proprietary binaries. Preserve original internal prefab/faction IDs when possible to avoid broken saves and configs.

## Required return report from EACH engineering chat
- Latest tested Valheim version; build/hash and modpack/profile involved.
- What is already implemented/accepted; test evidence; what has changed since the last handoff.
- For each V/F/E/R/P ID: EXISTING / PARTIAL / GAP / DEFERRED / CONFLICT, plus impacted files/contract and a minimum acceptance test.
- Recommendation: reuse current service vs evaluate external mod vs new authorized feature, with risks.
- No file edits without the project's explicit implementation-stage authorization; report only findings.