#!/usr/bin/env python3
"""Publish a verified native test build in the owner's existing repository."""
import argparse,hashlib,json,pathlib,plistlib,subprocess,urllib.request,urllib.error,urllib.parse,zipfile
p=argparse.ArgumentParser();p.add_argument('ipa',type=pathlib.Path);p.add_argument('--notes',required=True,type=pathlib.Path);args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]
subprocess.run(['python3',str(root/'Tools/verify-ipa.py'),str(args.ipa)],check=True)
with zipfile.ZipFile(args.ipa) as z:
    name=next(n for n in z.namelist() if n.startswith('Payload/') and n.count('/')==2 and n.endswith('.app/Info.plist'))
    info=plistlib.loads(z.read(name))
assert info['CFBundleIdentifier']=='app.zanderrroff.motorcoast'
version=info['CFBundleShortVersionString'];build=info['CFBundleVersion'];tag=f'ios-v{version}-build-{build}'
raw=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\n\n',capture_output=True,text=True,check=True).stdout
auth='Bearer '+dict(line.split('=',1) for line in raw.splitlines() if '=' in line)['password']
base='https://api.github.com/repos/zanderrroff/tesla-roadster-drive'
def api(path,method='GET',data=None):
    req=urllib.request.Request(base+path,method=method,data=json.dumps(data).encode() if data is not None else None,headers={'Authorization':auth,'Accept':'application/vnd.github+json','Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=120) as response:return json.load(response)
sha=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
try:release=api('/releases/tags/'+tag)
except urllib.error.HTTPError as error:
    if error.code!=404:raise
    release=api('/releases','POST',{'tag_name':tag,'target_commitish':sha,'name':f'Motor Coast {version} ({build}) — iPhone test build','body':args.notes.read_text(),'prerelease':True})
digest=hashlib.sha256(args.ipa.read_bytes()).hexdigest()
existing=next((a for a in release['assets'] if a['name']==args.ipa.name),None)
if existing:assert existing['size']==args.ipa.stat().st_size and existing.get('digest')=='sha256:'+digest
else:
    url=release['upload_url'].split('{',1)[0]+'?name='+urllib.parse.quote(args.ipa.name)
    assert urllib.parse.urlsplit(url).hostname=='uploads.github.com'
    with args.ipa.open('rb') as payload:
        req=urllib.request.Request(url,method='POST',data=payload,headers={'Authorization':auth,'Content-Type':'application/octet-stream','Content-Length':str(args.ipa.stat().st_size)})
        with urllib.request.urlopen(req,timeout=900) as response:asset=json.load(response)
    assert asset['state']=='uploaded' and asset['size']==args.ipa.stat().st_size and asset.get('digest')=='sha256:'+digest
print(release['html_url']);print('SHA-256:',digest)
