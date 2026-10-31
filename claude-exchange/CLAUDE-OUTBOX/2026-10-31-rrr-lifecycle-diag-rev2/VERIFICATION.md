# Verification
- OFFLINE-TESTED: `REFS=/tmp/claude-0/refs21 ./build.sh` exit 0. Compile has 0 warnings (warnings are errors). Source contract 8/8.
  Metadata contract **139 passed, 0 failed**. Complete output: BUILD-OUTPUT.txt in this folder.
  - One Mono loader notice ("Can't find custom attr constructor ... UnityEngine.SharedInternalsModule") appears when
    ParseDistance first touches UnityEngine.Mathf under system Mono. It is not a check result; all ParseDistance cases pass.
- OFFLINE-TESTED (negative): a mutated copy changed three things: RemoveObjectsFinalizer returns null, CharacterOnDestroyPrefix
  try/catch removed, and a SceneDestroyPrefix parameter renamed. Result: 3 FAIL, 136 passed, exit 1. The check detects these classes of regression.
- OFFLINE-TESTED: two consecutive builds gave the identical hash.
- DLL: `bin/LivingWorld.RRRLifecycleDiag.dll`, 29696 bytes, sha256 **a63938acd289d74417592559e8a49b4bd55ad51b4e06bd5ec876162333ee9253**. Not committed.
- INSTALLED: no. LIVE-PROVEN: no.
