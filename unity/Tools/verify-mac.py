#!/usr/bin/env python3
"""Verify a local Intel Mac player and its native driving plugin."""
import argparse,json,pathlib,plistlib,subprocess

p=argparse.ArgumentParser();p.add_argument('app',type=pathlib.Path);args=p.parse_args()
app=args.app.resolve();assert app.is_dir() and app.suffix=='.app', 'Missing application bundle'
info=plistlib.loads((app/'Contents/Info.plist').read_bytes())
assert info['CFBundleIdentifier']=='app.zanderrroff.motorcoast.mac'
executable=app/'Contents/MacOS'/info['CFBundleExecutable']
architecture=subprocess.check_output(['file','-b',str(executable)],text=True).strip()
assert 'Mach-O' in architecture and 'x86_64' in architecture,architecture
plugins=list(app.rglob('libMotorCoastNative.dylib'));assert len(plugins)==1,plugins
symbols=subprocess.check_output(['nm','-gU',str(plugins[0])],text=True)
assert '_MCStep' in symbols and '_MCReset' in symbols,'Native driving ABI is missing'
data=app/'Contents/Resources/Data';assert data.is_dir() and any((data/n).is_file() for n in ('data.unity3d','globalgamemanagers'))
subprocess.run(['codesign','--verify','--deep','--strict',str(app)],check=True)
print(json.dumps({'app':str(app),'version':info['CFBundleShortVersionString'],'architecture':architecture,'nativePlugin':str(plugins[0].relative_to(app)),'strictSignature':'PASS','bytes':sum(f.stat().st_size for f in app.rglob('*') if f.is_file() and not f.is_symlink())},ensure_ascii=False,indent=2))
