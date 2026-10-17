#!/usr/bin/env python3
"""Validate a real native IPA; never manufacture one from the website."""
import pathlib,plistlib,sys,zipfile
p=pathlib.Path(sys.argv[1])
if not p.exists():
    candidates=list(p.parent.glob('*.ipa'))
    if len(candidates)!=1:raise SystemExit('Exactly one exported IPA is required')
    p=candidates[0]
with zipfile.ZipFile(p) as z:
    names=z.namelist();infos=[n for n in names if n.startswith('Payload/') and n.count('/')==2 and n.endswith('/Info.plist')]
    assert len(infos)==1,'Missing app bundle'
    info=plistlib.loads(z.read(infos[0]));root=infos[0].removesuffix('Info.plist')
    assert info['CFBundleIdentifier']=='app.zanderrroff.motorcoast'
    assert root+info['CFBundleExecutable'] in names
    assert root+'embedded.mobileprovision' in names,'IPA has no provisioning profile'
    assert any(n.startswith(root+'_CodeSignature/') for n in names),'App is not signed'
    assert any('/UnityFramework.framework/UnityFramework' in n for n in names),'Not a Unity native player'
    assert any(n.endswith('/Data/globalgamemanagers') or n.endswith('/Data/boot.config') for n in names),'No Unity game data'
    print(f'IPA structure PASS: {p.name}; {info["CFBundleIdentifier"]}; {info["CFBundleShortVersionString"]} ({info["CFBundleVersion"]}); {p.stat().st_size:,} bytes')
