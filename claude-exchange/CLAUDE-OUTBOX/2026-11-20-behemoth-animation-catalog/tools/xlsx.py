import os
WORK = os.environ.get('LWC_ANIM_WORK', '.')
import json
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.utils import get_column_letter

C = json.load(open(os.path.join(WORK, 'catalog.json')))
idx = {}
for o in C:
    idx.setdefault((o['Vendor'], o['Clip']), []).append(o)

def get(vendor, clip, rm=None):
    cands = idx.get((vendor, clip), [])
    for o in cands:
        if rm is None or o['Root motion'] == rm: return o
    for o in cands:
        if 'Inplace' not in o['Folder'] and not o['File'].upper().endswith('_S.FBX'): return o
    raise KeyError((vendor, clip))

HDR = PatternFill('solid', fgColor='EDEDED')
wb = Workbook()

# ---------------- Behemoth Set ----------------
ws = wb.active; ws.title = 'Behemoth Set'
NATIVE = {  # from native-brute-controller-clips.json (GoblinBruteAnimator, installed Valheim)
 'Idle1': 8.25, 'Idle2': 8.333, 'Walk1': 2.5, 'WalkAlerted': 2.533, 'Attack1': 1.875, 'Attack2': 2.708,
 'AttackRage': 5.833, 'Taunt': 4.167, 'Stagger': 1.458, 'Swimming': 4.533, 'Treading Water': 3.0}
REC = [
 # slot, role, pick-rank, vendor, clip, rm, why
 ('Idle1', 'Main idle loop', 'Primary', 'RamsterZ', 'GiantGolem_Idle', None, 'Club-holding giant idle authored for this body type; long 13 s loop avoids visible repetition.'),
 ('Idle1', 'Main idle loop', 'Alt', 'Kevin Iglesias', 'Giant@Idle01', None, 'Clean 2.7 s heavy-breathing loop; Humanoid, ready now.'),
 ('Idle1', 'Main idle loop', 'Alt', 'HQA3', '4_GIANT_Idle_C', None, 'Combat-ready giant idle; needs Generic->Humanoid re-import first.'),
 ('Idle2', 'Idle variety', 'Primary', 'RamsterZ', 'GiantGolem_Idle_LookingAround01', None, 'Scans surroundings - reads as a guardian watching its hoard.'),
 ('Idle2', 'Idle variety', 'Alt', 'RamsterZ', 'GiantGolem_Idle_CheckLeftSide', None, 'Pair with CheckRightSide for directional checks.'),
 ('Idle2', 'Idle variety', 'Alt', 'RamsterZ', 'GiantGolem_Idle_Restless', None, 'Agitated shift - good while alerted but no target.'),
 ('Idle2', 'Idle variety', 'Alt', 'Kevin Iglesias', 'Giant@Idle01 - Action01', None, 'One-shot idle break, 3.3 s.'),
 ('Walk1', 'Walk', 'Primary', 'RamsterZ', 'GiantGolem_Move_Walk_Forward', None, 'Slowest gait in the whole catalog (about 0.45 hip-heights/s): the heaviest-reading walk. Has root motion.'),
 ('Walk1', 'Walk', 'Alt', 'Kevin Iglesias', 'Giant@Walk01 - Forward', 'In-place', 'About 0.87 hip-heights/s; has an [RM] twin. Includes 8-way strafes/backpedal.'),
 ('Walk1', 'Walk', 'Alt', 'wemakethegame', 'move_walk_front', 'Root motion', 'About 0.77 hip-heights/s; root-motion and in-place versions; 8-way.'),
 ('Walk1', 'Walk', 'Alt', 'HQA3', '4_GIANT_Walk_F', None, 'In-place; foot-contact estimate about 0.45 hip-heights/s (as heavy as RamsterZ). Needs re-import.'),
 ('WalkAlerted', 'Chase gait', 'Primary', 'RamsterZ', 'GiantGolem_Move_Run_Forward', None, 'Heavy jog about 1.05 hip-heights/s; same rig/style as the walk, so the blend matches.'),
 ('WalkAlerted', 'Chase gait', 'Alt', 'Kevin Iglesias', 'Giant@Run01 - Forward', 'In-place', 'About 1.75 hip-heights/s; [RM] twin and strafes available.'),
 ('WalkAlerted', 'Chase gait', 'Alt', 'wemakethegame', 'move_run_front', 'Root motion', 'About 2.65 hip-heights/s - fast; may look light for a giant.'),
 ('Attack1', 'Basic club strike', 'Primary', 'RamsterZ', 'GiantGolem_Attack_Swing_Low01_Quick', None, 'Club-authored swing (hand grip matches a club). Fastest limb right hand at about 1.07 s. Untick loop.'),
 ('Attack1', 'Basic club strike', 'Alt', 'Explosive', 'Attack3', 'Root motion', '1.8 s, closest to the native 1.875 s; ships with a Hit event at the impact frame. Lighter, human-brute feel.'),
 ('Attack1', 'Basic club strike', 'Alt', 'wemakethegame', 'attack03', 'Root motion', 'Right-hand overhead; body drops 0.43 hip-heights into the blow.'),
 ('Attack2', 'Second strike', 'Primary', 'RamsterZ', 'GiantGolem_Attack_Swing_TurnAround_Smash', None, 'Turn-around smash - punishes players circling behind. Right hand at about 1.7 s.'),
 ('Attack2', 'Second strike', 'Alt', 'RamsterZ', 'GiantGolem_Attack_Swing_Behind_Low01', None, 'Rear low sweep.'),
 ('Attack2', 'Second strike', 'Alt', 'RamsterZ', 'GiantGolem_Attack_Stomp01', None, 'Stomp - no weapon contact needed; pairs with ground-impact VFX.'),
 ('Attack2', 'Second strike', 'Alt', 'wemakethegame', 'attack_whirlwind', 'Root motion', 'Spin attack, 3.6 s.'),
 ('AttackRage', 'Rage / heavy', 'Primary', 'RamsterZ', 'GiantGolem_Attack_Swing_Forward_PowerSmash', None, '6.2 s vs native 5.8 s - nearest duration match; travels about 1.9 hip-heights into the smash. Right hand at about 3.1 s.'),
 ('AttackRage', 'Rage / heavy', 'Alt', 'RamsterZ', 'GiantGolem_Attack_Swing_SmashDown01', None, 'Overhead smash-down.'),
 ('AttackRage', 'Rage / heavy', 'Alt', 'wemakethegame', 'attack_ground01', 'Root motion', 'Ground slam; body drops 0.47 hip-heights.'),
 ('AttackRage', 'Rage / heavy', 'Alt', 'wemakethegame', 'attack_jumpAtk', 'Root motion', 'Leap attack: body rises a full hip-height.'),
 ('AttackRage', 'Rage / heavy', 'Alt', 'HQA3', '4_GIANT_Skill_01', None, 'Vendor-unnamed skill; measured leap (rises 0.91). Preview needed; re-import needed.'),
 ('Taunt', 'Taunt / roar', 'Primary', 'RamsterZ', 'GiantGolem_Idle_Roar01', None, 'Giant roar, 5.3 s (native taunt 4.2 s).'),
 ('Taunt', 'Taunt / roar', 'Alt', 'RamsterZ', 'GiantGolem_Idle_Roar02', None, '6.0 s variant.'),
 ('Taunt', 'Taunt / roar', 'Alt', 'Kevin Iglesias', 'Giant@Spawn01', None, 'Rise-and-display, 1.9 s.'),
 ('Stagger', 'Stagger', 'Primary', 'Kevin Iglesias', 'Giant@Damage02', None, '1.07 s - closest to the native 1.46 s stagger window.'),
 ('Stagger', 'Stagger', 'Alt', 'RamsterZ', 'GiantGolem_Stagger_Back01', None, 'Directional set (Back/Forward/Left/Right x2) - heavier, but 4-5 s is far longer than native.'),
 ('Stagger', 'Stagger', 'Alt', 'wemakethegame', 'hit_shoulder_left', 'Root motion', '1.5 s shoulder knock with real root travel (v1.45 fix).'),
 ('Swimming', 'Swim', 'Keep native', None, None, None, 'No pack has a swim clip. Keep the native player-derived Swimming clip.'),
 ('Treading Water', 'Tread water', 'Keep native', None, None, None, 'No pack has one. Keep native.'),
 ('NEW: charge/roll', 'Special travelling attack', 'Candidate', 'wemakethegame', 'attack_DashAtk', 'Root motion', 'Charging attack that travels about 6.7 hip-heights in 4 s - a rush to sit beside the planned Player Dive roll.'),
 ('NEW: charge/roll', 'Special travelling attack', 'Candidate', 'Explosive', 'MoveAttack1', 'Root motion', 'Travels about 7.7 hip-heights; ships with a Hit event.'),
 ('NEW: recovery', 'Post-roll recovery', 'Candidate', 'wemakethegame', 'Stun_End', 'Root motion', '1.6 s recover-to-stand: fits "brief recovery, fast reacquisition".'),
 ('NEW: recovery', 'Post-roll recovery', 'Candidate', 'wemakethegame', 'rise01', 'Root motion', '4.3 s full get-up if the roll ends grounded.'),
 ('NEW: turn', 'Turn in place', 'Candidate', 'RamsterZ', 'GiantGolem_Move_Turn_Left90', None, 'Heavy pivot (45/90/180 each way). Native controller has no turn clips.'),
 ('NEW: grab', 'Grab & throw', 'Candidate', 'RamsterZ', 'GiantGolem_Attack_PickUpBiteAndThrow', None, '12.3 s pick-up, bite and throw - an execution-style grab.'),
 ('NEW: throw', 'Rock throw', 'Candidate', 'Kevin Iglesias', 'Giant@ThrowObject01_A ThrowRockScript EVENT', None, 'Ships with SpawnRock/GrabRock/ThrowRock/RemoveRock events already timed.'),
 ('Story: Neck captive', 'Bound by shaman', 'Candidate', 'RamsterZ', 'GiantGolem_Chained_Idle', None, 'Chained idle - the enslaved-hoard-guard look from your lore.'),
 ('Story: Neck captive', 'Bound by shaman', 'Candidate', 'RamsterZ', 'GiantGolem_Chained_Stressed', None, 'Straining against the binding.'),
 ('Story: Neck captive', 'Binding breaks', 'Candidate', 'RamsterZ', 'GiantGolem_Chained_BreakFree', None, '17.7 s break-free - a set piece for when the binding fails.'),
 ('Death (optional)', 'Death', 'Candidate', 'RamsterZ', 'GiantGolem_CriticalHit_Kneel_Death01', None, 'Kneel-then-fall. The native controller has no death clip, so this only applies if you add a death state.'),
 ('Death (optional)', 'Death', 'Candidate', 'Kevin Iglesias', 'Giant@Death01_A', None, '2.1 s forward fall.'),
]
cols = ['Behemoth slot', 'Pick', 'Clip', 'Pack', 'Behemoth-ready', 'Seconds', 'Native clip (s)', 'Root motion',
        'Speed (hip-heights/s)', 'Fastest limb (hit candidate)', 'Fastest-limb time (s)', 'Why', 'Role', 'Vendor', 'File']
r0 = 1
ws.cell(r0, 1, 'Behemoth animation set - recommended picks from packs you own (preview each in the Workshop before committing)').font = Font(bold=True, size=12)
ws.cell(r0 + 1, 1, 'Native = GoblinBruteAnimator clip the Behemoth uses today (installed Valheim, read 2026-11-06). An override controller swaps clips inside these existing states; NEW rows need new states.')
hr = r0 + 3
for j, c in enumerate(cols, 1):
    ws.cell(hr, j, c).fill = HDR
r = hr + 1
for slot, role, pick, vend, clip, rm, why in REC:
    if vend:
        o = get(vend, clip, rm)
        sp = o['Speed (hip-heights/s)'] or o.get('Ground speed from foot contact (hip-h/s)') or None
        vals = [slot, pick, clip, o['Pack'], o['Behemoth-ready'].split(' - ')[0], o['Seconds'], NATIVE.get(slot),
                o['Root motion'], sp if o['Category'] == 'Locomotion' else None,
                o['Fastest limb (hit candidate)'] or None, o['Fastest-limb time (s)'] or None, why, role, vend, o['File']]
    else:
        vals = [slot, pick, '(keep native)', None, None, None, NATIVE.get(slot), None, None, None, None, why, role, None, None]
    for j, v in enumerate(vals, 1): ws.cell(r, j, v)
    r += 1
widths = [18, 11, 44, 30, 10, 9, 10, 12, 11, 14, 11, 70, 22, 15, 40]
for j, w in enumerate(widths, 1): ws.column_dimensions[get_column_letter(j)].width = w
for row in ws.iter_rows(min_row=hr + 1, max_row=r, min_col=12, max_col=12):
    for c in row: c.alignment = Alignment(wrap_text=True, vertical='top')
ws.freeze_panes = ws.cell(hr + 1, 4)

# ---------------- category sheets ----------------
ORDER = ['Clip', 'Type', 'Family', 'Behemoth-ready', 'Behemoth slot', 'Seconds', 'Root motion',
         'Travel (hip-heights)', 'Speed (hip-heights/s)', 'Ground speed from foot contact (hip-h/s)',
         'Body rise (hip-h)', 'Body drop (hip-h)', 'Fastest limb (hit candidate)', 'Fastest-limb time (s)',
         'Fastest-limb % of clip', 'Loop (authored)', 'Frames @30', 'Rig (shipped)', 'Notes', 'Vendor', 'Pack', 'File', 'Folder']
FAMORD = {'Giant': 0, 'Giant / Golem (club)': 1, 'Golem': 2, 'Ogre': 3, 'Brute (heavy humanoid)': 4, 'Troll (own rig)': 5,
          'Goblin (small; donor only)': 6, 'Shaman (for Neck shaman, not Behemoth)': 7}
def sheet(name, pred, cols=ORDER, typesort=True):
    s = wb.create_sheet(name)
    rows = [o for o in C if pred(o)]
    rows.sort(key=lambda o: (o['Type'] if typesort else '', FAMORD.get(o['Family'], 9), o['Pack'], o['Clip'], o['Root motion']))
    for j, c in enumerate(cols, 1): s.cell(1, j, c).fill = HDR
    for i, o in enumerate(rows, 2):
        for j, c in enumerate(cols, 1):
            v = o.get(c)
            if v == '': v = None
            if c == 'Behemoth-ready' and v: v = v.split(' - ')[0]
            s.cell(i, j, v)
    w = {'Clip': 44, 'Type': 24, 'Family': 22, 'Behemoth-ready': 10, 'Behemoth slot': 16, 'Notes': 60, 'Pack': 30, 'File': 40, 'Folder': 50}
    for j, c in enumerate(cols, 1): s.column_dimensions[get_column_letter(j)].width = w.get(c, 12)
    s.freeze_panes = 'B2'
    return len(rows)

n = {}
n['Attacks'] = sheet('Attacks', lambda o: o['Category'] == 'Attack')
n['Idles'] = sheet('Idles', lambda o: o['Category'] == 'Idle')
n['Locomotion'] = sheet('Locomotion', lambda o: o['Category'] == 'Locomotion')
n['Reactions & Death'] = sheet('Reactions & Death', lambda o: o['Category'] in ('Reaction', 'Death', 'Defense'))
n['Taunt Story Ambient'] = sheet('Taunt Story Ambient', lambda o: o['Category'] in ('Taunt', 'Story', 'Ambient', 'Other'))

# ---------------- Speed match ----------------
s = wb.create_sheet('Speed Match')
s['A1'] = 'Locomotion speed matching - stops the "sneaking" / foot-sliding look'; s['A1'].font = Font(bold=True, size=12)
s['A2'] = 'Fill the two yellow inputs from the Behemoth prefab. Playback speed = creature speed / (clip speed in hip-heights/s x hip height).'
s['A4'] = 'Behemoth hip height (m)'; s['B4'] = None
s['A5'] = 'Behemoth walk speed (m/s)'; s['B5'] = None
s['A6'] = 'Behemoth run speed (m/s)'; s['B6'] = None
for c in ('B4', 'B5', 'B6'): s[c].fill = PatternFill('solid', fgColor='FFF2CC')
s['A7'] = 'Inputs are not known from the files read here - take them from the installed prefab (Character walk/run speed) and the scaled model.'
hdr = ['Clip', 'Gait', 'Pack', 'Root motion', 'Clip speed (hip-heights/s)', 'Speed source', 'Playback multiplier', 'Seconds', 'Behemoth-ready']
for j, c in enumerate(hdr, 1): s.cell(9, j, c).fill = HDR
loco = [o for o in C if o['Category'] == 'Locomotion' and o['Type'] in ('Walk forward', 'Run forward') and 'Shaman' not in o['Family'] and 'Goblin' not in o['Family']]
def _sp(o):
    return o['Speed (hip-heights/s)'] if (o['Root motion'] == 'Root motion' and o['Speed (hip-heights/s)']) else (o['Ground speed from foot contact (hip-h/s)'] or 9)
loco.sort(key=lambda o: (o['Type'], _sp(o)))
rr = 10
for o in loco:
    if o['Root motion'] == 'Root motion' and o['Speed (hip-heights/s)']:
        sp, src = o['Speed (hip-heights/s)'], 'measured root travel'
    else:
        sp, src = o['Ground speed from foot contact (hip-h/s)'], 'foot-contact estimate'
    gait = 'Walk' if o['Type'].startswith('Walk') else 'Run'
    vals = [o['Clip'], gait, o['Pack'], o['Root motion'], sp, src, None, o['Seconds'], o['Behemoth-ready'].split(' - ')[0]]
    for j, v in enumerate(vals, 1): s.cell(rr, j, v)
    spdcell = '$B$5' if gait == 'Walk' else '$B$6'
    s.cell(rr, 7, f'=IF(OR({spdcell}="",$B$4="",E{rr}=0),"",{spdcell}/(E{rr}*$B$4))')
    s.cell(rr, 7).number_format = '0.00'
    rr += 1
for j, w in enumerate([44, 8, 32, 12, 14, 22, 14, 9, 10], 1): s.column_dimensions[get_column_letter(j)].width = w
s.column_dimensions['A'].width = 44

# ---------------- Packs ----------------
p = wb.create_sheet('Packs')
ph = ['Pack', 'Vendor', 'Family', 'Clips', 'Rig as shipped', 'Behemoth-ready', 'Root-motion options', 'Notes', 'Path on your PC']
for j, c in enumerate(ph, 1): p.cell(1, j, c).fill = HDR
PACKINFO = [
 ('Giant Golem', 'RamsterZ', 'Unreal-style skeleton (root/pelvis/spine_01..)', 'Humanoid', 'Root motion in clips + FullRootMotion folder', 'Club-authored (ships StaticMesh_GiantClub). Every clip is flagged Loop - untick for one-shots. Includes Chained set.', 'RamsterZ/Animation/Giant Golem.unitypackage'),
 ('Giant Animations', 'Kevin Iglesias', 'B-* rig', 'Humanoid', 'In-place + [RM] twins', 'Unarmed giant. A/B variant pairs. Throw clips ship rock events.', 'Kevin Iglesias/Animation/Giant Animations.unitypackage'),
 ('Giant Golem AnimSet', 'wemakethegame', '3ds Max Biped (Bip001)', 'Humanoid', 'Root motion + /Inplace twins (v1.45)', 'PDF: no redistribution/posting of the animations - check before any public release.', 'wemakethegame/Animation/Giant Golem AnimSet.unitypackage'),
 ('Monster Animations - Giant', 'HQA3', '3ds Max Biped (Bip001)', 'Generic', 'In-place only', 'Re-import as Humanoid (Biped auto-maps). Skills are unnamed by vendor - motion measured.', 'HQA3/Animation/Monster Animations Giant.unitypackage'),
 ('Monster Animations - Golem', 'HQA3', '3ds Max Biped (Bip001)', 'Generic', 'In-place (a few skills travel)', 'Largest set: 75 clips. Re-import as Humanoid.', 'HQA3/Animation/Monster Animations Golem.unitypackage'),
 ('Monster Animations - Hunter Ogre', 'HQA3', '3ds Max Biped (Bip001)', 'Generic', 'In-place only', 'Re-import as Humanoid.', 'HQA3/Animation/Monster Animations Hunter Ogre.unitypackage'),
 ('Monster Animations - Shamanic Sorcerer', 'HQA3', '3ds Max Biped (Bip001)', 'Generic', 'In-place only', 'For the Neck shaman, not the Behemoth. Casting = SK_Firing_01..06.', 'HQA3/Animation/Monster Animations Shamanic SorcererThe Wand Wielder.unitypackage'),
 ('Brute Warrior Mecanim Animation Pack', 'Explosive', 'B_* rig', 'Humanoid', 'Root motion + _S stationary twins', 'Ships Hit / FootL / FootR events. Faster, human-scale timing.', 'Explosive/Animation/Brute Warrior Mecanim Animation Pack.unitypackage'),
 ('Creature Cave Troll', 'Silvano Junior', 'CAT rig (cave_troll_*)', 'Generic', 'Root motion', 'Own model only (8 clips in one take). Already used for Thurs own-clips build.', 'Silvano Junior/3D ModelsCharactersCreatures/Creature Cave Troll.unitypackage'),
 ('Goblin Anims', 'RamsterZ', 'Unreal-style skeleton', 'Humanoid', 'Root motion', 'Small-creature timing - donor only (get-ups, evades, worship).', 'RamsterZ/Animation/Goblin Anims.unitypackage'),
]
for i, (pk, v, sk, rig, rmopt, notes, path) in enumerate(PACKINFO, 2):
    cnt = sum(1 for o in C if o['Pack'] == pk)
    ready = {'Humanoid': 'YES', 'Generic': 'CONVERT' if 'Biped' in sk else 'NO'}[rig]
    fam = next((o['Family'] for o in C if o['Pack'] == pk), '')
    for j, val in enumerate([pk, v, fam, cnt, rig + ' - ' + sk, ready, rmopt, notes, '%APPDATA%/Unity/Asset Store-5.x/' + path], 1):
        p.cell(i, j, val)
for j, w in enumerate([36, 15, 26, 7, 40, 10, 34, 70, 80], 1): p.column_dimensions[get_column_letter(j)].width = w

# ---------------- All clips ----------------
n['All Clips'] = sheet('All Clips', lambda o: True, typesort=False)

# ---------------- Method ----------------
m = wb.create_sheet('Method & Limits')
lines = [
 'HOW THIS WAS BUILT',
 'Source: the 10 .unitypackage files in your Unity Asset Store cache, read directly (no Unity import). 505 clip rows.',
 'Clip names, frame ranges, loop flags, rig type and authored events come from each FBX .meta (ModelImporter).',
 'Durations: clip frame range / FBX frame rate (30 fps for all ten packs).',
 'Motion numbers: hip, hand and foot bones evaluated in world space through the full bone chain at 30 fps.',
 'Units: everything is in hip-heights (the pack\'s standing hip height), so packs of different scale compare directly.',
 'Travel / Speed: root displacement start->end. Zero means an in-place clip.',
 'Ground speed from foot contact: speed of the planted foot sliding under the body. Validated against root-motion clips: walks within about 5%; runs under-read 5-35% (airborne phase).',
 'Fastest limb: the hand/foot with the highest body-relative speed and when it peaks. It is a candidate for where to put the Valheim Hit event, NOT a confirmed impact frame - check in preview.',
 'Behemoth native baseline: GoblinBruteAnimator, 11 clips, from your 0.1.29 RageRoll audit (installed Valheim). Native AttackRage events: Speed 0.02 s, Effect 0.53 s, TrailOn 2.17 s, Hit 2.50 s, TrailOff 2.62 s.',
 '',
 'WHAT IS NOT VERIFIED',
 'No clip was played or watched. Recommendations rest on names, timing and measured motion - preview every pick in the Workshop on the real Behemoth before committing.',
 'HQA3 "Skill_xx" clips are unnamed by the vendor; the descriptions are measured motion only.',
 'Generic->Humanoid conversion of the HQA3 packs is expected to work (standard Biped skeleton) but has not been done; retarget quality is unknown.',
 'Root-motion columns read the bone curves; how Unity extracts root motion also depends on import settings (bake into pose flags).',
 'Behemoth hip height and Valheim walk/run speeds were not available in the files read; Speed Match needs them filled in.',
 '',
 'IMPORTANT FOR PORTING',
 'New attack clips need Valheim animation events authored (at least Hit; TrailOn/TrailOff for weapon trails). Only Explosive (Hit/Foot) and Kevin Iglesias throws (rock events) ship any events.',
 'RamsterZ and wemakethegame flag almost every clip as looping - untick Loop Time on one-shots (attacks, reactions, deaths, taunts).',
 'Most giant attacks here are 3-6 s; the native Attack1/Attack2 are 1.9/2.7 s. Longer clips change attack cadence unless state speed is raised.',
 'Licensing: bundling these clips into a public mod is redistribution. The wemakethegame PDF explicitly forbids redistribution/posting; check every pack\'s terms before public upload (not legal advice).',
]
for i, t in enumerate(lines, 1):
    m.cell(i, 1, t)
    if t.isupper(): m.cell(i, 1).font = Font(bold=True)
m.column_dimensions['A'].width = 160

wb.save(os.path.join(WORK, 'Behemoth-Animation-Catalog.xlsx'))
print(n, 'rec rows', len(REC))
