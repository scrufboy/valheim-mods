$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.VikingAISalvage.Benchmark.exe'
try {
    & $compiler /nologo /langversion:5 /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'EquipmentLoadoutPolicy.cs') `
        (Join-Path $PSScriptRoot 'Benchmark.cs')
    if ($LASTEXITCODE -ne 0) { throw "VikingAI salvage benchmark compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "VikingAI salvage benchmark failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
}
