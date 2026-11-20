"""Catalog animation clips in extracted .unitypackage folders.
Reads pathname + asset.meta (ModelImporter / .anim YAML) + FBX AnimationStack timing."""
import os, sys, re, json, struct, zlib

KTIME = 46186158000.0

def fbx_nodes(data):
    """Minimal binary FBX parser -> yields (path, name, props) for AnimationStack and GlobalSettings."""
    if not data.startswith(b'Kaydara FBX Binary'):
        return None
    ver = struct.unpack('<I', data[23:27])[0]
    big = ver >= 7500
    out = []
    def read_prop(p):
        t = chr(data[p]); p += 1
        if t == 'Y': return struct.unpack('<h', data[p:p+2])[0], p+2
        if t == 'C': return data[p] != 0, p+1
        if t == 'I': return struct.unpack('<i', data[p:p+4])[0], p+4
        if t == 'F': return struct.unpack('<f', data[p:p+4])[0], p+4
        if t == 'D': return struct.unpack('<d', data[p:p+8])[0], p+8
        if t == 'L': return struct.unpack('<q', data[p:p+8])[0], p+8
        if t in 'SR':
            n = struct.unpack('<I', data[p:p+4])[0]; p += 4
            v = data[p:p+n]; return (v.decode('utf-8', 'replace') if t == 'S' else v), p+n
        if t in 'fdlib':
            n, enc, clen = struct.unpack('<III', data[p:p+12]); p += 12
            return None, p+clen
        raise ValueError(t)
    def walk(p, end, path):
        while p < end:
            if big:
                eo, np_, pl = struct.unpack('<QQQ', data[p:p+24]); p += 24
            else:
                eo, np_, pl = struct.unpack('<III', data[p:p+12]); p += 12
            nl = data[p]; p += 1
            if eo == 0: return
            name = data[p:p+nl].decode('ascii', 'replace'); p += nl
            props = []; q = p
            for _ in range(np_):
                v, q = read_prop(q); props.append(v)
            p += pl
            out.append((path + '/' + name, props))
            if p < eo and name in ('Objects', 'AnimationStack', 'Properties70', 'GlobalSettings', 'Takes', 'Take'):
                walk(p, eo, path + '/' + name)
            p = eo
    walk(27, len(data), '')
    return out

def fbx_info(path):
    try:
        data = open(path, 'rb').read()
    except Exception:
        return {}
    info = {'stacks': [], 'fps': None}
    nodes = fbx_nodes(data)
    if nodes is None:
        # ASCII fallback
        txt = data.decode('latin1', 'replace')
        for m in re.finditer(r'AnimationStack: \d+, "AnimStack::([^"]+)"', txt):
            info['stacks'].append({'name': m.group(1)})
        return info
    cur = None
    for path_, props in nodes:
        if path_.endswith('/Objects/AnimationStack'):
            nm = props[1].split('\x00')[0] if len(props) > 1 and isinstance(props[1], str) else '?'
            cur = {'name': nm}
            info['stacks'].append(cur)
        elif path_.endswith('/AnimationStack/Properties70/P') and cur is not None and props:
            if props[0] in ('LocalStop', 'ReferenceStop') and len(props) > 4:
                cur.setdefault('stop', props[4] / KTIME)
            if props[0] in ('LocalStart',) and len(props) > 4:
                cur['start'] = props[4] / KTIME
        elif path_.endswith('/GlobalSettings/Properties70/P') and props:
            if props[0] == 'CustomFrameRate' and len(props) > 4 and props[4] and props[4] > 0:
                info['fps'] = props[4]
            if props[0] == 'TimeMode' and len(props) > 4:
                info['timemode'] = props[4]
    tm = {1: 120, 2: 100, 3: 60, 4: 50, 5: 48, 6: 30, 7: 30, 8: 29.97, 10: 25, 11: 24}
    if not info['fps'] and info.get('timemode') in tm:
        info['fps'] = tm[info['timemode']]
    for s in info['stacks']:
        if 'stop' in s:
            s['seconds'] = round(s['stop'] - s.get('start', 0.0), 4)
    return info

ANIMTYPE = {'0': 'None', '1': 'Legacy', '2': 'Generic', '3': 'Humanoid'}

def parse_meta(txt):
    m = {}
    a = re.search(r'\n\s+animationType: (\d)', txt)
    m['animationType'] = ANIMTYPE.get(a.group(1), a.group(1)) if a else None
    a = re.search(r'avatarSetup: (\d)', txt)
    m['avatarSetup'] = {'0': 'NoAvatar', '1': 'CreateFromThisModel', '2': 'CopyFromOther'}.get(a.group(1)) if a else None
    m['importAnimation'] = (re.search(r'importAnimation: (\d)', txt) or [None, None])[1]
    m['clips'] = []
    blk = re.search(r'clipAnimations:(.*?)\n    isReadable|clipAnimations:(.*?)\n    \w', txt, re.S)
    body = txt.split('clipAnimations:', 1)[1] if 'clipAnimations:' in txt else ''
    for c in re.split(r'\n\s+- serializedVersion: \d+\n\s+name: ', '\n' + body)[1:]:
        name = c.split('\n', 1)[0].strip()
        def g(k):
            r = re.search(r'\n\s+' + k + r': ([^\n]*)', c)
            return r.group(1).strip() if r else None
        ev = re.findall(r'functionName: ([^\n]*)', c.split('transformMask')[0])
        m['clips'].append({
            'name': name, 'take': g('takeName'),
            'first': g('firstFrame'), 'last': g('lastFrame'),
            'loop': g('loopTime') == '1',
            'rootRotBake': g('loopBlendOrientation') == '1',
            'rootYBake': g('loopBlendPositionY') == '1',
            'rootXZBake': g('loopBlendPositionXZ') == '1',
            'mirror': g('mirror') == '1',
            'events': [e.strip() for e in ev][:12],
        })
        if 'clipAnimations' in name: break
    return m

def parse_anim(txt):
    d = {}
    for k in ['m_Name', 'm_StopTime', 'm_StartTime', 'm_LoopTime', 'm_SampleRate']:
        r = re.search(k + r': ([^\n]*)', txt)
        d[k] = r.group(1).strip() if r else None
    d['humanoid'] = bool(re.search(r'm_MuscleClipSize: [1-9]', txt)) or 'm_Muscle' in txt and 'attribute: 0' not in txt
    d['events'] = re.findall(r'functionName: ([^\n]*)', txt)[:12]
    return d

def main(root):
    res = {}
    for pk in sorted(os.listdir(root)):
        items = []
        for g in os.listdir(os.path.join(root, pk)):
            gd = os.path.join(root, pk, g)
            pn = os.path.join(gd, 'pathname')
            if not os.path.exists(pn): continue
            path = open(pn, encoding='utf-8', errors='replace').read().splitlines()[0]
            low = path.lower()
            meta = open(os.path.join(gd, 'asset.meta'), encoding='utf-8', errors='replace').read() if os.path.exists(os.path.join(gd, 'asset.meta')) else ''
            asset = os.path.join(gd, 'asset')
            if low.endswith('.fbx'):
                mi = parse_meta(meta); fi = fbx_info(asset)
                items.append({'kind': 'fbx', 'path': path, **mi, 'fps': fi.get('fps'), 'stacks': fi.get('stacks', [])})
            elif low.endswith('.anim'):
                try: t = open(asset, encoding='utf-8', errors='replace').read()
                except Exception: t = ''
                items.append({'kind': 'anim', 'path': path, **parse_anim(t)})
            elif low.endswith('.controller') or low.endswith('.overridecontroller'):
                try: t = open(asset, encoding='utf-8', errors='replace').read()
                except Exception: t = ''
                states = re.findall(r'AnimatorState:\n(?:.*\n){0,6}?\s+m_Name: ([^\n]*)', t)
                params = re.findall(r'm_Name: ([^\n]*)\n\s+m_Type: (\d)', t)
                items.append({'kind': 'controller', 'path': path, 'states': states, 'params': params})
        res[pk] = sorted(items, key=lambda x: x['path'])
    json.dump(res, open(sys.argv[2], 'w'), indent=1)
    for pk, items in res.items():
        k = {}
        for i in items: k[i['kind']] = k.get(i['kind'], 0) + 1
        types = sorted({str(i.get('animationType')) for i in items if i['kind'] == 'fbx'})
        print(pk, k, types)

main(sys.argv[1])
