# Living World C.O.R.E. Tooler — Brian / scrufboy

Maintained and packaged for Living World C.O.R.E. by **Brian / scrufboy**.

Version 2.6.5 puts the two current Elder-pattern FormidableAI stress raids directly on the **Raids** page: **Formidable Arena** and **Formidable Souls Swamp**, with the standard noon setup and stop-event controls beside them. All eight hostile slots—including NinoBosque and both OrcBrute heavy slots—are enabled and use Valheim's native `HuntPlayer=true` pursuit so the full six-actor roster converges from the 70–95 m ring. The earlier NinoBosque/OrcBrute quarantine was based on an unproven timing correlation and is removed; the separate lifecycle observer records evidence if the removal error recurs. This does not create a companion defend order or leader status, and no hostile stats, group sizes, caps or combat rules changed. Every vanilla raid control remains available, and Version 2.5.0's separate **Site Survey** module remains intact.

This is the Living World C.O.R.E. Tooler: one Living World plugin identity,
Living World branding, the familiar proven interface/colors/control layout,
the complete player, catalog, entity, terrain, ESP, and utility feature set,
plus a dedicated **Living World** toolbar section containing the complete custom
trainer: lifecycle, community, combat, party, raids, player shortcuts,
item/prefab spawning, time of day, environment/weather, terrain, map/admin,
confirmed destructive actions, and an arbitrary command bridge. Invalid
unfinished ObjectDB item entries are skipped without flooding `LogOutput.log`.

The Raids page launches the installed `lwc_formidable_arena` and
`lwc_formidable_souls_swamp` long-range training events. Its package includes
the matching Custom Raids definitions. The Tooler issues the normal Valheim
event commands and does not create a separate combat, faction, or
defended-player state.

The World page includes Midnight, Morning, Noon, Evening, normal time-flow,
time skipping, named environment forcing, Clear, Rain, Thunderstorm, Fog,
environment reset, world save, and guarded location generation controls.

## Interface and module standard

The visible Tooler is a stable interface contract. The original Player,
Entities, Terrain Shaper, and Misc pages remain the proven foundation. Living
World responsibilities are registered as separate pages for lifecycle,
community behavior, combat, party control, raids, player testing, catalogs and
spawning, world control, terrain, administration, confirmed destructive
actions, and the command bridge. A module may repair or extend its own duty; it
may not silently remove another duty, replace the established shell, or alter
the current interface as an unrelated side effect. The build fails when a
required page or baseline capability disappears.

Default interface key: **Delete**. Settings are stored under the Living World
C.O.R.E. Tooler configuration names, separate from any legacy Tooler install.

## Credits

- Living World integration, current compatibility maintenance, and package:
  Brian / scrufboy.
- Original ValheimTooler: Yohann MARTIN (Astropilot).
- Valheim 1.0 compatibility groundwork: EggyTeam compatibility fork.

The earlier MIT-licensed ValheimTooler implementation was used as the starting
point for compatible feature behavior and interface assets. Attribution remains
here and in `LICENSE`; it is not the installed product identity.
