import csv, json, re, subprocess, sys
from pathlib import Path
A=Path(sys.argv[1]); OUT=Path(sys.argv[2]); OUT.mkdir(parents=True, exist_ok=True)
lic={}
for l in open(A/'esc50/frog_licences.txt'):
    m=re.match(r'- \[(\S+)\.ogg\]: clip derived from (.*) \((http\S+)\) by (.*) \[(.*)\]$', l.strip())
    if m: lic[m.group(1)]=dict(orig=m.group(2), url=m.group(3), by=m.group(4), lic=m.group(5))
def meta(fn):
    if fn.startswith('esc50_'):
        k=fn[6:-4].rsplit('-',1)[0]; d=lic.get(k,{})
        t={'CC0':'SHIP','CC-BY':'SHIP-ATTRIB'}.get(d.get('lic'),'NC')
        return t, d.get('lic',''), f"{d.get('by','')} - {d.get('orig','')} - {d.get('url','')}"
    if fn.startswith('frog_'): return 'REF','unknown (no licence in repo)','github.com/Yfyangd/frog (Taiwan frog recordings)'
    return 'REF','unknown (no licence in repo)','github.com/h-phantasmagorical/froggy-sounds (Lowland Leopard Frog)'
rows=[]
for r in csv.DictReader(open(A/'features.csv')):
    s,e=float(r['start_s']),float(r['end_s'])
    if e-s<0.1: continue
    slot=r['slot_guess'].split('|')[0]; tier,l,credit=meta(r['file'])
    name=f"{tier}__{Path(r['file']).stem}__e{int(r['event']):02d}.mp3"
    d=OUT/'clips'/slot; d.mkdir(parents=True, exist_ok=True)
    ss=max(0,s-0.05); dur=min(e-s+0.1, 30)
    subprocess.run(['ffmpeg','-nostdin','-loglevel','error','-y','-ss',f'{ss:.3f}','-t',f'{dur:.3f}','-i',str(A/'wav'/r['file']),
                    '-ac','1','-ar','44100','-af','afade=t=in:d=0.02,areverse,afade=t=in:d=0.04,areverse','-b:a','160k',str(d/name)],check=True)
    rows.append(dict(clip=f"clips/{slot}/{name}", slot_guess=r['slot_guess'], tier=tier, licence=l, credit=credit,
        source_file=r['file'], start_s=r['start_s'], dur_s=r['dur_s'], centroid_hz=r['centroid_hz'],
        f0_hz=r['f0_hz'], periodicity=r['periodicity'], low_ratio=r['low_ratio'], keep='', real_slot='', notes=''))
with open(OUT/'MANIFEST.csv','w',newline='',encoding='utf-8') as f:
    w=csv.DictWriter(f,list(rows[0].keys())); w.writeheader(); w.writerows(rows)
(OUT/'manifest.js').write_text('window.CLIPS='+json.dumps(rows)+';')
print(len(rows),'clips')
