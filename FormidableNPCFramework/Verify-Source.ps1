param([Parameter(Mandatory=$true)][string]$KernelDirectory)
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
python -m unittest discover -s (Join-Path $here 'migration') -p test_import_souls_roster.py
if ($LASTEXITCODE -ne 0) { throw 'Roster importer checks failed' }
$kernel = (Resolve-Path -LiteralPath $KernelDirectory).Path
$target = Join-Path $env:TEMP 'LWC-Formidable-DartPolicy-Tests.exe'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /langversion:5 /warn:4 /warnaserror+ /target:exe "/out:$target" (Join-Path $kernel 'CombatFrame.cs') (Join-Path $kernel 'CombatContracts.cs') (Join-Path $kernel 'CombatArbitrator.cs') (Join-Path $here 'src\DartSkirmisherModule.cs') (Join-Path $here 'tests\DartSkirmisherTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Dart module test compilation failed' }
& $target
if ($LASTEXITCODE -ne 0) { throw 'Dart module checks failed' }
