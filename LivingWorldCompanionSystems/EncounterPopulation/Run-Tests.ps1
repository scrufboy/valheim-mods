$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.EncounterPopulation.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot '../EncounterLoadouts/EncounterLoadoutDomain.cs') `
        (Join-Path $PSScriptRoot 'EncounterPopulation.cs') `
        (Join-Path $PSScriptRoot 'ClientEncounterContract.cs') `
        (Join-Path $PSScriptRoot 'ServerPresentationProjection.cs') `
        (Join-Path $PSScriptRoot 'ServerCombatKitPolicy.cs') `
        (Join-Path $PSScriptRoot 'EncounterPopulationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Encounter population test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "Encounter population tests failed: $LASTEXITCODE" }
} finally { Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue }
