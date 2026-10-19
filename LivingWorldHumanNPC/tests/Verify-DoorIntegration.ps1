param(
 [Parameter(Mandatory=$true)][string]$AssemblyPath,
 [Parameter(Mandatory=$true)][string]$NativeAssemblyPath,
 [Parameter(Mandatory=$true)][string]$BaselinePath,
 [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$candidate=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
$native=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($NativeAssemblyPath)
$baseline=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($BaselinePath)
$checks=0
function Require([bool]$ok,[string]$label){if(-not $ok){throw "Door integration FAIL: $label"};$script:checks++}
function Method($type,[string]$name){$m=@($type.Methods | Where-Object Name -eq $name);Require ($m.Count -eq 1 -and $m[0].HasBody) "method $($type.Name).$name";$m[0]}
function Calls($method){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]} | ForEach-Object {$_.Operand.FullName})}
function Has($method,[string]$needle){Require (@(Calls $method | Where-Object {$_ -like "*$needle*"}).Count -gt 0) "$($method.Name) calls $needle"}
try {
 $door=$native.MainModule.Types | Where-Object Name -eq Door
 $open=Method $door Open
 $can=Method $door CanInteract
 $rpc=Method $door RPC_UseDoor
 Has $open 'ZNetView::InvokeRPC'
 Has $rpc 'Door::CanInteract'
 Has $rpc 'ZDO::Set(System.Int32,System.Int32,System.Boolean)'
 Has $rpc 'Door::UpdateState'
 Has $can 'Animator::GetCurrentAnimatorStateInfo'
 $nativeAi=$native.MainModule.Types | Where-Object Name -eq MonsterAI
 $nativeBase=$native.MainModule.Types | Where-Object Name -eq BaseAI
 $nativeUpdate=Method $nativeAi UpdateAI
 $avoidCall=@($nativeUpdate.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'AvoidFire'})
 $followCall=@($nativeUpdate.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'Follow'})
 Require ($avoidCall.Count -eq 1 -and $followCall.Count -eq 1 -and $avoidCall[0].Offset -lt $followCall[0].Offset) 'native fire avoidance evaluated before follow'
 Has (Method $nativeBase AvoidFire) 'BaseAI::RandomMovementArroundPoint'
 $nativeFollow=Method $nativeBase Follow
 Require (@($nativeFollow.Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Ldc_R4' -and $_.Operand -eq 3}).Count -eq 1) 'native follow has 3m arrival threshold'
 Has $nativeFollow 'BaseAI::StopMoving'
 Has $nativeFollow 'BaseAI::MoveTo'
 Require ($open.Parameters.Count -eq 1 -and $open.Parameters[0].ParameterType.FullName -eq 'UnityEngine.Vector3') 'native Open vector signature'
 foreach($name in @('m_keyItem','m_canNotBeClosed','m_checkGuardStone')){Require (@($door.Fields | Where-Object Name -eq $name).Count -eq 1) "native $name field"}
 $interact=Method $door Interact
 Has $interact 'PrivateArea::CheckAccess'
 Has $interact 'Game::IncrementPlayerStat'
 $travel=$candidate.MainModule.Types | Where-Object Name -eq HumanDoorTraversal
 Require ($null -ne $travel) 'dedicated door component present'
 $allCalls=@($travel.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
 Require (@($allCalls | Where-Object {$_ -match 'ZDO::Set\(|ClaimOwnership|ResetZDO|SetFollowTarget|SetPatrolPoint|ResetPatrolPoint|SaveOrder|Character::SetMoveDir|set_position|set_rotation|Teleport|Animator::Play|Door::Interact'}).Count -eq 0) 'no state/ownership/order/transform bypass or player-stat interaction'
 Require (@($allCalls | Where-Object {$_ -match 'FindObjectsOfType|OverlapSphere\(|RaycastAll'}).Count -eq 0) 'no unbounded scene scan or per-frame raycast allocation'
 $before=Method $travel BeforeMove
 Has $before 'GetComponent<LivingWorld.HumanNpc.HumanDoorTraversal>'
 Has $before 'HumanDoorTraversal::Step'
 Has $before 'HumanDoorTraversal::Fault'
 $firstStep=@($before.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'Step'})[0]
 Require (@($before.Body.Instructions | Where-Object {$_.Offset -lt $firstStep.Offset -and $_.OpCode.Code.ToString() -eq 'Ret' -and $_.Previous.OpCode.Code.ToString() -eq 'Ldc_I4_1'}).Count -gt 0) 'unmarked AI exits to unchanged native behavior'
 Has (Method $travel Access) 'ZNetView::IsValid'
 Has (Method $travel Access) 'ZNetView::IsOwner'
 Has (Method $travel Access) 'PrivateArea::CheckAccess'
 Has (Method $travel Access) 'DoorTravelPolicy::Access'
 Has (Method $travel Observe) 'DoorTravelPolicy::Evaluate'
 Has (Method $travel Observe) 'DoorTravelPolicy::HasCrossed'
 Has (Method $travel Discover) 'HumanCompanion::get_HasAuthorizedCombat'
 Require (@((Method $travel Observe).Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Ldstr' -and $_.Operand -eq 'Combat passage complete; opened door left available for party pursuit'}).Count -eq 1) 'combat pursuit explicitly releases an opened passage without closing it'
 Has (Method $travel CrossingClear) 'Character::GetAllCharacters'
 Has (Method $travel CrossingClear) 'Character::IsDead'
 Has (Method $travel CrossingClear) 'Component::get_gameObject'
 Has (Method $travel CrossingClear) 'Character::GetRadius'
 Has (Method $travel CrossingClear) 'DoorTravelPolicy::OccupiesDoorway'
 Has (Method $travel DirectBlockingDoor) 'Physics::RaycastNonAlloc'
 Has (Method $travel Discover) 'HumanDoorTraversal::DirectBlockingDoor'
 Has (Method $travel Discover) 'DoorTravelPolicy::DirectBlocker'
 Has (Method $travel SegmentBlocked) 'Physics::RaycastNonAlloc'
 Has (Method $travel Discover) 'DoorTravelPolicy::RouteCandidate'
 Has (Method $travel Discover) 'HumanDoorTraversal::FullPath'
 Has (Method $travel Discover) 'HumanDoorTraversal::ResetRoute'
 Has (Method $travel FullPath) 'Pathfinding::GetPath'
 Has (Method $travel FullPath) 'DoorTravelPolicy::FullLeg'
 Has (Method $travel NeedsCloseFollow) 'DoorTravelPolicy::UseLeaderDoorGoal'
 Has (Method $travel NeedsCloseFollow) 'HumanDoorTraversal::DirectBlockingDoor'
 Require (@($travel.Methods | Where-Object Name -eq BeforeFollow).Count -eq 0) 'old door follow executor removed; presence owns one follow hook'
 $presence=$candidate.MainModule.Types | Where-Object Name -eq HumanFollowPresence
 Has (Method $presence BeforeFollow) 'GetComponent<LivingWorld.HumanNpc.HumanFollowPresence>'
 Has (Method $presence Follow) 'HumanDoorTraversal::NeedsCloseFollow'
 Has (Method $travel Update) 'HumanDoorTraversal::MotionReport'
 $motion=Method $travel MotionReport
 Require (@(Calls $motion | Where-Object {$_ -match 'SetValue|SetFollowTarget|MoveTo|SetMoveDir|set_position'}).Count -eq 0) 'motion diagnostics are observation only'
 Has (Method $travel Cancel) 'HumanDoorTraversal::ResetRoute'
 Has (Method $travel Observe) 'HumanDoorTraversal::ResetRoute'
 Has (Method $travel Discover) 'HumanDoorTraversal::CaptureGeometry'
 Has (Method $travel Step) 'HumanCompanion::IsDoorTravelGoal'
 foreach($name in @('OnDisable','OnDestroy')){Has (Method $travel $name) 'HumanDoorTraversal::Cancel'}
 $companion=$candidate.MainModule.Types | Where-Object Name -eq HumanCompanion
 $permission=Method $companion get_MayTravelThroughDoors
 Has $permission 'HumanCompanion::Ready'
 Has $permission 'Player::GetPlayerID'
 Has $permission 'BaseAI::IsAlerted'
 Require (@($permission.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq 'authorizedTarget'}).Count -gt 0) 'door permission distinguishes peaceful orders from authorized combat travel'
 Has (Method $companion IsDoorTravelGoal) 'Character::IsDead'
 Has (Method $companion IsDoorTravelGoal) 'UnityEngine.Vector3::Distance'
 $factory=$candidate.MainModule.Types | Where-Object Name -eq NativeHumanFactory
 Has (Method $factory Create) 'AddComponent<LivingWorld.HumanNpc.HumanDoorTraversal>'
 $plugin=$candidate.MainModule.Types | Where-Object Name -eq ManualTestPlugin
 Has (Method $plugin Awake) 'HumanDoorTraversal::InstallPatches'
 # Compare method instruction streams with the actual tested 0.3.0 binary.
 # New door permission getters are allowed; all previous companion decisions,
 # composition, visuals and acceptance methods stay identical. The sole settings
 # settings are independently versioned: door checks must not freeze obsolete
 # combat tuning while all door component and policy methods remain preserved.
 foreach($name in @('HumanCompanion','CompanionPolicy','HumanNpcSettings','HumanAppearance','HumanCompositionIdentity','HumanMigrationPolicy','ManualAcceptance','ManualLabBuilder','HumanRigPolicy')){
   $oldType=$baseline.MainModule.Types | Where-Object Name -eq $name
   $newType=$candidate.MainModule.Types | Where-Object Name -eq $name
   Require ($null -ne $oldType -and $null -ne $newType) "baseline type $name"
   foreach($oldMethod in $oldType.Methods | Where-Object HasBody){
     # 0.3.8 opt-in party extension: these exact changes are checked by Verify-PartyIntegration.
     if($name -eq 'HumanCompanion' -and $oldMethod.Name -in @('InstallPatches','get_PresenceLeader','Authorize','IsActiveThreat','RefreshNearbyThreat','FindActiveThreat','AuthorizationValid','SafeEquippedWeapon','StopCombatCore','DropCombatTarget','ReconcileLeader','Tick','UpdateTarget','BeforeUpdate','BeforeTarget','BeforeDamaged','BeforeSetTarget','BeforeAttack','BeforeAttackTrigger','AfterEnemy')){continue}
     if($name -eq 'CompanionPolicy' -and $oldMethod.Name -eq 'Motion'){continue}
     if($name -eq 'HumanAppearance' -and $oldMethod.Name -eq 'Update'){continue} # IdentityAdapterHarness covers new optional identity path.
     if($name -eq 'HumanCompanion' -and $oldMethod.Name -in @('Authorize','StopCombat')){continue} # Optional activity cancellation, verified by integration tests.
     $newMethod=@($newType.Methods | Where-Object FullName -eq $oldMethod.FullName)
     Require ($newMethod.Count -eq 1) "retained method $($oldMethod.FullName)"
     $oldInstructions=@($oldMethod.Body.Instructions | ForEach-Object ToString)
     if($name -eq 'HumanNpcSettings' -and $oldMethod.Name -eq 'Configure'){
       $stores=@($oldMethod.Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Stfld' -and $_.Operand.Name -eq 'm_avoidFire'})
       Require ($stores.Count -eq 1 -and $stores[0].Previous.OpCode.Code.ToString() -eq 'Ldc_I4_1') 'baseline avoidance enabled'
       $newStores=@($newMethod[0].Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Stfld' -and $_.Operand.Name -eq 'm_avoidFire'})
       Require ($newStores.Count -eq 1 -and $newStores[0].Previous.OpCode.Code.ToString() -eq 'Ldc_I4_0') 'human fire-circle avoidance disabled'
       continue
     }
     $oldIL=$oldInstructions -join "`n"
     $newIL=($newMethod[0].Body.Instructions | ForEach-Object ToString) -join "`n"
     Require ($oldIL -ceq $newIL) "unchanged tested method $($oldMethod.FullName)"
   }
 }
 "DoorIntegration PASS: $checks native-contract, scope, and baseline-preservation checks. Unity door traversal NOT RUN."
} finally {$candidate.Dispose();$native.Dispose();$baseline.Dispose()}
