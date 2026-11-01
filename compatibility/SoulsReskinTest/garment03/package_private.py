"""Package a verified private Garment03 DLL; all profile accesses are read-only."""
import argparse,hashlib,json,shutil,zipfile
from pathlib import Path
import dnfile

def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def resources(p):
 pe=dnfile.dnPE(str(p))
 try:return {str(r.name):hashlib.sha256(r.data).hexdigest() for r in pe.net.resources if isinstance(r.data,bytes)}
 finally:pe.close()
def package(profile,build,art):
 root=Path(__file__).resolve().parents[1];assert build.resolve().is_relative_to((root.parents[1]/'build').resolve())
 stage=build/'Pack';stage.mkdir(exist_ok=False)
 source_dir=profile/'BepInEx/plugins/LWC-Souls_Art_Test';source_dll=source_dir/'LWC.SoulsArtTest.dll';candidate=build/'LWC.SoulsArtTest.dll'
 configs=profile/'BepInEx/config/Souls Of The Land - Enemy Pack';before={str(p):digest(p) for p in [source_dll,profile/'mods.yml',*configs.iterdir()] if p.is_file()}
 bundle_report=json.loads((build/'bundle-verification.json').read_text());assert digest(source_dll)==bundle_report['source_dll_sha256']
 old,new=resources(source_dll),resources(candidate);assert old.keys()==new.keys()
 assert new['SagesVault.sagerobes']==bundle_report['output_bundle_sha256']
 assert all(new[n]==h for n,h in old.items() if n!='SagesVault.sagerobes')
 plugin=stage/'BepInEx/plugins/LWC-Souls_Garment03_Art_Test';plugin.mkdir(parents=True);shutil.copy2(candidate,plugin/'LWC.SoulsArtTest.dll')
 manifest={'name':'Souls_Garment03_Art_Test','version_number':'0.1.4','description':'PRIVATE ART CANDIDATE: LWC Souls Garment03 Art Test. Eleven Garment03 PNGs embedded; seven clothing materials retargeted. Existing six NPC definitions unchanged. NOT INSTALLED.','dependencies':[],'website_url':''}
 # This local private manifest does not pretend the legacy runtime dependencies vanished.
 readme='''# LWC Souls Garment03 Art Test — 0.1.4

**PRIVATE ART CANDIDATE. OFFLINE-TESTED ONLY. NOT INSTALLED. NOT A PUBLIC RELEASE.**

Eleven Garment03 PNGs are embedded in a copied Souls equipment runtime. Seven brown/black robe, chest, tunic, and hood materials use the matching maps. Robe, chest, and hood normals/metal are distinct. Existing color tints, shader keywords, gameplay, all other art/materials, and the fixed memory loader are retained. Six NPC definitions and the natural spawn config are copied byte for byte from the selected Arena environment. ArmorIronLegs and ArmorRootLegs remain as configured.

Startup label: LWC Souls Garment03 Art Test 0.1.4. Compatibility GUID remains fantu.sagesvault. Do not load alongside another Sages Vault/LWC Souls Art Test equipment runtime. This ZIP is staged for review; nothing was enabled or installed.

This still contains the original embedded registration runtime, prefabs, meshes and existing other artwork. It is not a claim of independent implementation or public redistribution clearance. The Cursor Garment03 source/provenance README is preserved in Art-Provenance. No new permission or ownership claim is inferred from texture replacement.

Requires the existing test environment: BepInEx, Jotunn, RRRCore, Spawn That, RunicPack, MadHatter and current compatibility components. The empty local manifest dependency array is not a complete environment specification. Runtime code and dependencies are unchanged.

Verification includes exact input PNG pixels after bundle reload, complete mip chains, linear normal/metal data flags, actual declared shader properties, all untouched serialized object bytes, unchanged assembly method instructions and resource hashes, unchanged configs, ZIP hashes. In-game rendering and appearance remain UNKNOWN for this candidate. Do not replace the active baseline without a separate installation step.
'''
 for destination in [stage,plugin]:
  (destination/'manifest.json').write_text(json.dumps(manifest,indent=2));(destination/'README.md').write_text(readme,encoding='utf-8');shutil.copy2(source_dir/'icon.png',destination/'icon.png')
 provenance=stage/'Art-Provenance';provenance.mkdir();shutil.copy2(art/'README.md',provenance/'Garment03-README.md')
 records=[];target=stage/'BepInEx/config/Souls Of The Land - Enemy Pack';target.mkdir(parents=True)
 for p in sorted(configs.iterdir()):
  if p.suffix not in ('.json','.cfg'):continue
  dest=target/p.name;shutil.copy2(p,dest);assert digest(p)==digest(dest);records.append({'file':p.name,'before':digest(p),'after':digest(dest),'status':'UNCHANGED'})
 assert sum(x['file'].endswith('.json') for x in records)==6
 evidence=stage/'Verification';evidence.mkdir()
 for name in ['bundle-verification.json','assembly-verification.json','label-verification.json']:shutil.copy2(build/name,evidence/name)
 (evidence/'npc-configs-before-after.json').write_text(json.dumps(records,indent=2))
 (evidence/'resources-verification.json').write_text(json.dumps({'source':old,'candidate':new,'unchanged_resources_except_sagerobes':True},indent=2))
 hashes={p.relative_to(stage).as_posix():digest(p) for p in stage.rglob('*') if p.is_file()};(stage/'SHA256.json').write_text(json.dumps(hashes,indent=2))
 archive=build/'PRIVATE-LWC-Souls-Garment03-Art-Test-0.1.4.zip';assert not archive.exists()
 with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
  for p in sorted(stage.rglob('*')):
   if p.is_file():z.write(p,p.relative_to(stage))
 with zipfile.ZipFile(archive) as z:
  assert z.testzip() is None
  for name,h in hashes.items():assert hashlib.sha256(z.read(name)).hexdigest()==h,name
 for p,h in before.items():assert digest(Path(p))==h,p
 result={'status':'OFFLINE-TESTED','name':'LWC Souls Garment03 Art Test','version':'0.1.4','private_art_candidate':True,'archive':str(archive.resolve()),'archive_sha256':digest(archive),'dll_sha256':digest(candidate),'configs_unchanged':len(records),'npc_definitions_unchanged':6,'profile_files_unchanged':len(before),'added_pngs':11,'target_materials':7,'slot_changes':21,'unresolved_shader_properties':[],'installed':False,'enabled':False,'live_rendering':'UNKNOWN'}
 (build/'package-verification.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--profile',type=Path,required=True);p.add_argument('--build',type=Path,required=True);p.add_argument('--art',type=Path,required=True);a=p.parse_args();package(a.profile,a.build,a.art)
