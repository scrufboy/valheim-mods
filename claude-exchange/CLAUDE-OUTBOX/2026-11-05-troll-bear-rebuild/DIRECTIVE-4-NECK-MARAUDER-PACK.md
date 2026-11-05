# DIRECTIVE 4 — Release graphics + the Neck Marauder Pack. 2026-11-05 07:40. TODAY.

Supersedes Directive 3 item 1 (icons). Everything else in D3 stands.
Brian: "Please work together to make this happen TODAY!!!!" "all of this is easy now… The colossus proves it."

## A. RELEASE GRAPHICS — one consistent set

| Package | Verdict (Brian) | Do |
|---|---|---|
| Bjorn Reborn | "fine" | Keep. It is the style reference. |
| Þurs | "gross" | Replace. You already produced a painted Troll piece in the same pose/look as the set — it only lacked the LWC logo. Find it (or remake it identically), add the LWC hex badge bottom-right, title "ÞURS" in the set's carved Norse lettering. Not a game render. |
| Undead Colossus | needs swamp | Same art, swamp background. Show the darker decayed skin. |
| Neck Marauder | needs swamp | Same art, swamp background. Must match the shipped variant(s) (see C). |

Same composition rules for all: painted creature, misty Nordic scene, title top, LWC hex badge bottom-right, 256×256 icon + a larger banner per mod. Show Brian one sheet before packaging. Claude verified: no painted Troll art exists in Exports/External Assets on Brian's PC; the installed Þurs icon is a game-render face crop (`NATIVE_SVG_COMPOSITION_EXISTING_LWC_BADGE`).

## B. WHOLE-CREATURE RULE (what makes LWC distinct)

Brian: "we mod the entire creature, the corpse, the trophy head and the loot skins.. drops.. etc. this way it all matches."
Every creature ships with matching: body, corpse, trophy head, hide/loot items, drops, **and hit/death VFX**.
- **Þurs: the blue hit/death shreds are still vanilla Troll blue and do not match.** Retint the Troll hit/death/blood-splinter effects to Þurs' colorway.
- Apply the same check to Bjorn, Colossus, Marauder: any vanilla-colored hit/death effect that clashes gets retinted.

## C. THE NECK MARAUDER PACK

One themed release: the two Marauder variants, the little Necks' behavior, the Neck Shaman, new swamp locations, and the Zombie Colossus (roaming zombie faction edition).

### C1. Sunken Marauder — TWO variants (Brian wants both, for diversity)
- **Miner variant (current build is the base):** fake bindle/backpack with a fake pickaxe prop on the back. Fights with **two pickaxes in hand used as early axes** (human early-axe attacks). Uses the human stone-picking animation for mock work; **stows the picks to walk and to work** per Brian.
- **Fuling-arms variant:** all of the model's own weapons removed. Fake pickaxe prop on the back only. Fights with **Fuling weapons — spears, clubs, swords, shields** — like any Fuling.
- Both: underground/sunken spawn near crypts; **always mock-pickaxing stone until aggroed** by zombies, the player or companions; rise from the ground (skeleton emergence, already built).

### C2. Little Necks
- Hunt **Leeches**; found in the **upper swamp** doing so.
- **Only aggro on zombies when the zombies attack them.**

### C3. Neck Marauders
- **Attack only Leeches unless provoked.**
- **Respond to the Neck Shaman** (shaman leads them, as the Fuling shaman does for Fulings).

### C4. Neck Shaman (new)
- A Neckman in **crimson robes** — use the crimson/red robe items the game already has.
- Takes the **Fuling shaman role** for Necks and Neck Marauders.
- **Can summon a Zombie Colossus to fight for them** — against anything and everything; it kills all.
- **Can take over a roaming Colossus and send it berserk against anyone.**

### C5. Zombie Colossus — roaming zombie-faction edition
- Wanders the **decrepit stone castle ruins our mod places in the swamp** (new location prefabs = Colossus spawn points) and **wherever crypts are**.
- **Responds to sounds like the Troll does and comes from far off** — especially when a Neck Shaman is near.
- Keeps: approved rig/AI/motion, darker skin, Heavy Zombie voices, biggest poison death cloud, zombie raid entry.

### C6. Factions (Brian)
- **Humans do not belong in the swamp.** They outcast the swamp folk, cut them off, and left them to the swamp and all its dangers.
- Necks/Marauders vs zombies: defensive only (C2, C3). Leeches: prey.

### C7. Lore / creation / immersion story
Write it from `LORE-DRAFT.md` (this folder) — Claude drafted it strictly from Brian's statements. Put a short version in each README and the full version on the pack page. Brian approves the wording.

### C8. FACTION MAP — our Souls pack's Neck faction (Brian, 07:35–07:37)
Brian: "we make both neck marauders... one surface type... with the spears... that is common, and one type, with picks. This technically gives us the ability to adopt the entire fuling system into the neck marauder type. we build their faction of our souls pack here." "perhaps surface prefabs.. for now sunken ones in the crypt.. should be there working and buzzing about.. fake working until aggro"

| Fuling system | Neck faction |
|---|---|
| Fuling (grunt) | **Surface Marauder — spears, COMMON.** Fuling weapons (spear default; club/sword/shield allowed). Spawns from **surface prefabs** in the swamp. |
| (new) | **Sunken Marauder — picks.** Lives **inside crypts**, working and buzzing about, **fake working until aggro**. |
| Fuling camp/village | Marauder camps at our swamp castle ruins (surface). |
| Berserker/Brute | Zombie Colossus (already on the Berserker setup). |
| Shaman | Neck Shaman (crimson robes) — leads, summons/takes over Colossus. |
| Raids | Neck raids from the swamp. |

C1 variant names follow this: "Fuling-arms variant" = Surface Marauder (spears, common); "Miner variant" = Sunken Marauder (picks, crypt interiors).

Known risk: the Neckman is not on the Fuling rig, so Fuling shaman animations cannot play on it. Shaman spells must use Neck animations or a Marauder-style body. Try once; if blocked 20 min, tell Brian.
Not ours: Souls of the Land's own Neck/Neckman/Shaman prefabs. Reusing them needs Souls as a dependency or the author's permission — list it in the licence table. Marauder and Colossus are built from Brian's purchased models.
Home in repo: enemy species belong to Living World content (`LivingWorldEnemyFamilies`) per REPOSITORY-LAYOUT; Brian confirms placement.

## D. ORDER (today)
1. Graphics sheet (A) → Brian.
2. Þurs VFX retint (B).
3. Marauder Fuling-arms variant + miner stow/work (C1).
4. Neck Shaman + Colossus summon/takeover (C4/C5) and Neck/Marauder aggro rules (C2/C3).
5. Swamp ruin location prefabs + Colossus spawns (C5).
6. Package the pack + READMEs + licence table (D3) → one yes/no to install privately.
Split across agents where possible: graphics ∥ VFX ∥ Marauder variant ∥ shaman/AI ∥ locations. Assign first, then build.

## OPEN — ask Brian, do not guess
- Is the Miner variant's in-hand weapon the same two pickaxes it mines with (stowed to walk/work, drawn to fight)? Claude read it that way.
- Shaman summon: how often / cooldown / one Colossus at a time?
- Do Leeches stay vanilla, or get a matching visual pass?

RULES: unchanged (no test frameworks, real-game screenshots only, 20-minute stuck rule, Git state on every stop, report to CHATGPT-OUTBOX).
