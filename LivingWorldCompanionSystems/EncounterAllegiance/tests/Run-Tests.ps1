$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $PSScriptRoot 'EncounterAllegiancePolicyTests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $root 'EncounterAllegianceDomain.cs') `
        (Join-Path $root 'AllegianceMatrix.cs') `
        (Join-Path $root 'EncounterAllegiancePolicy.cs') `
        (Join-Path $root 'EncounterAllegiancePersistence.cs') `
        (Join-Path $PSScriptRoot 'EncounterAllegiancePolicyTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Encounter allegiance test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "Encounter allegiance tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
}
