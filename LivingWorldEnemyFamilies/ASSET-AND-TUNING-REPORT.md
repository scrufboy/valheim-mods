# Asset and tuning report

This approval pack defines two independent adult humanlike peoples on `RRR_NPC` at scale `1,1,1`. It contains configuration only and redistributes no third-party assets.

## Rimebound (`RRRN_LWC_Rimebound`)

- Faction `MountainMonsters`; health 220; frost resistant; fire weak.
- Gear: vanilla `ArmorWolfChest`, `ArmorWolfLegs`, `CapeWolf`; MadHatter `GrimAgeHornedHelm`; RunicPack `RunicAxeBlue`.
- Normal override: 30 slash, 10 pierce, 8 chop, 6 frost.
- Special: `troll_throw`, `mace_secondary`, `dragon_ice_projectile`, 14-second interval, 6 blunt plus 28 frost.
- Drops: WolfPelt 1–2 at 65%; FreezeGland 1 at 25%; both level-scaled.
- Pale tint and blue-white RRR body smoke/flames.
- Disabled spawn proposal: Mountain and DeepNorth, night, altitude 50+.

## Emberworn (`RRRN_LWC_Emberworn`)

- Faction `Demon`; health 230; fire resistant; frost weak.
- Gear: vanilla `ArmorFenringChest`, `ArmorFenringLegs`, `CapeLinen`; MadHatter `Dragon_Headdress`; RunicPack `RunicAxeRed`.
- Normal override: 32 slash, 8 pierce, 8 chop, 8 fire.
- Special: `imp_fireball_attack`, `mace_secondary`, `Imp_fireball_projectile`, 14-second interval, 6 blunt plus 28 fire.
- Drops: CharredBone 1–2 at 55%; Coal 1–3 at 70%; both level-scaled.
- Ember tint and orange-red RRR body smoke/flames.
- Disabled spawn proposal: AshLands.

Both are hostile non-boss NPCs, cannot be tamed or commanded, and have one bounded special each. Natural spawning stays off until manual approval. Original Souls definitions are technical references only and are neither changed nor included.

Validation checks provider-prefab strings in installed MadHatter and RunicPack assemblies. Vanilla names are already consumed by the installed Valheim/RRR/Souls stack. Final live approval should review `BepInEx/LogOutput.log` after both manual spawn tests.

## Configurable lore-name overlay

| Internal prefab | Existing display name | Default Living World display name | Registration verification |
|---|---|---|---|
| `RRRN_Neckman` | Swamp Neckman | Fen Draugr | Runtime overlay logs `verified=True` after lookup |
| `RRRN_Servant` | Corrupted Servant | Myrkvidr Thrall | Runtime overlay logs `verified=True` after lookup |
| `RRRN_OrcBrute` | Krauguk Legions | Eitrherjar Legionary (retained canonical name) | Runtime overlay logs `verified=True` after lookup |

`RRRN_NinoBosque`, `RRRN_WoodsSoul`, and `RRRN_Pillager` are deliberately unchanged.
The overlay touches only `Character.m_name` on the registered prefab. IDs, factions,
biomes, stats, equipment, attacks, and spawning remain owned by their source definitions.
The default ASCII `Myrkvidr` spelling is safe for the generated BepInEx config and can
be changed by Brian to `Myrkviðr Thrall` after confirming local config encoding. `Thrall`
preserves the existing Corrupted Servant's identity; the earlier word `Wanderer` was only
an informal description used while recalling which existing NPC was meant.
