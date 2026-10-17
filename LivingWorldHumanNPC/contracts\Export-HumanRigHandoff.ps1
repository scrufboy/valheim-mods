param(
 [string]$EvidenceDirectory='C:\Users\scruf\Documents\Codex\2026-10-13\begin-now-dedicated-independent-valheim-living\work\human-npc\assets',
 [string]$OutputPath=(Join-Path $PSScriptRoot '..\HUMAN-ANIMATION-HANDOFF.json')
)
$ErrorActionPreference='Stop'
$graphPath=Join-Path $EvidenceDirectory 'Player-graph.json'
$animationPath=Join-Path $EvidenceDirectory 'Player-animation-assets.json'
$graph=Get-Content -LiteralPath $graphPath -Raw | ConvertFrom-Json -AsHashtable
$animation=Get-Content -LiteralPath $animationPath -Raw | ConvertFrom-Json -AsHashtable
$events=@($graph.components | Where-Object {$_.type -eq 'CharacterAnimEvent'})[0].data
$animator=@($graph.components | Where-Object {$_.type -eq 'Animator'})[0].data
$names=@{}
foreach($pair in $animation.m_Controller.data.m_TOS){$names[[string]$pair[0]]=$pair[1]}
$parameters=@($animation.m_Controller.data.m_Controller.m_Values.data.m_ValueArray | ForEach-Object {
 [ordered]@{Name=$names[[string]$_.m_ID];NativeType=$_.m_Type;Hash=$_.m_ID}
})
foreach($required in @(@('crouching',4),@('emote_sit',4),@('emote_stop',9))){
 if(@($parameters | Where-Object {$_.Name -ceq $required[0] -and $_.NativeType -eq $required[1]}).Count -ne 1){throw "Native animation parameter changed: $($required[0])"}
}
if(-not $events.m_headRotation -or -not $animation.m_Controller.data.m_Controller.m_LayerArray[0].data.m_IKPass){throw 'Native human head rotation / IK pass unavailable'}
$clips=@($animation.clips | Where-Object {$_.name -match '^(Workbench|hammer_Swing|Sitting idle|Player_Sit_Chair|Wave|Cheer|Kneel Start|Kneel Loop|KneelDeath|Bow|Despair|Standing Up from sit)$'} | ForEach-Object {
 [ordered]@{Name=$_.name;Events=@($_.events | ForEach-Object {[ordered]@{Function=$_.functionName;Time=$_.time}});ForbiddenForSocial=($_.name -eq 'KneelDeath');GameplayWiringVerified=$false}
})
[ordered]@{
 Schema=1;Purpose='Metadata-only human animation handoff; no game meshes, controllers or clips redistributed';
 SourceEvidence=@(@{Path=$graphPath;SHA256=(Get-FileHash -LiteralPath $graphPath).Hash},@{Path=$animationPath;SHA256=(Get-FileHash -LiteralPath $animationPath).Hash});
 Avatar=$animation.m_Avatar.data.m_Name;
 Animator=[ordered]@{ApplyRootMotion=$animator.m_ApplyRootMotion;UpdateMode=$animator.m_UpdateMode;CullingMode=$animator.m_CullingMode;Layers=@($animation.m_Controller.data.m_Controller.m_LayerArray | ForEach-Object {[ordered]@{Index=$_.data.m_StateMachineIndex;IKPass=$_.data.m_IKPass}})};
 HeadLook=[ordered]@{Enabled=$events.m_headRotation;Weight=$events.m_lookWeight;BodyWeight=$events.m_bodyLookWeight;HeadWeight=$events.m_headLookWeight;EyeWeight=$events.m_eyeLookWeight;EyeTransformCount=$events.m_eyes.Count;Clamp=$events.m_lookClamp;Adapter='CharacterAnimEvent.m_lookAt; native smoothing, owner replication and IK retained'};
 SkeletonPaths=@($graph.nodes | Where-Object {$_.path -match '^Player/Visual/Armature/' -and $_.path -notmatch 'ClothCollider|Magica |Breath Particles'} | ForEach-Object {$_.path});
 Parameters=@($parameters | Where-Object {$_.Name -match '^emote_|^crouching$|^crafting$'});
 RelevantClips=$clips;
 AuthoringRules=@('No Player root/controller lifecycle on NPC','No root displacement in new social/work clips','No Die/Hit/OnAttackTrigger event in friendly emote or work loop','Native combat interrupts work/social pose','New clips require enter/loop/exit, cancellation, held-tool alignment and both body-model checks','Never reuse KneelDeath for surrender: Die event is present','No new animations authored by this report');
 Runtime='NOT RUN; static extracted asset metadata only'
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $OutputPath -Encoding utf8
'HumanRigHandoff PASS: native head IK and crouching/emote_sit/emote_stop parameters confirmed; metadata-only handoff generated.'
