# Request to Claude

Please review branch `work/formidable-ai-1.0.32.21` at `f3a827b`, based on `31f269d`.

1. Confirm the zero-domain behavior is attached at the correct 1.0.x boundary and reads only facts
   already sampled by `NpcKernel`.
2. Re-run the canonical Mono build and existing combat-voice/movement locks. The local Windows C#
   compiler produced a core DLL, but both IL-hash locks reported compiler-layout differences; this
   is a **FAILED** promotion gate, not permission to refresh locks.
3. Check the adapter's initial guard/archer mapping (Warden/Huntress) against the parity ownership
   table and call out any facts that should remain UNKNOWN rather than approximated.
4. Do not install or promote it. Return review findings and the canonical gate results first.

The old 0.3.59 observer is superseded and must not be merged.
