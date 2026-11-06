# Installed 1.0.32.7 play-session evidence

Date: 2026-10-28. Brian reported he was back out of Valheim. The game process
was no longer running when Codex checked. This record preserves the profile's
current BepInEx log before a later launch can overwrite it. It responds to the
1.0.32.7 launch prerequisite named in Claude's 1.0.32.8b spacing review.

- INSTALLED: `LWC Human Lab v1` still contains `LivingWorld.HumanNpc.dll` with
  SHA-256 `89B5BEAF24550E79978CF8C7A6E69EC2A2B54BBEE6ACDADC01FD45EE408A1E6F`.
- LIVE-PROVEN (limited): `LogOutput.log` line 27 says BepInEx loaded `Living
  World C.O.R.E. NPC 1.0.32.7`; line 30 says the plugin loaded; line 1138 says
  native guard and archer humans registered. Line 1345 reports a female guard's
  appearance ready, with name `Signy Thorstensdottir`.
- SOURCE-INSPECTED: the preserved log includes voice and combat activity for
  named NPCs, including death events. It does **not** by itself prove spacing,
  no-push behavior, save persistence, or approval of the next test candidate.
- UNKNOWN: the user's visual experience and test verdict. Ask Brian for it if
  it is needed for promotion; do not infer a pass from the absence of errors.

The saved log is `LogOutput.log` in this directory, SHA-256
`991A848E359CF60EF222CD7355233547AE09455172B4E2C6B1EFF8BA0893EA73`.
No plugin, config, or game-world file was changed here. The Norse naming DLL from
the 0.3.58 line remains uninstalled and incompatible with this profile's line.
