#!/usr/bin/env python3
"""Package only this compiled lab mod; never installs or bundles game resources."""
import hashlib
import json
from pathlib import Path
import struct
import zipfile
import zlib

ROOT = Path(__file__).resolve().parent

def digest(data):
    return hashlib.sha256(data).hexdigest()

def icon():
    # Original, generated package mark. No external font, texture or game asset.
    glyphs = {'L': ('10000','10000','10000','10000','10000','10000','11111'),
              'W': ('10001','10001','10001','10101','10101','11011','10001'),
              'S': ('01111','10000','10000','01110','00001','00001','11110')}
    rows = []
    for y in range(256):
        row = bytearray()
        for x in range(256):
            color = (20, 28, 23)
            if 15 <= x <= 240 and 15 <= y <= 240: color = (108, 72, 40)
            if 28 <= x <= 227 and 28 <= y <= 227: color = (0, 190 + (y - 28) // 4, 0)
            for i, letter in enumerate('LWS'):
                gx, gy = (x - (36 + i * 65)) // 10, (y - 92) // 10
                if 0 <= gx < 5 and 0 <= gy < 7 and glyphs[letter][gy][gx] == '1': color = (242, 239, 217)
            row.extend(color)
        rows.append(b'\x00' + bytes(row))
    def chunk(tag, data):
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 256, 256, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(b''.join(rows), 9)) + chunk(b'IEND', b'')

def main():
    report = json.loads((ROOT / 'build/build-verification.json').read_text())
    if report['status'] != 'COMPILED': raise SystemExit('Compile must pass before packaging.')
    dll = (ROOT / 'build/LivingWorld.Studio.dll').read_bytes()
    if digest(dll) != report['sha256']: raise SystemExit('Compiled DLL hash mismatch.')
    for entry in report['sources']:
        if digest((ROOT / entry['path']).read_bytes()) != entry['sha256']:
            raise SystemExit('Source changed after compile: ' + entry['path'])
    tests = (ROOT / 'build/journal-tests.txt').read_text()
    if 'PASS StudioJournal: 28 assertions.' not in tests: raise SystemExit('Current journal tests must pass.')
    metadata = json.loads((ROOT / 'manifest.json').read_text())
    if metadata['version_number'] != '0.1.0': raise SystemExit('Unexpected package version.')
    files = {'BepInEx/plugins/LivingWorldStudio/LivingWorld.Studio.dll': dll,
             'icon.png': icon(), 'manifest.json': (ROOT / 'manifest.json').read_bytes(),
             'LICENSE': (ROOT.parent / 'LICENSE').read_bytes()}
    for name in ('README.md', 'CHANGELOG.md', 'PHOTOGRAPHY-PLAN.md', 'API-AUDIT.md'):
        files[name] = (ROOT / name).read_bytes()
    summary = {'version': '0.1.0', 'status': 'COMPILED / OFFLINE-TESTED', 'installed': False,
               'live_tested': False, 'reference_game_version': '1.0.17', 'journal_assertions': 28,
               'scope': 'Disposable local player/scenery lab; no real NPC transfer/posing or multiplayer',
               'dll_sha256': digest(dll), 'sources': report['sources'],
               'references': [{'name': Path(r['path']).name, 'sha256': r['sha256']} for r in report['references']]}
    files['BUILD-VERIFICATION.json'] = (json.dumps(summary, indent=2) + '\n').encode()
    files['SHA256SUMS.txt'] = ''.join(digest(data) + '  ' + name + '\n' for name, data in sorted(files.items())).encode()
    destination = ROOT / 'dist/scrufboy-LivingWorld_Studio_Lab-0.1.0.zip'
    destination.parent.mkdir(exist_ok=True)
    with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as package:
        for name, data in sorted(files.items()): package.writestr(name, data)
    with zipfile.ZipFile(destination) as package:
        assert package.testzip() is None
        assert [n for n in package.namelist() if n.endswith('.dll')] == ['BepInEx/plugins/LivingWorldStudio/LivingWorld.Studio.dll']
    destination.with_suffix('.zip.sha256').write_text(digest(destination.read_bytes()) + '  ' + destination.name + '\n')
    print(destination)
    print('DLL SHA256 ' + digest(dll))
    print('Package SHA256 ' + digest(destination.read_bytes()))

if __name__ == '__main__':
    main()
