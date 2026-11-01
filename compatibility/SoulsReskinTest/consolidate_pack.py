"""Consolidate the Arena Souls configs and art runtime into one managed entry."""
from pathlib import Path
import datetime
import hashlib
import json
import re
import shutil
import zipfile


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def main():
    root = Path(__file__).resolve().parent
    record = json.loads((root / 'build/install-verification.json').read_text())
    profile = Path(record['profile'])
    plugins = (profile / 'BepInEx/plugins').resolve()
    old_key = 'Unknown-Souls-of-the-Land-Enemy-Pack-JSON-Fix.zip'
    old_dir = (plugins / old_key).resolve()
    current = (plugins / 'LWC-Souls_Art_Test').resolve()
    assert old_dir.parent == plugins and current.parent == plugins
    assert old_dir.is_dir() and current.is_dir()
    assert not list(old_dir.rglob('*.dll')), 'Legacy entry unexpectedly contains runtime code'
    mods = profile / 'mods.yml'
    original_yaml = mods.read_bytes()
    blocks = re.split(r'(?=^- manifestVersion:)', original_yaml.decode('utf-8-sig').replace('\r\n', '\n'), flags=re.M)
    old_index = [i for i, b in enumerate(blocks) if f'  name: {old_key}\n' in b]
    new_index = [i for i, b in enumerate(blocks) if '  name: LWC-Souls_Art_Test\n' in b]
    assert len(old_index) == len(new_index) == 1
    new_block = re.sub(r'^  description:.*$',
                       '  description: Souls NPC test pack with embedded LWC Preview 01 textures. Build 0.1.2.',
                       blocks[new_index[0]], flags=re.M)
    updated = []
    for i, block in enumerate(blocks):
        if i == old_index[0]:
            updated.append(new_block)
        elif i != new_index[0]:
            updated.append(block)
    new_yaml = ''.join(updated)
    assert old_key not in new_yaml
    assert new_yaml.count('  name: LWC-Souls_Art_Test\n') == 1
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    backup = (profile / 'BepInEx/LivingWorldBackups' / ('SoulsConsolidate-' + stamp)).resolve()
    assert backup.is_relative_to((profile / 'BepInEx/LivingWorldBackups').resolve())
    backup.mkdir(parents=True, exist_ok=False)
    shutil.copy2(mods, backup / 'mods.yml')
    for name in ('README.md', 'manifest.json', 'mm_v2_manifest.json'):
        shutil.copy2(current / name, backup / name)
    config = profile / 'BepInEx/config/Souls Of The Land - Enemy Pack'
    config_hashes = {p.name: sha(p) for p in config.iterdir() if p.suffix in ('.json', '.cfg')}
    assert len([n for n in config_hashes if n.endswith('.json')]) == 6
    dll = current / 'LWC.SoulsArtTest.dll'
    before_dll = sha(dll)
    metadata = json.loads((current / 'mm_v2_manifest.json').read_text())
    metadata['description'] = 'Complete Souls NPC test pack with embedded LWC Preview 01 equipment art. Build 0.1.2.'
    manifest = json.loads((current / 'manifest.json').read_text())
    manifest['description'] = metadata['description']
    inventory = {'name': 'LWC Souls Art Test', 'version': '0.1.2', 'art_pass': 'Preview 01',
                 'equipment_runtime': 'LWC.SoulsArtTest.dll', 'equipment_sha256': before_dll,
                 'npc_definitions_and_spawn_configuration': config_hashes,
                 'config_folder': 'BepInEx/config/Souls Of The Land - Enemy Pack'}
    readme = (current / 'README.md').read_text(encoding='utf-8')
    readme += '''

## One complete test-pack entry

This managed entry includes the existing six Souls NPC definitions and natural
spawn configuration as well as the embedded equipment art runtime. It replaces
the former Unknown/Souls JSON Fix manager entry. That obsolete metadata folder
is backed up outside plugins, not left as another installed pack.

The original config folder name is retained for loader compatibility. These
are the same NPC definitions and settings, unchanged. PACK-CONTENTS.json lists
the exact included configs and hashes. RunicPack, MadHatter, RRRCore, Spawn That
and the rest of the existing Arena environment remain separate dependencies.
'''
    archived_dir = (backup / 'Legacy-Souls-Entry').resolve()
    assert archived_dir.parent == backup and not archived_dir.exists()
    # Both resolved endpoints are checked above; this is a recoverable directory rename.
    old_dir.rename(archived_dir)
    try:
        mods.write_text(new_yaml, encoding='utf-8')
        (current / 'mm_v2_manifest.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
        (current / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
        (current / 'README.md').write_text(readme, encoding='utf-8')
        (current / 'PACK-CONTENTS.json').write_text(json.dumps(inventory, indent=2), encoding='utf-8')
        assert sha(dll) == before_dll
        for name, h in config_hashes.items():
            assert sha(config / name) == h
        assert old_key not in mods.read_text(encoding='utf-8')
    except Exception:
        mods.write_bytes(original_yaml)
        archived_dir.rename(old_dir)
        for name in ('README.md', 'manifest.json', 'mm_v2_manifest.json'):
            shutil.copy2(backup / name, current / name)
        raise
    stage = root / 'dist/Named'
    plugin_stage = stage / 'BepInEx/plugins/LWC-Souls_Art_Test'
    plugin_stage.mkdir(parents=True, exist_ok=True)
    for p in current.iterdir():
        if p.is_file():
            shutil.copy2(p, plugin_stage / p.name)
    target_config = stage / 'BepInEx/config/Souls Of The Land - Enemy Pack'
    target_config.mkdir(parents=True, exist_ok=True)
    for name in config_hashes:
        shutil.copy2(config / name, target_config / name)
    for name in ('manifest.json', 'README.md', 'icon.png'):
        shutil.copy2(current / name, stage / name)
    archive = root / 'dist/LWC-Souls_Art_Test-0.1.2.zip'
    hashes = {p.relative_to(stage).as_posix(): sha(p) for p in stage.rglob('*')
              if p.is_file() and p.name != 'SHA256.json'}
    (stage / 'SHA256.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
        for p in sorted(stage.rglob('*')):
            if p.is_file():
                z.write(p, p.relative_to(stage))
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        for name, h in hashes.items():
            assert hashlib.sha256(z.read(name)).hexdigest() == h, name
    result = {'status': 'PASS', 'manager_entries_for_test_pack': 1,
              'obsolete_manager_entry_removed': old_key, 'package_name': 'LWC Souls Art Test',
              'version': '0.1.2', 'enabled': metadata['enabled'],
              'configs_unchanged': len(config_hashes), 'runtime_unchanged': True,
              'backup': str(backup), 'archive': str(archive), 'archive_sha256': sha(archive),
              'ui_verified': False}
    (root / 'build/consolidation-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
