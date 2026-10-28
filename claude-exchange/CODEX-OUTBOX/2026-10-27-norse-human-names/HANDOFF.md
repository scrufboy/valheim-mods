# Human NPC Norse naming handoff for Claude

Date: 2026-10-27. Owner request: install and verify the expanded personal-name
catalog in the Living World human NPC lab, then report what the game actually
shows. This submission follows `claude-exchange/PROTOCOL.md`.

## Candidate and exact build

- OFFLINE-TESTED: built plugin is
  `C:\Users\scruf\Documents\ChatGPT\Living World C.O.R.E. Custom NPC CREATION and AI\valheim-mods\LivingWorldHumanNPC\build\LivingWorld.HumanNpc.dll`.
- OFFLINE-TESTED: SHA-256 is
  `482AF55D6494A651E94FD95716193AB7F88299750988C0D2847EC3C0F57E02C9`.
- SOURCE-INSPECTED: plugin declares `Living World Human NPC Lab 0.3.58`, GUID
  `local.livingworld.humannpc`.
- OFFLINE-TESTED: `build/build-verification.json` reports PASS, with
  `ProfileInstalled: false`, `GameLaunched: false`, `UnityRuntime: NOT RUN`.
- SOURCE-INSPECTED: the source changes currently live in the `valheim-mods`
  working tree; they are uncommitted. Preserve and review them before merging.

The target profile previously recorded in this workspace is
`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1`.
At handoff time its installed
`BepInEx\plugins\LivingWorldHumanNpc\LivingWorld.HumanNpc.dll` has SHA-256
`89B5BEAF24550E79978CF8C7A6E69EC2A2B54BBEE6ACDADC01FD45EE408A1E6F`.
This differs from the candidate hash. The previous installed version is not
established by that hash alone; `claude-exchange/CURRENT.md` is an older
2026-10-17 snapshot and should not override direct file inspection.

## Behavior changed

- SOURCE-INSPECTED: fresh human profiles now choose from 128 masculine and 138
  feminine curated Norse given-name entries, then from a masculine parent-name
  genitive with `son` or `dóttir`. Example: `Kolli Bjarnarson`; `Þóra
  Bjarnardóttir` is a possible female form. The pool yields 34,048 full-name
  combinations, though duplicate names across distinct actors remain possible.
- SOURCE-INSPECTED: exact saved display names load without regeneration. Already
  saved humans keep their existing names; only genuinely fresh identities use
  the new catalog.
- SOURCE-INSPECTED: Unicode validation permits curated precomposed Norse letters
  and still refuses delimiters, markup, controls and invisible formatting.
- SOURCE-INSPECTED: name selection still uses exactly two seeded draws, so
  appearance and temperament from a given fresh seed stay on their former
  sequence. It does not change body models or combat behavior.
- SOURCE-INSPECTED: this is a generated *background* patronymic. No parent NPC,
  parent ID, sibling relation or family tree is created by this change.
- SOURCE-INSPECTED: names are not weighted by biome. The generator receives no
  birthplace/origin context. Regional associations and the future real-kinship
  contract are documented in `LivingWorldCORE/NAMING-CANON.md`.

## Install and live check

1. Identify the active r2modman Valheim profile and confirm it is `LWC Human Lab
   v1`. Close Valheim. Save the currently installed DLL as a rollback copy
   outside the plugin scan directory; record its hash.
2. Copy the built `LivingWorld.HumanNpc.dll` above to
   `<active profile>\BepInEx\plugins\LivingWorldHumanNpc\LivingWorld.HumanNpc.dll`.
   This is a DLL replacement, not a source-file or catalog-file install. Keep
   the existing Jotunn/BepInEx dependencies and other profile contents. Check
   the installed DLL hash equals the candidate hash before launch.
3. Launch only the disposable test world `LWC_HumanLab_v1` with character
   `LWC_Tester`, as the local host without other peers. Confirm the new startup
   log reports `Living World Human NPC Lab 0.3.58`. Because the version string
   was not bumped for this naming change, *also* verify the installed hash;
   version text alone cannot distinguish this build.
4. Open F5. Run `lwtest help`, `lwtest status`, then `lwtest setup` if the lab
   has not been built in that test world. Run `lwtest spawn` on clear ground.
   Inspect the displayed two-part personal name, including any accents and the
   correct `son`/`dóttir` ending for the selected native human model. Capture
   a screenshot and relevant BepInEx log lines. Do not run `lwtest cleanup`
   during this acceptance check: it removes loaded lab-tagged actors.
5. Save, leave, and reload the same world. Check the same actor retains the
   exact spelling and accents, and that the log has no identity parse refusal
   or Unicode/name rendering error. Check a previously saved scout, if one is
   available, still displays its old name unchanged.
6. To see more names and both model styles, use fresh disposable test worlds or
   separately preserved test fixtures. The command intentionally refuses a
   duplicate nearby scout. Do not delete or rename existing campaign people
   just to sample the catalog. Record examples from each style when observed.
7. Report the installed path and SHA-256, startup log evidence, screenshots,
   exact sample names, reload result, and any failed glyphs or behavior. Keep
   the old DLL available for rollback until live checks pass.

Installation and in-game checks above are **REQUESTED**, not completed here.
No live profile was changed by this submission. The package promotion rules
in `claude-exchange/PROTOCOL.md` still apply.
