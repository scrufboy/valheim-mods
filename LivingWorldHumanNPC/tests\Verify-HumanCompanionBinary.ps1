param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$CecilPath='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\independent-core\tools\Mono.Cecil.dll'
)
# Independent structural call-flow gate over the compiled candidate. This never
# loads Unity and does not claim to execute Harmony/AI or prove gameplay quality.
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $AssemblyPath).Path)
$checks=0
function Require([bool]$pass,[string]$reason){if(-not $pass){throw "Human companion binary FAIL: $reason"};$script:checks++}
function Method([string]$name){
    $found=@($companion.Methods | Where-Object Name -eq $name)
    Require ($found.Count -eq 1 -and $found[0].HasBody) "unique companion method $name"
    $found[0]
}
function Calls($method){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]})}
function HasCalls($method,[string[]]$expected){
    $names=@(Calls $method | ForEach-Object {$_.Operand.FullName})
    foreach($needle in $expected){Require (@($names | Where-Object {$_.Contains($needle)}).Count -ge 1) "$($method.Name) reaches $needle"}
}
function HasFields($method,[string[]]$expected){
    $names=@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference]} | ForEach-Object {$_.Operand.FullName})
    foreach($needle in $expected){Require (@($names | Where-Object {$_.Contains($needle)}).Count -ge 1) "$($method.Name) observes $needle"}
}
function ScopedPrefix([string]$name,[string]$guardCall,[bool]$boolReturn){
    $method=Method $name; $instructions=@($method.Body.Instructions)
    $component=@(Calls $method | Where-Object {$_.Operand.FullName.Contains('GetComponent<LivingWorld.HumanNpc.HumanCompanion>')})
    $guard=@(Calls $method | Where-Object {$_.Operand.FullName.Contains($guardCall)})
    Require ($component.Count -eq 1 -and $guard.Count -ge 1 -and $component[0].Offset -lt $guard[0].Offset) "$name scopes to attached companion before policy operation"
    $returns=@($instructions | Where-Object {$_.Offset -gt $component[0].Offset -and $_.Offset -lt $guard[0].Offset -and $_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ret})
    Require ($returns.Count -ge 1) "$name has non-companion early return"
    if($boolReturn){Require (@($returns | Where-Object {$_.Previous.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldc_I4_1}).Count -ge 1) "$name permits untouched native execution for non-companion"}
    HasCalls $method @('LivingWorld.HumanNpc.HumanCompanion::Fault')
}
try {
    $companion=@($assembly.MainModule.Types | Where-Object FullName -eq LivingWorld.HumanNpc.HumanCompanion)[0]
    Require ($null -ne $companion) 'human-specific adapter compiled'
    $allCalls=@($companion.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
    Require (@($allCalls | Where-Object {$_.Operand.FullName -match 'UnityEngine.Transform::set_position|UnityEngine.Transform::set_rotation|UnityEngine.Rigidbody::MovePosition|Character::SetMoveDir|Character::TeleportTo|BaseAI::MoveTo|Animator::Play|Animator::CrossFade'}).Count -eq 0) 'adapter never replaces native locomotion/animation by forcing position, movement or playback'
    Require (@($allCalls | Where-Object {$_.Operand.FullName -match 'ZNetView::ClaimOwnership|ZNetView::ResetZDO|Player::Set|ZDO::SetPrefab|ZDO::SetPosition|ZDO::SetRotation'}).Count -eq 0) 'adapter never steals ownership or changes saved native identity/position/player lifecycle'
    $install=Method InstallPatches
    $strings=@($install.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr} | ForEach-Object {$_.Operand})
    foreach($name in @('UpdateAI','UpdateTarget','OnDamaged','SetTarget','DoAttack','OnAttackTrigger','IsEnemy')){Require ($strings -contains $name) "explicit native patch target $name"}
    $patchOwners=@{UpdateAI='MonsterAI';UpdateTarget='MonsterAI';OnDamaged='MonsterAI';SetTarget='MonsterAI';DoAttack='MonsterAI';OnAttackTrigger='Attack';IsEnemy='BaseAI'}
    foreach($name in $patchOwners.Keys){
        $anchor=@($install.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr -and $_.Operand -eq $name})
        Require ($anchor.Count -eq 1 -and $anchor[0].Previous.Previous.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldtoken -and $anchor[0].Previous.Previous.Operand.FullName -eq $patchOwners[$name]) "$name resolves exactly $($patchOwners[$name]), not Player/Humanoid/Character/Animator"
    }
    foreach($pair in @(@('BeforeUpdate','::Tick', $true),@('BeforeTarget','::UpdateTarget',$true),@('BeforeDamaged','::Authorize',$false),@('BeforeSetTarget','::AuthorizationValid',$true),@('BeforeAttack','::AuthorizationValid',$true),@('BeforeAttackTrigger','::AuthorizationValid',$true),@('AfterEnemy','::AuthorizationValid',$false))){ScopedPrefix $pair[0] $pair[1] $pair[2]}
    HasCalls (Method Owner) @('ZNetView::IsValid','ZNetView::IsOwner')
    $ready=Method Ready
    HasCalls $ready @('UnityEngine.Behaviour::get_isActiveAndEnabled','::Owner','Character::IsDead','BaseAI::HuntPlayer','MonsterAI::DespawnInDay','MonsterAI::IsEventCreature','BaseAI::IsSleeping','HumanCompositionIdentity::get_IsReady','::ReadOrder')
    HasFields $ready @('HumanAppearance::Initialized','HumanCompanion::patchesReady','HumanCompanion::localFault')
    HasCalls (Method ReadOrder) @('ZDO::GetString','CompanionPolicy::TryRead','CompanionPolicy::Valid')
    $save=Method SaveOrder
    HasCalls $save @('::Owner','CompanionPolicy::Write','ZDO::Set(System.String,System.String)')
    Require (@(Calls $save | Where-Object {$_.Operand.DeclaringType.FullName -eq 'ZDO' -and $_.Operand.Name -eq 'Set'}).Count -eq 1) 'saved order has one string-field commit'
    Require (@($save.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr -and $_.Operand -eq 'livingworld_companion_order_record'}).Count -eq 1) 'saved order writes only its versioned custom record'
    HasCalls (Method MayCommand) @('::Ready','Character::IsDead','Player::GetPlayerID','CompanionPolicy::MayIssue')
    HasFields (Method MayCommand) @('Player::m_localPlayer')
    HasCalls (Method TryIssueFollow) @('::MayCommand','::SaveOrder','::StopCombat','::ReconcileLeader')
    HasCalls (Method TryIssueHold) @('::MayCommand','::SaveOrder','::StopCombat','::ReconcileLeader')
    HasCalls (Method TryAuthorizeCombatTarget) @('::MayCommand','::Authorize')
    HasCalls (Method BeginTemporaryFollow) @('::MayCommand')
    foreach($name in @('BeginTemporaryFollow','EndTemporaryOrder')){
        $method=Method $name
        Require (@(Calls $method | Where-Object {$_.Operand.Name -eq 'SaveOrder' -or $_.Operand.DeclaringType.FullName -eq 'ZDO'}).Count -eq 0) "$name does not persist temporary test order"
    }
    HasCalls (Method ReconcileLeader) @('Player::GetPlayer','CompanionPolicy::Motion','CompanionPolicy::MustClearPatrol','BaseAI::ResetPatrolPoint','MonsterAI::SetFollowTarget','CompanionPolicy::MustCaptureWaitAnchor')
    HasFields (Method ReconcileLeader) @('CompanionOrderRecord::OwnerId','HumanCompanion::waitAnchor')
    HasCalls (Method Authorize) @('::Ready','CompanionPolicy::MayAuthorize','::IsActiveThreat','CompanionPolicy::RefreshPursuit','CompanionPolicy::MayReacquireRetired')
    HasCalls (Method IsActiveThreat) @('GetComponent<MonsterAI>','BaseAI::GetTargetCreature','BaseAI::CanSeeTarget','BaseAI::CanHearTarget','BaseAI::IsEnemy','::ProtectingOwner')
    Require (@(Calls (Method IsActiveThreat) | Where-Object {$_.Operand.Name -eq 'IsAlerted'}).Count -eq 0) 'independent companion sensing does not wait for hostile alert state'
    HasCalls (Method ProtectingOwner) @('Character::IsDead','CompanionPolicy::MayProtectOwner','::OrderAnchor')
    HasCalls (Method AuthorizationValid) @('::Ready','Character::IsDead','Character::IsPlayer','Character::IsTamed','CompanionPolicy::WithinTacticalCombatBounds')
    HasCalls (Method BeforeAttack) @('::AuthorizationValid','::SafeEquippedWeapon','::DropCombatTarget')
    HasCalls (Method BeforeAttackTrigger) @('System.Object::ReferenceEquals','::AuthorizationValid','::SafeEquippedWeapon','::SafeNativeAttack','::DropCombatTarget')
    HasCalls (Method DropCombatTarget) @('::StopCombatCore')
    HasFields (Method DropCombatTarget) @('HumanCompanion::encounterOwned','HumanCompanion::encounterClearObserved','HumanCompanion::encounterUntil')
    HasCalls (Method SafeEquippedWeapon) @('Humanoid::GetCurrentWeapon','Humanoid::GetInventory','Inventory::GetAllItems','::Contains','::SafeNativeAttack')
    HasFields (Method SafeEquippedWeapon) @('ItemDrop/ItemData::m_equipped','HumanAppearance::WeaponName','ItemDrop/ItemData/SharedData::m_attack','ItemDrop/ItemData/SharedData::m_secondaryAttack')
    HasCalls (Method SafeNativeAttack) @('CompanionPolicy::SafeNativeAttack')
    $clear=Method ClearNativeCombat
    HasCalls $clear @('::Owner','Attack::Abort','Attack::Stop','System.Reflection.FieldInfo::SetValue')
    HasFields $clear @('HumanCompanion::CurrentAttackField','Attack::m_attackKillsSelf','HumanCompanion::TargetField','HumanCompanion::StaticTargetField')
    $fields=@($clear.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.FullName.Contains('HumanCompanion::CurrentAttackField')})
    Require ($fields.Count -ge 2) 'current attack is read then retired after abort/conditional stop'
    HasCalls (Method Tick) @('::Owner','::Ready','::ClearNativeCombat','::ReconcileLeader','::RefreshNearbyThreat','::FindActiveThreat','BaseAI::StopMoving')
    HasCalls (Method UpdateTarget) @('::Ready','::AuthorizationValid','BaseAI::CanHearTarget','BaseAI::CanSeeTarget')
    Require (@($companion.Fields | Where-Object Name -eq 'rawEnemyQuery' | ForEach-Object {$_.CustomAttributes | Where-Object {$_.AttributeType.FullName -eq 'System.ThreadStaticAttribute'}}).Count -eq 1) 'native eligibility bypass is thread-local'
    "HumanCompanionBinary PASS: $checks structural IL integration checks. Binary SHA256: $((Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash)"
    'Native Harmony routing, physics, animation, companion persistence and full combat gameplay NOT RUN.'
} finally {$assembly.Dispose()}

