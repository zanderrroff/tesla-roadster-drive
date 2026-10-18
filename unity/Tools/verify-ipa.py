#!/usr/bin/env python3
"""Validate a real native IPA; never manufacture one from the website."""
import pathlib,plistlib,sys,zipfile,subprocess,tempfile,struct,hashlib,datetime
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
    executable=z.read(root+info['CFBundleExecutable']);magic,cpu=struct.unpack('<II',executable[:8]);assert magic==0xfeedfacf and cpu==0x0100000c,'Main executable must be native ARM64 Mach-O'
    assert root+'embedded.mobileprovision' in names,'IPA has no provisioning profile'
    assert any(n.startswith(root+'_CodeSignature/') for n in names),'App is not signed'
    assert any('/UnityFramework.framework/UnityFramework' in n for n in names),'Not a Unity native player'
    assert any(n.endswith('/Data/globalgamemanagers') or n.endswith('/Data/boot.config') for n in names),'No Unity game data'
    print(f'IPA structure PASS: {p.name}; {info["CFBundleIdentifier"]}; {info["CFBundleShortVersionString"]} ({info["CFBundleVersion"]}); {p.stat().st_size:,} bytes')

with tempfile.TemporaryDirectory(prefix='motorcoast-ipa-verify-') as temporary:
    subprocess.run(['ditto','-x','-k',str(p),temporary],check=True,capture_output=True)
    apps=list((pathlib.Path(temporary)/'Payload').glob('*.app'));assert len(apps)==1
    subprocess.run(['codesign','--verify','--deep','--strict',str(apps[0])],check=True,capture_output=True)
    profile=subprocess.run(['security','cms','-D','-i',str(apps[0]/'embedded.mobileprovision')],check=True,capture_output=True)
    provision=plistlib.loads(profile.stdout)
    assert provision['Entitlements']['application-identifier']=='U8QHYG2G45.app.zanderrroff.motorcoast'
    assert not provision['Entitlements'].get('get-task-allow',False),'Release must not allow debugging'
    assert provision['ExpirationDate']>datetime.datetime.utcnow(),'Provisioning profile has expired'
    prefix=str(pathlib.Path(temporary)/'signing-certificate-')
    subprocess.run(['codesign','--display','--extract-certificates='+prefix,str(apps[0])],check=True,capture_output=True)
    leaf=pathlib.Path(prefix+'0').read_bytes()
    assert hashlib.sha256(leaf).digest() in [hashlib.sha256(c).digest() for c in provision['DeveloperCertificates']],'Signing certificate is not authorized by the embedded profile'
    print('Signature verification PASS; release provisioning belongs to Motor Coast.')
