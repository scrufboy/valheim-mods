$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path $PSScriptRoot 'EncounterLoadoutPolicyTests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'EncounterLoadoutDomain.cs') `
        (Join-Path $PSScriptRoot 'EncounterLoadoutPolicy.cs') `
        (Join-Path $PSScriptRoot 'EncounterLoadoutPersistence.cs') `
        (Join-Path $PSScriptRoot 'SplitAshProfiles.cs') `
        (Join-Path $PSScriptRoot 'EncounterLoadoutPolicyTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Encounter loadout test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "Encounter loadout tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
}
