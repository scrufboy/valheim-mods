# Creature damage-type research (Claude research agent, 2026-10-29)

Brian asked for this: which creatures are hurt by which effects, prompted by skeletons showing no poison. Source:
the Valheim wiki (weirdgloop), cross-checked against fandom and physgun (a game-file database, 1.0.15 data). This is
**RESEARCH / PUBLIC REFERENCE**: wiki data, not game files inspected by us. Prefab damage modifiers live in asset
bundles, not in decompiled code.

## Answers
- **Undead are immune to poison: confirmed.** This covers Skeleton, Rancid Remains, Draugr and Draugr Elite. Fandom
  says Draugr is poison-"resistant", but weirdgloop and physgun both say immune; fandom is likely outdated.
- **Blob and Oozer are immune: confirmed.**
- **Also poison-immune:** Wraith, Abomination, Stone Golem, Growth, Ghost, Root, Surtling, Bat, all Charred, Lava
  Blob, Skugg, and the bosses Elder, Bonemass and Yagluth.
- **The 20 vs 40 in our log:** the Ooze bomb does **40 Poison**. Greylings and all Greydwarfs *resist* poison (×0.5),
  so they took 20. Fulings and Berserkers are *neutral*, so they took 40. This **corrects Claude's 2026-10-29 inference
  that Fulings are weak.**
- **Weak to poison (×1.5):** only Ulv and Cultist. No creature is very weak to it.
- **Wet:** ×1.5 Frost and Lightning taken, and fire-resistant, for players and creatures. Drake spit is 90 Frost per
  ball. Our measured ~1.8× on wet companions fits ×1.5 applied before armor's non-linear reduction (hypothesis, not
  confirmed).
- **Bile bomb:** 4 m cloud for 10 s, 15 Fire over 5 s that stacks while in the cloud, plus 30 Poison over 6 s.
  Against poison-immune undead only its fire works, and Draugr resist fire.
- **Fire:** Greydwarf types and Greylings take ×2. Skeletons take ×1.5. Draugr resist. Drakes take ×1.5.
- **Blunt:** best against Skeletons, slimes and Growth.

## Creature table (W = ×1.5, VW = ×2, R = ×0.5, VR = ×0.25, Imm = immune, N = neutral)
| Creature (prefab) | Blunt | Slash | Pierce | Fire | Frost | Lightning | Poison | Spirit |
|---|---|---|---|---|---|---|---|---|
| Greydwarf / Brute / Shaman / Greyling | N | N | N | VW | N | N | R | Imm |
| Troll | R | N | W | N | N | N | N | Imm |
| Skeleton / Rancid Remains | W | N | R | W | R | N | Imm | N |
| Draugr / Draugr Elite | N | N | N | R | N | N | Imm | N |
| Blob / Oozer | W | R | R | R | W | W | Imm | N |
| Wraith | R | R | R | W | Imm | N | Imm | W |
| Leech | N | N | N | Imm | N | UNKNOWN | R | Imm |
| Abomination | R | N | VR | W | Imm | N | Imm | Imm |
| Drake (Hatchling) | N | N | N | W | Imm | N | N | Imm |
| Wolf | N | N | N | N | N | N | N | Imm |
| Fenring | N | N | N | W | N | N | R | N |
| Stone Golem | N | R | R | Imm | Imm | N | Imm | Imm |
| Ulv | N | N | N | VR | N | N | W | N |
| Cultist | N | N | N | Imm | N | N | W | N |
| Fuling / Berserker / Shaman | N | N | N | N | N | N | N | Imm |
| Deathsquito | N | N | N | N | N | N | N | Imm |
| Lox | R | R | N | W | R | N | N | Imm |
| Growth | W | R | R | W | R | N | Imm | N |
| Seeker / Seeker Soldier (body) | R | R | R | N | N | N | N | Imm |
| Gjall (body) | N | N | N | R | N | N | N | Imm |
| Tick | N | N | W | N | N | N | N | Imm |
| Dvergr Rogue | N | N | N | N | N | N | N | Imm |
| Dvergr Mage | N | N | N | R | R | N | N | Imm |
| Charred Warrior | N | N | R | VR | N | N | Imm | W |
| Morgen | R | R | R | R or VR (sources conflict) | N | W | N | N |
| Asksvin | N | N | N | R / VR (conflict) | N | N | R / VR (conflict) | Imm |

## Use for the aim/purpose model (proposal; Brian decides)
The bomb choice should check the target family's modifier.
- Never throw a poison bomb at a poison-immune family. Last night some throws at skeletons, draugr and a troll raid
  were spent on immune targets; the troll is neutral, so poison does hurt it.
- Fire suits Greydwarf crowds.

## Sources
weirdgloop: Damage, Creatures, Poison, Bombs, Ooze_Bomb, Bile_Bomb, Smoke_Bomb, Resistance, Cold_Effect,
Freezing_Effect, plus the creature infoboxes. Cross-checks: fandom Creatures and Wet pages; physgun Draugr.
