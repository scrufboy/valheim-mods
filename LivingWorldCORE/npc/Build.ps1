# Living World C.O.R.E. NPC — Windows build + verify.
# Mirrors build.sh: refuses to produce DLLs unless every gate passes.
# Requires: .NET Framework csc (ships with Windows), Python 3 with `pip install dnfile`.
param(
    [string]$Managed = 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$Profile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1",
    [string]$Python = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$build = Join-Path $root 'build'
$null = New-Item -ItemType Directory -Force -Path $build
Remove-Item -Force -ErrorAction SilentlyContinue "$build\LivingWorld.HumanNpc.dll","$build\LivingWorld.Telemetry.dll","$build\ledger.exe"

$names = 'assembly_valheim','assembly_utils','assembly_guiutils','Splatform','SoftReferenceableAssets','MagicaClothV2',
         'netstandard','UnityEngine','UnityEngine.CoreModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule',
         'UnityEngine.TerrainPhysicsModule','UnityEngine.AudioModule','UnityEngine.AIModule','UnityEngine.ParticleSystemModule',
         'UnityEngine.IMGUIModule','UnityEngine.UI','UnityEngine.TextRenderingModule','Unity.TextMeshPro','UnityEngine.InputLegacyModule'
$refs = @($names | ForEach-Object { Join-Path $Managed ($_ + '.dll') })
$refs += Join-Path $Profile 'BepInEx\core\BepInEx.dll'
$refs += Join-Path $Profile 'BepInEx\core\0Harmony.dll'
$refs += Join-Path $Profile 'BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll'
foreach ($r in $refs) { if (-not (Test-Path -LiteralPath $r)) { throw "Missing reference: $r" } }
$refArgs = $refs | ForEach-Object { '/r:' + $_ }
$formidableRoot = Resolve-Path (Join-Path $root '..\..\LivingWorldCompanionSystems\FormidableAI')
$formidableSources = @(
    Get-ChildItem -Recurse -Filter *.cs $formidableRoot |
        Where-Object { $_.Name -notmatch 'Tests\.cs$|Benchmark\.cs$' } |
        ForEach-Object FullName
)

function Step($text) { Write-Host "== $text" -ForegroundColor Cyan }
function Check($code, $what) { if ($code -ne 0) { throw "$what failed" } }

Step '1/6 Regression ledger'
& $csc /nologo /langversion:5 /out:"$build\ledger.exe" "$root\tests\RegressionLedgerTests.cs" `
    "$root\src\Kernel\Kernel.Pure.cs" "$root\src\Kernel\GuardCoverage.Pure.cs" "$root\src\Kernel\PartySpacing.Pure.cs" "$root\src\Kernel\Gait.Pure.cs" "$root\src\Kernel\Bomb.Pure.cs" "$root\src\Kernel\Throw.Pure.cs" "$root\src\Kernel\BodyVoice.Pure.cs" "$root\src\Kernel\BodyState.Pure.cs" "$root\src\Kernel\Hazard.Pure.cs" "$root\src\Kernel\ReadyWatch.Pure.cs" "$root\src\Kernel\Awareness.Pure.cs" "$root\src\Identity\NpcIdentityRecord.cs"
Check $LASTEXITCODE 'ledger compile'
& "$build\ledger.exe"; Check $LASTEXITCODE 'regression ledger'

Step '2/6 Native compatibility gate'
& $Python -I "$root\tools\native_gate.py" (Join-Path $Managed 'assembly_valheim.dll') "$root\src\Kernel\NativeApi.cs"
Check $LASTEXITCODE 'native gate'

Step '3/6 Compile core'
$coreSources = Get-ChildItem -Recurse -Filter *.cs "$root\src" | Where-Object { $_.FullName -notmatch '\\Telemetry\\' } | Sort-Object FullName | ForEach-Object FullName
& $csc /noconfig /nologo /target:library /optimize+ /langversion:5 /nostdlib+ /r:"$Managed\mscorlib.dll" /r:"$Managed\System.dll" `
    /r:"$Managed\System.Core.dll" @refArgs /out:"$build\LivingWorld.HumanNpc.dll" @coreSources @formidableSources
Check $LASTEXITCODE 'core compile'

Step '4/6 Ownership gate'
& $Python "$root\tools\ownership_gate.py" "$build\LivingWorld.HumanNpc.dll"; Check $LASTEXITCODE 'ownership gate'

Step 'Combat-voice lock (companion combat module, approved 1.0.32.6)'
& $Python -I "$root\tools\combat_voice_gate.py" "$build\LivingWorld.HumanNpc.dll"; Check $LASTEXITCODE 'combat-voice lock'

Step 'Movement-writer gate (native Follow is the only calm-movement writer)'
& $Python -I "$root\tools\movement_writer_gate.py" "$build\LivingWorld.HumanNpc.dll"; Check $LASTEXITCODE 'movement-writer gate'

Step '5/6 Compile telemetry'
& $csc /noconfig /nologo /target:library /optimize+ /langversion:5 /nostdlib+ /r:"$Managed\mscorlib.dll" /r:"$Managed\System.dll" `
    /r:"$Managed\System.Core.dll" @refArgs /r:"$build\LivingWorld.HumanNpc.dll" /out:"$build\LivingWorld.Telemetry.dll" `
    "$root\src\Telemetry\CoreTelemetry.cs"
Check $LASTEXITCODE 'telemetry compile'

Step '6/6 Analyzer self-test'
& $Python "$root\tools\analyzer_selftest.py"; Check $LASTEXITCODE 'analyzer self-test'

Get-FileHash "$build\LivingWorld.HumanNpc.dll","$build\LivingWorld.Telemetry.dll" -Algorithm SHA256 | Format-Table Hash, Path
Write-Host 'BUILD VERIFIED (offline). Live proof = play session + tools\analyze_session.py' -ForegroundColor Green
