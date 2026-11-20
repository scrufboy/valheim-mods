"""Measure root/hip travel per FBX clip (binary FBX). Outputs JSON keyed by package-relative asset path."""
import os, sys, struct, zlib, json, re
import numpy as np

KT = 46186158000.0
ARR = {'f': ('<f', 4), 'd': ('<d', 8), 'l': ('<q', 8), 'i': ('<i', 4), 'b': ('<?', 1)}

def parse(data):
    ver = struct.unpack('<I', data[23:27])[0]; big = ver >= 7500
    def prop(p):
        t = chr(data[p]); p += 1
        if t == 'Y': return struct.unpack('<h', data[p:p+2])[0], p+2
        if t == 'C': return data[p] != 0, p+1
        if t == 'I': return struct.unpack('<i', data[p:p+4])[0], p+4
        if t == 'F': return struct.unpack('<f', data[p:p+4])[0], p+4
        if t == 'D': return struct.unpack('<d', data[p:p+8])[0], p+8
        if t == 'L': return struct.unpack('<q', data[p:p+8])[0], p+8
        if t in 'SR':
            n = struct.unpack('<I', data[p:p+4])[0]; p += 4
            v = data[p:p+n]; return (v.decode('utf-8', 'replace') if t == 'S' else b''), p+n
        if t in ARR:
            n, enc, cl = struct.unpack('<III', data[p:p+12]); p += 12
            raw = data[p:p+cl]
            if enc: raw = zlib.decompress(raw)
            fmt, sz = ARR[t]
            return np.frombuffer(raw, dtype=np.dtype(fmt)), p+cl
        raise ValueError(t)
    def node(p):
        if big: eo, np_, pl = struct.unpack('<QQQ', data[p:p+24]); p += 24
        else: eo, np_, pl = struct.unpack('<III', data[p:p+12]); p += 12
        nl = data[p]; p += 1
        if eo == 0: return None, p
        name = data[p:p+nl].decode('ascii', 'replace'); p += nl
        props = []
        for _ in range(np_):
            v, p = prop(p); props.append(v)
        kids = []
        while p < eo:
            k, p2 = node(p)
            if k is None: p = p2; break
            kids.append(k); p = k['_end']
        return {'n': name, 'p': props, 'k': kids, '_end': eo}, eo
    top = []; p = 27
    while p < len(data) - 200:
        k, p2 = node(p)
        if k is None: break
        top.append(k); p = k['_end']
    return top

def child(n, name):
    for k in n['k']:
        if k['n'] == name: return k

def p70(n):
    out = {}
    pp = child(n, 'Properties70')
    if pp:
        for P in pp['k']:
            out[P['p'][0]] = P['p'][4:]
    return out

ROOTS = ['Bip001', 'B-root', 'root', 'Root', 'Hips', 'B-hips', 'pelvis', 'Bip001 Pelvis', 'Hip', 'hips']
HIPS = ['Bip001', 'B-hips', 'pelvis', 'Hips', 'Bip001 Pelvis', 'hips', 'Hip']

def analyze(path):
    data = open(path, 'rb').read()
    if not data.startswith(b'Kaydara FBX Binary'): return {'error': 'ascii'}
    top = parse(data)
    gs = child([t for t in top if t['n'] == 'GlobalSettings'][0], 'Properties70') if any(t['n'] == 'GlobalSettings' for t in top) else None
    g = p70([t for t in top if t['n'] == 'GlobalSettings'][0]) if gs else {}
    unit = (g.get('UnitScaleFactor') or [1.0])[0]  # cm per unit
    up = (g.get('UpAxis') or [1])[0]
    objs = [t for t in top if t['n'] == 'Objects'][0]
    conns = [t for t in top if t['n'] == 'Connections'][0]
    models, cnodes, curves = {}, {}, {}
    for o in objs['k']:
        if o['n'] == 'Model':
            models[o['p'][0]] = o['p'][1].split('\x00')[0]
            o['_props'] = p70(o)
            models[str(o['p'][0]) + '_obj'] = o
        elif o['n'] == 'AnimationCurveNode':
            cnodes[o['p'][0]] = o['p'][1].split('\x00')[0]
        elif o['n'] == 'AnimationCurve':
            kt = child(o, 'KeyTime'); kv = child(o, 'KeyValueFloat')
            if kt and kv: curves[o['p'][0]] = (kt['p'][0] / KT, kv['p'][0].astype(float))
    cn_model = {}; cur_cn = {}
    for c in conns['k']:
        if c['p'][0] == 'OP':
            a, b, prop = c['p'][1], c['p'][2], c['p'][3]
            if a in cnodes and b in models and prop == 'Lcl Translation': cn_model[b] = a
            if a in curves and b in cnodes: cur_cn.setdefault(b, {})[prop] = a
    name2id = {v: k for k, v in models.items() if isinstance(k, int)}
    res = {'unitScale_cm': unit, 'upAxis': up}
    def track(bone):
        mid = name2id.get(bone)
        if mid is None: return None
        o = models[str(mid) + '_obj']
        base = (o['_props'].get('Lcl Translation') or [0, 0, 0])[:3]
        cn = cn_model.get(mid)
        chans = cur_cn.get(cn, {}) if cn else {}
        out = []
        for i, ax in enumerate(['d|X', 'd|Y', 'd|Z']):
            if ax in chans and chans[ax] in curves:
                out.append(curves[chans[ax]])
            else:
                out.append((np.array([0.0]), np.array([float(base[i])])))
        return out
    best = None
    for bone in ROOTS:
        tr = track(bone)
        if tr is None: continue
        # sample on union time grid
        ts = np.unique(np.concatenate([t for t, v in tr]))
        P = np.stack([np.interp(ts, t, v) for t, v in tr], 1)
        rng = P.max(0) - P.min(0)
        cand = {'bone': bone, 'P': P, 'ts': ts, 'rng': rng}
        if best is None or rng.sum() > best['rng'].sum(): best = cand
        if bone in ('Bip001',): break
    hip = None
    for bone in HIPS:
        tr = track(bone)
        if tr is not None:
            P0 = np.array([v[0] for t, v in tr]); hip = (bone, P0); break
    if best is None: return {'error': 'no root bone', **res}
    P = best['P']
    # vertical axis: for hips use largest abs coordinate at frame 0; else file up axis
    if hip is not None and np.abs(hip[1]).max() > 1e-3:
        vax = int(np.argmax(np.abs(hip[1]))); hh = float(np.abs(hip[1][vax]))
    else:
        vax = up; hh = None
    if best['bone'] in ('Bip001',) :
        vax = int(np.argmax(np.abs(P[0]))) if np.abs(P[0]).max() > 1e-3 else vax
        hh = float(abs(P[0][vax]))
    h = [i for i in range(3) if i != vax]
    d = P[-1] - P[0]
    horiz = float(np.hypot(d[h[0]], d[h[1]]))
    path_len = float(np.sum(np.hypot(np.diff(P[:, h[0]]), np.diff(P[:, h[1]]))))
    vy = P[:, vax] - P[0, vax]
    sign = 1.0 if (hh is None or (P[0][vax] >= 0)) else -1.0
    m = unit / 100.0
    res.update({
        'rootBone': best['bone'], 'vertAxis': 'XYZ'[vax],
        'netHorizontal_m': round(horiz * m, 3), 'pathHorizontal_m': round(path_len * m, 3),
        'rise_m': round(float((vy * sign).max()) * m, 3), 'drop_m': round(float((vy * sign).min()) * m, 3),
        'hipHeight_m': round(hh * m, 3) if hh else None,
        'span_s': round(float(best['ts'][-1] - best['ts'][0]), 3),
    })
    return res

if __name__ == '__main__':
    root = sys.argv[1]; out = {}
    for pk in sorted(os.listdir(root)):
        for gdir in os.listdir(os.path.join(root, pk)):
            gd = os.path.join(root, pk, gdir)
            pn = os.path.join(gd, 'pathname')
            if not os.path.exists(pn): continue
            path = open(pn, encoding='utf-8', errors='replace').read().splitlines()[0]
            if not path.lower().endswith('.fbx'): continue
            try:
                out[pk + '|' + path] = analyze(os.path.join(gd, 'asset'))
            except Exception as e:
                out[pk + '|' + path] = {'error': repr(e)[:200]}
    json.dump(out, open(sys.argv[2], 'w'), indent=1)
    print(len(out), 'files;', sum('error' in v for v in out.values()), 'errors')
