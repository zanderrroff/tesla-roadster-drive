// Split welded driver-side coachwork into a hinged panel while preserving the
// closed silhouette, original surface attributes and the model's own cabin.
import fs from 'node:fs/promises';import crypto from 'node:crypto';
import {NodeIO} from '@gltf-transform/core';import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
import {prune,weld,dedup,simplify} from '@gltf-transform/functions';import {Vector3} from 'three';
import {MeshoptSimplifier} from 'meshoptimizer';await MeshoptSimplifier.ready;
import {getRecords} from './lib/fleet-geometry.mjs';
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS),path='unity/fleet-expansion-manifest.json';
const manifest=JSON.parse(await fs.readFile(path));
const sourceCars=JSON.parse(await fs.readFile('unity/fleet-source-manifest.json')).map(x=>x.car);
for(const item of manifest.vehicles){
 const spec=item.spec;if(item.doors)continue;
 const category=sourceCars.find(c=>c.id===spec.id)?.category;
 for(const key of new Set([spec.modelUrl,spec.highModelUrl])){
  const filename='unity/artifacts/expanded-content/'+key.split('/').at(-1),doc=await io.read(filename),root=doc.getRoot();
  if(root.listNodes().some(n=>n.getName()==='Door_Driver')){await save(doc,filename,key,item);continue;}
  const side=spec.driverPosition.x<0?1:-1,outer=spec.colliderSize.x*.49,inner=spec.colliderSize.x*.29;
  const front=Math.min(spec.wheelPosition.z+.34,Math.max(...spec.wheelCenters.map(w=>w.z))-spec.wheelRadius-.12);
  const back=Math.max(front-(category==='sports'?1.42:1.22),Math.min(...spec.wheelCenters.map(w=>w.z))+spec.wheelRadius+.16);
  const top=spec.roofY-.025,bottom=-.43;
  const hinge=new Vector3(side*outer,.08,front);
  if(spec.id.includes('300sl-gullwing'))hinge.set(0,top,(front+back)/2);
  const chassis=root.listNodes().find(n=>n.getName()==='Chassis'),door=doc.createNode('Door_Driver').setTranslation(hinge.toArray());chassis.addChild(door);
  const records=getRecords(doc,{}).filter(r=>r.parent.includes('Chassis')&&!r.parent.includes('SteeringWheel'));
  const buffer=root.listBuffers()[0];let moving=0;
  const planes=[[side,0,0,-inner],[-side,0,0,outer+.15],[0,1,0,-bottom],[0,-1,0,top],[0,0,1,-back],[0,-.60,-1,front+.60*spec.wheelPosition.y]];
  for(const rec of records){
   const semantics=rec.primitive.listSemantics(),groups=[[],[]];
   const vertex=i=>Object.fromEntries(semantics.map(s=>[s,s==='POSITION'?rec.positions[i].toArray():s==='NORMAL'?rec.normals[i].toArray():s==='TANGENT'?rec.tangents[i]:rec.primitive.getAttribute(s).getElement(i,[])]));
   const interpolate=(a,b,t)=>Object.fromEntries(semantics.map(s=>[s,a[s].map((v,i)=>v+(b[s][i]-v)*t)]));
   const emit=(polygon,group)=>{for(let i=1;i+1<polygon.length;i++)groups[group].push(polygon[0],polygon[i],polygon[i+1]);};
   for(let i=0;i<rec.indices.length;i+=3){
    let polygon=rec.indices.slice(i,i+3).map(vertex);
    for(const plane of planes){
     const inside=[],outside=[];for(let n=0;n<polygon.length;n++){
      const a=polygon[n],b=polygon[(n+1)%polygon.length],distance=v=>v.POSITION.reduce((s,x,k)=>s+x*plane[k],plane[3]);const da=distance(a),db=distance(b);
      (da>=0?inside:outside).push(a);
      if((da>=0)!==(db>=0)){const p=interpolate(a,b,da/(da-db));inside.push(p);outside.push(p);}
     }emit(outside,0);polygon=inside;if(!polygon.length)break;
    }emit(polygon,1);
   }
   if(!groups[1].length)continue;
   const make=(vertices,isDoor)=>{
    const p=doc.createPrimitive().setMaterial(rec.material);
    for(const semantic of semantics){const values=[];for(const v of vertices){let a=v[semantic];if(semantic==='POSITION'&&isDoor)a=[a[0]-hinge.x,a[1]-hinge.y,a[2]-hinge.z];if(semantic==='NORMAL')a=new Vector3(...a).normalize().toArray();values.push(...a);}
     p.setAttribute(semantic,doc.createAccessor().setBuffer(buffer).setType(rec.primitive.getAttribute(semantic).getType()).setArray(new Float32Array(values)));}
    p.setIndices(doc.createAccessor().setBuffer(buffer).setType('SCALAR').setArray(Uint32Array.from({length:vertices.length},(_,i)=>i)));return p;
   };
   const originalParents=rec.primitive.listParents().filter(p=>p.propertyType==='Mesh');
   if(groups[0].length){const body=make(groups[0],false);for(const mesh of originalParents){mesh.removePrimitive(rec.primitive);mesh.addPrimitive(body);}}
   else for(const mesh of originalParents)mesh.removePrimitive(rec.primitive);
   door.addChild(doc.createNode('Opening_'+rec.name).setMesh(doc.createMesh().addPrimitive(make(groups[1],true))));moving+=groups[1].length/3;
  }
  if(moving<100)throw Error(spec.id+': no usable opening door');
  await doc.transform(weld(),dedup(),prune());
  for(const list of [root.listNodes(),root.listMeshes()]){const names=new Set();list.forEach((o,i)=>{let n=o.getName()||'Part';if(names.has(n))n+='__'+i;o.setName(n);names.add(n);});}
  await save(doc,filename,key,item);
  console.log(spec.id,key===spec.modelUrl?'standard':'ultra',moving,'door triangles');
 }
 item.doors=true;item.changes+=' Driver-side door separated along fitted panel planes, retaining original materials and cabin surfaces.';
 await fs.writeFile(path,JSON.stringify(manifest,null,2)+'\n');
}

async function save(doc,filename,key,item){
 const spec=item.spec,root=doc.getRoot();const count=()=>root.listMeshes().reduce((s,m)=>s+m.listPrimitives().reduce((n,p)=>n+p.getIndices().getCount()/3,0),0);
 if(key===spec.modelUrl&&count()>149000)await doc.transform(simplify({simplifier:MeshoptSimplifier,ratio:140000/count(),error:.006,lockBorder:true}),prune());
 if(key===spec.modelUrl&&count()>150000)await doc.transform(simplify({simplifier:MeshoptSimplifier,ratio:130000/count(),error:.012,lockBorder:false}),prune());
 if(key===spec.modelUrl&&count()>150000)throw Error('Door split exceeds mobile budget: '+spec.id);
 await io.write(filename,doc);const bytes=await fs.readFile(filename),hash=crypto.createHash('sha256').update(bytes).digest('hex');
 if(key===spec.modelUrl){spec.modelSha256=hash;spec.downloadBytes=bytes.length;item.standardTriangles=count();}
 if(key===spec.highModelUrl){spec.highModelSha256=hash;spec.highDownloadBytes=bytes.length;item.highTriangles=count();}
}
