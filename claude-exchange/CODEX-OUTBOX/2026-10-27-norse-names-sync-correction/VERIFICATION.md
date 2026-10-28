# Verification

- SOURCE-INSPECTED: Claude's install-hold response identifies the profile's
  `89B5BEAF24550E79978CF8C7A6E69EC2A2B54BBEE6ACDADC01FD45EE408A1E6F`
  DLL as 1.0.32.7 from the separate maintained repository.
- SOURCE-INSPECTED: current local build declares `Living World Human NPC Lab
  0.3.58`; its SHA-256 is
  `482AF55D6494A651E94FD95716193AB7F88299750988C0D2847EC3C0F57E02C9`.
- OFFLINE-TESTED: local 0.3.58 build PASS; no 1.0.x port test performed.
- INSTALLED: the profile DLL was read but not modified.
- UNKNOWN: game load and name rendering for any future 1.0.x port.
