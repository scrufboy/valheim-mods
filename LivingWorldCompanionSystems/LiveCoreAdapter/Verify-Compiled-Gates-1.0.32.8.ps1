param(
    [string]$CoreRoot = (Join-Path $PSScriptRoot '..\..\..\lwc-core-norse-port\LivingWorldCORE\npc'),
    [string]$Python = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
)
$dll = Join-Path $PSScriptRoot 'build-1.0.32.8\LivingWorld.HumanNpc.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Run Compile-Against-Core-1.0.32.8.ps1 first.' }
& $Python (Join-Path $CoreRoot 'tools\ownership_gate.py') $dll
$ownership = $LASTEXITCODE
& $Python -I (Join-Path $CoreRoot 'tools\combat_voice_gate.py') $dll
$voice = $LASTEXITCODE
& $Python -I (Join-Path $CoreRoot 'tools\movement_writer_gate.py') $dll
$movement = $LASTEXITCODE
Write-Host "Gate results: ownership=$ownership combatVoice=$voice movement=$movement"
if ($ownership -or $voice -or $movement) { exit 1 }
