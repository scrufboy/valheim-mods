param([string]$Compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path ([System.IO.Path]::GetTempPath()) ('EncounterCuePresentationPolicyTests-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & $Compiler /nologo /warnaserror+ /out:$output `
        (Join-Path $here 'EncounterCuePresentationPolicy.cs') `
        (Join-Path $here 'EncounterCuePresentationPolicyTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Encounter cue presentation policy compilation failed' }
    & $output
    if ($LASTEXITCODE -ne 0) { throw 'Encounter cue presentation policy tests failed' }
}
finally {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
}
