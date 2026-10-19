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
