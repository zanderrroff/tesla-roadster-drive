#!/usr/bin/env python3
"""Refresh optional specs and attribution without reconverting embedded models."""
import json,pathlib
r=pathlib.Path(__file__).resolve().parents[1]
p=r/'unity/MotorCoast/Assets/Resources/Data/vehicles.json'
items=json.loads((r/'unity/fleet-expansion-manifest.json').read_text())['vehicles']
names={'bmw-m3-need':'BMW M3 GTR','mercedes-benz-mersedes-s-class':'Mercedes-Benz S-Class','mercedes-benz-mersedes-s65':'Mercedes-Benz S65','generico-sedan-80':'Классический седан · 1980-е','generico-van-92':'Минивэн · 1990-е'}
for item in items:
    spec=item['spec'];spec['label']=names.get(spec['id'],spec['label'])
    if spec['id']=='porsche-taycan':spec['electric']=True
catalog=json.loads(p.read_text());catalog['vehicles']=[c for c in catalog['vehicles'] if c.get('resource')]+[i['spec'] for i in items]
p.write_text(json.dumps(catalog,indent=2)+'\n')
p=p.with_name('CREDITS.txt');prefix=p.read_text().split('EXPANDED GARAGE 2.1')[0]
lines=['EXPANDED GARAGE 2.1','Original cabins retained; wheels, steering and driver doors separated. Standard textures up to 1K; Ultra up to 2K. BY-SA derivatives retain their source license.']
for i in items:
 c=i['credit'];lines+=['',i['spec']['label']+' — '+c['author']+' / '+i['sourceLicense']['label'],i['source'],i['sourceLicense']['url'],'Changes: '+i['changes']]
lines+=['','50 adult characters and 17 motion clips: Microsoft Rocketbox (MIT, copyright 2020 Microsoft).',
'https://github.com/microsoft/Microsoft-Rocketbox/tree/0943055db6ec570bcef9f2c8b41c9e5467c808f9',
'Changes: normalized height, seated IK, rotation retargeting, root motion removal, texture conversion and clothing tint masks. Source files and full MIT license included.']
p.write_text(prefix+'\n'.join(lines)+'\n');print(len(catalog['vehicles']),'vehicles')
