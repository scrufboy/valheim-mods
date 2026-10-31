# RRR lifecycle diagnostic — fixture switch enabled (Claude, 2026-10-31)

Follows 439d71e.

## Change (INSTALLED; read back from the device and re-hashed)
`LWC Formidable Arena\BepInEx\config\livingworld.core.rrrlifecyclediag.cfg`
- before: sha256 23c7a1f21173529b791ce993ed0ea23cda1bf864c08c707ddf224bd4cab330be, 786 bytes (the BepInEx-rewritten file Codex captured)
- after:  sha256 **cd326fd59db8da5954f5020ba652cc619fb76ea78b19a389fb5826317719e9e1**, 785 bytes
- The only difference is the line `EnableFixtureCommands = false` → `true`. CRLF line endings are kept. Read-back values:
  EnableFixtureCommands=true, Enabled=true, AutoAudit=true, StackDepth=14.
- Process note: the first write delivered a cached copy of the original install file (57ec6ef5…; same values, fixtures still false).
  The second write used a fresh staging path and was verified as above. No other content was ever written.

Unchanged: the DLL is still 88fa1dabfdfe27efeeee5edd7496b3fc02c407cb002ce8ae101c85271a9cdd09 (re-hashed). Tooler 2.6.4, the actor containment and every other plugin and config are untouched.

## Brian's run (Codex's sequence)
Same throwaway world, no companions:
1. `lwc_rrrdiag fixture RRRN_WoodsSoul 30 1337`, wait 10 s, `lwc_rrrdiag remove`
2. `lwc_rrrdiag fixture RRRN_NinoBosque 30 1337`, wait 10 s, `lwc_rrrdiag remove`
3. `lwc_rrrdiag fixture RRRN_OrcBrute 30 1337`, wait 10 s, `lwc_rrrdiag remove`
Stop at once on a refused command, a DIAG-FAULT, or a repeating error burst. Return the complete LogOutput.log.
