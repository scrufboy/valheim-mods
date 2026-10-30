$ErrorActionPreference = 'Stop'
$suites = @(
    'Kernel\Run-Tests.ps1',
    'Defense\Run-Tests.ps1',
    'Classes\Run-Tests.ps1',
    'Integration\Run-Tests.ps1'
)
foreach ($suite in $suites) {
    & (Join-Path $PSScriptRoot $suite)
    if ($LASTEXITCODE -ne 0) { throw "FormidableAI suite failed: $suite" }
}
Write-Output 'PASS: all FormidableAI offline suites'
