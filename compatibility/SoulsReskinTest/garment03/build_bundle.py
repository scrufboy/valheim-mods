"""Build ONLY the private Garment03 texture resource; never writes a profile."""
import argparse,copy,hashlib,json,math
from pathlib import Path
import dnfile,UnityPy
from UnityPy.export import Texture2DConverter
from PIL import Image

def sha(data):return hashlib.sha256(data).hexdigest()
FILES={'sagerobe_brown_d.png','sagerobe_black_d.png','sagerobechest_brown_d.png','sagerobechest_black_d 1.png','sagehood_d.png','sagecloth_n.png','sagecloth_m.png','sagechest_n.png','sagechest_m.png','sagehood_n.png','sagehood_m.png'}
TARGETS={
 'sagerobe_brown_mat':{'_MainTex':'sagerobe_brown_d.png','_BumpMap':'sagecloth_n.png','_MetallicGlossMap':'sagecloth_m.png'},
 'sagerobe_black_mat':{'_MainTex':'sagerobe_black_d.png','_BumpMap':'sagecloth_n.png','_MetallicGlossMap':'sagecloth_m.png'},
 'sagerobechest_brown_mat':{'_ChestTex':'sagerobechest_brown_d.png','_ChestBumpMap':'sagechest_n.png','_ChestMetal':'sagechest_m.png'},
 'sagerobechest_black_mat':{'_ChestTex':'sagerobechest_black_d 1.png','_ChestBumpMap':'sagechest_n.png','_ChestMetal':'sagechest_m.png'},
 'sagetunicchest_black_mat':{'_MainTex':'sagerobechest_black_d 1.png','_BumpMap':'sagechest_n.png','_MetallicGlossMap':'sagechest_m.png'},
 'sagehood_brown':{'_MainTex':'sagehood_d.png','_BumpMap':'sagehood_n.png','_MetallicGlossMap':'sagehood_m.png'},
 'sagehood_black':{'_MainTex':'sagehood_d.png','_BumpMap':'sagehood_n.png','_MetallicGlossMap':'sagehood_m.png'},
}
def build(source,art,output):
 output.mkdir(parents=True,exist_ok=False)
 actual={p.name for p in art.glob('*.png') if not p.name.startswith('_')};assert actual==FILES,(actual,FILES)
 pe=dnfile.dnPE(str(source))
 try:raw=next(r.data for r in pe.net.resources if str(r.name)=='SagesVault.sagerobes')
 finally:pe.close()
 env=UnityPy.load(raw);env.path=str(source.parent);objects=list(env.objects)
 originals={o.path_id:sha(o.get_raw_data()) for o in objects};template=next(o for o in objects if o.type.name=='Texture2D');added={};inputs={};edits=[];expected={};seen=set();shader_checks=[]
 for i,name in enumerate(sorted(FILES)):
  p=art/name;obj=copy.copy(template);obj.path_id=7100000000000000000+i;assert obj.path_id not in template.assets_file.objects
  obj.set_raw_data(template.get_raw_data());template.assets_file.objects[obj.path_id]=obj;tex=obj.read();tex.m_Name='LWC_Garment03_'+p.stem
  with Image.open(p) as image:
   mip_count=int(math.floor(math.log2(max(image.size))))+1
   rgba=image.convert('RGBA');tex.set_image(rgba,target_format=4,mipmap_count=1)
   # UnityPy's generic helper stops at 4x4 (compressed-texture convention).
   # Our uncompressed RGBA32 textures can carry the complete chain to 1x1.
   pixels=tex.image_data;level=rgba
   while level.size!=(1,1):
    level=level.resize((max(1,level.width//2),max(1,level.height//2)),Image.Resampling.BICUBIC)
    pixels+=Texture2DConverter.image_to_texture2d(level,4)[0]
   tex.image_data=pixels;tex.m_CompleteImageSize=len(pixels);tex.m_MipCount=mip_count
   if tex.m_MipMap is not None:tex.m_MipMap=True
  linear=p.stem.endswith(('_n','_m'));tex.m_ColorSpace=0 if linear else 1;tex.m_TextureSettings.m_FilterMode=1;tex.save();added[name]=obj.path_id
  inputs[name]={'sha256':sha(p.read_bytes()),'path_id':obj.path_id,'linear_data':linear,'color_space':tex.m_ColorSpace,'mips':mip_count,'width':tex.m_Width,'height':tex.m_Height}
 for obj in objects:
  if obj.type.name!='Material':continue
  tree=obj.read_typetree();name=tree['m_Name']
  if name not in TARGETS:continue
  assert name not in seen;seen.add(name);slots=dict(tree['m_SavedProperties']['m_TexEnvs']);ref=tree['m_Shader'];assert ref['m_FileID']==0
  shader=obj.assets_file.objects[ref['m_PathID']].read_typetree();properties={p['m_Name'] for p in shader['m_ParsedForm']['m_PropInfo']['m_Props']}
  for slot,filename in TARGETS[name].items():
   assert slot in slots and slot in properties,(name,slot)
   before=copy.deepcopy(slots[slot]['m_Texture']);slots[slot]['m_Texture']={'m_FileID':0,'m_PathID':added[filename]}
   edits.append({'material':name,'material_id':obj.path_id,'slot':slot,'file':filename,'before':before,'after':added[filename]})
  shader_checks.append({'material':name,'shader':shader['m_ParsedForm'].get('m_Name',shader['m_Name']),'verified_properties':sorted(TARGETS[name]),'keywords_preserved':tree.get('m_ShaderKeywords')})
  expected[obj.path_id]=copy.deepcopy(tree);obj.save_typetree(tree)
 assert seen==set(TARGETS)
 bundle=next(iter(env.files.values())).save(packer='lz4');(output/'sagerobes-garment03.bundle').write_bytes(bundle)
 check=UnityPy.load(bundle);after={o.path_id:o for o in check.objects}
 for obj in objects:
  if obj.path_id in expected:assert after[obj.path_id].read_typetree()==expected[obj.path_id]
  else:assert sha(after[obj.path_id].get_raw_data())==originals[obj.path_id],obj.path_id
 for name,info in inputs.items():
  tex=after[info['path_id']].read()
  with Image.open(art/name) as im:assert tex.image.convert('RGBA').tobytes()==im.convert('RGBA').tobytes(),name
  assert tex.m_ColorSpace==info['color_space'] and tex.m_MipCount==info['mips'] and tex.m_TextureSettings.m_FilterMode==1,name
 assert len(after)==len(objects)+11
 report={'status':'OFFLINE-TESTED','source_dll_sha256':sha(source.read_bytes()),'source_bundle_sha256':sha(raw),'output_bundle_sha256':sha(bundle),'added_textures':inputs,'material_slot_edits':edits,'shader_checks':shader_checks,'original_object_count':len(objects),'untouched_objects_byte_identical':len(objects)-len(expected),'changed_materials':len(expected),'all_other_material_fields_unchanged':True,'installed':False,'unresolved_shader_properties':[]}
 (output/'bundle-verification.json').write_text(json.dumps(report,indent=2));print('PASS11textures,7materials,21slots;reloadpixels/mips/linear/unchangedobjects')
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--art',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();build(a.source,a.art,a.output)
