"""raw.json (from catalog.py) -> rows.json: one row per clip with seconds computed from frames/fps."""
import json, re, sys
r = json.load(open(sys.argv[1]))
rows = []
for pk, items in r.items():
    for i in items:
        if i['kind'] != 'fbx': continue
        p = i['path']
        if re.search(r'/(Demo Elements|SuperCharacterController|Mesh|Models|Characters/Brute\.FBX)', p) and not re.search(r'Animations', p): continue
        clips = i['clips'] or [{'name': s['name'], 'first': None, 'last': None, 'loop': None, 'events': []} for s in i['stacks']]
        st = {s['name']: s for s in i['stacks']}
        for c in clips:
            fps = i['fps'] or 30
            if c.get('first') not in (None, '') and c.get('last') not in (None, ''):
                secs = round((float(c['last']) - float(c['first'])) / fps, 3)
            else:
                s = st.get(c.get('take')) or (i['stacks'][0] if i['stacks'] else {})
                secs = s.get('seconds')
            rows.append(dict(pack=pk, file=p.split('/')[-1], path=p, clip=c['name'], rig=i['animationType'], fps=fps,
                             first=c.get('first'), last=c.get('last'), seconds=secs, loop=c.get('loop'), events=c.get('events', [])))
json.dump(rows, open(sys.argv[2], 'w'), indent=1)
print(len(rows), 'rows')
