# Verification and state

- OFFLINE-TESTED: `pwsh -NoProfile -File LivingWorldHumanNPC/Build-HumanNpc.ps1`
  completed with `HumanNpc 0.3.58 offline build PASS`.
- OFFLINE-TESTED: HumanProfile suite passed 25,291 assertions, including 4,096
  generated identities, catalog coverage, Unicode round trips, old-record
  preservation, and seeded appearance/temperament stability.
- OFFLINE-TESTED: IdentityAdapterHarness passed 50 assertions with game API
  doubles. FreshSpawnTransaction passed 25 assertions.
- OFFLINE-TESTED: final DLL is 243,712 bytes and SHA-256
  `482AF55D6494A651E94FD95716193AB7F88299750988C0D2847EC3C0F57E02C9`.
- SOURCE-INSPECTED: installed profile DLL before handoff is 156,160 bytes and
  SHA-256 `89B5BEAF24550E79978CF8C7A6E69EC2A2B54BBEE6ACDADC01FD45EE408A1E6F`.
- UNKNOWN: no claim of installed candidate, Unity load, font rendering, save
  reload in-game, multiplayer visibility, or live genealogy.

Build output also contains existing unrelated compiler warnings about obsolete
Unity `FindObjectsOfType` calls and one unused field. The build completed PASS.
