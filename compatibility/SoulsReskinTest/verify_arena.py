"""Offline prefab/material resolution simulation of the installed Arena candidate.

This does not run Unity physics, AI, shaders or an in-game scene.
"""
from pathlib import Path
import hashlib
import json
import re

import dnfile
import UnityPy
from PIL import Image


def sha(data):
    return hashlib.sha256(data).hexdigest()


def refs(value):
    if isinstance(value, dict):
        if 'm_FileID' in value and 'm_PathID' in value:
            yield value['m_FileID'], value['m_PathID']
        else:
            for child in value.values():
                yield from refs(child)
    elif isinstance(value, (list, tuple)):
        for child in value:
            yield from refs(child)


def load(path):
    pe = dnfile.dnPE(str(path))
    data = next(r.data for r in pe.net.resources if str(r.name) == 'SagesVault.sagerobes')
    env = UnityPy.load(data)
    return env, {o.path_id: o for o in env.objects}


def verify():
    root = Path(__file__).resolve().parent
    install = json.loads((root / 'build/install-verification.json').read_text())
    profile = Path(install['profile'])
    dll = Path(install['plugin_path'])
    assert sha(dll.read_bytes()) == install['plugin_sha256']
    report = json.loads((root / 'build/bundle-verification.json').read_text())
    baseline = next(p for p in (profile / 'BepInEx/LivingWorldBackups').glob('SoulsReskin-*/SagesVault.dll')
                    if sha(p.read_bytes()) == report['source_dll_sha256'])
    old_env, old = load(baseline)
    env, objects = load(dll)
    changed_ids = {e['material_id'] for e in report['material_slot_edits']}
    for pid, obj in old.items():
        if pid not in changed_ids:
            assert obj.get_raw_data() == objects[pid].get_raw_data(), pid
    for filename, info in report['added_textures'].items():
        tex = objects[info['path_id']].read()
        art = root.parent.parent / 'art/sages-vault-original' / filename
        assert sha(art.read_bytes()) == info['sha256'], filename
        with Image.open(art) as im:
            assert tex.image.convert('RGBA').tobytes() == im.convert('RGBA').tobytes(), filename
        assert tex.m_MipCount > 1
        linear = filename in ('sagecloth_n.png', 'sagecloth_m.png', 'SageStaff_Normal.png', 'SageStaff_MetallicSmoothness.png')
        assert tex.m_ColorSpace == (0 if linear else 1), filename
        assert tex.m_TextureSettings.m_FilterMode == 1, filename
    for e in report['material_slot_edits']:
        t = objects[e['material_id']].read_typetree()
        slots = dict(t['m_SavedProperties']['m_TexEnvs'])
        assert slots[e['slot']]['m_Texture'] == {'m_FileID': 0, 'm_PathID': e['texture_id']}
        before = old[e['material_id']].read_typetree()
        for key in ('m_Colors', 'm_Floats'):
            assert t['m_SavedProperties'][key] == before['m_SavedProperties'][key]

    # Walk the complete serialized reference closure for every exported prefab.
    # Baseline dangling local references, if present, must not grow in candidate.
    cache = {}
    old_cache = {}
    def walk(pid, pool, trees):
        seen, missing, stack = set(), set(), [pid]
        while stack:
            current = stack.pop()
            if not current or current in seen:
                continue
            if current not in pool:
                missing.add(current)
                continue
            seen.add(current)
            if current not in trees:
                trees[current] = pool[current].read_typetree()
            for file_id, path_id in refs(trees[current]):
                if file_id == 0 and path_id:
                    stack.append(path_id)
        return seen, missing

    prefabs = []
    names = set()
    assert set(env.container) == set(old_env.container)
    for path, ptr in env.container.items():
        if not path.endswith('.prefab'):
            continue
        assert ptr.path_id == old_env.container[path].path_id
        reachable, missing = walk(ptr.path_id, objects, cache)
        old_reachable, old_missing = walk(ptr.path_id, old, old_cache)
        assert missing == old_missing, (path, missing - old_missing)
        name = objects[ptr.path_id].read_typetree()['m_Name']
        names.add(name)
        bindings = [e for e in report['material_slot_edits'] if e['material_id'] in reachable]
        for e in bindings:
            assert e['texture_id'] in reachable
        prefabs.append({'name': name, 'serialized_objects_resolved': len(reachable),
                        'repainted_material_slots_reachable': len(bindings),
                        'baseline_dangling_local_refs': len(old_missing), 'new_dangling_refs': 0})

    def strings(value):
        if isinstance(value, str):
            yield value
        elif isinstance(value, dict):
            for x in value.values():
                yield from strings(x)
        elif isinstance(value, list):
            for x in value:
                yield from strings(x)
    roster = []
    config = profile / 'BepInEx/config/Souls Of The Land - Enemy Pack'
    expected_config = root / 'dist/Souls/BepInEx/config/Souls Of The Land - Enemy Pack'
    for p in config.glob('*.json'):
        assert p.read_bytes() == (expected_config / p.name).read_bytes()
        d = json.loads(p.read_text(encoding='utf-8-sig'))
        equipment = sorted({s for s in strings(d['Category_Humanoid']) if s.startswith(('Sage', 'Scholar'))})
        assert all(s in names for s in equipment), (p.name, set(equipment) - names)
        roster.append({'prefab': 'RRRN_' + d['sNewPrefabName'], 'equipment_resolved': equipment,
                       'definition_unchanged': True})
    assert len(roster) == 6
    cfg = 'spawn_that.world_spawners.souls_of_the_wood.cfg'
    assert (config / cfg).read_bytes() == (expected_config / cfg).read_bytes()
    plugins = profile / 'BepInEx/plugins'
    guid_copies = [str(p.relative_to(plugins)) for p in plugins.rglob('*.dll')
                   if b'fantu.sagesvault' in p.read_bytes()]
    assert len(guid_copies) == 1, guid_copies
    required = ['Jotunn.dll', 'RRRCore.dll', 'Valheim.SpawnThat.dll',
                'MadHatter.dll', 'RunicPack.dll', 'LivingWorld.RRRCoreCompat.dll']
    dependencies = {}
    for name in required:
        hits = list(plugins.rglob(name))
        assert len(hits) == 1, (name, hits)
        dependencies[name] = sha(hits[0].read_bytes())
    core = plugins / 'LivingWorldHumanNpc/LivingWorld.HumanNpc.dll'
    assert sha(core.read_bytes()) == 'd376d63beafa85714f9b0ec1912e5f98aef5e0697219c44220ec7443be7b6ab5'
    metadata = json.loads((dll.parent / 'mm_v2_manifest.json').read_text())
    assert metadata['enabled'] is True
    mods = (profile / 'mods.yml').read_text(encoding='utf-8-sig')
    blocks = re.split(r'(?=^- manifestVersion:)', mods, flags=re.M)
    block = next(b for b in blocks if f"  name: {install['manager_slot']}\n" in b)
    assert re.search(r'^  enabled: true\s*$', block, re.M)
    result = {'status': 'PASS', 'kind': 'OFFLINE prefab/material resolution simulation',
              'embedded_textures_verified': len(report['added_textures']),
              'material_slots_verified': len(report['material_slot_edits']),
              'prefabs_walked': len(prefabs), 'new_dangling_local_refs': 0,
              'npc_definitions_verified': len(roster), 'npc_equipment': roster,
              'prefab_results': prefabs, 'dependencies': dependencies,
              'active_plugin_guid_copies': len(guid_copies), 'core_baseline_unchanged': True,
              'enabled': True, 'unity_physics_ai_or_rendering_simulated': False,
              'in_game_rendering': 'UNKNOWN'}
    (root / 'build/arena-simulation-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k not in ('prefab_results', 'dependencies')}, indent=2))


if __name__ == '__main__':
    verify()
