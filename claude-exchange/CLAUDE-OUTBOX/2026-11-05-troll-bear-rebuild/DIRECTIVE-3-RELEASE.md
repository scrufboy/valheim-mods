# DIRECTIVE 3 — Public-release packages. 2026-11-05 07:05. Execute.

Brian: "i was also intrigued with the nordic naming but he regressed the shit out of the 256x256 image, taking it completely out of our themed images. so a complete package is wanted... something we can publicly release without getting laughed at!!!"

Claude checked the four candidate ZIPs:
- Troll, Bear, Marauder and Colossus builds follow the orders. Good.
- **Þurs icon.png = raw dark face crop. No painted scene, no title, no theme. Rejected.**
- **Marauder icon shows the old axes; the build now uses pickaxes. Out of date.**
- **Colossus icon shows the old pale skin; the build is darker. Out of date.**
- Manifests say "Private test candidate", website_url empty, names carry "_Prototype".

## DO

1. **Icons, 256×256, one matching set.** The house style is the Bjorn Reborn / Neck Marauder / Undead Colossus icons: painted creature, misty Nordic forest/swamp, carved Norse title lettering top, LWC hex badge bottom right. Make all four match that style exactly and show the **current** creature:
   - Þurs: the original Cave Troll as it looks now, title "ÞURS".
   - Marauder: pickaxe in each hand, pickaxe on the back.
   - Colossus: the darker, decayed skin.
   - Bjorn: keep unless it no longer matches the model.
   Show Brian the four as one sheet before packaging.
2. **Nordic naming.** Brian liked Þurs. Propose Nordic display names for the other three (one line each, with meaning). Brian picks. Do NOT rename package IDs or prefabs until he picks.
3. **Manifest:** public description (one sentence, what it is), `website_url` = the public repo or Thunderstore page, correct dependencies, version bump. No "private", "candidate", "prototype" wording in public text.
4. **README per mod:** what it is; spawn/biome/raid; features; config; spawn command; compatibility; known gaps stated plainly (Bjorn: no death/stagger/swim/sleep clips); 2–3 screenshots taken in the real game from this exact build; credits.
5. **Licences — blocking for PUBLIC only.** For each purchased asset (Cave Troll, Realistic Grizzly Bear, Colossus model, Frog Marauder model, Heavy Zombie audio, any textures), write the licence name, where it came from, and whether it allows redistribution inside a free game mod AssetBundle. Quote the clause. If unclear, say UNKNOWN and Brian decides. Do not publish anything public. Packaging and private install can proceed.
6. **Deliver:** one release folder with the four final ZIPs, icon sheet, READMEs, licence table. Ask Brian one line: install privately yes/no; public release is his separate decision after the licence table.

## RULES
Same as before: no test frameworks; real-game screenshots only; stuck 20 min → tell Brian; Git state on every stop; report to `valheim-mods/claude-exchange/CHATGPT-OUTBOX/`.
