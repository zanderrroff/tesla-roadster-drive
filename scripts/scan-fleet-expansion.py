#!/usr/bin/env python3
"""Read public GLB scene metadata before downloading or accepting new cars."""
import concurrent.futures,json,pathlib,struct,urllib.request,re
R=pathlib.Path(__file__).resolve().parents[1]
cars=json.loads((R/'.asset-work/cars.json').read_text())['cars']
def scan(car):
    result={'car':car}
    try:
        url='https://raw.githubusercontent.com/pedrosoandre/carros/main/assets/'+car['file']
        with urllib.request.urlopen(url,timeout=45) as response:
            header=response.read(20)
            if header[:4]!=b'glTF':raise ValueError('Not GLB')
            length=struct.unpack('<I',header[12:16])[0]
            if length>15000000:raise ValueError('JSON too large')
            doc=json.loads(response.read(length))
        names=[n.get('name','') for group in ['nodes','materials'] for n in doc.get(group,[])]
        result['interior']=[n for n in names if re.search(r'seat|sitze|cockpit|interior|salon|steer|lenkrad|volante',n,re.I)]
        result['tires']=[n for n in names if re.search(r'tire|tyre|wheel|reifen|pneu',n,re.I)]
        result['doors']=[n for n in names if re.search(r'door|porte|tuer',n,re.I)]
        result['materials']=[n.get('name','') for n in doc.get('materials',[])]
        if result['interior'] and result['tires']:
            (R/'.asset-work/car-scan').mkdir(exist_ok=True)
            (R/('.asset-work/car-scan/'+car['id']+'.json')).write_text(json.dumps(doc))
    except Exception as e:result['error']=str(e)
    return result
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    results=[]
    for i,result in enumerate(pool.map(scan,cars)):
        results.append(result)
        if i%25==0:print(i,'scanned;',sum(bool(r.get('interior') and r.get('tires')) for r in results),'candidates',flush=True)
(R/'.asset-work/fleet-expansion-scan.json').write_text(json.dumps(results,indent=2))
print('Done',len(results),'candidates',sum(bool(r.get('interior') and r.get('tires')) for r in results))
