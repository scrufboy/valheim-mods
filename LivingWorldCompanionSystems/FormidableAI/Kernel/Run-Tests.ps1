$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.FormidableAI.Kernel.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'CombatFrame.cs') `
        (Join-Path $PSScriptRoot 'CombatContracts.cs') `
        (Join-Path $PSScriptRoot 'CombatArbitrator.cs') `
        (Join-Path $PSScriptRoot 'KernelTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI kernel test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI kernel tests failed: $LASTEXITCODE" }
} finally { Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue }
