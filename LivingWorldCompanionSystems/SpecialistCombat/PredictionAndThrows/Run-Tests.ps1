$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.PredictionAndThrows.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'PredictionAndThrowsPolicy.cs') `
        (Join-Path $PSScriptRoot 'PredictionAndThrowsTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Prediction and throws test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "Prediction and throws tests failed: $LASTEXITCODE" }
} finally { Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue }
