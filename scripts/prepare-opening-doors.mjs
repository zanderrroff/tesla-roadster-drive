// Separate the original exterior door geometry without replacing its material
// or UVs. Closed poses remain identical. Re-run after prepare-unity-assets.
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {NodeIO} from '@gltf-transform/core';
import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
import {prune,weld,dedup} from '@gltf-transform/functions';
import {Vector3} from 'three';
import {getRecords,connectedParts} from './lib/fleet-geometry.mjs';
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS);
const root='unity/MotorCoast/Assets/Resources';
const regions={
 roadster:[.50,-.46,-.49,1.01,.50,.78],
 'mercedes-benz-amg-gt':[.60,-.34,-.88,1.02,.61,.44],
 'mercedes-benz-190e-evo':[.55,-.38,-.205,1,.72,.96],
 'porsche-911-carrera':[.56,-.34,-.60,1,.62,.73],
 'ferrari-458':[.60,-.36,-.57,1.04,.64,.75],
 'bmw-m3-e30':[.505,-.35,-.40,.90,.72,.73],
 'nissan-skyline-gt-r':[.60,-.36,-.43,.95,.68,.61]
};
const catalog=JSON.parse(await fs.readFile(root+'/Data/vehicles.json'));
for(const spec of catalog.vehicles.filter(c=>c.resource))for(const resource of new Set([spec.resource,spec.highResource])){
 const filename=root+'/'+resource+'.gltf',doc=await io.read(filename),r=doc.getRoot();
 if(r.listNodes().some(n=>n.getName()==='Door_Driver')){await optimize(doc,filename);continue;}
 const region=regions[spec.id];if(!region)continue;
 const chassis=r.listNodes().find(n=>n.getName()==='Chassis');
 const records=getRecords(doc,{}).filter(x=>x.parent.includes('Chassis')&&!x.parent.includes('SteeringWheel'));
 const buffer=r.listBuffers()[0],hinge=new Vector3((region[0]+region[3])/2,.08,region[5]-.04);
 const door=doc.createNode('Door_Driver').setTranslation(hinge.toArray());chassis.addChild(door);
 let count=0;
 for(const rec of records){
  const selected=new Set();
  for(const part of connectedParts(rec)){
   const b=part.bounds;
   if(b.min.x>=region[0]&&b.min.y>=region[1]&&b.min.z>=region[2]&&b.max.x<=region[3]&&b.max.y<=region[4]&&b.max.z<=region[5])for(const i of part.indices)selected.add(i);
  }
  // These two source bodies have welded coachwork. Split only the painted
  // panel along its front-door seam; the glass is already disconnected above.
  const cut=(spec.id==='bmw-m3-e30'||spec.id==='nissan-skyline-gt-r')&&/paint|body/i.test(rec.material.getName());
  const semantics=rec.primitive.listSemantics();const groups=[[],[]];
  const vertex=i=>Object.fromEntries(semantics.map(s=>[s,s==='POSITION'?rec.positions[i].toArray():s==='NORMAL'?rec.normals[i].toArray():s==='TANGENT'?rec.tangents[i]:rec.primitive.getAttribute(s).getElement(i,[])]));
  const lerp=(a,b,t)=>Object.fromEntries(semantics.map(s=>[s,a[s].map((v,i)=>v+(b[s][i]-v)*t)]));
  const clip=(polygon,axis,edge,positive)=>{
   const inside=[],outside=[];if(!polygon.length)return [inside,outside];
   for(let n=0;n<polygon.length;n++){
    const a=polygon[n],b=polygon[(n+1)%polygon.length],da=(a.POSITION[axis]-edge)*positive,db=(b.POSITION[axis]-edge)*positive;
    (da>=0?inside:outside).push(a);
    if((da>=0)!==(db>=0)){const p=lerp(a,b,da/(da-db));inside.push(p);outside.push(p);}
   }return [inside,outside];
  };
  const triangles=(polygon,target)=>{for(let n=1;n+1<polygon.length;n++)groups[target].push(polygon[0],polygon[n],polygon[n+1]);};
  for(let n=0;n<rec.indices.length;n+=3){
   const indices=rec.indices.slice(n,n+3),polygon=indices.map(vertex);
   if(indices.every(i=>selected.has(i))){triangles(polygon,1);continue;}
   if(!cut){triangles(polygon,0);continue;}
   let remaining=polygon;
   for(const [axis,edge,sign] of [[0,region[0],1],[0,region[3],-1],[1,region[1],1],[1,region[4],-1],[2,region[2],1],[2,region[5],-1]]){
    const [inside,outside]=clip(remaining,axis,edge,sign);triangles(outside,0);remaining=inside;
   }triangles(remaining,1);
  }
  if(!groups[1].length)continue;
  const make=(vertices,moving)=>{
   const p=doc.createPrimitive().setMaterial(rec.material);
   for(const s of semantics){const values=[];for(const v of vertices){let a=v[s];if(s==='POSITION'&&moving)a=[a[0]-hinge.x,a[1]-hinge.y,a[2]-hinge.z];if(s==='NORMAL')a=new Vector3(...a).normalize().toArray();values.push(...a);}p.setAttribute(s,doc.createAccessor().setBuffer(buffer).setType(rec.primitive.getAttribute(s).getType()).setArray(new Float32Array(values)));}
   p.setIndices(doc.createAccessor().setBuffer(buffer).setType('SCALAR').setArray(Uint32Array.from({length:vertices.length},(_,i)=>i)));return p;
  };
  // Original primitives are in chassis coordinates in the prepared assets.
  if(groups[0].length){const p=make(groups[0],false);for(const parent of rec.primitive.listParents())if(parent.propertyType==='Mesh'){parent.removePrimitive(rec.primitive);parent.addPrimitive(p);}}
  else for(const parent of rec.primitive.listParents())if(parent.propertyType==='Mesh')parent.removePrimitive(rec.primitive);
  door.addChild(doc.createNode('Door_'+rec.name).setMesh(doc.createMesh().addPrimitive(make(groups[1],true))));count+=groups[1].length/3;
 }
 if(count<50)throw Error('No usable door in '+spec.id);
 await optimize(doc,filename);console.log(resource,count,'moving door triangles');
}
async function optimize(doc,filename){
 await doc.transform(weld(),dedup(),prune());
 for(const objects of [doc.getRoot().listNodes(),doc.getRoot().listMeshes()]){const used=new Set();objects.forEach((o,i)=>{let name=o.getName()||'Part';if(used.has(name))name+='__'+i;o.setName(name);used.add(name);});}
 await io.write(filename,doc);
}
// Keep source verification authoritative after the geometry-only transform.
const manifest=JSON.parse(await fs.readFile('unity/asset-manifest.json'));
for(const item of manifest){
 const file=root+'/'+item.file,j=JSON.parse(await fs.readFile(file));let total=0;
 for(const entry of item.files){const bytes=await fs.readFile(root+'/'+entry.file);entry.bytes=bytes.length;entry.sha256=crypto.createHash('sha256').update(bytes).digest('hex');total+=bytes.length;}
 item.bytes=total;item.triangles=j.meshes.reduce((n,m)=>n+m.primitives.reduce((s,p)=>s+j.accessors[p.indices].count/3,0),0);
}
await fs.writeFile('unity/asset-manifest.json',JSON.stringify(manifest,null,2)+'\n');
