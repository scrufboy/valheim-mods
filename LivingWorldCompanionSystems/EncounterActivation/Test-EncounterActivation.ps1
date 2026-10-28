$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.EncounterActivation.Tests.exe'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
& $compiler /nologo /target:exe "/out:$output" `
    (Join-Path $root 'EncounterActivationDomain.cs') `
    (Join-Path $root 'EncounterActivationPolicy.cs') `
    (Join-Path $root 'RuntimeAdapterContracts.cs') `
    (Join-Path $root 'EncounterActivationPolicyTests.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $output
exit $LASTEXITCODE
