#!/bin/bash
# Living World C.O.R.E. NPC — build + verify (Linux/Mono).
# Refuses to produce a package unless every gate passes.
#   REFS=<dir with game/BepInEx/Jotunn DLLs> ./build.sh
set -euo pipefail
cd "$(dirname "$0")"
REFS="${REFS:-/home/claude/refs}"
OUT="${OUT:-$PWD/dist}"
mkdir -p "$OUT" build
# Never let a stale DLL from an earlier build pass a failed compile.
rm -f build/LivingWorld.HumanNpc.dll build/LivingWorld.Telemetry.dll build/ledger.exe

refs=""
for d in assembly_valheim assembly_utils assembly_guiutils Splatform SoftReferenceableAssets MagicaClothV2 netstandard \
         UnityEngine UnityEngine.CoreModule UnityEngine.AnimationModule UnityEngine.PhysicsModule \
         UnityEngine.TerrainPhysicsModule UnityEngine.AudioModule UnityEngine.AIModule UnityEngine.ParticleSystemModule \
         UnityEngine.IMGUIModule UnityEngine.UI UnityEngine.TextRenderingModule Unity.TextMeshPro \
         UnityEngine.InputLegacyModule BepInEx 0Harmony Jotunn; do
  refs="$refs -r:$REFS/$d.dll"
done
CSC="mcs -nologo -target:library -langversion:5 -optimize+ -nostdlib -r:$REFS/mscorlib.dll -r:$REFS/System.dll -r:$REFS/System.Core.dll $refs"

echo "== 1/6 Regression ledger (offline policy tests)"
mcs -nologo -langversion:5 -out:build/ledger.exe tests/RegressionLedgerTests.cs \
    src/Kernel/Kernel.Pure.cs src/Kernel/GuardCoverage.Pure.cs src/Kernel/PartySpacing.Pure.cs src/Kernel/Gait.Pure.cs src/Kernel/Bomb.Pure.cs src/Kernel/Throw.Pure.cs src/Kernel/BodyVoice.Pure.cs src/Kernel/BodyState.Pure.cs src/Kernel/Hazard.Pure.cs src/Kernel/ReadyWatch.Pure.cs src/Kernel/Awareness.Pure.cs src/Identity/NpcIdentityRecord.cs
mono build/ledger.exe | tail -1

echo "== 2/6 Native compatibility gate"
python3 -I tools/native_gate.py "$REFS/assembly_valheim.dll" src/Kernel/NativeApi.cs

echo "== 3/6 Compile core (C# 5, matches the Windows .NET Framework compiler)"
$CSC -out:build/LivingWorld.HumanNpc.dll \
    $(find src -name '*.cs' ! -path 'src/Telemetry/*' | sort) \
    $(find ../../LivingWorldCompanionSystems/FormidableAI -name '*.cs' ! -name '*Tests.cs' ! -name '*Benchmark.cs' | sort) \
    2>&1 | grep -E "error|succeeded" || true
test -f build/LivingWorld.HumanNpc.dll || { echo "CORE COMPILE FAILED"; exit 1; }

echo "== 4/6 Ownership gate (no native writes outside NpcBody)"
python3 tools/ownership_gate.py build/LivingWorld.HumanNpc.dll

echo "== 5/6 Compile telemetry recorder"
$CSC -r:build/LivingWorld.HumanNpc.dll -out:build/LivingWorld.Telemetry.dll src/Telemetry/CoreTelemetry.cs 2>&1 | grep -E "error|succeeded" || true
test -f build/LivingWorld.Telemetry.dll || { echo "TELEMETRY COMPILE FAILED"; exit 1; }

echo "== 6/6 Analyzer self-test"
python3 tools/analyzer_selftest.py

echo "== Combat-voice lock (companion combat module, approved 1.0.32.6; set VOICES=<voices/companion dir> to check the pools too)"
python3 -I tools/combat_voice_gate.py build/LivingWorld.HumanNpc.dll ${VOICES:+--voices "$VOICES"}

echo "== Movement-writer gate (native Follow is the only calm-movement writer; party spacing writes nothing)"
python3 -I tools/movement_writer_gate.py build/LivingWorld.HumanNpc.dll

cp build/LivingWorld.HumanNpc.dll build/LivingWorld.Telemetry.dll "$OUT/"
( cd "$OUT" && sha256sum LivingWorld.HumanNpc.dll LivingWorld.Telemetry.dll | tee SHA256SUMS )
echo "BUILD VERIFIED. Offline only: live behavior is proven by a play session + tools/analyze_session.py."
