#!/usr/bin/env python3
"""Validate and upload a signed native IPA; API credentials stay on this Mac."""
import argparse,json,os,pathlib,plistlib,subprocess,sys,zipfile

p=argparse.ArgumentParser()
p.add_argument('ipa',type=pathlib.Path)
p.add_argument('--key-id',required=True)
p.add_argument('--issuer',required=True)
p.add_argument('--private-keys-dir',type=pathlib.Path,required=True)
p.add_argument('--validate-only',action='store_true')
args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]
assert (args.private_keys_dir/('AuthKey_'+args.key_id+'.p8')).is_file(),'App Store Connect API key is missing'
subprocess.run([sys.executable,str(root/'Tools/verify-ipa.py'),str(args.ipa)],check=True)
with zipfile.ZipFile(args.ipa) as archive:
    entries=[n for n in archive.namelist() if n.startswith('Payload/') and n.count('/')==2 and n.endswith('/Info.plist')]
    assert len(entries)==1
    info=plistlib.loads(archive.read(entries[0]))
assert info['CFBundleIdentifier']=='app.zanderrroff.motorcoast'
env=dict(os.environ,API_PRIVATE_KEYS_DIR=str(args.private_keys_dir.resolve()))
auth=['--apiKey',args.key_id,'--apiIssuer',args.issuer,'--output-format','json']
version=info['CFBundleShortVersionString']+'-'+info['CFBundleVersion']

def deliver(action,arguments):
    print(action+' '+version,flush=True)
    result=subprocess.run(['xcrun','altool',*arguments,*auth],env=env,capture_output=True,text=True)
    log=root/'artifacts'/('apple-'+action.lower()+'-'+version+'.txt')
    log.write_text(result.stdout+'\n'+result.stderr)
    try:
        report=json.loads(result.stdout)
    except json.JSONDecodeError:
        report={'output':result.stdout[-4000:],'diagnostics':result.stderr[-4000:]}
    print(json.dumps(report,ensure_ascii=False),flush=True)
    if result.returncode:raise SystemExit(action+' failed; see '+str(log))

deliver('Validation',['--validate-app','-f',str(args.ipa),'-t','ios'])
if not args.validate_only:
    deliver('Upload',['--upload-package',str(args.ipa),'--type','ios',
        '--asc-public-id',args.issuer,'--apple-id','6824150042',
        '--bundle-id',info['CFBundleIdentifier'],'--bundle-version',info['CFBundleVersion'],
        '--bundle-short-version-string',info['CFBundleShortVersionString']])
    print('Upload accepted. Apple processing and TestFlight availability must be checked separately.')
