#!/usr/bin/env python3
"""Sign a real native Xcode archive locally; private keys never enter CI."""
import argparse,datetime,hashlib,json,os,pathlib,plistlib,re,secrets,shlex,shutil,subprocess,tempfile
p=argparse.ArgumentParser();p.add_argument('archive',type=pathlib.Path);args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]
keys=pathlib.Path(os.environ.get('MOTORCOAST_SIGNING_DIR',str(pathlib.Path.home()/'.private_keys/motorcoast')))
metadata=json.loads((keys/'signing.json').read_text())
apps=list((args.archive/'Products/Applications').glob('*.app'))
assert len(apps)==1,'Exactly one application must exist in the Xcode archive'
source=apps[0];info=plistlib.loads((source/'Info.plist').read_bytes())
assert info['CFBundleIdentifier']==metadata['bundleId']=='app.zanderrroff.motorcoast'
assert (source/'Frameworks/UnityFramework.framework/UnityFramework').is_file(),'Not a native Unity player'
def run(*command):
    result=subprocess.run(command,capture_output=True)
    if result.returncode:raise RuntimeError(command[0]+' failed: '+result.stderr.decode(errors='replace'))
    return result.stdout
profile=plistlib.loads(run('security','cms','-D','-i',str(keys/'MotorCoast-AppStore.mobileprovision')))
assert profile['ExpirationDate']>datetime.datetime.utcnow()
assert not profile['Entitlements'].get('get-task-allow',True)
assert metadata['certificateSha1'] in [hashlib.sha1(c).hexdigest().upper() for c in profile['DeveloperCertificates']]
destination=root/'artifacts/ipa';destination.mkdir(parents=True,exist_ok=True)
os.umask(0o077)
with tempfile.TemporaryDirectory(prefix='sign-',dir=keys) as signing, tempfile.TemporaryDirectory(prefix='motorcoast-package-',dir=root/'artifacts') as packaging:
    signing=pathlib.Path(signing);packaging=pathlib.Path(packaging)
    password=secrets.token_urlsafe(40);password_file=signing/'password';password_file.write_text(password)
    keychain=signing/'build.keychain-db';pem=signing/'certificate.pem';p12=signing/'identity.p12'
    run('openssl','x509','-inform','DER','-in',str(keys/'distribution.cer'),'-out',str(pem))
    run('openssl','pkcs12','-export','-inkey',str(keys/'distribution-private.pem'),'-in',str(pem),'-out',str(p12),'-passout','file:'+str(password_file))
    original_search=shlex.split(run('security','list-keychains','-d','user').decode())
    run('security','create-keychain','-p',password,str(keychain))
    try:
        run('security','list-keychains','-d','user','-s',str(keychain),*original_search)
        run('security','unlock-keychain','-p',password,str(keychain))
        run('security','set-keychain-settings','-lut','7200',str(keychain))
        run('security','import',str(p12),'-k',str(keychain),'-P',password,'-T','/usr/bin/codesign','-T','/usr/bin/security')
        # An isolated keychain needs the issuing intermediate as well as the
        # identity. Reuse Apple's existing certificate without changing trust.
        certificates=run('security','find-certificate','-a','-c','Apple Worldwide Developer Relations','-p')
        intermediates=re.findall(rb'-----BEGIN CERTIFICATE-----.*?-----END CERTIFICATE-----',certificates,re.S)
        for index,certificate in enumerate(intermediates):
            intermediate=signing/('wwdr-'+str(index)+'.pem');intermediate.write_bytes(certificate)
            run('security','import',str(intermediate),'-k',str(keychain))
        run('security','set-key-partition-list','-S','apple-tool:,apple:,codesign:','-s','-k',password,str(keychain))
        identities=run('security','find-identity','-v','-p','codesigning',str(keychain)).decode()
        assert metadata['certificateSha1'] in identities,'Distribution identity is not valid in the signing keychain: '+identities
        payload=packaging/'Payload';payload.mkdir();app=payload/source.name
        shutil.copytree(source,app,symlinks=True)
        for stale in app.rglob('_CodeSignature'):
            if stale.is_dir():shutil.rmtree(stale)
        shutil.copyfile(keys/'MotorCoast-AppStore.mobileprovision',app/'embedded.mobileprovision')
        entitlements=dict(profile['Entitlements'])
        if 'keychain-access-groups' in entitlements:entitlements['keychain-access-groups']=[g.replace('*',info['CFBundleIdentifier']) for g in entitlements['keychain-access-groups']]
        entitlement_file=signing/'entitlements.plist';entitlement_file.write_bytes(plistlib.dumps(entitlements))
        command=['codesign','--force','--sign',metadata['certificateSha1'],'--keychain',str(keychain),'--timestamp=none','--generate-entitlement-der']
        for dylib in app.rglob('*.dylib'):run(*command,str(dylib))
        for framework in sorted(app.rglob('*.framework'),key=lambda x:len(x.parts),reverse=True):run(*command,str(framework))
        run(*command,'--entitlements',str(entitlement_file),str(app))
        run('codesign','--verify','--deep','--strict',str(app))
        ipa=destination/('MotorCoast-'+info['CFBundleShortVersionString']+'-'+info['CFBundleVersion']+'.ipa')
        run('ditto','-c','-k','--keepParent',str(payload),str(ipa))
    finally:
        try:run('security','list-keychains','-d','user','-s',*original_search)
        finally:run('security','delete-keychain',str(keychain))
subprocess.run(['python3',str(root/'Tools/verify-ipa.py'),str(ipa)],check=True)
print('Signed native IPA:',ipa)
