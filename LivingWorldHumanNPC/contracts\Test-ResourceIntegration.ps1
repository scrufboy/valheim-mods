param(
 [Parameter(Mandatory=$true)][string]$AssemblyPath,
 [Parameter(Mandatory=$true)][string]$NativeAssemblyPath,
 [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$candidate=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
$native=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($NativeAssemblyPath)
$checks=0
function Require([bool]$condition,[string]$message){if(-not $condition){throw "ResourceIntegration FAIL: $message"};$script:checks++}
function ResolveType($assembly,[string]$name){$result=@($assembly.MainModule.Types | Where-Object Name -eq $name);Require ($result.Count -eq 1) "type $name";$result[0]}
function Method($type,[string]$name){$result=@($type.Methods | Where-Object Name -eq $name);Require ($result.Count -eq 1 -and $result[0].HasBody) "method $($type.Name).$name";$result[0]}
function Calls($method){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]} | ForEach-Object {$_.Operand.FullName})}
function Has($method,[string]$part){Require (@(Calls $method | Where-Object {$_ -like "*$part*"}).Count -gt 0) "$($method.Name) calls $part"}
try {
 $character=ResolveType $native Character
 $human=ResolveType $native Humanoid
 $player=ResolveType $native Player
 foreach($name in 'UseStamina','AddStamina'){
   Require (((Method $character $name).Body.Instructions | ForEach-Object {$_.OpCode.Code}) -join ',' -eq 'Ret') "native Character.$name is stubbed"
 }
 foreach($name in 'GetMaxStamina','GetStaminaPercentage','UseStamina','AddStamina','HaveStamina'){
   Require (@($human.Methods | Where-Object Name -eq $name).Count -eq 0) "Humanoid inherits $name"
 }
 Require (@((Method $player HaveStamina).Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Cgt'}).Count -ge 1) 'player uses strict greater-than stamina availability'
 foreach($name in 'm_baseStamina','m_staminaRegen','m_staminaRegenTimeMultiplier','m_staminaRegenDelay'){
   Require (@($player.Fields | Where-Object {$_.Name -eq $name -and $_.IsPublic -and $_.FieldType.FullName -eq 'System.Single'}).Count -eq 1) "native numerical parameter $name"
 }
 $attack=ResolveType $native Attack
 Has (Method $attack Start) 'Character::HaveStamina'
 Has (Method $attack Update) 'Character::UseStamina'
 Has (Method $attack FireProjectileBurst) 'Character::UseStamina'
 Has (Method $human BlockAttack) 'Character::UseStamina'
 Has (Method $human BlockAttack) 'Character::AddStamina'
 # Native item operations exist for Humanoid; no Player impersonation required.
 Has (Method $human EquipItem) 'Inventory::ContainsItem'
 foreach($name in 'UnequipItem','HideHandItems','ShowHandItems'){ $null=Method $human $name }
 $provider=ResolveType $candidate HumanResources
 $allCalls=@($provider.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
 Require (@($allCalls | Where-Object {$_ -match 'MoveTo|SetMoveDir|SetTarget|StartAttack|EquipItem|SetHealth|Heal\(|ClaimOwnership|SetFollowTarget|FindObjects|OverlapSphere|GetAllCharacters'}).Count -eq 0) 'resource provider owns no movement, attacks, gear, healing or scans'
 Has (Method $provider Use) 'HumanResourcePool::Use'
 Has (Method $provider Use) 'HumanResources::Persist'
 Has (Method $provider Add) 'HumanResourcePool::Add'
 Has (Method $provider Update) 'SEMan::ModifyStaminaRegen'
 Has (Method $provider ReadyOwner) 'ZNetView::IsOwner'
 Has (Method $provider ReadyOwner) 'HumanResourcePool::TryRestore'
 Has (Method $provider Persist) 'ZNetView::IsOwner'
 Has (Method $provider Persist) 'ZDO::Set(System.String,System.String)'
 $patchStrings=@((Method $provider InstallPatches).Body.Instructions | Where-Object {$_.OpCode.Code.ToString() -eq 'Ldstr'} | ForEach-Object Operand)
 foreach($name in 'GetMaxStamina','GetStaminaPercentage','HaveStamina','UseStamina','AddStamina'){
   Require (@($patchStrings | Where-Object {$_ -eq $name}).Count -eq 1) "one scoped $name hook"
 }
 Require (@($patchStrings | Where-Object {$_ -in 'Start','Update','OnAttackTrigger','DoAttack','BlockAttack'}).Count -eq 0) 'no duplicate attack/block debit hook'
 Has (Method (ResolveType $candidate NativeHumanFactory) Create) 'AddComponent<LivingWorld.HumanNpc.HumanResources>'
 Has (Method (ResolveType $candidate ManualTestPlugin) Awake) 'HumanResources::InstallPatches'
 Require (@($candidate.MainModule.AssemblyReferences | Where-Object {$_.Name -match 'RRR|MobAI|Reclamation|LivingVillage|Souls'}).Count -eq 0) 'core human has no legacy/village/content-pack assembly dependency'
 "ResourceIntegration PASS: $checks native API, ownership and scope checks. Gameplay NOT RUN."
} finally {$candidate.Dispose();$native.Dispose()}
