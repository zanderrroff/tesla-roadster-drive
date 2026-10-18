#!/usr/bin/env python3
"""Import selected original Rocketbox FBX assets; preserve MIT attribution.

Textures are downloaded into memory and resized to 1K PNG, never storing the
multi-gigabyte uncompressed TGA library on the user's machine.
"""
import concurrent.futures, hashlib, io, json, pathlib, sys, urllib.request
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[1]
TREE = json.loads((ROOT / '.asset-work/rocketbox-tree.json').read_text())
REV = TREE['sha']
BASE = f'https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/{REV}/'
OUT = ROOT / 'unity/MotorCoast/Assets/CharacterSource'
OUT.mkdir(parents=True, exist_ok=True)
files = TREE['tree']
folders = sorted({str(pathlib.PurePosixPath(p['path']).parents[1]) for p in files
                  if p['path'].startswith('Assets/Avatars/Adults/') and '/Export/' in p['path']})
extra = ['Sports_Female_01','Sports_Female_02','Sports_Male_01','Sports_Male_02',
         'Sports_Male_03','Sports_Male_04','Business_Female_01','Business_Female_02',
         'Business_Male_01','Business_Male_02']
folders += ['Assets/Avatars/Professions/' + n for n in extra]
if '--all' not in sys.argv:
    folders = [p for p in folders if p.endswith(('Male_Adult_01','Female_Adult_01'))]

def fetch(path):
    req=urllib.request.Request(BASE + urllib.parse.quote(path), headers={'User-Agent':'MotorCoast-asset-builder'})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=90) as r: return r.read()
        except Exception:
            if attempt==3: raise

def character(folder):
    name=folder.split('/')[-1]; dest=OUT/name; dest.mkdir(exist_ok=True)
    manifest=[]
    chosen=[p for p in files if p['path'].startswith(folder+'/') and
            (p['path'].endswith('/Export/'+name+'.fbx') or
             ('/Textures/' in p['path'] and any(x in p['path'] for x in ['_color.tga','_normal.tga'])))]
    for entry in chosen:
        source=entry['path']; filename=source.split('/')[-1]
        target=dest/(filename.replace('.tga','.png'))
        if not target.exists():
            data=fetch(source)
            if source.endswith('.tga'):
                image=Image.open(io.BytesIO(data)); image.thumbnail((1024,1024),Image.Resampling.LANCZOS)
                image.save(target,format='PNG',optimize=True)
            else: target.write_bytes(data)
        data=target.read_bytes()
        manifest.append({'file':str(target.relative_to(OUT)),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'source':BASE+source})
    print(name, 'ready', flush=True)
    return {'id':name,'label':name.replace('_',' '),'resource':'Characters/'+name,'files':manifest}

with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    people=list(pool.map(character,folders))
animations={
    'Idle':'static/m_idle_breathe_01', 'Walk':'xy/m_walk_neutral', 'Run':'xy/m_run_neutral',
    'Crouch':'static/m_crouch_idle', 'Dance':'static/m_dancing_cool',
    'Wave':'static/m_gestic_listen_neutral_01', 'Clap':'static/m_claphands_01',
    'Cheer':'static/m_cheer_01', 'Laugh':'static/m_gestic_laugh_loud',
    'SitDown':'xyz/m_sit_down_chair_01', 'StandUp':'xyz/m_sit_stand_up_chair_01',
    'WalkStart':'xy/m_walk_start', 'WalkStop':'xy/m_walk_stop',
    'RunStart':'xy/m_run_start', 'RunStop':'xy/m_run_stop',
    'FemaleDance':'static/f_dancing_cool', 'FemaleIdle':'static/f_idle_breathe_01',
}
animation_files=[]
for name,suffix in animations.items():
    source='Assets/Animations/all_animations_max_motextr_'+suffix+'.max.fbx'
    if not any(p['path']==source for p in files):
        raise RuntimeError('Animation missing in pinned upstream: '+source)
    target=OUT/'Animations'/(name+'.fbx');target.parent.mkdir(exist_ok=True)
    if not target.exists(): target.write_bytes(fetch(source))
    data=target.read_bytes();animation_files.append({'file':str(target.relative_to(OUT)),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'source':BASE+source})
(OUT/'LICENSE.txt').write_bytes(fetch('LICENSE.md'))
manifest={'source':'https://github.com/microsoft/Microsoft-Rocketbox','revision':REV,'license':'MIT','changes':'Selected original high-detail meshes; textures resized to 1024 PNG; in-place animation extraction and rig adaptation for Motor Coast.','characters':people,'animations':animation_files}
(ROOT/'unity/character-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Ready:',len(people),'distinct characters and',len(animation_files),'animation sources')
