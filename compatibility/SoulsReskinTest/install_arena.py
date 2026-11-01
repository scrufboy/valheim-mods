"""Install an approved candidate into Arena, preserving the managed plugin slot."""
from pathlib import Path
import datetime
import hashlib
import json
import re
import shutil
import subprocess
import zipfile


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def install():
    root = Path(__file__).resolve().parent
    profile = Path.home() / 'AppData/Roaming/r2modmanPlus-local/Valheim/profiles/LWC Formidable Arena'
    archive = root / 'dist/LWC-Souls-Embedded-Reskin-Test-0.1.1.zip'
    package_report = json.loads((root / 'build/package-verification.json').read_text())
    assembly_report = json.loads((root / 'build/assembly-verification.json').read_text(encoding='utf-8-sig'))
    expected_zip = package_report['sha256']
    expected_old = {'d2d9ea86325cc516c90d86512270cd940aff01600a42a4a55d3b3ec7c8b82b3d',
                    '7083d5843a9a492b4b91652230c2cd8f91873322efb99b7bc402bc760442f5e2'}
    expected_new = assembly_report['outputDllSha256'].lower()
    processes = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq valheim.exe', '/FO', 'CSV'], capture_output=True, text=True, check=True)
    assert '"valheim.exe"' not in processes.stdout.lower(), 'Close Valheim before installing'
    assert sha(archive) == expected_zip, 'Candidate archive changed'
    slot = profile / 'BepInEx/plugins/Unknown-SagesVault-1.1.4-DIAGNOSTIC-1-Artifact-Log-Cleanup.zip'
    dll = slot / 'SagesVault.dll'
    assert sha(dll) in expected_old, 'Installed baseline differs from reviewed source'
    active = list((profile / 'BepInEx/plugins').rglob('SagesVault.dll'))
    assert active == [dll], f'Unexpected duplicate equipment runtime: {active}'
    protected = {}
    for folder in ('BepInEx/plugins', 'BepInEx/config'):
        for p in (profile / folder).rglob('*'):
            if p.is_file():
                protected[p.relative_to(profile).as_posix()] = sha(p)
    with zipfile.ZipFile(archive) as z:
        hashes = json.loads(z.read('SHA256.json'))
        for name, h in hashes.items():
            assert hashlib.sha256(z.read(name)).hexdigest() == h, name
        for name in hashes:
            if name.startswith('BepInEx/config/'):
                assert sha(profile / name) == hashes[name], f'Config drift: {name}'
        candidate = z.read('BepInEx/plugins/LWC-SoulsReskinTest/SagesVault.dll')
    assert hashlib.sha256(candidate).hexdigest() == expected_new
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    backup = profile / 'BepInEx/LivingWorldBackups' / ('SoulsReskin-' + stamp)
    backup.mkdir(parents=True, exist_ok=False)
    mods = profile / 'mods.yml'
    old_mods = mods.read_bytes()
    key = 'Unknown-SagesVault-1.1.4-DIAGNOSTIC-1-Artifact-Log-Cleanup.zip'
    text = old_mods.decode('utf-8-sig')
    blocks = re.split(r'(?=^- manifestVersion:)', text, flags=re.M)
    hits = [i for i, b in enumerate(blocks) if f'  name: {key}\n' in b.replace('\r\n', '\n')]
    assert len(hits) == 1, 'Expected exactly one managed equipment entry'
    index = hits[0]
    assert re.search(r'^  enabled: true\s*$', blocks[index], re.M), 'Managed equipment entry is not enabled'
    block = blocks[index]
    # Retain the manager identity/path: no stale duplicate slot and no dependency churn.
    block = re.sub(r'^  displayName:.*$', '  displayName: LWC Souls Embedded Reskin Test 0.1.1', block, flags=re.M)
    blocks[index] = block
    updated = ''.join(blocks).encode('utf-8')
    manifest_path = slot / 'mm_v2_manifest.json'
    old_manifest = manifest_path.read_bytes()
    manifest = json.loads(old_manifest)
    manifest['displayName'] = 'LWC Souls Embedded Reskin Test 0.1.1'
    manifest['description'] = 'Embedded LWC clothing, crown, book and staff art. Existing Sages registration retained for local Souls visual test.'
    manifest['enabled'] = True
    backup_files = [dll, mods, manifest_path]
    for p in backup_files:
        shutil.copy2(p, backup / p.name)
    (backup / 'profile-before-sha256.json').write_text(json.dumps(protected, indent=2), encoding='utf-8')
    changed = []
    try:
        dll.write_bytes(candidate)
        changed.append(dll)
        mods.write_bytes(updated)
        changed.append(mods)
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
        changed.append(manifest_path)
        assert sha(dll) == expected_new
        assert re.search(r'^  enabled: true\s*$', block, re.M)
        assert len(list((profile / 'BepInEx/plugins').rglob('SagesVault.dll'))) == 1
        allowed = {dll.relative_to(profile).as_posix(), manifest_path.relative_to(profile).as_posix()}
        untouched = 0
        for relative, before in protected.items():
            if relative not in allowed:
                assert sha(profile / relative) == before, f'Unexpected mutation: {relative}'
                untouched += 1
    except Exception:
        for p in reversed(changed):
            shutil.copy2(backup / p.name, p)
        raise
    result = {'installed': True, 'profile': str(profile), 'plugin_path': str(dll),
              'plugin_sha256': sha(dll), 'enabled': True, 'active_equipment_copies': 1,
              'backup': str(backup), 'unchanged_profile_files': untouched,
              'game_launched': False, 'in_game_rendering': 'UNKNOWN',
              'manager_slot': key, 'manager_display': manifest['displayName']}
    (root / 'build/install-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    (backup / 'install-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    install()
