param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $AssemblyPath).Path)
$checks=0
function Require([bool]$pass,[string]$reason){if(-not $pass){throw "Formidable shadow boundary FAIL: $reason"};$script:checks++}
try {
    $type=$assembly.MainModule.Types | Where-Object FullName -eq 'LivingWorld.HumanNpc.HumanFormidableShadow'
    Require ($null -ne $type) 'observer component exists'
    Require (-not @($type.CustomAttributes | Where-Object {$_.AttributeType.FullName -like 'HarmonyLib.HarmonyPatch*'}).Count) 'observer has no Harmony patch'
    foreach($name in @('Update','Describe','Observe')){Require (@($type.Methods|Where-Object Name -eq $name).Count -eq 1) "observer has one $name method"}

    $forbidden=@(
        'ZDO::Set','ZNetView::ClaimOwnership','ZNetView::RequestOwn',
        'BaseAI::MoveTo','BaseAI::StopMoving','BaseAI::SetTargetInfo','MonsterAI::SetTarget',
        'MonsterAI::DoAttack','MonsterAI::SetFollowTarget','Character::SetMoveDir','Character::SetRun',
        'Character::Jump','Character::Dodge','Character::SetBlocking','BaseAI::LookAt',
        'Humanoid::StartAttack','Humanoid::EquipItem','UnityEngine.Transform::set_position',
        'UnityEngine.Transform::set_rotation','UnityEngine.Rigidbody::MovePosition','UnityEngine.Rigidbody::MoveRotation'
    )
    $calls=@()
    foreach($method in @($type.Methods|Where-Object HasBody)){
        foreach($instruction in $method.Body.Instructions){
            if($instruction.Operand -is [Mono.Cecil.MethodReference]){$calls += $instruction.Operand.FullName}
            if($instruction.OpCode.Name -eq 'stfld' -and $instruction.Operand -is [Mono.Cecil.FieldReference]){
                $owner=$instruction.Operand.DeclaringType.FullName
                Require ($owner -eq $type.FullName -or
                    $owner -eq 'LivingWorld.Companions.FormidableAI.Classes.ClassCombatFacts' -or
                    $owner -eq 'LivingWorld.FormidableAI.Integration.FormidableIntegrationInput') "observer writes only its state or new observation DTOs: $($instruction.Operand.FullName)"
            }
        }
    }
    foreach($name in $forbidden){Require (-not @($calls|Where-Object {$_ -like "*$name*"}).Count) "observer never calls $name"}
    Require (@($calls|Where-Object {$_ -like '*BaseAI::GetTargetCreature*'}).Count -gt 0) 'observer reads public native target'
    Require (@($calls|Where-Object {$_ -like '*CombatArbitrator*' -or $_ -like '*FormidableIntegration::Evaluate*'}).Count -gt 0) 'observer evaluates Formidable arbitration'
    Require (@($calls|Where-Object {$_ -like '*BoundedShadowTrace::Add*'}).Count -gt 0) 'observer records bounded shadow trace'

    $factory=$assembly.MainModule.Types | Where-Object FullName -eq 'LivingWorld.HumanNpc.NativeHumanFactory'
    $create=@($factory.Methods|Where-Object Name -eq 'Create')
    Require ($create.Count -eq 1 -and $create[0].HasBody) 'factory Create body exists'
    $factoryBody=($create[0].Body.Instructions|ForEach-Object ToString)-join "`n"
    Require ($factoryBody.Contains('AddComponent<LivingWorld.HumanNpc.HumanFormidableShadow>')) 'factory attaches exactly one observer component'

    $plugin=$assembly.MainModule.Types | Where-Object FullName -eq 'LivingWorld.ManualTest.ManualTestPlugin'
    $awake=@($plugin.Methods|Where-Object Name -eq 'Awake')
    Require ($awake.Count -eq 1 -and $awake[0].HasBody) 'plugin Awake body exists'
    $awakeBody=($awake[0].Body.Instructions|ForEach-Object ToString)-join "`n"
    Require ($awakeBody.Contains('ShadowTelemetry')) 'observer config key is compiled'
    Require ($awakeBody.Contains('ldc.i4.0')) 'observer config includes a default-false constant'

    "FormidableShadowBoundary PASS: $checks compiled read-only/default-off checks. Unity observation NOT RUN."
} finally {$assembly.Dispose()}
