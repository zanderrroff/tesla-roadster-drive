#!/usr/bin/env python3
"""Download and verify an archive from this game's GitHub Actions workflow."""
import argparse, concurrent.futures, hashlib, json, os, pathlib, subprocess, urllib.error, urllib.parse, urllib.request

p=argparse.ArgumentParser()
p.add_argument('artifact_id',type=int)
p.add_argument('output',type=pathlib.Path)
args=p.parse_args()
base='https://api.github.com/repos/zanderrroff/tesla-roadster-drive'
credential=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\n\n',text=True,capture_output=True,check=True).stdout
token=dict(line.split('=',1) for line in credential.splitlines() if '=' in line)['password']
headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28'}
url=base+'/actions/artifacts/'+str(args.artifact_id)
with urllib.request.urlopen(urllib.request.Request(url,headers=headers),timeout=60) as response:
    artifact=json.load(response)
assert artifact['name']=='MotorCoast-unsigned-archive' and not artifact['expired']
expected=artifact['digest'].removeprefix('sha256:')
assert len(expected)==64
def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda:source.read(1024*1024),b''):h.update(block)
    return h.hexdigest()
if args.output.exists():
    assert digest(args.output)==expected,'Existing output is not this artifact'
    print('Verified existing archive:',args.output)
    raise SystemExit(0)
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args):return None
try:
    urllib.request.build_opener(NoRedirect).open(urllib.request.Request(url+'/zip',headers=headers),timeout=60)
    raise RuntimeError('Expected an artifact redirect')
except urllib.error.HTTPError as error:
    if error.code!=302:raise
    location=error.headers['Location']
assert urllib.parse.urlsplit(location).scheme=='https'
# The GitHub credential is never forwarded to the temporary storage URL.
with urllib.request.urlopen(urllib.request.Request(location,method='HEAD'),timeout=60) as response:
    total=int(response.headers['Content-Length'])
assert total==artifact['size_in_bytes']
args.output.parent.mkdir(parents=True,exist_ok=True)
partial=args.output.with_suffix(args.output.suffix+'.part')
with partial.open('wb') as output:output.truncate(total)
def part(bounds):
    begin,end=bounds;position=begin
    for attempt in range(3):
        try:
            req=urllib.request.Request(location,headers={'Range':f'bytes={position}-{end}'})
            with urllib.request.urlopen(req,timeout=120) as response,partial.open('r+b') as output:
                assert response.status==206
                assert response.headers['Content-Range']==f'bytes {position}-{end}/{total}'
                output.seek(position)
                while position<=end:
                    block=response.read(min(1024*1024,end-position+1))
                    if not block:raise IOError('Truncated artifact range')
                    output.write(block);position+=len(block)
            return end-begin+1
        except Exception:
            if attempt==2:raise
            if position>end:return end-begin+1
ranges=[(start,min(total-1,start+32*1024*1024-1)) for start in range(0,total,32*1024*1024)]
completed=0
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    for future in concurrent.futures.as_completed([pool.submit(part,item) for item in ranges]):
        completed+=future.result();print(f'Archive: {completed/total:.0%} ({completed/1024**2:.1f} MiB)',flush=True)
assert digest(partial)==expected,'Official GitHub artifact SHA-256 does not match'
os.replace(partial,args.output)
print('Verified native archive:',args.output,'SHA-256:',expected)
