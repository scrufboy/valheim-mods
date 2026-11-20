"""World-space hip trajectory per FBX (binary). Samples at 30 fps over the whole take.
Output: {pack|path: {hips, up, unit_cm, t:[...], P:[[x,y,z],...]}}"""
import os, sys, json, re
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from motion import parse, child, p70, KT

HIP_NAMES = ['Bip001 Pelvis', 'B-hips', 'B_Pelvis', 'pelvis', 'Hips', 'mixamorig:Hips', 'cave_troll_Hub001', 'Pelvis', 'hips']

def rx(a): c, s = np.cos(a), np.sin(a); return np.array([[1,0,0],[0,c,-s],[0,s,c]])
def ry(a): c, s = np.cos(a), np.sin(a); return np.array([[c,0,s],[0,1,0],[-s,0,c]])
def rz(a): c, s = np.cos(a), np.sin(a); return np.array([[c,-s,0],[s,c,0],[0,0,1]])
ORD = {0: 'XYZ', 1: 'XZY', 2: 'YZX', 3: 'YXZ', 4: 'ZXY', 5: 'ZYX'}
def euler(v, order=0):
    a = np.radians(v); m = {'X': rx(a[0]), 'Y': ry(a[1]), 'Z': rz(a[2])}
    o = ORD.get(order, 'XYZ')
    R = np.eye(3)
    for ax in o: R = m[ax] @ R   # first axis applied first
    return R

def analyze(path):
    data = open(path, 'rb').read()
    if not data.startswith(b'Kaydara FBX Binary'): return {'error': 'ascii'}
    top = parse(data)
    G = p70([t for t in top if t['n'] == 'GlobalSettings'][0])
    up = int((G.get('UpAxis') or [1])[0]); ups = int((G.get('UpAxisSign') or [1])[0])
    unit = float((G.get('UnitScaleFactor') or [1.0])[0])
    objs = [t for t in top if t['n'] == 'Objects'][0]
    conns = [t for t in top if t['n'] == 'Connections'][0]
    M, CN, CV = {}, {}, {}
    for o in objs['k']:
        if o['n'] == 'Model': M[o['p'][0]] = {'name': o['p'][1].split('\x00')[0], 'props': p70(o)}
        elif o['n'] == 'AnimationCurveNode': CN[o['p'][0]] = {}
        elif o['n'] == 'AnimationCurve':
            kt = child(o, 'KeyTime'); kv = child(o, 'KeyValueFloat')
            if kt is not None and kv is not None: CV[o['p'][0]] = (kt['p'][0] / KT, kv['p'][0].astype(float))
    parent, anim = {}, {}
    for c in conns['k']:
        typ, a, b = c['p'][0], c['p'][1], c['p'][2]
        if typ == 'OO' and a in M and b in M: parent[a] = b
        if typ == 'OP':
            prop = c['p'][3]
            if a in CN and b in M: anim.setdefault(b, {})[prop] = a
            if a in CV and b in CN: CN[b][prop] = a
    byname = {v['name']: k for k, v in M.items()}
    hid = None
    for n in HIP_NAMES:
        if n in byname: hid = byname[n]; break
    if hid is None:
        c = [k for k, v in M.items() if re.search(r'hip|pelvis', v['name'], re.I)]
        if c: hid = sorted(c, key=lambda k: len(M[k]['name']))[0]
    if hid is None: return {'error': 'no hips', 'models': [v['name'] for v in M.values()][:12]}
    LIMBS = {'handR': ['Bip001 R Hand','B-hand.R','B_R_Hand','hand_r','RightHand','cave_troll_RArmPalm'],
             'handL': ['Bip001 L Hand','B-hand.L','B_L_Hand','hand_l','LeftHand','cave_troll_LArmPalm'],
             'footR': ['Bip001 R Foot','B-foot.R','B_R_Foot','foot_r','RightFoot','cave_troll_RLegAnkle'],
             'footL': ['Bip001 L Foot','B-foot.L','B_L_Foot','foot_l','LeftFoot','cave_troll_LLegAnkle']}
    targets = {'hips': hid}
    for key, names in LIMBS.items():
        for n in names:
            if n in byname: targets[key] = byname[n]; break
    allchain = set()
    for tk in targets.values():
        k = tk
        while k in M: allchain.add(k); k = parent.get(k)
    chain = []; k = hid
    while k in M:
        chain.append(k); k = parent.get(k)
    times = [np.array([0.0])]
    for k in allchain:
        for prop, cn in anim.get(k, {}).items():
            for ch, cv in CN.get(cn, {}).items():
                times.append(CV[cv][0])
    tmax = max(t.max() for t in times); tmin = min(t.min() for t in times)
    ts = np.arange(tmin, tmax + 1e-6, 1 / 30.0)
    def channel(k, prop, default):
        base = np.array((M[k]['props'].get(prop) or default)[:3], float)
        out = np.tile(base, (len(ts), 1))
        cn = anim.get(k, {}).get(prop)
        if cn is not None:
            for i, ax in enumerate(['d|X', 'd|Y', 'd|Z']):
                cv = CN[cn].get(ax)
                if cv is not None:
                    t, v = CV[cv]; out[:, i] = np.interp(ts, t, v)
        return out
    cache = {}
    def world(k):
        if k in cache: return cache[k]
        T = channel(k, 'Lcl Translation', [0, 0, 0])
        R = channel(k, 'Lcl Rotation', [0, 0, 0])
        S = channel(k, 'Lcl Scaling', [1, 1, 1])
        pre = euler(np.array((M[k]['props'].get('PreRotation') or [0, 0, 0])[:3], float))
        order = int((M[k]['props'].get('RotationOrder') or [0])[0])
        L = np.zeros((len(ts), 4, 4)); L[:, 3, 3] = 1
        for i in range(len(ts)):
            L[i, :3, :3] = pre @ euler(R[i], order) @ np.diag(S[i])
            L[i, :3, 3] = T[i]
        p = parent.get(k)
        Wm = (world(p) @ L) if p in M else L
        cache[k] = Wm
        return Wm
    out = {}
    for key, tk in targets.items():
        out[key] = np.round(world(tk)[:, :3, 3], 4).tolist()
    P = np.array(out['hips'])
    return {'hips': M[hid]['name'], 'up': up, 'upSign': ups, 'unit_cm': unit, 'chain': [M[k]['name'] for k in chain],
            't': np.round(ts, 4).tolist(), 'P': np.round(P, 5).tolist(), 'limbs': {k: v for k, v in out.items() if k != 'hips'}}

if __name__ == '__main__':
    root = sys.argv[1]; out = {}
    for pk in sorted(os.listdir(root)):
        for g in os.listdir(os.path.join(root, pk)):
            pn = os.path.join(root, pk, g, 'pathname')
            if not os.path.exists(pn): continue
            path = open(pn, encoding='utf-8', errors='replace').read().splitlines()[0]
            if not path.lower().endswith('.fbx'): continue
            try: out[pk + '|' + path] = analyze(os.path.join(root, pk, g, 'asset'))
            except Exception as e: out[pk + '|' + path] = {'error': repr(e)[:200]}
    json.dump(out, open(sys.argv[2], 'w'))
    print(len(out), 'files;', sum('error' in v for v in out.values()), 'errors')
    for k, v in out.items():
        if 'error' in v: print('ERR', k.split('/')[-1], v)
