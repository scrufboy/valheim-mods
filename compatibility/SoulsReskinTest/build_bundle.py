"""Build a private Souls equipment reskin; never writes to a game profile."""
import argparse
import copy
import hashlib
import json
from pathlib import Path

import dnfile
import UnityPy
from PIL import Image


def sha(data):
    return hashlib.sha256(data).hexdigest()


def build(source, art, output):
    output.mkdir(parents=True, exist_ok=True)
    pe = dnfile.dnPE(str(source))
    original = next(r.data for r in pe.net.resources if str(r.name) == 'SagesVault.sagerobes')
    env = UnityPy.load(original)
    objects = list(env.objects)
    textures = {o.path_id: o for o in objects if o.type.name == 'Texture2D'}
    original_raw = {o.path_id: sha(o.get_raw_data()) for o in objects}
    template = next(iter(textures.values()))
    added = {}
    inputs = {}
    # Add isolated texture objects: do not recolor shared vanilla maps/effects.
    for i, p in enumerate(sorted(art.glob('*.png'))):
        if p.name.startswith('_preview_'):
            continue
        obj = copy.copy(template)
        obj.path_id = 7000000000000000000 + i
        assert obj.path_id not in template.assets_file.objects
        obj.set_raw_data(template.get_raw_data())
        template.assets_file.objects[obj.path_id] = obj
        tex = obj.read()
        tex.m_Name = 'LWC_' + p.stem
        with Image.open(p) as im:
            tex.set_image(im.convert('RGBA'), target_format=4, mipmap_count=12)
        tex.save()
        added[p.name] = obj.path_id
        inputs[p.name] = {'sha256': sha(p.read_bytes()), 'path_id': obj.path_id,
                          'width': tex.m_Width, 'height': tex.m_Height}

    edits = []
    original_materials = {}
    for obj in objects:
        if obj.type.name != 'Material':
            continue
        t = obj.read_typetree()
        original_materials[obj.path_id] = copy.deepcopy(t)
        name = t['m_Name']
        slots = dict(t['m_SavedProperties']['m_TexEnvs'])
        desired = {}
        if name.startswith('sagehood_'):
            desired['_MainTex'] = 'sagehood_d.png'
        elif name in ('SageCrownGold', 'SageCrownSilver', 'SageCrownObsidian'):
            desired['_MainTex'] = 'sagecrown_d.png'
        elif name.startswith('sagerobe_'):
            desired = {'_BumpMap': 'sagecloth_n.png', '_MetallicGlossMap': 'sagecloth_m.png'}
            color = name.split('_')[1]
            desired['_MainTex'] = f'sagerobe_{color}_d.png'
        elif name.startswith(('sagerobechest_', 'sagetunicchest_')):
            color = name.split('_')[1]
            suffix = ' 1' if color in ('gray', 'white', 'black') else ''
            if name.startswith('sagerobechest_'):
                # An unused placeholder has no chest albedo; preserve that fact.
                if slots.get('_ChestTex', {}).get('m_Texture', {}).get('m_PathID', 0):
                    desired = {'_ChestTex': f'sagerobechest_{color}_d{suffix}.png',
                               '_ChestBumpMap': 'sagecloth_n.png', '_ChestMetal': 'sagecloth_m.png'}
            else:
                desired = {'_MainTex': f'sagerobechest_{color}_d{suffix}.png',
                           '_BumpMap': 'sagecloth_n.png', '_MetallicGlossMap': 'sagecloth_m.png'}
        elif name == 'SagePants_mat':
            desired = {'_LegsTex': 'SagePants_d.png', '_LegsMetal': 'sagecloth_m.png',
                       '_LegsBumpMap': 'sagecloth_n.png'}
        elif name == 'SagePantsMesh_Mat':
            desired = {'_MainTex': 'SagePants_Chest_meshes_chest_d.png',
                       '_MetallicGlossMap': 'sagecloth_m.png', '_BumpMap': 'sagecloth_n.png'}
        elif name == 'sagebooks':
            desired['_MainTex'] = 'sagebooks.png'
        elif name in [f'SageStaff{i:02}' for i in range(1, 7)]:
            desired = {'_MainTex': name + '_BaseColor.png', '_BumpMap': 'SageStaff_Normal.png',
                       '_MetallicGlossMap': 'SageStaff_MetallicSmoothness.png',
                       '_EmissionMap': 'SageStaff_Emission.png'}
        elif name == 'ScholarStaffFire':
            desired['_MainTex'] = 'ScholarStaffFire_BaseColor.png'
        for slot, filename in desired.items():
            assert filename in added, (name, filename)
            assert slot in slots, (name, slot)
            old = copy.deepcopy(slots[slot]['m_Texture'])
            slots[slot]['m_Texture'] = {'m_FileID': 0, 'm_PathID': added[filename]}
            edits.append({'material_id': obj.path_id, 'material': name, 'slot': slot,
                          'before': old, 'file': filename, 'texture_id': added[filename]})
        if desired:
            obj.save_typetree(t)

    bundle = next(iter(env.files.values())).save(packer='lz4')
    bundle_path = output / 'sagerobes-reskinned.bundle'
    bundle_path.write_bytes(bundle)
    # Reload the serialized output, rather than trusting in-memory edits.
    check = UnityPy.load(bundle)
    after = {o.path_id: o for o in check.objects}
    material_edits = {}
    for e in edits:
        material_edits.setdefault(e['material_id'], {})[e['slot']] = e['texture_id']
    for obj in objects:
        if obj.path_id not in material_edits:
            assert sha(after[obj.path_id].get_raw_data()) == original_raw[obj.path_id], obj.path_id
        else:
            expected = copy.deepcopy(original_materials[obj.path_id])
            for slot, val in expected['m_SavedProperties']['m_TexEnvs']:
                if slot in material_edits[obj.path_id]:
                    val['m_Texture'] = {'m_FileID': 0, 'm_PathID': material_edits[obj.path_id][slot]}
            assert after[obj.path_id].read_typetree() == expected, obj.path_id
    for filename, info in inputs.items():
        tex = after[info['path_id']].read()
        with Image.open(art / filename) as im:
            assert tex.image.convert('RGBA').tobytes() == im.convert('RGBA').tobytes(), filename
        assert tex.m_MipCount > 1, filename
    assert len(after) == len(objects) + len(added)
    report = {'source_dll_sha256': sha(source.read_bytes()), 'original_bundle_sha256': sha(original),
              'output_bundle_sha256': sha(bundle), 'added_textures': inputs,
              'material_slot_edits': edits, 'original_object_count': len(objects),
              'verified_unchanged_original_objects': len(objects) - len(material_edits),
              'changed_material_count': len(material_edits),
              'verification': 'Reloaded output; exact top-level PNG pixels; mipmaps; only planned material references changed; every other original object byte-identical.'}
    (output / 'bundle-verification.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f'PASS: {len(added)} embedded PNGs, {len(material_edits)} materials, {len(edits)} slots; all original non-target objects unchanged.')


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--source', type=Path, required=True)
    p.add_argument('--art', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    build(args.source, args.art, args.output)
