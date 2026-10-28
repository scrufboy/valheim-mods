# Changed files

The implementation is in the current `valheim-mods` working tree:

- `LivingWorldHumanNPC/src/NorseNameCatalog.cs` — curated given names and
  explicit masculine genitives.
- `LivingWorldHumanNPC/src/HumanProfile.cs` — fresh-name selection and Unicode
  validation; persisted-name format unchanged.
- `LivingWorldHumanNPC/tests/HumanProfileTests.cs` — name variety, grammar,
  seeded stability, Unicode, legacy records and input checks.
- `LivingWorldHumanNPC/Build-HumanNpc.ps1` — catalog source in the explicit
  build allowlist, profile test, and identity adapter harness.
- `LivingWorldHumanNPC/README.md` — links to naming canon and scope.
- `LivingWorldCORE/NAMING-CANON.md` — research, examples, meanings, regional
  lore policy, and future recorded genealogy contract.
- `research/NORSE-LORE-SOURCE-NOTES.md` — naming-canon link and evidence boundary.

These exchange files are the handoff record, not build inputs. No source catalog
file needs to be copied into BepInEx; the compiled DLL contains the names.
