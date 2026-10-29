$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path ([System.IO.Path]::GetTempPath()) 'LivingWorld.RuntimeIntegration.Tests.exe'
try {
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$output" `
        (Join-Path $root 'Discovery\DiscoveryPolicy.cs') `
        (Join-Path $root 'CampLifecycle\CampLifecyclePolicy.cs') `
        (Join-Path $root 'FriendInNeed\FriendInNeedContract.cs') `
        (Join-Path $root 'EncounterAllegiance\EncounterAllegianceDomain.cs') `
        (Join-Path $root 'EncounterAllegiance\AllegianceMatrix.cs') `
        (Join-Path $root 'EncounterAllegiance\EncounterAllegiancePolicy.cs') `
        (Join-Path $root 'EncounterActivation\EncounterActivationDomain.cs') `
        (Join-Path $root 'EncounterActivation\EncounterActivationPolicy.cs') `
        (Join-Path $root 'EncounterLoadouts\EncounterLoadoutDomain.cs') `
        (Join-Path $root 'EncounterLoadouts\EncounterLoadoutPolicy.cs') `
        (Join-Path $root 'NeedsAndAffinity\NeedsAndAffinityDomain.cs') `
        (Join-Path $root 'NeedsAndAffinity\NeedsAndAffinityPolicy.cs') `
        (Join-Path $root 'QuestBroker\QuestBrokerDomain.cs') `
        (Join-Path $root 'QuestBroker\QuestBrokerPolicy.cs') `
        (Join-Path $root 'SpecialistCombat\PredictionAndThrows\PredictionAndThrowsPolicy.cs') `
        (Join-Path $root 'SpecialistCombat\MeleeRoles\MeleeRolePolicy.cs') `
        (Join-Path $root 'RescueRelease\RescueReleaseDomain.cs') `
        (Join-Path $root 'RescueRelease\RescueReleasePolicy.cs') `
        (Join-Path $PSScriptRoot 'EncounterArcRuntime.cs') `
        (Join-Path $PSScriptRoot 'RuntimeIntegrationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Runtime integration test compilation failed: $LASTEXITCODE" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "Runtime integration tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
}
