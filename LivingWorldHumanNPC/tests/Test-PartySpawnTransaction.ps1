param([string]$Compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
$ErrorActionPreference='Stop'
$candidate=Split-Path $PSScriptRoot -Parent
$output=Join-Path $candidate 'build\party-spawn-transaction-checks'
$null=New-Item -ItemType Directory -Force -Path $output
$source=Get-Content -LiteralPath (Join-Path $candidate 'src\ManualTestPlugin.cs') -Raw
$start=$source.IndexOf('        void PrepareParty()', [StringComparison]::Ordinal)
$end=$source.IndexOf('        Vector3 FindPartySpawnPoint()', $start, [StringComparison]::Ordinal)
if($start -lt 0 -or $end -le $start){throw 'Exact production PrepareParty transaction boundaries unavailable.'}
$methods=$source.Substring($start,$end-$start)
$extracted=Join-Path $output 'ProductionPartySpawn.cs'
$wrapper="using System;`nusing System.Linq;`nusing UnityEngine;`nusing LivingWorld.HumanNpc;`nnamespace LivingWorld.ManualTest { public sealed partial class ManualTestPlugin {`n"+$methods+"`n} }"
[IO.File]::WriteAllText($extracted,$wrapper)
$executable=Join-Path $output 'PartySpawnTransactionHarness.exe'
& $Compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$executable" $extracted (Join-Path $PSScriptRoot 'PartySpawnTransactionHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Party spawn transaction harness compilation failed.'}
& $executable
if($LASTEXITCODE -ne 0){throw 'Party spawn transaction harness failed.'}
