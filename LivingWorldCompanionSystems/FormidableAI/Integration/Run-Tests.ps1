$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([IO.Path]::GetTempPath()) 'LivingWorld.FormidableAI.Integration.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot '..\Kernel\CombatFrame.cs') `
        (Join-Path $PSScriptRoot '..\Kernel\CombatContracts.cs') `
        (Join-Path $PSScriptRoot '..\Kernel\CombatArbitrator.cs') `
        (Join-Path $PSScriptRoot '..\Defense\DefensePolicy.cs') `
        (Join-Path $PSScriptRoot '..\Classes\ClassCapabilities.cs') `
        (Join-Path $PSScriptRoot '..\Classes\ClassTacticalPolicy.cs') `
        (Join-Path $PSScriptRoot 'FormidableIntegration.cs') `
        (Join-Path $PSScriptRoot 'IntegrationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI integration test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI integration tests failed: $LASTEXITCODE" }
} finally { Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue }
