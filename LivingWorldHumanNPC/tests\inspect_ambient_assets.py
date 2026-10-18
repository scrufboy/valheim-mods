"""Read native crafting animation graph/events. No Unity scene or game writes."""
import argparse, json, sys
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('--unitypy-dir', default=r'C:\Users\scruf\Documents\Codex\2026-10-13\begin-now-dedicated-independent-valheim-living\work\human-npc\assets\python-libs')
p.add_argument('--bundles-dir', default=r'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles')
a = p.parse_args()
sys.path.insert(0, a.unitypy_dir)
import UnityPy
from UnityPy.classes.PPtr import PPtr
env = UnityPy.load(str(Path(a.bundles_dir) / 'c4210710'))
script_names = json.loads((Path(a.unitypy_dir).parent / 'native-script-names.json').read_text())
def ref(v, owner):
    return PPtr(m_FileID=v['m_FileID'], m_PathID=v['m_PathID'], assetsfile=owner.assets_file).deref()
controllers = [o for o in env.objects if o.type.name == 'AnimatorController' and o.read_typetree()['m_Name'] == 'Player_animator']
assert len(controllers) == 1, 'Native player graph missing/ambiguous'
controller = controllers[0]
graph = controller.read_typetree()
names = dict(graph['m_TOS'])
gesture_id = next(key for key, value in names.items() if value == 'emote_point')
assert any(v['m_ID'] == gesture_id and v['m_Type'] == 9 for v in graph['m_Controller']['m_Values']['data']['m_ValueArray']), 'Cleric gesture must be a native trigger'
gestures = [raw['data'] for machine in graph['m_Controller']['m_StateMachineArray'] for raw in machine['data']['m_StateConstantArray'] if names.get(raw['data']['m_NameID']) == 'Emote_point']
assert len(gestures) == 1, 'Cleric gesture state missing/ambiguous'
gesture_nodes = gestures[0]['m_BlendTreeConstantArray'][0]['data']['m_NodeArray']
assert len(gesture_nodes) == 1, 'Unexpected cleric gesture blend tree'
gesture_clip = ref(graph['m_AnimationClips'][gesture_nodes[0]['data']['m_ClipID']], controller).read_typetree()
assert gesture_clip['m_Name'] == 'Point' and not gesture_clip['m_Events'], 'Cleric gesture must not carry attack/gameplay events'
print('Cleric native emote_point -> Point, no animation events')
crafting = next(key for key, value in names.items() if value == 'crafting')
expected = {'Crafting_Workbench': ('Workbench', 1), 'Crafting_forge': ('Forge', 2)}
checked = set()
for machine in graph['m_Controller']['m_StateMachineArray']:
    states = machine['data']['m_StateConstantArray']
    for index, raw in enumerate(states):
        state = raw['data']
        name = names.get(state['m_NameID'])
        if name not in expected:
            continue
        clip_name, value = expected[name]
        nodes = state['m_BlendTreeConstantArray'][0]['data']['m_NodeArray']
        assert len(nodes) == 1, 'Unexpected crafting blend graph'
        clip_index = nodes[0]['data']['m_ClipID']
        clip = ref(graph['m_AnimationClips'][clip_index], controller).read_typetree()
        assert clip['m_Name'] == clip_name and state['m_Loop'], 'Craft state must loop its inspected clip'
        entries = [t['data'] for s in states for t in s['data']['m_TransitionConstantArray'] if t['data']['m_DestinationState'] == index]
        assert any(any(c['data']['m_EventID'] == crafting and c['data']['m_ConditionMode'] == 6 and c['data']['m_EventThreshold'] == value for c in t['m_ConditionConstantArray']) for t in entries), 'Craft entry must use matching crafting integer'
        assert any(any(c['data']['m_EventID'] == crafting and c['data']['m_ConditionMode'] == 7 and c['data']['m_EventThreshold'] == value for c in t['data']['m_ConditionConstantArray']) for t in state['m_TransitionConstantArray']), 'Craft exit must observe cleared crafting integer'
        checked.add(name)
        print(name, 'native crafting int', value, '->', clip_name, '(loop, parameter-driven exit)')
assert checked == set(expected), 'Missing inspected crafting state'
clips = [o for o in env.objects if o.type.name == 'AnimationClip' and o.read_typetree()['m_Name'] in ('Workbench', 'Forge')]
assert len(clips) == 2, 'Craft clips missing/ambiguous'
for clip in clips:
    d = clip.read_typetree()
    events = d['m_Events']
    print(d['m_Name'], 'events:', [e['functionName'] for e in events])
    if d['m_Name'] == 'Workbench':
        assert not events, 'Workbench must remain event-free'
    else:
        assert [e['functionName'] for e in events] == ['Attach', 'Effect']
        for event in events:
            root = ref(event['objectReferenceParameter'], clip)
            def walk(obj):
                data = obj.read_typetree()
                components = []
                transform = None
                for item in data['m_Component']:
                    c = ref(item['component'], obj)
                    cd = c.read_typetree()
                    name = c.type.name
                    if name == 'MonoBehaviour':
                        name = script_names.get(str(cd['m_Script']['m_PathID']), 'UNKNOWN')
                    components.append(name)
                    if name == 'Transform': transform = (c, cd)
                print(event['functionName'], data['m_Name'], components)
                assert set(components) <= {'Transform', 'MeshFilter', 'MeshRenderer', 'TimedDestruction', 'ZSFX', 'AudioSource', 'ParticleSystem', 'ParticleSystemRenderer'}, 'Unsafe/unreviewed crafting event prefab'
                if transform:
                    for child in transform[1]['m_Children']:
                        ct = ref(child, transform[0])
                        walk(ref(ct.read_typetree()['m_GameObject'], ct))
            walk(root)

# The standalone Fuling alert cue uses the same one-shot BaseAI effect path the
# companions use. Only the sound effect is referenced; no creature root or AI
# definition participates in the composed human.
voices = [(key, pointer) for key, pointer in env.container.items() if key.endswith('/sfx_goblin_alerted.prefab')]
assert len(voices) == 1, 'Native Fuling alert voice missing/ambiguous'
voice = voices[0][1].deref() if isinstance(voices[0][1], PPtr) else voices[0][1]
voice_sets = []
for item in voice.read_typetree()['m_Component']:
    component = ref(item['component'], voice)
    if component.type.name != 'MonoBehaviour':
        continue
    fields = component.read_typetree()
    if 'm_audioClips' in fields:
        voice_sets.append(fields)
assert len(voice_sets) == 1 and len(voice_sets[0]['m_audioClips']) == 3, 'Native Fuling alert variants changed'
assert voice_sets[0]['m_minPitch'] >= 1.09 and voice_sets[0]['m_maxPitch'] <= 1.21, 'Native Fuling alert pitch changed'
print('Native companion alert source -> sfx_goblin_alerted (3 clips, source pitch 1.10-1.20); compiled adapter must clone and lower it to 0.80-0.90')
print('NativeAmbientAssets PASS: native Workbench/Forge events and combat exclamation inspected. Unity NOT RUN.')
