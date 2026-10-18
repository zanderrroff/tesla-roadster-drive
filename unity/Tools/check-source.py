#!/usr/bin/env python3
"""Source-only preflight. This does NOT replace Unity compilation or device testing."""
import json,pathlib,hashlib,re,sys,plistlib
root=pathlib.Path(__file__).resolve().parents[1]
assets=root/'MotorCoast/Assets'
manifest=json.loads((root/'asset-manifest.json').read_text())+json.loads((root/'scenery-manifest.json').read_text())
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
    else:assert 'CoastalCliff' in names
gui=json.loads((root/'gui-manifest.json').read_text())
assert len(gui['files'])>=10
for entry in gui['files']:
    data=(assets/'Resources'/entry['file']).read_bytes()
    assert len(data)==entry['bytes'] and hashlib.sha256(data).hexdigest()==entry['sha256'],entry['file']
catalog=json.loads((assets/'Resources/Data/vehicles.json').read_text())['vehicles']
assert len(catalog)==7 and len({c['id'] for c in catalog})==7
for c in catalog:
    assert len(c['wheelCenters'])==4
    for name in ['resource','highResource']:assert (assets/'Resources'/(c[name]+'.gltf')).is_file()
for p in assets.rglob('*.asmdef'):json.loads(p.read_text())
json.loads((root/'MotorCoast/Packages/manifest.json').read_text())
plistlib.loads((root/'Tools/ExportOptions.plist').read_bytes())
for p in assets.rglob('*'):
    if p.is_file():assert p.suffix not in ['.p8','.p12','.mobileprovision','.ulf'],str(p)
print(f'Source preflight PASS: 7 cars, {len(manifest)} models, hashes, wheel pivots, driver joints, local resources, package JSON and export plist')
print('Unity Editor compilation, URP rendering, physics tests and IPA export are separate required gates.')
