param([string]$Compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
$ErrorActionPreference='Stop'
$candidate=Split-Path $PSScriptRoot -Parent
$output=Join-Path $candidate 'build\spawn-transaction-checks'
$null=New-Item -ItemType Directory -Force -Path $output
$source=Get-Content -LiteralPath (Join-Path $candidate 'src\ManualTestPlugin.cs') -Raw
$start=$source.IndexOf('        void Spawn()', [StringComparison]::Ordinal)
$end=$source.IndexOf('        void FindNpc()', $start, [StringComparison]::Ordinal)
if($start -lt 0 -or $end -le $start){throw 'Exact production Spawn method boundaries unavailable.'}
# Mechanical extraction keeps the executed method byte-for-byte identical to
# production. Only the surrounding game APIs are doubles, not the transaction.
$method=$source.Substring($start,$end-$start)
$extracted=Join-Path $output 'ProductionSpawn.cs'
$wrapper="using System;`nusing System.Linq;`nusing System.Collections.Generic;`nusing UnityEngine;`nusing LivingWorld.HumanNpc;`nnamespace LivingWorld.ManualTest { public sealed partial class ManualTestPlugin {`n"+$method+"`n} }"
[IO.File]::WriteAllText($extracted,$wrapper)
$executable=Join-Path $output 'FreshSpawnTransactionHarness.exe'
& $Compiler /nologo /optimize+ /target:exe "/out:$executable" $extracted (Join-Path $PSScriptRoot 'FreshSpawnTransactionHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Fresh spawn transaction harness compilation failed.'}
& $executable
if($LASTEXITCODE -ne 0){throw 'Fresh spawn transaction harness failed.'}
