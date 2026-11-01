"""Give the approved Arena art test a genuine managed package identity/version."""
from pathlib import Path
import datetime
import hashlib
import json
import re
import shutil
import zipfile

import dnfile
from PIL import Image, ImageDraw, ImageFont


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def bundle(p):
    pe = dnfile.dnPE(str(p))
    try:
        return next(r.data for r in pe.net.resources if str(r.name) == 'SagesVault.sagerobes')
    finally:
        pe.close()


def main():
    root = Path(__file__).resolve().parent
    record = json.loads((root / 'build/install-verification.json').read_text())
    profile = Path(record['profile'])
    plugins = (profile / 'BepInEx/plugins').resolve()
    old_dir = Path(record['plugin_path']).parent.resolve()
    new_dir = (plugins / 'LWC-Souls_Art_Test').resolve()
    # Check absolute boundaries before the directory rename.
    assert old_dir.parent == plugins and new_dir.parent == plugins
    assert old_dir.is_dir() and not new_dir.exists()
    source = Path(record['plugin_path'])
    assert sha(source) == record['plugin_sha256']
    labelled = root / 'build/LWC.SoulsArtTest.dll'
    assert bundle(source) == bundle(labelled), 'Labelling changed the art bundle'
    mods = profile / 'mods.yml'
    old_yaml = mods.read_bytes()
    text = old_yaml.decode('utf-8-sig')
    blocks = re.split(r'(?=^- manifestVersion:)', text, flags=re.M)
    old_key = record['manager_slot']
    hits = [i for i, b in enumerate(blocks) if f'  name: {old_key}\n' in b]
    assert len(hits) == 1
    index = hits[0]
    old_block = blocks[index]
    new_key = 'LWC-Souls_Art_Test'
    assert not any(f'  name: {new_key}\n' in b for b in blocks)
    block = old_block
    for key, value in {'name': new_key, 'authorName': 'LWC',
                       'displayName': 'LWC Souls Art Test',
                       'description': 'Preview 01 - original LWC clothing, books, crowns and staff textures. Art test build 0.1.2.'}.items():
        block = re.sub(r'^  ' + key + r':.*$', '  ' + key + ': ' + value, block, flags=re.M)
    block, count = re.subn(r'  versionNumber:\n    major: \d+\n    minor: \d+\n    patch: \d+',
                          '  versionNumber:\n    major: 0\n    minor: 1\n    patch: 2', block)
    assert count == 1 and re.search(r'^  enabled: true\s*$', block, re.M)
    blocks[index] = block
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    backup = profile / 'BepInEx/LivingWorldBackups' / ('SoulsLabels-' + stamp)
    backup.mkdir(parents=True, exist_ok=False)
    for p in old_dir.iterdir():
        assert p.is_file()
        shutil.copy2(p, backup / p.name)
    shutil.copy2(mods, backup / 'mods.yml')
    package_manifest = {'name': 'Souls_Art_Test', 'version_number': '0.1.2',
                        'description': 'LWC Souls Art Test - Preview 01. Embedded original LWC clothing, crown, book and staff textures.',
                        'dependencies': [], 'website_url': ''}
    manager = json.loads((old_dir / 'mm_v2_manifest.json').read_text())
    manager.update(name=new_key, authorName='LWC', displayName='LWC Souls Art Test',
                   description=package_manifest['description'], enabled=True,
                   versionNumber={'major': 0, 'minor': 1, 'patch': 2},
                   icon='icon.png', onlineSource=False)
    readme = '''# LWC Souls Art Test — 0.1.2

**Art pass: Preview 01.** Original LWC textures embedded for the Arena visual test.

This is the same art and material configuration as test 0.1.1. Build 0.1.2 adds
a proper package identity, version, icon, DLL filename and BepInEx startup label.
It does not repaint anything or change NPC stats, behavior, loadouts or raids.

Look first at Pillagers (brown robe, black/brown hood, black tunic) and Servants
(black robe). Other current NPC loadouts do not wear this equipment. Books,
crowns, pants and staves are included for item inspection.

Manager: LWC Souls Art Test / LWC / 0.1.2 / enabled.
Folder: BepInEx/plugins/LWC-Souls_Art_Test
DLL: LWC.SoulsArtTest.dll
Startup: Loading [LWC Souls Art Test 0.1.2]

The compatibility plugin GUID remains fantu.sagesvault. Only one equipment
runtime may be active. Existing Sages registration and prefabs remain the test
foundation; LWC authored the replacement PNGs. Runtime internals still use the
SagesVault assembly/resource names for compatibility. These are distinct from
the visible pack identity above. PNG attribution is in LICENSE-LWC-TEXTURES.

Offline texture, material and prefab checks passed. In-game visuals await
Brian's inspection. Prior versions are backed up under BepInEx/LivingWorldBackups.
'''
    icon = Image.new('RGB', (256, 256), '#14232c')
    draw = ImageDraw.Draw(icon)
    font = 'C:/Windows/Fonts/segoeuib.ttf'
    draw.rectangle((8, 8, 247, 247), outline='#c4a96b', width=5)
    for label, y, size, color in [('LWC', 18, 58, '#e5d1a5'), ('SOULS', 85, 36, 'white'),
                                  ('ART TEST', 131, 30, 'white'), ('0.1.2', 179, 38, '#e5d1a5')]:
        f = ImageFont.truetype(font, size)
        draw.text((128, y), label, anchor='mt', font=f, fill=color)
    old_dir.rename(new_dir)
    try:
        (new_dir / 'SagesVault.dll').rename(backup / 'installed-before-label.dll')
        shutil.copy2(labelled, new_dir / 'LWC.SoulsArtTest.dll')
        (new_dir / 'manifest.json').write_text(json.dumps(package_manifest, indent=2), encoding='utf-8')
        (new_dir / 'mm_v2_manifest.json').write_text(json.dumps(manager, indent=2), encoding='utf-8')
        (new_dir / 'README.md').write_text(readme, encoding='utf-8')
        shutil.copy2(root.parent.parent / 'art/sages-vault-original/LICENSE', new_dir / 'LICENSE-LWC-TEXTURES')
        icon.save(new_dir / 'icon.png')
        mods.write_text(''.join(blocks), encoding='utf-8')
        assert sha(new_dir / 'LWC.SoulsArtTest.dll') == sha(labelled)
        matches = [p for p in plugins.rglob('*.dll') if b'fantu.sagesvault' in p.read_bytes()]
        assert matches == [new_dir / 'LWC.SoulsArtTest.dll'], matches
    except Exception:
        # Restore prior runtime to the renamed folder; backup retains all old metadata.
        (backup / 'RECOVERY-NEEDED.txt').write_text('Labelling did not finish. Restore this backup to the original managed plugin folder and restore mods.yml.', encoding='utf-8')
        raise
    record.update(plugin_path=str(new_dir / 'LWC.SoulsArtTest.dll'),
                  plugin_sha256=sha(new_dir / 'LWC.SoulsArtTest.dll'),
                  manager_slot=new_key, manager_display='LWC Souls Art Test',
                  package_version='0.1.2', art_pass='Preview 01', label_backup=str(backup))
    (root / 'build/install-verification.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    # Produce an equally named and versioned local archive, including manager metadata.
    stage = root / 'dist/Named'
    plugin_stage = stage / 'BepInEx/plugins/LWC-Souls_Art_Test'
    plugin_stage.mkdir(parents=True, exist_ok=True)
    for p in new_dir.iterdir():
        if p.is_file():
            shutil.copy2(p, plugin_stage / p.name)
    (stage / 'manifest.json').write_text(json.dumps(package_manifest, indent=2), encoding='utf-8')
    (stage / 'README.md').write_text(readme, encoding='utf-8')
    icon.save(stage / 'icon.png')
    archive = root / 'dist/LWC-Souls_Art_Test-0.1.2.zip'
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
        for p in sorted(stage.rglob('*')):
            if p.is_file():
                z.write(p, p.relative_to(stage))
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        assert sha(labelled) == hashlib.sha256(z.read('BepInEx/plugins/LWC-Souls_Art_Test/LWC.SoulsArtTest.dll')).hexdigest()
    record['labelled_archive'] = str(archive)
    record['labelled_archive_sha256'] = sha(archive)
    (root / 'build/naming-verification.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(json.dumps(record, indent=2))


if __name__ == '__main__':
    main()
