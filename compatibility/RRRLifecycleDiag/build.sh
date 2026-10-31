#!/usr/bin/env bash
# Canonical Mono build + contract check for the RRR lifecycle diagnostic.
#
# Usage: REFS=<dir> [CHECK_REFS=<dir>] ./build.sh
# REFS holds exactly the 14 compile references listed below: 12 from Valheim valheim_Data/Managed and
# BepInEx.dll + 0Harmony.dll from the target profile's BepInEx/core. Only these are compiled against.
# CHECK_REFS (optional) holds further current valheim_Data/Managed DLLs that the metadata contract needs at
# runtime so Mono can load Character/Humanoid (e.g. UnityEngine.ParticleSystemModule). They are never compile
# references and do not affect the DLL. Nothing from REFS/CHECK_REFS is committed. Output: ./bin (git-ignored).
set -euo pipefail
cd "$(dirname "$0")"
: "${REFS:?set REFS to the reference directory}"

# Explicit reference inputs. Compile-time only; Unity's own mscorlib/System/System.Core are the BCL.
COMPILE_REFS="mscorlib System System.Core netstandard assembly_valheim assembly_utils SoftReferenceableAssets Splatform
  UnityEngine UnityEngine.CoreModule UnityEngine.PhysicsModule UnityEngine.AnimationModule BepInEx 0Harmony"

echo "== reference inputs (sha256)"
args=""
for r in $COMPILE_REFS; do
  f="$REFS/$r.dll"
  [ -f "$f" ] || { echo "MISSING reference: $f"; exit 2; }
  printf '%s  %s\n' "$(sha256sum "$f" | cut -c1-16)" "$r.dll"
  args="$args -r:$f"
done
echo "mcs: $(mcs --version)"

mkdir -p bin
OUT=bin/LivingWorld.RRRLifecycleDiag.dll
echo "== compile"
mcs -nologo -target:library -langversion:5 -optimize+ -nostdlib -warnaserror+ $args -out:$OUT Plugin.cs
echo "built $OUT ($(stat -c %s $OUT) bytes)"

echo "== source contract"
fail=0
check() { if grep -qE "$2" Plugin.cs; then echo "PASS $1"; else echo "FAIL $1"; fail=1; fi; }
check "observer switch defaults false"  'Bind\("General", "Enabled", false,'
check "fixture switch defaults false"   'Bind\("Fixture", "EnableFixtureCommands", false,'
check "tracked set is exactly three"    'TrackedActors = \{ "RRRN_WoodsSoul", "RRRN_NinoBosque", "RRRN_OrcBrute" \};'
check "hooks bail out when disabled"    'if \(!cfgEnabled.Value\)'
check "fixture refuses without switch"  'if \(!cfgFixtures.Value\) \{ Say\(args, "fixture refused'
check "fixture one-at-a-time"           'still active; run .lwc_rrrdiag remove. first'
check "fixture single-player guard"     'string refusal = SinglePlayerRefusal\(\);'
if grep -qE 'TrackAllRRRN|cfgPrefabs' Plugin.cs; then echo "FAIL configurable/global tracking present"; fail=1; else echo "PASS no configurable/global tracking"; fi
[ $fail -eq 0 ] || { echo "source contract FAILED"; exit 3; }

echo "== metadata contract (Mono reflection)"
# System Mono must use its own BCL, so stage the game/BepInEx DLLs without Unity's mscorlib/System*.
STAGE="$(mktemp -d)"; trap 'rm -rf "$STAGE"' EXIT
for d in ${CHECK_REFS:+"$CHECK_REFS"} "$REFS"; do
  for f in "$d"/*.dll; do
    case "$(basename "$f")" in mscorlib.dll|System.dll|System.Core.dll|System.*.dll) ;; *) cp "$f" "$STAGE/";; esac
  done
done
if [ -n "${CHECK_REFS:-}" ]; then
  echo "check-only runtime references: $(ls "$CHECK_REFS"/*.dll | wc -l) files from $CHECK_REFS (sha256 of the set: $(cd "$CHECK_REFS" && sha256sum *.dll | sha256sum | cut -c1-16))"
fi
cp $OUT "$STAGE/"
mcs -nologo -langversion:5 -r:System.Core -out:bin/ContractCheck.exe tools/ContractCheck.cs
MONO_PATH="$STAGE" mono bin/ContractCheck.exe "$STAGE/assembly_valheim.dll" "$STAGE/LivingWorld.RRRLifecycleDiag.dll"

echo "== result"
sha256sum $OUT
