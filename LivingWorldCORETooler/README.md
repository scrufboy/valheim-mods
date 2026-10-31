# Living World C.O.R.E. Tooler — Brian / scrufboy

Maintained and packaged for Living World C.O.R.E. by **Brian / scrufboy**.

Version 2.6.4 puts the two current Elder-pattern FormidableAI stress raids directly on the **Raids** page: **Formidable Arena** and **Formidable Souls Swamp**, with the standard noon setup and stop-event controls beside them. Every active hostile raid slot uses Valheim's native `HuntPlayer=true` pursuit so actors entering on the 70–95 m ring converge as a raid. `RRRN_NinoBosque` and the two intended `RRRN_OrcBrute` heavy slots remain defined but are temporarily disabled: the blue NinoBosque logged a spawn without ever appearing in combat, video or telemetry, while OrcBrute never logged a completed spawn; both were eligible around the two removal-error bursts. This is containment pending per-prefab lifecycle tracing, not a declared root-cause fix. It does not create a companion defend order or leader status, and no other raid balance or combat rule changes. Every vanilla raid control remains available, and Version 2.5.0's separate **Site Survey** module remains intact.

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
