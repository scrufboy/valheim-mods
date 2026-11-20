import os
WORK = os.environ.get('LWC_ANIM_WORK', '.')
import json, re, sys, statistics
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
T = json.load(open(os.path.join(WORK, 'traj.json')))
rows = json.load(open(os.path.join(WORK, 'rows.json')))

PACKS = {
 'Monster_Animations_Giant':      ('HQA3', 'Monster Animations - Giant', 'Giant'),
 'Monster_Animations_Golem':      ('HQA3', 'Monster Animations - Golem', 'Golem'),
 'Monster_Animations_Hunter_Ogre':('HQA3', 'Monster Animations - Hunter Ogre', 'Ogre'),
 'Monster_Animations_Shamanic_SorcererThe_Wand_Wielder': ('HQA3', 'Monster Animations - Shamanic Sorcerer', 'Shaman (for Neck shaman, not Behemoth)'),
 'Giant_Animations':              ('Kevin Iglesias', 'Giant Animations', 'Giant'),
 'Giant_Golem':                   ('RamsterZ', 'Giant Golem', 'Giant / Golem (club)'),
 'Giant_Golem_AnimSet':           ('wemakethegame', 'Giant Golem AnimSet', 'Golem'),
 'Brute_Warrior_Mecanim_Animation_Pack': ('Explosive', 'Brute Warrior Mecanim Animation Pack', 'Brute (heavy humanoid)'),
 'Creature_Cave_Troll':           ('Silvano Junior', 'Creature Cave Troll', 'Troll (own rig)'),
 'Goblin_Anims':                  ('RamsterZ', 'Goblin Anims', 'Goblin (small; donor only)'),
}
READY = {
 'Humanoid': 'YES - Humanoid, retargets to Behemoth avatar now',
 'GenericBiped': 'CONVERT - Generic as shipped; 3ds Max Biped skeleton, re-import as Humanoid',
 'GenericOwn': 'NO - Generic custom (CAT) rig; plays on its own troll model only',
}

def readiness(pk, rig):
    if rig == 'Humanoid': return READY['Humanoid']
    if pk.startswith('Monster_Animations'): return READY['GenericBiped']
    if pk == 'Creature_Cave_Troll': return READY['GenericOwn']
    return 'CHECK - ' + str(rig)

def classify(name):
    n = name.lower().replace(' ', '_')
    if re.search(r't[-_]?pose', n): return ('Reference', 'T-pose', None)
    if 'deadbody' in n or 'down_deadbody' in n: return ('Death', 'Corpse pose (1 frame)', 'Death')
    if re.search(r'die|death|dead0', n): return ('Death', 'Death fall', 'Death')
    if re.search(r'attack', n) and 'smashdown' in n: return ('Attack', 'Overhead smash-down', 'AttackRage')
    if 'passout' in n: return ('Reaction', 'Pass-out / knockout', 'Recovery')
    if re.search(r'get_?up|rise0|revive', n): return ('Reaction', 'Get-up / recovery', 'Recovery')
    if re.search(r'down_loop', n): return ('Reaction', 'Knocked-down loop', 'Recovery')
    if re.search(r'(^|_)down(_|$)|down0', n): return ('Reaction', 'Knock-down', 'Stagger')
    if 'stun' in n: return ('Reaction', 'Stun', 'Stagger')
    if 'stagger' in n: return ('Reaction', 'Stagger', 'Stagger')
    if 'criticalhit' in n: return ('Reaction', 'Critical hit / kneel', 'Stagger')
    if re.search(r'dmg|damage|hit_(front|back|left|right|shoulder)|lighthit|take_damage|blockhitreact', n):
        return ('Reaction', 'Hit react' + (' (upper body)' if 'upper' in n else ''), 'Stagger')
    if 'blockbreak' in n: return ('Reaction', 'Guard broken', 'Stagger')
    if re.search(r'block|defence', n): return ('Defense', 'Block / guard', None)
    if 'chained' in n: return ('Story', 'Chained / bound', 'Story: Neck captive')
    if re.search(r'worship|eat_|sleep|sit_|interact_kneel', n): return ('Ambient', 'Ambient activity', 'Idle variety')
    if re.search(r'pickup(?!bite)|pick_up|pickupclub', n): return ('Ambient', 'Pick up / handle object', 'Idle variety')
    if re.search(r'taunt|roar|win0|lose0|spawn', n): return ('Taunt', 'Taunt / roar / display', 'Taunt')
    if 'idle' in n and 'combat_special' in n: return ('Idle', 'Combat idle variant', 'Idle2')
    if 'idle' in n and 'action' in n: return ('Idle', 'Idle break (one-shot)', 'Idle2')
    if re.search(r'idle_c(_|$)', n): return ('Idle', 'Combat-ready idle', 'Idle1')
    if re.search(r'idle_p', n): return ('Idle', 'Passive idle / idle break', 'Idle2')
    if 'idle' in n or 'idle_break' in n: return ('Idle', 'Idle', 'Idle1')
    if re.search(r'atk|attack|skill|smash|stomp|punt|swing|throw|whirlwind|firing|ambush', n):
        if re.search(r'throw|firing|range', n): return ('Attack', 'Throw / ranged', 'Attack2')
        if 'bite' in n: return ('Attack', 'Grab & bite & throw', 'AttackRage')
        if re.search(r'stomp|foot', n): return ('Attack', 'Stomp / kick', 'Attack2')
        if 'punt' in n: return ('Attack', 'Kick / punt', 'Attack2')
        if 'ground' in n: return ('Attack', 'Ground slam', 'AttackRage')
        if re.search(r'jump', n): return ('Attack', 'Leap attack', 'AttackRage')
        if re.search(r'dash|moveattack', n): return ('Attack', 'Charging attack (travels)', 'Special: charge/roll slot')
        if re.search(r'whirlwind|turnaround|behind', n): return ('Attack', 'Spin / rear sweep', 'Attack2')
        if re.search(r'combo|3swing', n): return ('Attack', 'Multi-hit combo', 'AttackRage')
        if 'low' in n: return ('Attack', 'Low sweep', 'Attack2')
        if re.search(r'smash|power|special', n): return ('Attack', 'Heavy power smash', 'AttackRage')
        if 'ambush' in n: return ('Attack', 'Ambush leap', None)
        if 'unarmed' in n: return ('Attack', 'Unarmed strike', 'Attack1')
        if 'skill' in n: return ('Attack', 'Skill (vendor-unnamed)', None)
        return ('Attack', 'Melee strike', 'Attack1')
    if re.search(r'turn', n): return ('Locomotion', 'Turn in place', 'New: turn')
    if re.search(r'evade', n): return ('Locomotion', 'Evade / sidestep', None)
    if re.search(r'dash', n): return ('Locomotion', 'Dash', None)
    if re.search(r'jump|fall|land', n): return ('Locomotion', 'Jump / fall / land', None)
    if re.search(r'run', n):
        if re.search(r'_end|_start|couple', n): return ('Locomotion', 'Run start/stop', 'WalkAlerted')
        return ('Locomotion', 'Run' + direction(n), 'WalkAlerted' if fwd(n) else None)
    if re.search(r'walk', n):
        if re.search(r'_end|_start|couple', n): return ('Locomotion', 'Walk start/stop', None)
        return ('Locomotion', 'Walk' + direction(n), 'Walk1' if fwd(n) else None)
    return ('Other', '', None)

def fwd(n):
    return bool(re.search(r'forward(?!left|right|_?l|_?r)|front(?!l|r)|walk_f$|run_f|walk_f_|^walk$|^run$|walk0?1?$|_f_?\d*$|walkslow', n)) and not re.search(r'back|left|right', n)

def direction(n):
    for k, v in [('backl', ' back-left'), ('backr', ' back-right'), ('backwardleft', ' back-left'), ('backwardright', ' back-right'),
                 ('back', ' backward'), ('frontl', ' fwd-left'), ('frontr', ' fwd-right'), ('forwardleft', ' fwd-left'),
                 ('forwardright', ' fwd-right'), ('forward-left', ' fwd-left'), ('forward-right', ' fwd-right'),
                 ('left', ' strafe left'), ('right', ' strafe right')]:
        if k in n.replace('_', '').replace(' ', '') : return v
    return ' forward'

# per-pack reference hip height (from idle/walk frame 0)
ref = {}
for pk in PACKS:
    hs = []
    for k, v in T.items():
        if k.startswith(pk + '|') and 'error' not in v and re.search(r'idle|walk', k.split('/')[-1], re.I):
            up = v['up']; hs.append(abs(v['P'][0][up]))
    if not hs:
        hs = [abs(v['P'][0][v['up']]) for k, v in T.items() if k.startswith(pk + '|') and 'error' not in v]
    ref[pk] = statistics.median(hs) if hs else None

def clipmetrics(r):
    v = T.get(r['pack'] + '|' + r['path'])
    if not v or 'error' in v: return {}
    t = np.array(v['t']); P = np.array(v['P'])
    L = {k: np.array(x) for k, x in v['limbs'].items()}
    fps = r['fps'] or 30
    if r['first'] not in (None, '') and r['last'] not in (None, ''):
        a, b = float(r['first']) / fps, float(r['last']) / fps
        m = (t >= a - 1e-4) & (t <= b + 1e-4)
        if m.sum() >= 2:
            t, P = t[m], P[m]; L = {k: x[m] for k, x in L.items()}
    up, s = v['up'], v['upSign']; h = [i for i in range(3) if i != up]
    H = ref[r['pack']] or abs(P[0, up]) or 1
    d = P[-1] - P[0]
    net = float(np.hypot(d[h[0]], d[h[1]])) / H
    dur = max(float(t[-1] - t[0]), 1e-6)
    vy = (P[:, up] - P[0, up]) * s / H
    out = {'travel_hip': round(net, 2), 'speed_hipps': round(net / dur, 2),
           'rise_hip': round(float(vy.max()), 2), 'drop_hip': round(float(vy.min()), 2)}
    # strike estimate: limb with highest peak speed (body-relative = subtract hips travel)
    best = None
    for k, x in L.items():
        rel = x - P
        sp = np.linalg.norm(np.diff(rel, axis=0), axis=1) / np.maximum(np.diff(t), 1e-6) / H
        if len(sp) == 0: continue
        i = int(np.argmax(sp))
        if best is None or sp[i] > best[1]: best = (k, float(sp[i]), float(t[i] - t[0]))
    if best:
        out['strike_limb'] = {'handR': 'right hand', 'handL': 'left hand', 'footR': 'right foot', 'footL': 'left foot'}[best[0]]
        out['strike_t'] = round(best[2], 2)
        out['strike_frac'] = round(best[2] / dur, 2)
    # low-point of hips for slams (time of max drop)
    out['low_t'] = round(float(t[int(np.argmin(vy))] - t[0]), 2)
    # ground speed from planted foot (works for in-place clips): foot near lowest point -> its speed relative to hips = body speed
    gs = []
    for k in ('footL', 'footR'):
        if k not in L: continue
        x = L[k]; hgt = x[:, up] * s
        rel = x - P
        vel = np.hypot(np.diff(rel[:, h[0]]), np.diff(rel[:, h[1]])) / np.maximum(np.diff(t), 1e-6) / H
        mask = (hgt[:-1] - hgt.min()) < 0.04 * H
        if mask.sum() >= 3: gs.append(float(np.median(vel[mask])))
    out['contact_speed'] = round(float(np.mean(gs)), 2) if gs else None
    return out

def rm_flag(r, m):
    n = r['clip'] + ' ' + r['file']
    if re.search(r'\[RM\]|FullRootmotion', n, re.I): tag = 'Root motion'
    elif re.search(r'inplace|_S\.FBX', n, re.I): tag = 'In-place'
    else: tag = 'Root motion' if m.get('travel_hip', 0) > 0.15 else 'In-place'
    return tag

out = []
seen = set()
for r in rows:
    if r['pack'] not in PACKS: continue
    if '/Demo' in r['path'] or 'Giant@Falling01_DEMO' in r['clip']: continue
    cat, sub, slot = classify(r['clip'])
    if cat == 'Reference': continue
    if r['clip'] == 'Take 001' and (r['seconds'] or 0) < 0.1: continue
    m = clipmetrics(r)
    rm = rm_flag(r, m)
    vend, packname, fam = PACKS[r['pack']]
    frames = None
    if r['first'] not in (None, '') and r['last'] not in (None, ''):
        frames = int(float(r['last']) - float(r['first']))
    # skill descriptors from measured motion
    desc = ''
    if sub.startswith('Skill') and m:
        bits = []
        if m['rise_hip'] >= 0.5: bits.append(f"leaps (body rises {m['rise_hip']} hip-heights)")
        elif m['drop_hip'] <= -0.35: bits.append(f"deep crouch/slam (body drops {abs(m['drop_hip'])} hip-heights)")
        if m.get('strike_limb'): bits.append(f"fastest limb {m['strike_limb']} at {m['strike_t']}s")
        if (r['seconds'] or 0) >= 6: bits.append('long multi-phase')
        desc = 'Measured: ' + '; '.join(bits) if bits else 'Measured: no big body move; preview needed'
        slot = 'AttackRage' if (m['rise_hip'] >= 0.5 or m['drop_hip'] <= -0.35) else 'Attack2'
    key = (r['pack'], r['clip'], rm, r['path'].split('/')[-2])
    dup = key in seen; seen.add(key)
    notes = []
    if r['loop'] and cat in ('Attack', 'Reaction', 'Death', 'Taunt') and 'loop' not in sub.lower() and 'Stun' != sub:
        notes.append('Authored as LOOPING - untick Loop Time for a one-shot')
    if r['pack'] == 'Giant_Animations' and re.search(r'_[AB]( |$)', r['clip']):
        notes.append('A/B variant pair (identical hip motion)')
    if r['events']: notes.append('Has events: ' + ', '.join(dict.fromkeys(r['events'])))
    if desc: notes.append(desc)
    if r['pack'] == 'Giant_Golem_AnimSet' and '/Inplace/' in r['path']: notes.append('In-place twin of the root-motion clip')
    if r['pack'] == 'Brute_Warrior_Mecanim_Animation_Pack' and r['file'].upper().endswith('_S.FBX'): notes.append('_S = stationary twin')
    out.append({
        'Family': fam, 'Vendor': vend, 'Pack': packname, 'Clip': r['clip'], 'File': r['file'],
        'Folder': '/'.join(r['path'].split('/')[1:-1]),
        'Category': cat, 'Type': sub, 'Behemoth slot': slot or '',
        'Behemoth-ready': readiness(r['pack'], r['rig']), 'Rig (shipped)': r['rig'],
        'Seconds': r['seconds'], 'Frames @30': frames, 'Loop (authored)': 'Yes' if r['loop'] else 'No',
        'Root motion': rm,
        'Travel (hip-heights)': m.get('travel_hip'), 'Speed (hip-heights/s)': m.get('speed_hipps'),
        'Ground speed from foot contact (hip-h/s)': m.get('contact_speed') if cat == 'Locomotion' else '',
        'Body rise (hip-h)': m.get('rise_hip'), 'Body drop (hip-h)': m.get('drop_hip'),
        'Fastest limb (hit candidate)': m.get('strike_limb') if cat == 'Attack' else '',
        'Fastest-limb time (s)': m.get('strike_t') if cat == 'Attack' else '',
        'Fastest-limb % of clip': m.get('strike_frac') if cat == 'Attack' else '',
        'Notes': ' | '.join(notes),
    })
json.dump(out, open(os.path.join(WORK, 'catalog.json'), 'w'), indent=1)
import collections
print(len(out), 'rows')
print(collections.Counter((o['Family'], o['Category']) for o in out))
print(collections.Counter(o['Behemoth slot'] for o in out))
print('refs', {k: round(v, 3) if v else v for k, v in ref.items()})
