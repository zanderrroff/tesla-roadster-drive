#!/usr/bin/env python3
"""Stage or publish the optional, attributed fleet in the owner's repository.
Authentication comes from git's existing credential helper, never a source file.
"""
import argparse,concurrent.futures,hashlib,json,pathlib,subprocess,urllib.request,urllib.error,urllib.parse
p=argparse.ArgumentParser();p.add_argument('action',choices=['stage','publish','check']);args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]
tag='motorcoast-content-v2.1';base='https://api.github.com/repos/zanderrroff/tesla-roadster-drive'
raw=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\n\n',capture_output=True,text=True,check=True).stdout
auth='Bearer '+dict(line.split('=',1) for line in raw.splitlines() if '=' in line)['password']
def api(path,method='GET',data=None):
    req=urllib.request.Request(base+path,method=method,data=json.dumps(data).encode() if data is not None else None,headers={'Authorization':auth,'Accept':'application/vnd.github+json','Content-Type':'application/json','X-GitHub-Api-Version':'2022-11-28'})
    with urllib.request.urlopen(req,timeout=120) as response:
        data=response.read();return json.loads(data) if data else None
release=next((r for r in api('/releases?per_page=50') if r['tag_name']==tag),None)
sha=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
if release is None:
    if args.action!='stage':raise SystemExit('Stage content before publishing')
    release=api('/releases','POST',{'tag_name':tag,'target_commitish':sha,'name':'Motor Coast 2.1 — optional vehicle library','body':'Attributed modified models loaded by Motor Coast on demand. See fleet-expansion-manifest.json for each author, source, license and modifications. CC BY-SA derivatives retain CC BY-SA.','draft':True,'prerelease':True})
manifest=root/'fleet-expansion-manifest.json';models=json.loads(manifest.read_text())['vehicles'];files={manifest.name:(manifest,hashlib.sha256(manifest.read_bytes()).hexdigest())}
for item in models:
    for urlkey,hashkey in [('modelUrl','modelSha256'),('highModelUrl','highModelSha256')]:
        spec=item['spec'];name=urllib.parse.urlsplit(spec[urlkey]).path.rsplit('/',1)[1];file=root/'artifacts/expanded-content'/name
        assert file.is_file() and hashlib.sha256(file.read_bytes()).hexdigest()==spec[hashkey],name
        files[name]=(file,spec[hashkey])
assets={a['name']:a for a in api('/releases/'+str(release['id'])+'/assets?per_page=100')}
if len(assets)==100:
    assets.update({a['name']:a for a in api('/releases/'+str(release['id'])+'/assets?per_page=100&page=2')})
if args.action=='stage' and release['draft']:
    for name,asset in list(assets.items()):
        if name not in files:
            api('/releases/assets/'+str(asset['id']),'DELETE');del assets[name]
            print('Removed superseded draft asset: '+name,flush=True)
def upload(item):
    name,(file,digest)=item;old=assets.get(name)
    if old and old['state']=='uploaded' and old['size']==file.stat().st_size and old.get('digest')=='sha256:'+digest:return name+' verified'
    if args.action!='stage':raise RuntimeError('Missing or mismatched asset: '+name)
    if old:
        if not release['draft']:raise RuntimeError('Refusing to replace published content: '+name)
        api('/releases/assets/'+str(old['id']),'DELETE')
    url=release['upload_url'].split('{',1)[0]+'?name='+urllib.parse.quote(name)
    assert urllib.parse.urlsplit(url).hostname=='uploads.github.com'
    with file.open('rb') as payload:
        request=urllib.request.Request(url,method='POST',data=payload,headers={'Authorization':auth,'Content-Type':'application/octet-stream','Content-Length':str(file.stat().st_size)})
        with urllib.request.urlopen(request,timeout=600) as response:asset=json.load(response)
    assert asset['state']=='uploaded' and asset['size']==file.stat().st_size and asset.get('digest')=='sha256:'+digest
    return name+' uploaded'
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
    for result in pool.map(upload,files.items()):print(result,flush=True)
if args.action=='publish':
    release=api('/releases/'+str(release['id']),'PATCH',{'target_commitish':sha,'draft':False})
print('Files:',len(files),'Vehicles:',len(models),'Draft:',release['draft'],'URL:',release['html_url'])
