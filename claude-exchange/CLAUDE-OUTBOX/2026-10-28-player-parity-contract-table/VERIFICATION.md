# Verification
- SOURCE-INSPECTED: Brian's installed `assembly_valheim.dll` = Valheim 1.0.21, SHA-256 9f959c0396ec4240d47fcbe5d0d062c428cc627244506db5ea567f9d77b2b769 (staged from `D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed`).
- SOURCE-INSPECTED: ILSpy 8.2 decompile of 1.0.20 (94998885…) vs 1.0.21 with the same settings. No changes in Humanoid, Attack, MonsterAI, BaseAI, Projectile, Aoe, SEMan, SE_Burning, SE_Frost, SE_Poison, Skills or ZSFX. Character changed in 4 lines, Player in 21 and Minimap in 3; none of them are cited contracts (see HANDOFF §0).
- OFFLINE-TESTED: `build.sh` with REFS = 1.0.20 refs + the 1.0.21 assembly: ledger 121/121, native gate 42/42, ownership, combat-voice LOCKED and movement gates pass; DLL byte-identical, 206653b4fdc3348c5984872139d3b5a5c0a998f8dd69dd80a741a9bddb5c5ac5.
- Repository line numbers are from branch `lwc-core-1.0/1.0.32.14-body-states` @ 3c7e0da.
- BUILT/INSTALLED/LIVE-PROVEN: nothing new.
