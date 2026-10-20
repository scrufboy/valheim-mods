# Living World C.O.R.E. Tooler — Brian / scrufboy

Maintained and packaged for Living World C.O.R.E. by **Brian / scrufboy**.

Version 2.5.0 adds a separate **Site Survey** module. Walk and record four corners, calculate from live altered terrain, preview the computed settlement/arena campus, and export a concise report plus machine-readable JSON. Controller bindings are shown directly on the page. Terrain application remains locked and is not included in this build.

This is the Living World C.O.R.E. Tooler: one Living World plugin identity,
Living World branding, the familiar proven interface/colors/control layout,
the complete player, catalog, entity, terrain, ESP, and utility feature set,
plus a dedicated **Living World** toolbar section containing the complete custom
trainer: lifecycle, community, combat, party, raids, player shortcuts,
item/prefab spawning, time of day, environment/weather, terrain, map/admin,
confirmed destructive actions, and an arbitrary command bridge. Invalid
unfinished ObjectDB item entries are skipped without flooding `LogOutput.log`.

The Raids page includes two opt-in, real Valheim `RandomEvent` matchups for
Formidable companion evaluation: a bounded group of two to three Common Folk
Pillagers and a two-combatant Krauguk Legion. They use the installed Souls of
the Land RRR prefabs, hunt the player/party, and are excluded from random world
raid selection. Custom Raids 1.8.2 loads and synchronizes these event records.

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

