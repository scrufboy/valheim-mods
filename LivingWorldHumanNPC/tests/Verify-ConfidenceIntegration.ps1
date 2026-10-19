param(
 [Parameter(Mandatory=$true)][string]$AssemblyPath,
 [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$candidate=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
$checks=0
function Require([bool]$ok,[string]$label){if(-not $ok){throw "Confidence integration FAIL: $label"};$script:checks++}
function Method($type,[string]$name){$methods=@($type.Methods | Where-Object Name -eq $name);Require ($methods.Count -eq 1 -and $methods[0].HasBody) "method $($type.Name).$name";$methods[0]}
function Calls($method){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]} | ForEach-Object {$_.Operand.FullName})}
function Has($method,[string]$needle){Require (@(Calls $method | Where-Object {$_ -like "*$needle*"}).Count -gt 0) "$($method.Name) calls $needle"}
try {
 $service=$candidate.MainModule.Types | Where-Object Name -eq HumanPartyConfidence
 $policy=$candidate.MainModule.Types | Where-Object Name -eq ConfidencePolicy
 Require ($null -ne $service -and $null -ne $policy) 'service and policy included in candidate'
 $calls=@($service.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
 Require (@($calls | Where-Object {$_ -match 'SetMoveDir|MoveTo|StopMoving|SetLookDir|set_rotation|set_position|SetTarget|SetFollowTarget|SetPatrol|ClaimOwnership|Heal\(|AddHealth|Damage\(|Equip|UseStamina|AddStamina|AddStatusEffect|SetComfort|SetRested'}).Count -eq 0) 'confidence has no movement, combat, equipment, health, stamina or Rested writer'
 Require (@($calls | Where-Object {$_ -match 'GetAllCharacters|GetAllPlayers|FindObjects|OverlapSphere\(|Pathfinding::|Harmony::|AddComponent|GameObject::.ctor|get_PresenceLeader'}).Count -eq 0) 'no global scan, allocating overlap, patch, proxy or peaceful-only leader input'
 Has (Method $service Observe) 'HumanCompanion::TryGetConfidenceParty'
 Has (Method $service Observe) 'UnityEngine.Physics::OverlapSphereNonAlloc'
 Has (Method $service Observe) 'HumanPartyConfidence::SenseLayersReady'
 Has (Method $service SenseLayersReady) 'UnityEngine.LayerMask::NameToLayer'
 Has (Method $service Perceive) 'BaseAI::CanSeeTarget'
 Has (Method $service Perceive) 'BaseAI::CanHearTarget'
 Has (Method $service SameParty) 'HumanCompositionIdentity::get_IsReady'
 Has (Method $service SameParty) 'CompanionPolicy::TryRead'
 Has (Method $service Update) 'HumanPartyConfidence::ReadyOwner'
 Has (Method $service Update) 'ConfidencePolicy::Advance'
 Has (Method $service ReadyOwner) 'HumanCompositionIdentity::get_IsReady'
 Has (Method $service ReadyOwner) 'ConfidencePolicy::TryRestore'
 Has (Method $service ReadyOwner) 'HumanPartyConfidence::ForgetAuthority'
 Has (Method $service ReadyOwner) 'HumanPartyConfidence::ReadProfile'
 Has (Method $service ReadProfile) 'HumanIdentity::Read'
 Has (Method $service ReadyOwner) 'HumanProfile::get_InitialCourage'
 Has (Method $service Persist) 'ZNetView::IsOwner'
 Has (Method $service Persist) 'ConfidencePolicy::Save'
 Has (Method $service Persist) 'ZDO::Set(System.String,System.String)'
 foreach($method in $service.Methods | Where-Object {$_.HasBody -and $_.Name -ne 'Persist'}) {
   Require (@(Calls $method | Where-Object {$_ -match 'ZDO::Set\('}).Count -eq 0) "$($method.Name) cannot directly write saved data"
 }
 $snapshot=Method $service TryGetSnapshot
 Require (@(Calls $snapshot | Where-Object {$_ -match 'ReadyOwner|Observe\(|ZDO::Set\(|Persist\('}).Count -eq 0) 'snapshot read never initializes, samples, refills or writes'
 foreach($name in @('OnDisable','OnDestroy')){Has (Method $service $name) 'HumanPartyConfidence::ForgetAuthority'}
 $policyCalls=@($policy.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
 Require (@($policyCalls | Where-Object {$_ -match 'UnityEngine|ZDO|ZNetView|HumanCompanion|HumanResources|Village|Maypole|Reclamation'}).Count -eq 0) 'confidence resource policy independent of runtime, stamina, village and legacy systems'
 $companion=$candidate.MainModule.Types | Where-Object Name -eq HumanCompanion
 $leader=Method $companion TryGetConfidenceParty
 Has $leader 'HumanCompanion::Ready'
 Has $leader 'Player::GetPlayer'
 Require (@(Calls $leader | Where-Object {$_ -match 'IsAlerted|IsDead|SetFollowTarget|PresenceLeader'}).Count -eq 0) 'bound party input remains available during combat, Hold and loaded leader death'
 Require (@($leader.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -match 'authorizedTarget|activeMotion'}).Count -eq 0) 'bound party input independent of combat and motion state'
 Has (Method ($candidate.MainModule.Types | Where-Object Name -eq NativeHumanFactory) Create) 'AddComponent<LivingWorld.HumanNpc.HumanPartyConfidence>'
 "ConfidenceIntegration PASS: $checks compiled service, input contract, authority and no-gameplay-writer checks. Unity sensing NOT RUN."
} finally {$candidate.Dispose()}
