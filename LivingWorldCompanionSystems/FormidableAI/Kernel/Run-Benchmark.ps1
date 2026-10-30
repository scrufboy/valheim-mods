$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path ([IO.Path]::GetTempPath()) 'LivingWorld.FormidableAI.Kernel.Benchmark.exe'
try {
    & $compiler /nologo /langversion:5 /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'CombatFrame.cs') `
        (Join-Path $PSScriptRoot 'CombatContracts.cs') `
        (Join-Path $PSScriptRoot 'CombatArbitrator.cs') `
        (Join-Path $PSScriptRoot 'KernelBenchmark.cs')
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI kernel benchmark compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI kernel benchmark failed: $LASTEXITCODE" }
} finally { Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue }
