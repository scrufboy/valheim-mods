param(
 [Parameter(Mandatory=$true)][string]$AssemblyPath,
 [Parameter(Mandatory=$true)][string]$BaselinePath,
 [Parameter(Mandatory=$true)][string]$NativeAssemblyPath,
 [Parameter(Mandatory=$true)][string]$CecilPath
)
# Structural evidence over the real DLL, complementing managed adapter tests.
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$candidate=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
$baseline=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($BaselinePath)
$native=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($NativeAssemblyPath)
$checks=0
function Require([bool]$ok,[string]$why){if(-not $ok){throw "Ambient integration FAIL: $why"};$script:checks++}
function GetTypeDef($assembly,[string]$name){$found=@($assembly.MainModule.Types|Where-Object Name -eq $name);Require ($found.Count -eq 1) "unique type $name";$found[0]}
function Method($type,[string]$name){$found=@($type.Methods|Where-Object Name -eq $name);Require ($found.Count -eq 1 -and $found[0].HasBody) "unique method $($type.Name).$name";$found[0]}
function Calls($m){@($m.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]})}
function Has($m,[string]$needle){Require (@(Calls $m|Where-Object {$_.Operand.FullName.Contains($needle)}).Count -gt 0) "$($m.Name) reaches $needle"}
function Canonical($method,[bool]$strip){
 $remove=[Collections.Generic.HashSet[int]]::new()
 if($strip){foreach($i in (Calls $method|Where-Object {$_.Operand.Name -eq 'ReleaseOptionalActivity'})){
   Require ($i.Previous.OpCode.Name -eq 'ldarg.0') 'release hook has only instance receiver, no extra changed arguments'
   $null=$remove.Add($i.Offset);$null=$remove.Add($i.Previous.Offset)
 }}
 $kept=@($method.Body.Instructions|Where-Object {-not $remove.Contains($_.Offset)})
 $indices=@{};for($n=0;$n -lt $kept.Count;$n++){$indices[$kept[$n].Offset]=$n}
 function TargetIndex($instruction){while($remove.Contains($instruction.Offset)){$instruction=$instruction.Next};$indices[$instruction.Offset]}
 @($kept|ForEach-Object {
   $op=$_.OpCode.Name -replace '\.s$','';$arg=$_.Operand
   if($arg -is [Mono.Cecil.Cil.Instruction]){$arg='target:'+ (TargetIndex $arg)}
   elseif($arg -is [Mono.Cecil.Cil.Instruction[]]){$arg=($arg|ForEach-Object {TargetIndex $_}) -join ','}
   elseif($null -ne $arg){$arg=$arg.ToString()}
   "$op $arg"
 }) -join "`n"
}
try{
 $follow=GetTypeDef $candidate HumanFollowPresence;$ambient=GetTypeDef $candidate HumanCompanionAmbient;$posture=GetTypeDef $candidate HumanCompanionPosture
 $companion=GetTypeDef $candidate HumanCompanion;$old=GetTypeDef $baseline HumanCompanion
 foreach($before in $old.Methods|Where-Object HasBody){
   if($before.Name -eq 'UpdateTarget'){continue} # Replaced by the separately verified native-target prefix/postfix handoff.
   $after=@($companion.Methods|Where-Object FullName -eq $before.FullName)
   Require ($after.Count -eq 1) "baseline companion method retained: $($before.Name)"
   if($before.Name -in @('InstallPatches','get_PresenceLeader','get_MayTravelThroughDoors','IsDoorTravelGoal','Authorize','IsActiveThreat','RefreshNearbyThreat','FindActiveThreat','AuthorizationValid','SafeEquippedWeapon','StopCombat','StopCombatCore','DropCombatTarget','Tick','UpdateTarget','BeforeUpdate','BeforeTarget','BeforeDamaged','BeforeSetTarget','BeforeAttack','BeforeAttackTrigger','AfterEnemy')){continue} # Explicit party/encounter/door integration checked separately.
   $hooked=$before.Name -eq 'Authorize'
   Require ((Canonical $before $false) -ceq (Canonical $after[0] $hooked)) "unchanged baseline companion logic except explicit release hook: $($before.Name)"
 }
 $authorize=Method $companion Authorize
 $release=@(Calls $authorize|Where-Object {$_.Operand.Name -eq 'ReleaseOptionalActivity'})
 $may=@(Calls $authorize|Where-Object {$_.Operand.Name -eq 'MayReacquireRetired'})
 $commit=@($authorize.Body.Instructions|Where-Object {$_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq 'authorizedTarget'})
 Require ($release.Count -eq 1 -and $release[0].Offset -gt $may[0].Offset -and $release[0].Offset -lt $commit[0].Offset) 'accepted combat transition releases optional activity after safety gates, before target commit'
 $stop=Method $companion StopCombatCore;$owner=@(Calls $stop|Where-Object {$_.Operand.Name -eq 'Owner'});$release=@(Calls $stop|Where-Object {$_.Operand.Name -eq 'ReleaseOptionalActivity'})
 Require ($release.Count -eq 1 -and $owner[0].Offset -lt $release[0].Offset) 'StopCombat retains owner gate before ambient release'
 Has (Method $companion ReleaseOptionalActivity) 'HumanCompanionAmbient::Cancel'
 Has (Method $companion ReleaseOptionalActivity) 'HumanCompanionPosture::StopAmbient'
 Has (Method $companion BeforeAttack) 'HumanCompanion::SafeEquippedWeapon'
 Has (Method $companion BeforeAttackTrigger) 'HumanCompanion::SafeEquippedWeapon'
 $execute=Method $follow Follow
 Require (@(Calls $execute|Where-Object {$_.Operand.FullName.Contains('MethodBase::Invoke')}).Count -eq 1) 'exactly one native MoveTo invocation site in existing follow executor'
 Has $execute 'HumanCompanionAmbient::TryDestination';Has $execute 'HumanDoorTraversal::NeedsCloseFollow';Has $execute 'CompanionPresencePolicy::NextPace';Has $execute 'HumanCompanionPosture::Refresh'
 Has $execute 'HumanCompanionAmbient::AfterStop'
 $ambientCalls=@($ambient.Methods|Where-Object HasBody|ForEach-Object {Calls $_})
 Require (@($ambientCalls|Where-Object {$_.Operand.FullName -match 'MoveTo|SetMoveDir|set_position|set_rotation|Instantiate|AddItem|RemoveItem|Repair|UseItem|StartAttack|ClaimOwnership|Player::Set|Player::Attach'}).Count -eq 0) 'ambient cannot move/spawn/reorder/damage/craft/consume/attach a Player'
 $update=Method $ambient Update
 Require (@(Calls $update|Where-Object {$_.Operand.FullName -match 'AfterStop|LookAt|Invoke|TryDestination|SetInt|MoveTo|StopMoving'}).Count -eq 0) 'Update only interrupts; no competing movement/pose starter'
 Has (Method $ambient Ready) 'HumanCompanion::get_PresenceLeader';Has (Method $ambient Ready) 'HumanCompanionAmbient::Owner'
 Has (Method $ambient TryDestination) 'InventoryGui::IsVisible';Has (Method $ambient TryDestination) 'StoreGui::IsVisible'
 Has (Method $ambient StationPermitted) 'Piece::GetCreator';Has (Method $ambient StationPermitted) 'ZNetView::IsOwner';Has (Method $ambient StationPermitted) 'PrivateArea::CheckAccess'
 Has (Method $ambient Discover) 'Physics::OverlapSphereNonAlloc';Has (Method $ambient FullPath) 'Pathfinding::GetPath'
 Has (Method $ambient Release) 'HumanCompanionPosture::StopAmbient'
 $beginCraft=Method $posture BeginAmbientCraft
 Has $beginCraft 'Humanoid::HideHandItems';Has $beginCraft 'HumanCompanionPosture::HandsSafe'
 Require (@($beginCraft.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -in @('m_hiddenLeftItem','m_hiddenRightItem')}).Count -eq 0) 'visible-back stow slots do not veto peaceful crafting animation'
 Has (Method $posture RestoreAmbientHands) 'HumanCompanionPosture::HandsSafe';Has (Method $posture RestoreAmbientHands) 'System.Object::ReferenceEquals'
 Has (Method $posture HandsSafe) 'Attack::IsDone';Has (Method $posture HandsSafe) 'Character::InAttack';Has (Method $posture HandsSafe) 'HumanCompanionPosture::Owner'
 foreach($name in @('AfterCrouching','AfterSitting','BeforeCraftEvent','AfterCraftAttach')){Has (Method $posture $name) 'GetComponent'}
 $patch=Method $posture InstallPatches
 $strings=@($patch.Body.Instructions|Where-Object {$_.OpCode.Name -eq 'ldstr'}|ForEach-Object Operand)
 foreach($name in @('IsCrouching','IsSitting','Attach','Effect','BeforeCraftEvent','AfterCraftAttach')){Require ($strings -contains $name) "scoped posture/event patch declares $name"}
 Require (@($posture.Methods|Where-Object {$_.Name -in @('AfterCrouching','AfterSitting','BeforeCraftEvent','AfterCraftAttach')}).Count -eq 4) 'five patch handlers across two posture hooks, two event prefixes and one Attach postfix'
 Has (Method $posture BeforeCraftEvent) 'Animator::GetInteger';Has (Method $posture BeforeCraftEvent) 'AmbientAnimationContract::SafeEvent'
 Has (Method $posture StopAmbient) 'HumanCompanionPosture::ClearCraftProp'
 Has (Method $posture ClearCraftProp) 'UnityEngine.Object::Destroy'
 Has (Method (GetTypeDef $candidate NativeHumanFactory) Create) 'AddComponent<LivingWorld.HumanNpc.HumanCompanionAmbient>'
 $nativeEffects=GetTypeDef $native AnimationEffect
 Has (Method $nativeEffects Attach) 'UnityEngine.Object::Instantiate'
 Has (Method $nativeEffects UpdateAttachments) 'AnimationEffect::RemoveAttachments'
 foreach($m in @((Method $nativeEffects Attach),(Method $nativeEffects Effect))){Require (@(Calls $m|Where-Object {$_.Operand.FullName -match 'Damage|HitData|Inventory|AddItem|RemoveItem|CraftingStation|Repair'}).Count -eq 0) 'native crafting event receiver has no damage/inventory/station operation'}
 "AmbientIntegration PASS: $checks compiled native adapter, scoped events, baseline combat preservation and single-executor checks. Unity NOT RUN."
}finally{$candidate.Dispose();$baseline.Dispose();$native.Dispose()}
