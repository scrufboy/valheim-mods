$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.VikingAISalvage.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'VikingAiSalvageContracts.cs') `
        (Join-Path $PSScriptRoot 'EquipmentLoadoutPolicy.cs') `
        (Join-Path $PSScriptRoot 'PortalTransactionPolicy.cs') `
        (Join-Path $PSScriptRoot 'PerceptionEvidencePolicy.cs') `
        (Join-Path $PSScriptRoot 'WorkTransactionPolicy.cs') `
        (Join-Path $PSScriptRoot 'VikingAiSalvageTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "VikingAI salvage test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "VikingAI salvage tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
}
