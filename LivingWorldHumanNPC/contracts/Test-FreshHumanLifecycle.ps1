param(
    [string]$AssemblyPath='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll',
    [string]$CecilPath='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\independent-core\tools\Mono.Cecil.dll'
)
# Read-only native IL contract. This deliberately does not execute Unity code.
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $AssemblyPath).Path)
$checks=0
function Require([bool]$condition,[string]$message){
    if(-not $condition){throw "FreshHumanLifecycle FAIL: $message"}
    $script:checks++
}
function NativeType([string]$name){
    $type=$assembly.MainModule.Types | Where-Object FullName -eq $name
    if($null -eq $type){throw "Native type missing: $name"}
    $type
}
function Method([string]$type,[string]$name){
    $result=@((NativeType $type).Methods | Where-Object Name -eq $name)
    if($result.Count -ne 1 -or -not $result[0].HasBody){throw "Method missing/ambiguous: $type.$name"}
    $result[0]
}
function Body([string]$type,[string]$name){
    ((Method $type $name).Body.Instructions | ForEach-Object ToString) -join "`n"
}
function Has([string]$type,[string]$method,[string]$token){
    Require ((Body $type $method).Contains($token)) "$type.$method requires $token"
}
try {
    foreach($type in @('ZNetView','ZSyncTransform','ZSyncAnimation','VisEquipment','CharacterAnimEvent')){
        $method=Method $type Awake
        Require ($method.IsPrivate -and -not $method.IsVirtual) "$type.Awake private nonvirtual; no supported base-call shim"
    }
    foreach($type in @('Character','Humanoid','BaseAI','MonsterAI')){
        $method=Method $type Awake
        Require ($method.IsFamily -and $method.IsVirtual) "$type.Awake protected virtual"
    }
    foreach($type in @('ZNetView','ZSyncTransform','ZSyncAnimation','Humanoid','MonsterAI','VisEquipment')){
        Require (@((NativeType $type).CustomAttributes | Where-Object {$_.AttributeType.Name -eq 'DefaultExecutionOrder'}).Count -eq 0) "$type ordering must come from native MonoScript metadata, not managed attribute"
    }
    Has ZNetView Awake 'ZDOMan::CreateNewZDO'
    Has ZNetView Awake 'ZNetScene::AddInstance'
    Has ZNetView Awake 'ZNetView::m_forceDisableInit'
    Has ZSyncTransform Awake 'ZNetView::GetZDO'
    Has ZSyncTransform Awake 'UnityEngine.Behaviour::set_enabled'
    Has ZSyncAnimation Awake 'GetComponentInChildren<UnityEngine.Animator>'
    Has ZSyncAnimation Awake 'ZNetView::Register<System.String>'
    foreach($type in @('UnityEngine.CapsuleCollider','UnityEngine.Rigidbody','ZSyncAnimation','ZNetView','BaseAI')){
        Has Character Awake ('GetComponent<'+$type+'>')
    }
    Has Character Awake 'ldstr "Visual"'
    Has Character Awake 'GetComponent<CharacterAnimEvent>'
    Has Character Awake 'SEMan::.ctor'
    Has Character Awake 'Character::s_characters'
    Has Humanoid Awake 'Character::Awake'
    Has Humanoid Awake 'GetComponent<VisEquipment>'
    Has Humanoid Start 'Humanoid::GiveDefaultItems'
    foreach($field in @('m_defaultItems','m_randomWeapon','m_randomArmor','m_randomShield','m_randomSets','m_randomItems')){
        Require (-not (Body Humanoid '.ctor').Contains('Humanoid::'+$field)) "Humanoid constructor leaves $field uninitialized: factory must set nonnull"
        Has Humanoid GiveDefaultItems ('Humanoid::'+$field)
    }
    Has MonsterAI Awake 'BaseAI::Awake'
    Has MonsterAI Awake 'ZSyncAnimation::SetBool'
    Has BaseAI Awake 'Character::m_onDamaged'
    Has BaseAI Awake 'Character::m_onDeath'
    Has BaseAI Awake 'ZNetView::Register<System.Boolean,System.Int32>'
    Has BaseAI Awake 'UnityEngine.MonoBehaviour::InvokeRepeating'
    Has CharacterAnimEvent Awake 'GetComponentInParent<Character>'
    Has CharacterAnimEvent Awake 'GetComponent<MonsterAI>'
    Has VisEquipment Awake 'ZNetView::IsOwner'
    Has VisEquipment Awake 'System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Transform>::Add'
    Has VisEquipment SetHairItem 'ZDO::Set(System.Int32,System.Int32,System.Boolean)'
    Has VisEquipment SetBeardItem 'ZDO::Set(System.Int32,System.Int32,System.Boolean)'
    Has VisEquipment UpdateEquipmentVisuals 'ZDO::GetInt(System.Int32,System.Int32)'
    Has VisEquipment UpdateEquipmentVisuals 'VisEquipment::SetHairEquipped'
    Has VisEquipment UpdateEquipmentVisuals 'VisEquipment::SetBeardEquipped'
    Has FootStep Start 'Character::m_onLand'
    Has FootStep Start 'ZNetView::Register'
    Has EffectList '.ctor' 'System.Array::Empty<EffectList/EffectData>'
    Require (-not (Body Character '.ctor').Contains('Character::m_weakSpots')) 'Fresh weak-spot array starts null'
    Has Character GetWeakSpot 'WeakSpot[] Character::m_weakSpots'
    Has Character GetWeakSpot 'ldlen'
    foreach($register in (NativeType ZNetView).Methods | Where-Object Name -eq Register){
        $registerBody=($register.Body.Instructions | ForEach-Object ToString) -join "`n"
        Require ($registerBody.Contains('System.Collections.Generic.Dictionary`2<System.Int32,RoutedMethodBase>::Add')) 'Every RPC Register overload rejects duplicate IDs: never replay Awake'
    }
    Write-Output "FreshHumanLifecycle PASS: $checks static IL/field/lifecycle checks. Unity Awake/Start execution NOT RUN."
    Write-Output ('Native assembly SHA256: '+(Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash)
} finally {$assembly.Dispose()}
