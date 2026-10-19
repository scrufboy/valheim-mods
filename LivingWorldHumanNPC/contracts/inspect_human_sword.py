"""Read native sword decision parameters only; no Unity execution or writes."""
from pathlib import Path
import sys, json, argparse
parser = argparse.ArgumentParser()
parser.add_argument('--unitypy-dir', default=r'C:\Users\scruf\Documents\Codex\2026-10-13\begin-now-dedicated-independent-valheim-living\work\human-npc\assets\python-libs')
parser.add_argument('--bundles-dir', default=r'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles')
args = parser.parse_args()
sys.path.insert(0, args.unitypy_dir)
import UnityPy
from UnityPy.classes.PPtr import PPtr
bundle = Path(args.bundles_dir) / 'c4210710'
env = UnityPy.load(str(bundle))
prefabs = [(key, value) for key, value in env.container.items() if key.endswith('/SwordBronze.prefab')]
assert len(prefabs) == 1, 'Native SwordBronze identity ambiguous'
key, pointer = prefabs[0]
root = pointer.deref() if isinstance(pointer, PPtr) else pointer
data = root.read_typetree()
assert data['m_Name'] == 'SwordBronze'
for item in data['m_Component']:
    ptr = item['component']
    component = PPtr(m_FileID=ptr['m_FileID'], m_PathID=ptr['m_PathID'], assetsfile=root.assets_file).deref()
    if component.type.name != 'MonoBehaviour': continue
    fields = component.read_typetree()
    if 'm_itemData' not in fields: continue
    shared = fields['m_itemData']['m_shared']
    attack = shared['m_attack']
    result = {'prefab': key, 'componentPathID': component.path_id,
              'aiRange': shared['m_aiAttackRange'], 'aiMinimumRange': shared['m_aiAttackRangeMin'],
              'aiInterval': shared['m_aiAttackInterval'], 'aiTargetType': shared['m_aiTargetType'],
              'attackAnimation': attack['m_attackAnimation'], 'attackHitRange': attack['m_attackRange'],
              'chargeAnimationBool': attack['m_chargeAnimationBool'], 'requiresReload': bool(attack['m_requiresReload']),
              'bowDraw': bool(attack['m_bowDraw'])}
    print(json.dumps(result, indent=2))
    assert result['aiRange'] == 2.0 and result['aiMinimumRange'] == 0.0 and result['aiInterval'] == 2.0
    assert result['attackAnimation'] == 'swing_longsword' and result['chargeAnimationBool'] == ''
    assert not result['requiresReload'] and not result['bowDraw']
    print('NativeHumanSword PASS: 7 serialized native weapon assertions; live attacks NOT RUN.')
    break
else: raise SystemExit('Native sword ItemDrop not found')
