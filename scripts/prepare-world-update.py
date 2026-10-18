#!/usr/bin/env python3
"""Pinned, checksum-verified CC0 surface and UI assets for the native worlds."""
import concurrent.futures,hashlib,io,json,pathlib,urllib.request,zipfile
from PIL import Image,ImageOps
R=pathlib.Path(__file__).resolve().parents[1]; OUT=R/'unity/MotorCoast/Assets/Resources/Textures'
HEADERS={'User-Agent':'MotorCoast/1.0 (Unity asset pipeline)'}
assets={'concrete':'concrete_floor_01','rock':'rocky_terrain_02','sand':'sandy_gravel','grass':'aerial_grass_rock','snow':'snow_02','brick':'brick_wall_001'}
def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url,headers=HEADERS),timeout=120) as r:return r.read()
def surface(pair):
    name,source=pair; data=json.loads((R/('.asset-work/polyhaven-'+source+'.json')).read_text());files=[]
    for role,key in [('color','Diffuse'),('normal','nor_gl'),('arm','arm')]:
        record=data[key]['2k']['jpg'];cache=R/('.asset-work/'+name+'-'+role+'.jpg')
        if not cache.exists():cache.write_bytes(fetch(record['url']))
        raw=cache.read_bytes();assert hashlib.md5(raw).hexdigest()==record['md5']
        im=Image.open(io.BytesIO(raw)).convert('RGB')
        if role=='arm':
            a,r,m=im.split();Image.merge('RGBA',(m,m,m,ImageOps.invert(r))).save(OUT/(name+'-metal-smooth.png'));a.save(OUT/(name+'-ao.png'))
        else: im.save(OUT/(name+'-'+role+'.png'))
        files.append(record)
    print(name,'ready',flush=True)
    return {'name':name,'source':'https://polyhaven.com/a/'+source,'license':'CC0','files':files}
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:manifest=list(pool.map(surface,assets.items()))
z=zipfile.ZipFile(R/'.asset-work/kenney-ui.zip'); gui=R/'unity/MotorCoast/Assets/Resources/GUI'
for target,entry in [('panel','panel_background.png'),('menu-button','button_rectangle_flat.png')]:
    candidates=[n for n in z.namelist() if n.startswith('PNG/Grey/Double/') and n.endswith(entry)]
    if not candidates:
        candidates=[n for n in z.namelist() if n.startswith('PNG/Grey/Double/') and 'button_rectangle_flat' in n]
    (gui/(target+'.png')).write_bytes(z.read(candidates[0]))
(R/'unity/ThirdParty/GUI/Kenney-UI-LICENSE.txt').write_bytes(z.read('License.txt'))
(R/'unity/world-material-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
credit='\nWorld surfaces: Concrete Floor 01, Rocky Terrain 02, Sandy Gravel, Aerial Grass Rock, Snow 02, Brick Wall 001 — Poly Haven, CC0. https://polyhaven.com/ License: https://creativecommons.org/publicdomain/zero/1.0/ Converted to PNG, packed roughness as smoothness.\nMenu panels and buttons: Kenney UI Pack, CC0. https://kenney.nl/assets/ui-pack\n'
credits=R/'unity/MotorCoast/Assets/Resources/Data/CREDITS.txt'
if 'Kenney UI Pack' not in credits.read_text():credits.write_text(credits.read_text()+credit)
