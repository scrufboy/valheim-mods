# FORMIDABLE — Odin's Friends & Foes

**The Hearths of the Forgotten · Lore and research baseline v0.2 · 17 October 2026**

This directory holds the governing **narrative research packet** for FORMIDABLE, the friend-and-foe NPC content direction within Living World C.O.R.E. These documents are *design and research inputs*, not a new code baseline, implementation permission, or instructions to replace accepted working systems. The project's current engineering source and tests take precedence when describing what is already implemented.

## Read in this order

1. [Start here / project decisions](./00_READ_ME_FIRST.md)
2. [Lore bible and Reclamation Day](./01_FORMIDABLE_LORE_BIBLE_v0.2.md)
3. [Engineering gap-check handoff](./02_ENGINEERING_HANDOFF_v0.2.md)
4. [Claude independent research assignment](./03_CLAUDE_RESEARCH_BRIEF_v0.2.md)
5. [Source register and provenance](./04_SOURCE_REGISTER_v0.2.md)
6. [Photo-mode capture shot list](./05_SCREENSHOT_SHOTLIST_v0.2.md)
7. [Revision ledger](./06_REVISION_LEDGER.md)

A formatted Word edition and the original dated ZIP packet are retained separately in the ChatGPT Library under **FORMIDABLE Project Handoffs**. Markdown is the editable canonical story source in this repository.

## Nonnegotiable project boundaries

- Preserve existing prefab IDs, hostility/faction mappings, aggression and combat values until an engineering workstream explicitly approves a migration.
- Keep the corrupted legion knights in their established forest encounters; their **E-initial replacement name remains unresolved**. Do not silently substitute `Myrkherjar`.
- Player **never manually places** the reclaim maypole: a validated village clear leads to Hugin/Munin opt-in, then a safe automatic central placement with theatrical lightning. A declined offer leaves a persistent reactivation marker.
- Pillager camps should be sparse, visible by campfire, and populated by characters whose behavior responds naturally to the player. Village raids are deliberately modest and should not be troll waves.
- Use one authoritative existing NPC/identity/affiliation/settlement foundation. No second AI engine or duplicated name/faction/economy writer.
- Original third-party source and assets are not relicensed by changing display names or writing new lore.

## Current implementation ownership

Existing repository projects include [LivingWorldCORE](../../LivingWorldCORE/), [LivingWorldHumanNPC](../../LivingWorldHumanNPC/), [LivingVillageFoundation](../../LivingVillageFoundation/), [LivingWorldCompanionSystems](../../LivingWorldCompanionSystems/), [VillageReclaim](../../VillageReclaim/) and [PopVillageWorkers](../../PopVillageWorkers/). They already contain implementations and acceptance work that must be inspected before any new change.

**Handoff:** ask Claude or another research agent to read these Markdown files and return a sourced `existing / partial / gap / deferred` comparison. Do not authorize automatic coding or patch distribution.
