# CLAUDE — FORMIDABLE Research Assignment
**Suggested role:** independent research analyst; no coding or patch deployment without explicit stage approval.

## Start here
Read `00_READ_ME_FIRST.md`, `01_FORMIDABLE_LORE_BIBLE_v0.2.md`, `02_ENGINEERING_HANDOFF_v0.2.md`, `04_SOURCE_REGISTER_v0.2.md`, and `05_SCREENSHOT_SHOTLIST_v0.2.md` in the same packet. This folder is a research handoff from Living World HQ, NOT a statement that the rest of the project stopped or that you have current code. Ask for the latest engineering status or inspect actual current project materials before proposing any changes. We must preserve what already works.

## Mission
Establish a referenced, reproducible research base for **FORMIDABLE — Odin's Friends & Foes** and its village reclamation story so new narrative beats look and behave like they belong in Valheim 1.0. Build careful compatibility intelligence, original Norse-inspired story development, screenshot plans, and minimal implementation requirements without introducing code duplication.

## Research lanes — work independently then consolidate
**1. Canon versus invention.** Validate through authoritative current game sources, game data, officially available texts and reliable analysis: Odin and the tenth world; Forsaken; runestones; Hugin and Munin; Draugr villages; maypoles; Obliterator effect; Dvergr; biome contexts. Mark CONFIRMED IN-GAME, AUTHOR-ADDED, EXTRAPOLATION, or UNVERIFIED. Do not claim lore that isn't actually in the game. Derive Hugin=thought and Munin=memory as thematic interpretation, not mechanically enforced future/past roles.

**2. Nomenclature recovery.** Search accessible prior Valheim engineering chats/notes for the **specific E-initial Norse-inspired replacement name for Krauguk Legionnaires**. Return verbatim quoted proposal with date/source, not a fabricated reconstruction. 'Myrkherjar' is rejected as current replacement; don't assume 'Einherjar' equals hostile knights. If unavailable, leave unresolved and provide only optional *clearly novel* candidates backed by language research for later approval. Research grammatical and historical accuracy of all names.

**3. Existing Souls enemy roster and rights.** From Salpati's original release and the **latest actual** content profile, map external display name, stable internal prefab ID, original faction, biomes, combat aggression, equipment, special attacks, attack values, dependencies and lore. Identify Forest Children, Neckman, Common Folk Pillagers, Krauguk legionnaires/OrcBrute, Dark Servants, plus any revised pack-specific additional NPCs. Show known vs unknown. Don't alter enemy behavior to fit rewritten story; original story can be replaced in independently authorized content. Establish which assets require additional rights.

**4. Maypole cinematic and activation.** Research safe standalone VFX/audio from the vanilla Obliterator's lightning; prefab hierarchy and visual material maps; maypole scale/wood/greenery; Hugin/Munin dialogue interaction points; placement anchoring to true generated village footprint; refusal reactivation-stone interaction; idempotency and save-load. No item destruction, accidental AOE, replacement of player buildings or guessing public prefab field names. Separate viable, risky and unknown.

**5. Village reconstruction and economy.** Audit surviving Draugr village prefabs, layout bounds, Great Hall, palisades, farmland, smokehouse, brewery, farm/animal pens. Research PlanBuild, Infinity Hammer and JereKuusela's blueprint formats for extracting modular, terrain-appropriate building patches. Explicitly distinguish initial world generation from restoring an already explored/generated settlement. Research a lightweight performance model: authoritative village resources with fake visual work, offscreen bounded progress, no double counting, named role leadership and citizens.

**6. Camps, runestones and raid events.** Research campfires/clearings/camp animation, independent seed-consistent small camp placement and guard detection/targeting; ASharpPen Spawn That and Custom Raids; JereKuusela Expand World Data/Events as possible alternatives. Villages face **modest pillager raids**, not overwhelming troll waves. Keep forest-based armored legion encounters. Inspect runestone mesh/material/text/presentation; do not assume carved visual runes are unique or automatically generated from readable text. Find viable mod/data methods for new inscription text with accurate translations, effects and seed placement.

**7. Names, lore database and NPC stories.** Study offline Norse-style name catalogs with verifiable source/licensing, phonological generation and use of bynames, kin-name patterns, profession/residence epithets. Propose a **local, source-credited, testable names/lore dataset**, not live runtime calls to web APIs. Identity remains core-authored and persistent; ambient performers can be unnamed or lower fidelity. Village journal/story snippets come from deterministic milestone templates and stored facts, not unlimited LLM calls while playing. Avoid AI inventing NPC history inconsistent with saves. Do not design a vast class/ability system without approval.

**8. Screenshots / visual research.** Provide repeatable photo-mode and prefab-inspection procedure appropriate to current Valheim 1.0, including capture file names, map coordinates or world seed (if available), lighting, scale markers, attribution and licenses. Request screenshots from the lab; don't claim they exist.

## Required research deliverables (text/markdown preferred)
A. SOURCE MATRIX: claim, canonical status, public URL/file, publication/check date, limits, confidence.
B. NOMENCLATURE DOSSIER: recovered E-name if found, etymology, pronunciation, and alternatives only if needed.
C. ASSET/PREFAB MAP: model/texture/FX/runestone/totem/camp/biome applicability, licensing.
D. RECLAMATION EVENT SPEC: states and transitions including Yes/No, activation stone, lightning safe center, arrival groups, abort/reload, first repaired hearth.
E. RAID/CAMP DESIGN NOTE: comparative mod assessment (Custom Raids vs Expand World Events, not blindly both), density and fairness test ideas, source links.
F. LOCAL NAMES & HEARTH LORE DATA PROPOSAL: schemas only if aligned to latest core contracts; no parallel implementation.
G. OPEN QUESTIONS + PRIORITY: first 10 evidence-producing investigations for the lab, each with success criteria.
H. DIFF AGAINST CURRENT PROGRESS: completed / partial / absent / proposed — based only on verified latest engineering sources.

## Guardrails
- No implementation without permission. No claim that messages have been delivered to other chats or Claude automatically.
- No decompiled proprietary code copied into the independent core; credits retained and asset rights checked.
- Use first-party Valheim lore to establish compatibility but write **new** dialogue and lore, not verbatim game passages.
- Reuse existing authoritatives. No shadow faction tables, second AI engine, duplicate event writers, speculative endpoints, or shell scripts when they add no value.
- Do not 'fix' forest legionnaires by moving them out of the forest. Do not swap their stats, aggression or internal faction data to fit lore.
- Return source evidence and a concise summary the governing HQ can review before workstreams act.

**Paste-ready instruction to Claude:** 'Read the complete FORMIDABLE handoff packet as the current lore design (not current engineering status). Research and cite all eight lanes above, especially recovery of the E-initial legion name and safe vanilla-style reclamation effects. Report sources and verified gaps. Do not code, rename internal assets, add dependencies, or change working modules. Give HQ a prioritized evidence-led research report.'