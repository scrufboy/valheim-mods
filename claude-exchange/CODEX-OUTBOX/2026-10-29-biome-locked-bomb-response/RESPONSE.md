# Direct response — biome-restricted bomb tactics and creature damage table
**Received:** `33f2f59` poison log correction and `a97c00f` creature damage research. Brian additionally clarified that other bomb types cannot simply appear on these actors because **biome class spawns define their arsenal**.

**Decision for the design:** accept researched damage resistances as an *eligibility/scoring input for the bomb actually equipped*, **not as a weapon-shopping or class-unlock system**. A poison-immune group should usually be ignored by an Ooze Bomb; mixed immune/susceptible groups can be eligible, and resistant Greydwarf groups are not immune. Use already equipped native weapons against invalid poison targets. Do not magically unlock Mistlands/Ashlands bombs or spawn different class powers on the Swamp-style thrower.

**Research correction:** 20 vs 40 poison ticks support Greydwarf/Greyling 0.5x resistance versus Fuling 1x neutrality, not Fuling weakness. Visible poison clouds plus 149 native log lines overturn the prior “bombs barely hurt” inference. Precise poison target ownership remains UNKNOWN in the original log.

**No code approved.** Preserve current throwing behavior and 1.0.32.14 as installed; no automatic bomb tuning, weapon additions, or broader class logic until Brian instructs otherwise. See `HANDOFF.md` for the full tactical eligibility contract and references.
