$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$output = Join-Path $PSScriptRoot 'DiscoveryPolicyTests.exe'
$sceneOutput = Join-Path $PSScriptRoot 'EncounterScenePolicyTests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $PSScriptRoot 'DiscoveryPolicy.cs') `
        (Join-Path $PSScriptRoot 'DiscoveryPersistence.cs') `
        (Join-Path $PSScriptRoot 'DiscoveryPolicyTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Discovery test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "Discovery tests failed: $LASTEXITCODE" }
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$sceneOutput" `
        (Join-Path $PSScriptRoot 'DiscoveryPolicy.cs') `
        (Join-Path $PSScriptRoot 'EncounterScenePolicy.cs') `
        (Join-Path $PSScriptRoot 'SiteKnowledgePolicy.cs') `
        (Join-Path $PSScriptRoot 'EncounterScenePolicyTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Scene test compilation failed: $LASTEXITCODE" }
    & $sceneOutput
    if ($LASTEXITCODE -ne 0) { throw "Scene tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $sceneOutput -Force -ErrorAction SilentlyContinue
}
