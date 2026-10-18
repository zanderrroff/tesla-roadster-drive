#!/usr/bin/env python3
"""Download the accepted source snapshot, checking every original GLB hash."""
import concurrent.futures,hashlib,json,pathlib,urllib.request
root=pathlib.Path(__file__).resolve().parents[1]
items=json.loads((root/'unity/fleet-source-manifest.json').read_text())
dest=root/'.asset-work/expanded-cars';dest.mkdir(parents=True,exist_ok=True)
def fetch(item):
    path=dest/(item['car']['id']+'.glb')
    if not path.exists():
        with urllib.request.urlopen(item['downloadUrl'],timeout=120) as response:path.write_bytes(response.read())
    assert hashlib.sha256(path.read_bytes()).hexdigest()==item['sourceSha256'],str(path)
    print(item['car']['id'],flush=True)
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:list(pool.map(fetch,items))
(root/'.asset-work/expanded-candidates.json').write_text(json.dumps(items,indent=2)+'\n')
