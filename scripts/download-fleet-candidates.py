#!/usr/bin/env python3
import concurrent.futures,json,pathlib,re,urllib.request,sys,html
R=pathlib.Path(__file__).resolve().parents[1];dest=R/'.asset-work/expanded-cars';dest.mkdir(exist_ok=True)
scan=json.loads((R/'.asset-work/fleet-expansion-scan.json').read_text())
items=[r for r in scan if ((r.get('interior') and r.get('tires')) or ('--materials' in sys.argv and any(re.search('cabin|seat|interior|leather',n,re.I) for n in r.get('materials',[])) and any(re.search('tyre|tire|rim|wheel|reifen|pneu',n,re.I) for n in r.get('materials',[])))) and ('--broad' in sys.argv or any(re.search('steer|lenkrad|volante|driving_wheel',n,re.I) for n in r['interior']+r['tires']))]
def prepare(r):
    c=r['car'];result={'car':c};uid=re.search(r'[0-9a-f]{32}$',c['credit']['sourceUrl'])
    try:
        if not uid:raise ValueError('Source ID missing')
        path=dest/(c['id']+'.source.json')
        if path.exists():d=json.loads(path.read_text())
        else:
            if '--public-pages' in sys.argv:
                url='https://sketchfab.com/3d-models/'+c['id']+'-'+uid[0]
                with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'}),timeout=60) as f:page=f.read().decode()
                match=re.search(r'id="js-dom-data-prefetched-data">\s*<!--(.*?)-->',page,re.S)
                if not match:raise ValueError('Public model metadata missing')
                d=json.loads(html.unescape(match[1]))['/i/models/'+uid[0]]
            else:
                with urllib.request.urlopen('https://api.sketchfab.com/v3/models/'+uid[0],timeout=60) as f:d=json.load(f)
            path.write_text(json.dumps(d))
        if d.get('license',{}).get('slug') not in ['by','by-sa','by-nc','by-nc-sa']:raise ValueError('Source license does not allow derivatives')
        description=d.get('description','')
        if re.search(r'forza|gran turismo|grand theft|\bgta\b|ripped|csr.?2|need for speed|assetto corsa|from beamng',description,re.I):raise ValueError('Source describes another game; requires manual provenance review')
        if re.search(r'steering-wheel|tractor|crf-450|tomahawk|f1$|w13|ferrari-499|capri-group|zakspeed',c['id']):raise ValueError('Outside passenger car scope')
        target=dest/(c['id']+'.glb')
        if not target.exists():
            with urllib.request.urlopen('https://raw.githubusercontent.com/pedrosoandre/carros/main/assets/'+c['file'],timeout=90) as f:target.write_bytes(f.read())
        c['credit']['license']=d['license']['label']
        result.update(accepted=True,sourceLicense=d['license'],description=description)
        print('downloaded',c['id'],flush=True)
    except Exception as e:result['error']=str(e);print('skip',c['id'],result['error'],flush=True)
    return result
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:results=list(pool.map(prepare,items))
(R/'.asset-work/expanded-candidates.json').write_text(json.dumps(results,indent=2))
print('Downloaded',sum(r.get('accepted',False) for r in results),'candidates')
