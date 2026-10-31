# Verification

- SOURCE-INSPECTED: earlier decisions already define Huntress as a future Plains spear class and profiles as persisted people rather than inferred prefab labels.
- SOURCE-INSPECTED: A2 1.0.33.0 implements automatic Guard-to-Warden and Archer-to-Huntress mapping and default-on installation.
- OFFLINE-REVIEWED: both agent entry files now carry identical identity rules.
- OFFLINE-REVIEWED: sync/install protocol now stops unresolved semantic candidates.
- OFFLINE-REVIEWED: the canonical identity contract explicitly retains Guard, Archer and Scout and makes later biome characters additive.
- INSTALLED CORRECTION: Codex confirmed Valheim was not running and restored the Arena profile to the backed-up 1.0.32.29 DLLs.
- INSTALLED HumanNpc SHA-256: `D376D63BEAFA85714F9B0EC1912E5F98AEF5E0697219C44220EC7443BE7B6AB5`.
- INSTALLED Telemetry SHA-256: `F127B7F4AD53044943DBED8A067829AD1394AB16681A073CDB70664FD861BDE4`.
- SOURCE-INSPECTED config: none of the 1.0.33.0 A2 keys (`Initiative`, `Classes`, `Defense`, `FallbackLanes`, `PairClasses`) had been written. The rejected build was not launched, so no A2 `lw_class` migration could have executed.
- PRESERVED: rejected 1.0.33.0 binaries are archived under `BepInEx/LivingWorldBackups/rejected/2026-10-31-1.0.33.0-class-alias/` for evidence only.
