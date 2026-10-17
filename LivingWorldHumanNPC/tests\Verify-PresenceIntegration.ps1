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
function Require([bool]$ok,[string]$label){if(-not $ok){throw "Presence integration FAIL: $label"};$script:checks++}
function Method($type,[string]$name){$m=@($type.Methods | Where-Object Name -eq $name);Require ($m.Count -eq 1 -and $m[0].HasBody) "method $($type.Name).$name";$m[0]}
function Calls($method){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]} | ForEach-Object {$_.Operand.FullName})}
function Has($method,[string]$needle){Require (@(Calls $method | Where-Object {$_ -like "*$needle*"}).Count -gt 0) "$($method.Name) calls $needle"}
try {
 $events=$native.MainModule.Types | Where-Object Name -eq CharacterAnimEvent
 $head=Method $events UpdateHeadRotation
 foreach($name in @('m_lookAt','m_lookWeight','m_bodyLookWeight')){
   $field=@($events.Fields | Where-Object Name -eq $name)
   Require ($field.Count -eq 1 -and $field[0].IsPublic) "native public look field $name"
 }
 Has $head 'BaseAI::GetTargetCreature'
 Has $head 'ZNetView::IsOwner'
 Has $head 'ZDO::Set(System.Int32,UnityEngine.Vector3)'
 Require (@(Calls $head | Where-Object {$_ -match 'GetFollowTarget'}).Count -eq 0) 'native head does not automatically track the follow leader'
 $lookFields=@($head.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq 'm_lookAt'})
 Require ($lookFields.Count -ge 2) 'native head consumes explicit look transform'
 Has (Method $events UpdateLookat) 'Animator::SetLookAtPosition'
 Has (Method $events UpdateLookat) 'Animator::SetLookAtWeight'
 Has (Method $events UpdateLookat) 'Character::InAttack'
 Has (Method $events OnAnimatorIK) 'CharacterAnimEvent::UpdateLookat'
 $presence=$candidate.MainModule.Types | Where-Object Name -eq HumanFollowPresence
 $calls=@($presence.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
 Require (@($calls | Where-Object {$_ -match 'SetMoveDir|SetLookDir|LookTowards|LookAt\(|set_rotation|set_position|Rotate|Teleport|Animator::Play|Animator::CrossFade|SetTarget|SetFollowTarget|SetPatrolPoint|ClaimOwnership|ZDO::Set|SetWalk|SetRun'}).Count -eq 0) 'presence never steers/teleports/reorders/forces animation or writes combat/save state'
 Require (@($calls | Where-Object {$_ -match 'Physics::|FindObjects|GetAllCharacters|GetAllPlayers|Pathfinding::|AddComponent|GameObject::.ctor'}).Count -eq 0) 'no scene scan, extra path queries, proxy object or new runtime components'
 Has (Method $presence Follow) 'CompanionPresencePolicy::NextPace'
 Has (Method $presence Follow) 'HumanDoorTraversal::NeedsCloseFollow'
 Has (Method $presence Follow) 'UnityEngine.Transform::get_position'
 $formation=Method $presence FormationDestination
 Has $formation 'CompanionPresencePolicy::FormationSideOffset'
 Has $formation 'UnityEngine.Transform::get_forward'
 Has $formation 'UnityEngine.Transform::get_right'
 Has (Method $presence Follow) 'BaseAI::StopMoving'
 Has (Method $presence Follow) 'MethodBase::Invoke'
 Has (Method $presence BeforeFollow) 'GetComponent<LivingWorld.HumanNpc.HumanFollowPresence>'
 Has (Method $presence BeforeHeadLook) 'GetComponentInParent<LivingWorld.HumanNpc.HumanFollowPresence>'
 Has (Method $presence Attention) 'CompanionPresencePolicy::AttendLeader'
 Has (Method $presence Attention) 'HumanCompanion::get_CombatReadinessActive'
 Has (Method $presence Leader) 'HumanCompanion::get_PresenceLeader'
 $pose=$candidate.MainModule.Types | Where-Object Name -eq HumanCompanionPosture
 Has (Method $presence Follow) 'HumanCompanionPosture::Refresh'
 Has (Method $pose Refresh) 'CompanionPosturePolicy::Choose'
 Has (Method $pose Refresh) 'HumanCompanion::get_PresenceLeader'
 Has (Method $pose Refresh) 'HumanCompanionPosture::Owner'
 Has (Method $pose Refresh) 'Character::GetVelocity'
 Has (Method $pose Apply) 'HumanCompanionPosture::Owner'
 Has (Method $pose Apply) 'ZSyncAnimation::SetBool'
 Has (Method $pose Apply) 'ZSyncAnimation::SetTrigger'
 $readiness=$candidate.MainModule.Types | Where-Object Name -eq HumanEquipmentReadiness
 $stowMethod=Method $readiness EnsurePeacefulStow
 $stow=@($stowMethod.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'HideHandItems'})
 Require ($stow.Count -eq 1 -and $stow[0].Previous.OpCode.Code.ToString() -eq 'Ldc_I4_1' -and $stow[0].Previous.Previous.OpCode.Code.ToString() -eq 'Ldc_I4_0') 'readiness transaction uses native HideHandItems(false,true) stow path'
 Has (Method $readiness SyncBackVisuals) 'VisEquipment::SetLeftBackItem'
 Has (Method $readiness SyncBackVisuals) 'VisEquipment::SetRightBackItem'
 Has (Method $readiness WantsWeapons) 'HumanCompanion::get_CombatReadinessActive'
 Require (@(Calls (Method $readiness WantsWeapons) | Where-Object {$_ -match 'get_PresenceLeader|VisibleCombat'}).Count -eq 0) 'companion weapon draw is threat-driven, never copied from leader hand state'
 foreach($name in @('AfterCrouching','AfterSitting')){
   Has (Method $pose $name) 'GetComponent<LivingWorld.HumanNpc.HumanCompanionPosture>'
   Has (Method $pose $name) 'HumanCompanionPosture::Owner'
 }
 $poseCalls=@($pose.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
 Require (@($poseCalls | Where-Object {$_ -match 'MoveTo|SetMoveDir|SetLookDir|set_rotation|set_position|SetTarget|SetFollowTarget|ClaimOwnership|Player::StartEmote|Player::SetCrouch|Animator::Play|Animator::CrossFade'}).Count -eq 0) 'posture never moves, reorders or invokes Player lifecycle'
 foreach($name in @('OnDisable','OnDestroy','Fault','ObserveLeader','Attention')){Has (Method $presence $name) 'HumanFollowPresence::ReleaseLook'}
 $companion=$candidate.MainModule.Types | Where-Object Name -eq HumanCompanion
 $eligibility=Method $companion get_PresenceLeader
 Has $eligibility 'HumanCompanion::Ready'
 Has $eligibility 'Character::IsDead'
 Has $eligibility 'BaseAI::IsAlerted'
 Has $eligibility 'MonsterAI::GetFollowTarget'
 Require (@($eligibility.Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Stfld'}).Count -eq 0) 'eligibility only reads existing companion decisions'
 $patchStrings=@($candidate.MainModule.Types.Methods | Where-Object Name -eq InstallPatches | ForEach-Object {$_.Body.Instructions} | Where-Object {$_.OpCode.Code.ToString() -eq 'Ldstr'} | ForEach-Object Operand)
 Require (@($patchStrings | Where-Object {$_ -eq 'Follow'}).Count -eq 1) 'exactly one follow interception in plugin, not stacked movement controllers'
 Require (@($patchStrings | Where-Object {$_ -eq 'UpdateHeadRotation'}).Count -eq 1) 'one scoped head update hook'
 Has (Method ($candidate.MainModule.Types | Where-Object Name -eq NativeHumanFactory) Create) 'AddComponent<LivingWorld.HumanNpc.HumanFollowPresence>'
 Has (Method ($candidate.MainModule.Types | Where-Object Name -eq ManualTestPlugin) Awake) 'HumanFollowPresence::InstallPatches'
 foreach($name in @('HumanCompanion','CompanionPolicy','HumanNpcSettings','HumanAppearance','HumanCompositionIdentity','HumanMigrationPolicy','ManualAcceptance','ManualLabBuilder','HumanRigPolicy','DoorTravelPolicy','HumanDoorTraversal')){
   $old=$baseline.MainModule.Types | Where-Object Name -eq $name
   $new=$candidate.MainModule.Types | Where-Object Name -eq $name
   foreach($m in $old.Methods | Where-Object HasBody){
     if($name -eq 'HumanDoorTraversal' -and $m.Name -like '<Discover>*'){continue}
     # Explicit 0.3.8 party and retained door-close responsibility changes.
     if($name -eq 'HumanCompanion' -and $m.Name -in @('get_PresenceLeader','get_MayTravelThroughDoors','IsDoorTravelGoal','Authorize','IsActiveThreat','RefreshNearbyThreat','FindActiveThreat','AuthorizationValid','SafeEquippedWeapon','Tick','UpdateTarget','BeforeUpdate','BeforeAttack','BeforeAttackTrigger')){continue}
     if($name -eq 'HumanCompanion' -and $m.Name -in @('StopCombatCore','DropCombatTarget')){continue}
     if($name -eq 'CompanionPolicy' -and $m.Name -eq 'Motion'){continue}
     if($name -eq 'HumanDoorTraversal' -and $m.Name -in @('Step','Discover','Observe','Cancel','NeedsCloseFollow','CrossingClear')){continue}
     if($name -eq 'DoorTravelPolicy' -and $m.Name -eq 'Evaluate'){continue}
     # Optional persisted identity is exercised by the actual adapter harness.
     # All other appearance methods remain byte-for-byte baseline comparisons.
     if($name -eq 'HumanAppearance' -and $m.Name -eq 'Update'){continue}
     # Combat perception/timing is versioned by Verify-PartyIntegration. Presence
     # must preserve its own behavior without freezing fighter settings forever.
     if($name -eq 'HumanNpcSettings' -and $m.Name -eq 'Configure'){continue}
     if($name -eq 'HumanCompanion' -and $m.Name -in @('Authorize','StopCombat')){continue} # Optional activity cancellation, verified by integration tests.
     # The old Follow bridge is removed, not left behind as a second executor.
     if($name -eq 'HumanDoorTraversal' -and $m.Name -in @('InstallPatches','ContinueNearDoor','BeforeFollow','.cctor')){continue}
     $next=@($new.Methods | Where-Object FullName -eq $m.FullName)
     Require ($next.Count -eq 1) "retained 0.3.2 method $($m.FullName)"
     Require ((($m.Body.Instructions | ForEach-Object ToString) -join "`n") -ceq (($next[0].Body.Instructions | ForEach-Object ToString) -join "`n")) "identical 0.3.2 behavior $($m.FullName)"
   }
 }
 "PresenceIntegration PASS: $checks native head-look, scope and 0.3.2 preservation checks. Live animation/movement NOT RUN."
} finally {$candidate.Dispose();$native.Dispose();$baseline.Dispose()}

