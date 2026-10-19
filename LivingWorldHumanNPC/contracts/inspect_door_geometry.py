"""Read-only native prefab inspection; no scene execution or asset writes."""
import sys, json
from pathlib import Path
sys.path.insert(0, r'C:\Users\scruf\Documents\Codex\2026-10-13\begin-now-dedicated-independent-valheim-living\work\human-npc\assets\python-libs')
import UnityPy
from UnityPy.classes.PPtr import PPtr
bundle = Path(r'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles') / 'c4210710'
env = UnityPy.load(str(bundle))
def pointer(data, asset):
    return PPtr(m_FileID=data['m_FileID'], m_PathID=data['m_PathID'], assetsfile=asset).deref()
def walk(obj, depth=0):
    data = obj.read_typetree()
    print('  '*depth + data['m_Name'])
    transform = None
    for entry in data['m_Component']:
        component = pointer(entry['component'], obj.assets_file)
        fields = component.read_typetree()
        kind = component.type.name
        if kind == 'Transform':
            transform = component
            print('  '*depth + json.dumps({k: fields[k] for k in ('m_LocalPosition','m_LocalRotation','m_LocalScale')}))
        elif kind == 'BoxCollider':
            print('  '*depth + kind + ' ' + json.dumps({k: fields.get(k) for k in ('m_Center','m_Size','m_IsTrigger','m_Enabled')}))
        elif kind == 'MonoBehaviour' and 'm_checkGuardStone' in fields:
            print('  '*depth + 'Door ' + json.dumps({k:fields.get(k) for k in ('m_keyItem','m_checkGuardStone','m_canNotBeClosed')}))
    if transform:
        for child in transform.read_typetree()['m_Children']:
            tr = pointer(child, transform.assets_file)
            walk(pointer(tr.read_typetree()['m_GameObject'], tr.assets_file), depth+1)
found = False
for key, value in env.container.items():
    if key.lower().endswith('/wood_door.prefab'):
        print(key)
        walk(value.deref() if isinstance(value, PPtr) else value)
        found = True
if not found:
    print('Door prefab not in inspected bundle.')
    sys.exit(2)
