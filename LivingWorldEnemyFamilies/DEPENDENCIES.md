# Dependency and ownership notes

Required transitional lab runtime:

- BepInExPack Valheim 5.4.x
- RRRCore 3.1.5 repaired/diagnostic build already used by Souls of the Land
- Spawn That 1.2.20 (only for the disabled-by-default regional spawn entries)
- Jotunn 2.30.2 as required by the installed content stack
- RunicPack 1.1.6 for `RunicAxeBlue` and `RunicAxeRed`
- MadHatter 2.2.4 for `GrimAgeHornedHelm` and `Dragon_Headdress`

The remaining armor, capes, effects and projectiles are runtime Valheim prefabs.
No third-party binaries or asset bundles are included here. The JSON definitions,
spawn configuration, validation, packaging, documentation and IDs are Living World
authored. Clothing and projectile prefab names are dependency references only.

The installed original child definitions `WoodsSoul` / `RRRN_WoodsSoul` and
`NinoBosque` / `RRRN_NinoBosque` remain owned by the Souls pack and are never
copied into this package. They are attack/runtime references only. Rimebound and
Emberworn are separate adult Living World identities.
