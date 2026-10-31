param(
    [string]$LogPath = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\BepInEx\LogOutput.log"
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) { throw "Missing log: $LogPath" }
$text = Get-Content -LiteralPath $LogPath -Raw
function Need([string]$pattern, [string]$message) {
    if ($text -notmatch $pattern) { throw $message }
}
function Reject([string]$pattern, [string]$message) {
    if ($text -match $pattern) { throw $message }
}
Need 'Loading \[Living World RRRCore current-game compatibility 1\.0\.0\]' 'Compatibility plug-in did not load.'
Need 'Loading \[Spawn That! 1\.2\.20\]' 'Spawn That did not load; natural Souls spawns are inactive.'
Need 'RRR_NPC base rebuilt from the current Player prefab graph' 'RRR_NPC base repair did not execute.'
Need 'SOULS-PREFAB SUMMARY 6/6 ready' 'All six Souls prefabs were not registered.'
Reject 'RRRCore\.npc\.Npc\.DesignNpcBase' 'RRRCore DesignNpcBase still failed.'
Reject 'Unable to find spawn RRRN_' 'Custom Raids still has a missing Souls prefab.'
Write-Output 'WORLD LOAD GATE PASS: Spawn That loaded; Souls prefabs 6/6; no missing raid spawn.'

