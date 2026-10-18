param([Parameter(Mandatory=$true)][string]$AssemblyPath,[Parameter(Mandatory=$true)][string]$NativeAssemblyPath,[Parameter(Mandatory=$true)][string]$CecilPath)
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$party=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
$native=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($NativeAssemblyPath)
$checks=0
function Require([bool]$ok,[string]$why){if(-not $ok){throw "Party integration FAIL: $why"};$script:checks++}
function TypeDef($asm,$name){$t=@($asm.MainModule.Types|Where-Object Name -eq $name);Require ($t.Count -eq 1) "unique $name";$t[0]}
function Method($type,$name){$m=@($type.Methods|Where-Object Name -eq $name);Require ($m.Count -eq 1) "unique $($type.Name).$name";$m[0]}
function Calls($method){@($method.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]}|ForEach-Object {$_.Operand.FullName})}
function Strings($method){@($method.Body.Instructions|Where-Object {$_.OpCode.Code.ToString() -eq 'Ldstr'}|ForEach-Object Operand)}
function Has($type,$name,$call){Require (@(Calls (Method $type $name)|Where-Object {$_.Contains($call)}).Count -gt 0) "$($type.Name).$name calls $call"}
function StoredValue($method,[string]$field){
 $stores=@($method.Body.Instructions|Where-Object {$_.OpCode.Code.ToString() -eq 'Stfld' -and $_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq $field})
 Require ($stores.Count -eq 1) "one settings write for $field"
 $stores[0].Previous
}
function HasFloatSetting($method,[string]$field,[single]$value){
 $load=StoredValue $method $field
 Require ($load.OpCode.Code.ToString() -eq 'Ldc_R4' -and [single]$load.Operand -eq $value) "$field is $value"
}
try {
 $companion=TypeDef $party HumanCompanion
 $equipment=TypeDef $party HumanPartyEquipment
 $readiness=TypeDef $party HumanEquipmentReadiness
 $cleric=TypeDef $party HumanCleric
 $torch=TypeDef $party HumanUtilityTorch
 $lab=TypeDef $party ManualPartyLab
 $plugin=TypeDef $party ManualTestPlugin
 $visual=TypeDef $party ClericPresentation
 Has $companion IsActiveThreat 'TryGetConfidenceParty'
 Has $companion get_PresenceLeader 'HumanCleric::get_Casting'
 Has $companion BeforeUpdate 'HumanCleric::get_Casting'
 Has $companion BeforeAttack 'HumanPartyEquipment::MayAttack'
 Has $companion BeforeAttackTrigger 'HumanPartyEquipment::MayAttack'
 Has $companion SafeEquippedWeapon 'HumanPartyEquipment::AllowsWeapon'
 Has $companion AuthorizationValid 'CompanionPolicy::WithinTacticalCombatBounds'
 Has $companion ThreatPriority 'CompanionPolicy::ThreatPriority'
 Has $companion FindActiveThreat 'CompanionPolicy::ShouldSwitchThreat'
 Has $companion Authorize 'get_DetectionLeash'
 Has $companion Authorize 'HumanCleric::InterruptForCombat'
 Has $companion Authorize 'HumanUtilityTorch::ReleaseForWeapons'
 Has $companion FindActiveThreat 'get_DetectionLeash'
 $findThreat=Method $companion FindActiveThreat
 $drops=@($findThreat.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'DropCombatTarget'})
 Require ($drops.Count -eq 0) 'missed acquisition scan cannot revoke an already-authorized combat target'
 Has $lab SpawnWave '::TryFindWaveSite'
 Has $lab TryFindWaveSite 'ManualTestPlugin::GroundPoint'
 Has $lab TryFindWaveSite 'ManualTestPlugin::RequireClear'
 Has $lab TryFindWaveSite 'Pathfinding::GetPath'
 Has $lab TryFindWaveSite 'Quaternion::AngleAxis'
 Require (@(Strings (Method $lab SpawnWave)|Where-Object {$_ -like '*face a clearer approach*'}).Count -eq 0) 'wave spawn no longer refuses one fixed facing point'
 Has $equipment Update 'HumanCompanionPosture::get_OwnsAmbientHands'
 Has $equipment Update 'HumanEquipmentReadiness::EnsureCombatReady'
 Has $equipment PrepareArcher 'ManualTestPlugin::GuardWorld'
 Has $equipment PrepareArcher 'RestoreInventory'
 Has $equipment SaveInventory 'Inventory::Save'
 Has $equipment RestoreInventory 'Inventory::Load'
 Has $equipment RestoreInventory 'Humanoid::EquipItem'
 Has $equipment BeforeBowRelease 'PartyCombatPolicy::BowReleaseBridge'
 Has $equipment InstallPatches 'HarmonyLib.Harmony::Patch'
 Require ('DoAttack' -in (Strings (Method $equipment '.cctor'))) 'equipment reflects the native MonsterAI DoAttack boundary'
 Has $equipment MayAttack 'HasAmmo'
 $combatUpdateCalls=Calls (Method $equipment Update)
 Require (@($combatUpdateCalls|Where-Object {$_ -match 'LookAt|DoAttack|PartyCombatPolicy::Weapon|ReactToAttack|EquipItem|UnequipItem'}).Count -eq 0) 'equipment update does not run a parallel combat controller'
 Require (@($equipment.Methods|Where-Object {$_.Name -in @('AfterSelect','AdvanceDraw','ObserveBlock','TryRiposte','OnBlockResolved')}).Count -eq 0) 'manual weapon, draw, block and riposte decision methods removed'
 Require (@(Strings (Method $equipment InstallPatches)|Where-Object {$_ -eq 'BlockAttack'}).Count -eq 0) 'equipment does not patch native block decisions'
 Require (@(Calls (Method $equipment MayAttack)|Where-Object {$_.Contains('GetAttackDrawPercentage')}).Count -eq 0) 'bow permission never waits on a Player-only draw percentage'
 $validate=Method $equipment ValidateItems
 Require (@($validate.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq 'm_aiAttackInterval'}).Count -eq 0) 'party equipment does not impose a slower bow attack interval'
 Has $equipment BeforeSelect 'get_Ready'
 Has (TypeDef $native MonsterAI) SelectBestAttack 'Humanoid::EquipBestWeapon'
 Has $readiness EnsurePeacefulStow 'Humanoid::HideHandItems'
 Has $readiness EnsurePeacefulStow 'EquipmentPresentationPolicy::Stow'
 Has $readiness EnsurePeacefulStow 'EquipmentPresentationPolicy::VerifiedStowed'
 Has $readiness Update 'HumanEquipmentReadiness::EnsurePeacefulStow'
 Has $readiness SyncBackVisuals 'VisEquipment::SetLeftBackItem'
 Has $readiness SyncBackVisuals 'VisEquipment::SetRightBackItem'
 Has $readiness EnsureCombatReady 'HumanEquipmentReadiness::SyncBackVisuals'
 Has $readiness Update 'System.Reflection.MethodBase::Invoke'
 Has $readiness EnsureCombatReady 'System.Reflection.MethodBase::Invoke'
 Has $readiness Update 'HumanUtilityTorch::ReleaseForWeapons'
 Has $readiness Update 'HumanUtilityTorch::ClaimsPeacefulHands'
 Has $readiness WantsWeapons 'HumanCompanion::get_PartyLeader'
 Has $readiness WantsWeapons 'HumanEquipmentReadiness::LeaderReady'
 Has $torch ReleaseForWeapons 'Disable'
 Has $torch Update 'HumanEquipmentReadiness::WantsWeapons'
 Has $torch ClaimsPeacefulHands 'HumanEquipmentReadiness::WantsWeapons'
 $equipmentCalls=@($equipment.Methods|Where-Object HasBody|ForEach-Object {Calls $_})
 Require (@($equipmentCalls|Where-Object {$_ -match 'Character::Damage|HitData::|Projectile::Setup|Inventory::RemoveItem'}).Count -eq 0) 'equipment leaves damage/projectiles/ammo debit to native attack'
 Has (TypeDef $native Attack) UseAmmo 'Inventory::RemoveItem'
 $block=Method (TypeDef $native Humanoid) BlockAttack
 Require (@(Calls $block|Where-Object {$_ -match 'UseStamina'}).Count -gt 0) 'native block consumes stamina'
 Has $cleric Eligible 'ZNetView::IsOwner'
 Has $cleric Eligible 'TryGetConfidenceParty'
 Has $cleric Eligible 'Physics::Linecast'
 Has $cleric Eligible 'PartyCombatPolicy::HealEligible'
 Has $cleric Update 'HumanResourcePool::Use'
 Has $cleric Update 'HumanResourcePool::Advance'
 Has $cleric Update 'Character::Heal'
 Has $cleric Update 'ClericPresentation::Begin'
 Has $cleric Update 'ClericPresentation::Delivered'
 Has $cleric Save 'HumanResourcePool::Save'
 Has $torch Update 'PartyCombatPolicy::Torch'
 Has $torch AmbientDark 'EnvMan::IsNight'
 Has $torch AmbientDark 'EnvMan::IsDaylight'
 Has $torch AmbientDark 'PartyCombatPolicy::PerceivedLight'
 Has $torch Update 'HumanCompanionPosture::get_OwnsAmbientHands'
 Has $torch EnsureTorch 'ObjectDB::GetItemPrefab'
 Has $torch EnsureTorch 'System.Reflection.MethodBase::Invoke'
 Has $torch Update 'HumanUtilityTorch::TorchVisible'
 Has $torch Update 'Humanoid::UnequipItem'
 Has $torch Update 'Humanoid::EquipItem'
 Has $lab TryFindWaveSite 'Pathfinding::GetPath'
 Has $lab TryFindWaveSite 'ManualTestPlugin::RequireClear'
 Has $lab SpawnWave 'SaveLedger'
 Has $lab Stop 'ZNetScene::Destroy'
 Has $lab Stop 'ZDO::GetString'
 Has $lab BeforePieceDamage 'ManualTestPlugin::GuardWorld'
 Has $lab BeforePieceDamage 'ZNetView::IsOwner'
 Has $lab BeforePieceDamage 'ZDO::GetString'
 Has $plugin PrepareParty 'HumanPartyEquipment::PrepareArcher'
 Has $plugin PrepareParty 'HumanIdentity::InitializeFresh'
 Has $plugin BeginCombat 'ManualPartyLab::CaptureProtection'
 $spawn=Method $lab SpawnWave
 $firstInstantiate=@($spawn.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'Instantiate'})[0]
 $plan=@($spawn.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'TryFindWaveSite'})[0]
 Require ($plan.Offset -lt $firstInstantiate.Offset) 'whole wave site search and preflight precede spawning'
 $allCalls=@($equipment.Methods+$cleric.Methods+$torch.Methods+$lab.Methods|Where-Object HasBody|ForEach-Object {Calls $_})
 Require (@($allCalls|Where-Object {$_ -match 'ClaimOwnership|SetPrefab|Character::SetHealth|Player::SetMax|SetLevel|TeleportTo'}).Count -eq 0) 'new systems do not steal identity/authority, refill health, level or teleport actors'
 $visualCalls=@($visual.Methods|Where-Object HasBody|ForEach-Object {Calls $_})
 Require (@($visualCalls|Where-Object {$_ -match 'Character::Heal|Character::Damage|Aoe::|ZNetView::'}).Count -eq 0) 'visual has no healing/damage/network gameplay path'
 Has $visual Pulse 'GetComponentsInChildren<UnityEngine.Component>'
 Has $visual Pulse 'UnityEngine.Renderer::get_materials'
 Has $visual Begin 'UnityEngine.Animator::get_parameters'
 Require (@(Calls (Method $cleric Update)|Where-Object {$_.Contains('ClericPresentation::Casting')}).Count -eq 0) 'cleric does not emit repeated actor-centred combat sparks'
 Has $cleric Update 'ClericPresentation::Delivered'
 $doors=TypeDef $party HumanDoorTraversal
 Has $doors Step 'BaseAI::StopMoving'
 Has $doors Step 'ResetRoute'
 Has $doors NeedsCloseFollow 'DoorTravelPolicy::ContinueShortFollow'
 $settingsType=TypeDef $party HumanNpcSettings
 $settings=Method $settingsType Configure
 Has $plugin Register 'ConfigureAlertVoice'
 Has $plugin Register 'PrefabManager::CreateClonedPrefab'
 $registrationCalls=Calls (Method $plugin Register)
 Require (@($registrationCalls|Where-Object {$_.Contains('PrefabManager::AddPrefab')}).Count -eq 2) 'NPC and private alert voice are both registered through Jotunn'
 Require (@($registrationCalls|Where-Object {$_.Contains('DontDestroyOnLoad')}).Count -eq 0) 'alert voice cannot use the failed unregistered DontDestroyOnLoad lifecycle'
 HasFloatSetting (Method $settingsType TuneAlertVoice) m_minPitch .8
 HasFloatSetting (Method $settingsType TuneAlertVoice) m_maxPitch .9
 HasFloatSetting $settings m_viewRange 30
 HasFloatSetting $settings m_hearRange 9999
 HasFloatSetting $settings m_alertRange 20
 HasFloatSetting $settings m_maxChaseDistance 300
 HasFloatSetting $settings m_minAttackInterval 0
 HasFloatSetting $settings m_circleTargetDistance 8
 HasFloatSetting $settings m_circleTargetDuration 6
 HasFloatSetting $settings m_circleTargetInterval 8
 $circulate=StoredValue $settings m_circulateWhileCharging
 Require ($circulate.OpCode.Code.ToString() -eq 'Ldc_I4_1') 'native circulate-while-charging is enabled'
 'PartyIntegration PASS: '+$checks+' compiled integration/native-contract assertions. No Unity acceptance claimed.'
} finally {$party.Dispose();$native.Dispose()}

