"""Install the 0.1.3 memory-loader fix; retain embedded art and NPC settings."""
from pathlib import Path
import datetime
import hashlib
import json
import re
import shutil
import subprocess
import zipfile

import dnfile
from PIL import Image, ImageDraw, ImageFont


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def resource(p):
    a = dnfile.dnPE(str(p))
    try:
        return next(r.data for r in a.net.resources if str(r.name) == 'SagesVault.sagerobes')
    finally:
        a.close()


def main():
    root = Path(__file__).resolve().parent
    profile = Path.home() / 'AppData/Roaming/r2modmanPlus-local/Valheim/profiles/LWC Formidable Arena'
    folder = profile / 'BepInEx/plugins/LWC-Souls_Art_Test'
    dll = folder / 'LWC.SoulsArtTest.dll'
    source = root / 'build/0.1.3/LWC.SoulsArtTest.dll'
    assert sha(dll) == '21b3dae2eedfa494d50a1d63f5d834068ceb5cae5d0b2ef449e2723f9cb52c74'
    assert resource(source) == resource(dll), 'Loader fix changed embedded art'
    processes = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq valheim.exe', '/FO', 'CSV'], capture_output=True, text=True, check=True)
    assert '"valheim.exe"' not in processes.stdout.lower()
    protected = {str(p.relative_to(profile)): sha(p) for p in (profile / 'BepInEx/config').rglob('*') if p.is_file()}
    now = datetime.datetime.now(datetime.timezone.utc)
    backup = profile / 'BepInEx/LivingWorldBackups' / ('SoulsLoader-' + now.strftime('%Y%m%dT%H%M%SZ'))
    backup.mkdir(parents=True, exist_ok=False)
    for p in folder.iterdir():
        if p.is_file():
            shutil.copy2(p, backup / p.name)
    mods = profile / 'mods.yml'
    shutil.copy2(mods, backup / 'mods.yml')
    for log in [profile / 'BepInEx/LogOutput.log', Path.home() / 'AppData/LocalLow/IronGate/Valheim/Player.log']:
        if log.is_file():
            shutil.copy2(log, backup / log.name)
    text = mods.read_text(encoding='utf-8-sig')
    blocks = re.split(r'(?=^- manifestVersion:)', text, flags=re.M)
    hits = [i for i, b in enumerate(blocks) if '  name: LWC-Souls_Art_Test\n' in b]
    assert len(hits) == 1
    b = blocks[hits[0]]
    b = re.sub(r'  versionNumber:\n    major: 0\n    minor: 1\n    patch: 2',
               '  versionNumber:\n    major: 0\n    minor: 1\n    patch: 3', b)
    assert '    patch: 3' in b
    b = re.sub(r'^  installedAtTime:.*$', '  installedAtTime: ' + str(int(now.timestamp()*1000)), b, flags=re.M)
    b = re.sub(r'^  description:.*$', '  description: Souls NPC test pack with embedded LWC Preview 01 art. Memory-loader repair 0.1.3.', b, flags=re.M)
    blocks[hits[0]] = b
    mm = json.loads((folder / 'mm_v2_manifest.json').read_text())
    mm.update(versionNumber={'major': 0, 'minor': 1, 'patch': 3}, installedAtTime=int(now.timestamp()*1000),
              description='Complete Souls NPC test pack; embedded LWC Preview 01 artwork; repaired memory-backed bundle loader 0.1.3.')
    manifest = json.loads((folder / 'manifest.json').read_text())
    manifest.update(version_number='0.1.3', description=mm['description'])
    readme = (folder / 'README.md').read_text(encoding='utf-8').replace('0.1.2', '0.1.3')
    readme += '\n\n## 0.1.3 loader repair\n\nThe 0.1.2 bundle crashed during item loading because its loader disposed a stream still needed by Unity. The loader now copies the embedded resource into memory and calls LoadFromMemory. Artwork, material bindings, NPC definitions and behavior are unchanged.\n'
    contents = json.loads((folder / 'PACK-CONTENTS.json').read_text())
    contents.update(version='0.1.3', equipment_sha256=sha(source))
    icon = Image.new('RGB',(256,256),'#14232c');draw=ImageDraw.Draw(icon)
    draw.rectangle((8,8,247,247),outline='#c4a96b',width=5)
    for label,y,size,color in [('LWC',18,58,'#e5d1a5'),('SOULS',85,36,'white'),('ART TEST',131,30,'white'),('0.1.3',179,38,'#e5d1a5')]:
        draw.text((128,y),label,anchor='mt',font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',size),fill=color)
    shutil.copy2(source,dll)
    (folder/'mm_v2_manifest.json').write_text(json.dumps(mm,indent=2),encoding='utf-8')
    (folder/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    (folder/'PACK-CONTENTS.json').write_text(json.dumps(contents,indent=2),encoding='utf-8')
    (folder/'README.md').write_text(readme,encoding='utf-8');icon.save(folder/'icon.png')
    mods.write_text(''.join(blocks),encoding='utf-8')
    for rel,h in protected.items():
        assert sha(profile/rel)==h,rel
    stage=root/'dist/Fix13';target=stage/'BepInEx/plugins/LWC-Souls_Art_Test';target.mkdir(parents=True,exist_ok=True)
    for p in folder.iterdir():
        if p.is_file():shutil.copy2(p,target/p.name)
    c=profile/'BepInEx/config/Souls Of The Land - Enemy Pack'
    targetc=stage/'BepInEx/config/Souls Of The Land - Enemy Pack';targetc.mkdir(parents=True,exist_ok=True)
    for p in c.iterdir():
        if p.suffix in ('.json','.cfg'):shutil.copy2(p,targetc/p.name)
    for name in ('manifest.json','README.md','icon.png'):shutil.copy2(folder/name,stage/name)
    hashes={p.relative_to(stage).as_posix():sha(p) for p in stage.rglob('*') if p.is_file() and p.name!='SHA256.json'}
    (stage/'SHA256.json').write_text(json.dumps(hashes,indent=2),encoding='utf-8')
    archive=root/'dist/LWC-Souls_Art_Test-0.1.3.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(stage.rglob('*')):
            if p.is_file():z.write(p,p.relative_to(stage))
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        for name,h in hashes.items():assert hashlib.sha256(z.read(name)).hexdigest()==h,name
    record=json.loads((root/'build/install-verification.json').read_text())
    record.update(plugin_sha256=sha(dll),package_version='0.1.3',loader_backup=str(backup),game_launched=False)
    (root/'build/install-verification.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
    result={'version':'0.1.3','installed':True,'enabled':True,'bundle_unchanged':True,'configs_unchanged':len(protected),
            'dll_sha256':sha(dll),'backup':str(backup),'archive':str(archive),'archive_sha256':sha(archive),'unity_validation':'PENDING'}
    (root/'build/loader-fix-verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result,indent=2))


if __name__=='__main__':main()
