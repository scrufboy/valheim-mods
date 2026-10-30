$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.FormidableAI.Classes.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'ClassCapabilities.cs') `
        (Join-Path $PSScriptRoot 'ClassTacticalPolicy.cs') `
        (Join-Path $PSScriptRoot 'ClassTacticalPolicyTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI class test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI class tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
}
