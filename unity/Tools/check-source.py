#!/usr/bin/env python3
"""Source-only preflight. This does NOT replace Unity compilation or device testing."""
import json,pathlib,hashlib,re,sys,plistlib
root=pathlib.Path(__file__).resolve().parents[1]
assets=root/'MotorCoast/Assets'
manifest=json.loads((root/'asset-manifest.json').read_text())+json.loads((root/'scenery-manifest.json').read_text())+json.loads((root/'pine-manifest.json').read_text())
for entry in manifest:
    assert entry['triangles']<=350000,entry['file']
    for f in entry['files']:
        p=assets/'Resources'/f['file'];b=p.read_bytes()
        assert len(b)==f['bytes'] and hashlib.sha256(b).hexdigest()==f['sha256'],str(p)
    d=json.loads((assets/'Resources'/entry['file']).read_text())
    assert not any(x in d.get('extensionsRequired',[]) for x in ['KHR_draco_mesh_compression','EXT_meshopt_compression','EXT_texture_webp'])
    names={n.get('name') for n in d['nodes']}
    if entry['file'].startswith('Vehicles/'):
        assert {'Chassis','Wheel_0','Wheel_1','Wheel_2','Wheel_3'}<=names
    elif entry['file'].startswith('Driver/'):assert {'Bip01_L_Hand','Bip01_R_Hand','Bip01_LEye','Bip01_REye'}<=names
    elif 'coastal-cliff' in entry['file']:assert 'CoastalCliff' in names
gui=json.loads((root/'gui-manifest.json').read_text())
assert len(gui['files'])>=10
for entry in gui['files']:
    data=(assets/'Resources'/entry['file']).read_bytes()
    assert len(data)==entry['bytes'] and hashlib.sha256(data).hexdigest()==entry['sha256'],entry['file']
catalog=json.loads((assets/'Resources/Data/vehicles.json').read_text())['vehicles']
assert len(catalog)>=80 and len({c['id'] for c in catalog})==len(catalog)
fleet=json.loads((root/'fleet-expansion-manifest.json').read_text())['vehicles']
assert len({v['source'] for v in fleet})==len(fleet), 'Duplicate primary source models'
for v in fleet:
    assert v['standardTriangles']<=150000 and v['highTriangles']<=350000,v['spec']['id']
    assert v['doors'] and v['standardTextureSize']==1024,v['spec']['id']
sky=json.loads((root/'sky-manifest.json').read_text())
assert hashlib.sha256((assets/'Resources'/sky['file']).read_bytes()).hexdigest()==sky['sha256']
for c in catalog:
    assert len(c['wheelCenters'])==4
    if c.get('modelUrl'):
        for file,key in [(c['id']+'.glb','modelSha256'),(c['id']+('-ultra' if c['highModelSha256']!=c['modelSha256'] else '')+'.glb','highModelSha256')]:
            local=root/'artifacts/expanded-content'/file
            if local.exists():assert hashlib.sha256(local.read_bytes()).hexdigest()==c[key]
            elif '--require-content' in sys.argv:raise AssertionError('Missing optional content: '+file)
        assert c['hasInterior']
    else:
        for name in ['resource','highResource']:assert (assets/'Resources'/(c[name]+'.gltf')).is_file()
for p in assets.rglob('*.asmdef'):json.loads(p.read_text())
json.loads((root/'MotorCoast/Packages/manifest.json').read_text())
plistlib.loads((root/'Tools/ExportOptions.plist').read_bytes())
for p in assets.rglob('*'):
    if p.is_file():assert p.suffix not in ['.p8','.p12','.mobileprovision','.ulf'],str(p)
characters=json.loads((root/'character-manifest.json').read_text())['characters']
assert len(characters)==50
for character in characters:
    for item in character['files']:
        data=(assets/'CharacterSource'/item['file']).read_bytes();assert hashlib.sha256(data).hexdigest()==item['sha256']
print(f'Source preflight PASS: {len(catalog)} cars, 50 avatars, {len(manifest)} models, hashes, wheel pivots, driver joints, local resources, package JSON and export plist')
print('Unity Editor compilation, URP rendering, physics tests and IPA export are separate required gates.')
