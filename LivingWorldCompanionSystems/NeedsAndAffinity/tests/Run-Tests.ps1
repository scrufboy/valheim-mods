$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.NeedsAndAffinity.Tests.exe'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }

& $compiler /nologo /target:exe "/out:$output" `
    (Join-Path $root 'NeedsAndAffinityDomain.cs') `
    (Join-Path $root 'NeedsAndAffinityPolicy.cs') `
    (Join-Path $root 'NeedsPersistence.cs') `
    (Join-Path $root 'RuntimeAdapterContracts.cs') `
    (Join-Path $root 'tests\NeedsAndAffinityPolicyTests.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $output
exit $LASTEXITCODE
