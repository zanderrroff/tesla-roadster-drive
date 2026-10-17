import {NodeIO} from '@gltf-transform/core';
import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
import {simplifyPrimitive, prune, dedup, weld} from '@gltf-transform/functions';
import {MeshoptSimplifier} from 'meshoptimizer';
import fs from 'node:fs';
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS);
await MeshoptSimplifier.ready;
const input=process.argv[2];
const doc=await io.read(input);
const root=doc.getRoot(), scene=root.listScenes()[0], buffer=root.listBuffers()[0];
const nodes=[...root.listNodes()].filter(n=>n.getMesh());
// Source geometry is Z-up, front towards negative Y; normalize to Y-up, front +Z.
const wheelNodes=nodes.filter(n=>/^(Tires|Disc|Maxs|Breaks)/.test(n.getName()));
const bounds=Array.from({length:4},()=>({min:[Infinity,Infinity,Infinity],max:[-Infinity,-Infinity,-Infinity]}));
const bucket=(x,z)=>(z>0?0:2)+(x>0?1:0);
for(const n of wheelNodes.filter(n=>n.getName().startsWith('Tires')))for(const p of n.getMesh().listPrimitives()){
 const a=p.getAttribute('POSITION').getArray();for(let i=0;i<a.length;i+=3){const v=[a[i],a[i+2],-a[i+1]],b=bounds[bucket(v[0],v[2])];for(let j=0;j<3;j++){b.min[j]=Math.min(b.min[j],v[j]);b.max[j]=Math.max(b.max[j],v[j]);}}
}
const centers=bounds.map(b=>b.min.map((v,i)=>(v+b.max[i])/2));
const centerY=.58;
const groups=[doc.createNode('Chassis'),...centers.map((_,i)=>doc.createNode('Wheel_'+i))];
for(const n of nodes){const mesh=n.getMesh();const isWheel=wheelNodes.includes(n);
 for(const p of mesh.listPrimitives()){
 const src=p.getAttribute('POSITION').getArray(), norm=p.getAttribute('NORMAL')?.getArray(),idx=p.getIndices()?.getArray()||Uint32Array.from({length:src.length/3},(_,i)=>i);
 const pieces=new Map();
 for(let i=0;i<idx.length;i+=3){const k=isWheel?bucket(src[idx[i]*3],-src[idx[i]*3+1])+1:0;if(!pieces.has(k))pieces.set(k,[]);pieces.get(k).push(idx[i],idx[i+1],idx[i+2]);}
 for(const [k,indices] of pieces){const remap=new Map(),pos=[],nor=[],out=[];
 const off=k?centers[k-1]:[0,centerY,0];
 for(const vi of indices){if(!remap.has(vi)){remap.set(vi,remap.size);pos.push(src[vi*3]-off[0],src[vi*3+2]-off[1],-src[vi*3+1]-off[2]);if(norm)nor.push(norm[vi*3],norm[vi*3+2],-norm[vi*3+1]);}out.push(remap.get(vi));}
 const pp=doc.createPrimitive().setAttribute('POSITION',doc.createAccessor().setType('VEC3').setArray(new Float32Array(pos)).setBuffer(buffer)).setIndices(doc.createAccessor().setType('SCALAR').setArray(new Uint32Array(out)).setBuffer(buffer)).setMaterial(p.getMaterial());
 if(norm)pp.setAttribute('NORMAL',doc.createAccessor().setType('VEC3').setArray(new Float32Array(nor)).setBuffer(buffer));
 const child=doc.createNode(n.getName()).setMesh(doc.createMesh(n.getName()).addPrimitive(pp));groups[k].addChild(child);
 }
 }
}
for(const n of [...scene.listChildren()])scene.removeChild(n);
for(const g of groups)scene.addChild(g);
for(const m of root.listMaterials()){
 if(m.getName()==='Material.001'){m.setBaseColorFactor([.38,.012,.025,1]);m.setMetallicFactor(.65);m.setRoughnessFactor(.26);}
 if(m.getName()==='Material'){m.setBaseColorFactor([.025,.05,.07,.86]);m.setAlphaMode('BLEND');m.setRoughnessFactor(.15);}
}
await doc.transform(prune(),weld(),dedup());
const count=d=>d.getRoot().listMeshes().reduce((s,m)=>s+m.listPrimitives().reduce((t,p)=>t+(p.getIndices()?.getCount()||p.getAttribute('POSITION').getCount())/3,0),0);
const initial=count(doc);
for(const [name,ratio] of [['high',.32],['mobile',.065]]){const d=await io.readBinary(await io.writeBinary(doc));for(const mesh of d.getRoot().listMeshes()) for(const prim of mesh.listPrimitives()) simplifyPrimitive(prim,{simplifier:MeshoptSimplifier,ratio:mesh.getName()==='Body_Material.001_0'?(name==='mobile'?.4:.65):ratio,error:mesh.getName()==='Body_Material.001_0'?.002:(name==='mobile'?.008:.003)}); await d.transform(prune(),dedup());const tris=count(d);if(name==='mobile'&&tris>150000)throw Error('Mobile triangle budget exceeded: '+tris);await io.write('public/models/roadster-'+name+'.glb',d);console.log(name,tris,fs.statSync('public/models/roadster-'+name+'.glb').size);}
fs.writeFileSync('src/vehicle-dimensions.json',JSON.stringify({centers:centers.map(v=>[v[0],v[1]-centerY,v[2]]),radius:(bounds[0].max[1]-bounds[0].min[1])/2,triangles:initial},null,2));
console.log('wheel centers',centers);
