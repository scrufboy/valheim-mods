"""Read native MonoScript execution order; never launches Unity or writes game files."""
from pathlib import Path
import argparse, sys, json
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--unitypy-dir',type=Path,default=Path(__file__).resolve().parents[2] / 'human-npc' / 'assets' / 'python-libs',help='Read-only folder containing installed UnityPy and dependencies')
parser.add_argument('--bundles-dir',type=Path,default=Path(r'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles'),help='Native Valheim asset bundle directory (read-only)')
args=parser.parse_args()
if not (args.unitypy_dir / 'UnityPy').is_dir():
    raise SystemExit('UnityPy dependency missing; pass --unitypy-dir pointing to an existing read-only dependency directory')
sys.path.insert(0, str(args.unitypy_dir))
import UnityPy
base=args.bundles_dir
expected={'ZNetView':-20,'ZSyncTransform':-10,'ZSyncAnimation':-3,'Character':0,'Humanoid':0,'MonsterAI':0,'VisEquipment':0,'CharacterAnimEvent':0,'FootStep':0}
wanted=set(expected)
env=UnityPy.load(*[str(base/n) for n in ['6a33a62','2c2cce25','86c3d76e','8d5dbad8','9fe0899c','b7b69eaf','eb7a4308']])
found=[]
for obj in env.objects:
    if obj.type.name!='MonoScript': continue
    data=obj.read_typetree()
    if data.get('m_ClassName') in wanted:
        found.append({'class':data['m_ClassName'],'executionOrder':data.get('m_ExecutionOrder'),'pathId':obj.path_id,'assembly':data.get('m_AssemblyName')})
print(json.dumps(found,indent=2))
if {x['class'] for x in found} != wanted:
    raise SystemExit('Missing MonoScript definitions')
for entry in found:
    if entry['executionOrder'] != expected[entry['class']]:
        raise SystemExit('Native MonoScript execution order changed: '+entry['class'])
print('NativeScriptOrder PASS: 9 serialized execution-order assertions. Static asset evidence; no Unity lifecycle executed.')
