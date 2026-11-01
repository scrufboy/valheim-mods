"""Package an already embedded test DLL, without modifying the source profile."""
import argparse
import hashlib
import json
import shutil
import zipfile
from pathlib import Path


def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def package(profile, root):
    stage = root / 'dist/Souls'
    dll = stage / 'BepInEx/plugins/LWC-SoulsReskinTest/SagesVault.dll'
    assert dll.is_file(), 'Run Embed-Bundle.ps1 first'
    source = profile / 'BepInEx/config/Souls Of The Land - Enemy Pack'
    target = stage / 'BepInEx/config/Souls Of The Land - Enemy Pack'
    target.mkdir(parents=True, exist_ok=True)
    configs = []
    roster = []
    for p in sorted(source.iterdir()):
        if p.suffix not in ('.json', '.cfg'):
            continue
        dest = target / p.name
        shutil.copy2(p, dest)
        assert digest(p) == digest(dest)
        configs.append({'file': str(dest.relative_to(stage)), 'sha256': digest(dest)})
        if p.suffix == '.json':
            d = json.loads(p.read_text(encoding='utf-8-sig'))
            roster.append({'file': p.name, 'prefab': 'RRRN_' + d['sNewPrefabName'],
                           'before': digest(p), 'after': digest(dest), 'status': 'UNCHANGED'})
    assert len(roster) == 6
    art = root.parent.parent / 'art/sages-vault-original'
    for name in ('LICENSE', 'README.md'):
        dest = stage / 'LWC-Texture-Attribution' / name
        dest.parent.mkdir(exist_ok=True)
        shutil.copy2(art / name, dest)
    shutil.copy2(root / 'README.md', stage / 'README.md')
    evidence = stage / 'Verification'
    evidence.mkdir(exist_ok=True)
    for name in ('bundle-verification.json', 'assembly-verification.json'):
        shutil.copy2(root / 'build' / name, evidence / name)
    # Verify the final assembly really contains the tested bundle.
    import dnfile
    pe = dnfile.dnPE(str(dll))
    bundle = next(r.data for r in pe.net.resources if str(r.name) == 'SagesVault.sagerobes')
    report = json.loads((evidence / 'bundle-verification.json').read_text())
    assert hashlib.sha256(bundle).hexdigest() == report['output_bundle_sha256']
    (evidence / 'roster-before-after.json').write_text(json.dumps(roster, indent=2), encoding='utf-8')
    (stage / 'TEST-STATUS.txt').write_text(
        'OFFLINE-TESTED ONLY. Not installed. In-game bundle loading and rendering UNKNOWN.\n'
        'Replaces the separate Sages Vault equipment copy; retains its original plugin identity.\n'
        'DO NOT load both copies. Requires the existing BepInEx, Jotunn, RRRCore, Spawn That and compatibility environment.\n'
        'NPC definitions and natural spawn configuration copied unchanged from the selected test profile.\n'
        'Current Custom Raids and LWC character files are not packaged or modified.\n', encoding='utf-8')
    file_hashes = {p.relative_to(stage).as_posix(): digest(p) for p in stage.rglob('*')
                   if p.is_file() and p != stage / 'SHA256.json'}
    (stage / 'SHA256.json').write_text(json.dumps(file_hashes, indent=2), encoding='utf-8')
    archive = stage.parent / 'LWC-Souls-Embedded-Reskin-Test-0.1.1.zip'
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
        for p in sorted(stage.rglob('*')):
            if p.is_file():
                z.write(p, p.relative_to(stage))
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        for name, h in file_hashes.items():
            assert hashlib.sha256(z.read(name)).hexdigest() == h, name
    summary = {'archive': str(archive.resolve()), 'sha256': digest(archive),
               'bytes': archive.stat().st_size, 'npc_definitions_unchanged': len(roster),
               'configs_unchanged': len(configs), 'installed': False,
               'in_game_test': 'UNKNOWN', 'configuration_source': str(profile)}
    (root / 'build/package-verification.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--profile', type=Path, required=True)
    p.add_argument('--root', type=Path, default=Path(__file__).resolve().parent)
    a = p.parse_args()
    package(a.profile, a.root)
