# RRRCore 3.1.5 local Valheim 1.0.20 compatibility repair

RRRCore 3.1.5 embeds an older ServerSync call that loads
`ZRoutedRpc.Everybody` as a runtime static field. In the current Valheim assembly,
that member is a literal `long` constant with value zero. The CLR therefore throws
`MissingFieldException` during `RRRCore.Plugin.Awake()` before RRRCore completes
its startup.

`Repair-RRRCore-EverybodyLiteral.ps1` makes one narrow local IL substitution in a
known RRRCore binary: the single `ldsfld ZRoutedRpc::Everybody` instruction inside
`ServerSync.ConfigSync.sendZPackage` becomes `ldc.i8 0`. It verifies the original
SHA-256, requires an explicit non-overwriting backup path, writes through a
temporary file, and verifies the instruction after writing.

This repository intentionally contains no RRRCore binary. RRRCore remains its
author's work; this records only the reproducible local compatibility operation.

## Full active-profile audit

The first repair covered the startup-breaking load in
`ServerSync.ConfigSync.sendZPackage`, but a later instruction-level audit of every
active plug-in found additional legacy loads in RRRCore, RunicPack, and the Tooler
core. `Repair-LegacyEverybodyLoads.ps1` is the general, hash-gated repair used for
those remaining instructions. Every `ldsfld ZRoutedRpc::Everybody` becomes the
current game's equivalent literal `ldc.i8 0`; all other instructions are preserved.

The AWL compatibility plug-in is disabled in the lab profile because its own log
reported that its compatibility field was not active. The repaired consumer
assemblies therefore do not depend on that run-time shim. Original binaries are
kept outside the plug-in load tree under `BepInEx/backups`.

Verified repaired SHA-256 values for the 2026-10-20 lab profile are:

- RRRCore 3.1.5: `AC3AD43E2E7DD7DCECAE2DAEAE0FBEAB178E012B9A2413B0FBE549A92E16964E`
- RunicPack 1.1.6: `EF8EC75FB1B663FF8107A093DBC80DE04F92006187DD7F3357610E5341DCE496`
- Living World Tooler core 2.4.1: `6B793D5F6FFFE60F5AB8BABA3DFD5B552E9F06E123E2E9815B0AF49D93E6B4E9`
