import os
WORK = os.environ.get('LWC_ANIM_WORK', '.')
import json, numpy as np
T = json.load(open(os.path.join(WORK, 'traj.json')))
def metrics(pack, path, first=None, last=None, fps=30):
    v = T.get(pack + '|' + path)
    if not v or 'error' in v: return None
    t = np.array(v['t']); P = np.array(v['P'])
    if first not in (None, '') and last not in (None, ''):
        a, b = float(first) / fps, float(last) / fps
        m = (t >= a - 1e-4) & (t <= b + 1e-4)
        if m.sum() >= 2: t, P = t[m], P[m]
    up = v['up']; s = v['upSign']
    h = [i for i in range(3) if i != up]
    hip0 = P[0, up] * s
    d = P[-1] - P[0]
    net = float(np.hypot(d[h[0]], d[h[1]]))
    path_ = float(np.sum(np.hypot(np.diff(P[:, h[0]]), np.diff(P[:, h[1]]))))
    vy = (P[:, up] - P[0, up]) * s
    dur = float(t[-1] - t[0]) or 1e-6
    hh = abs(hip0) if abs(hip0) > 1e-6 else None
    return {
        'hips': v['hips'], 'hipHeight_units': round(abs(hip0), 4),
        'net_hip': round(net / hh, 3) if hh else None,
        'speed_hipPerSec': round(net / hh / dur, 3) if hh else None,
        'rise_hip': round(float(vy.max()) / hh, 3) if hh else None,
        'drop_hip': round(float(vy.min()) / hh, 3) if hh else None,
        'path_hip': round(path_ / hh, 3) if hh else None,
    }
