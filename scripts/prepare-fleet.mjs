/**
 * Rebuild the licensed fleet from the sources recorded in public/models/fleet/manifest.json.
 * Run from the repository root: node scripts/prepare-fleet.mjs
 * Source downloads are cached in .asset-work. Output is ready for GLTFLoader + the local Draco decoder.
 * Edits: ground planes removed; metric Y-up/+Z-front; four real wheel pivots;
 * neutral steering; material and draw-call cleanup; <=150k triangles; <=1024px textures.
 */
import fs from 'node:fs';
import {NodeIO} from '@gltf-transform/core';
import {ALL_EXTENSIONS, KHRMaterialsClearcoat} from '@gltf-transform/extensions';
import {dedup, prune, weld, simplifyPrimitive, joinPrimitives, textureCompress, draco} from '@gltf-transform/functions';
import {MeshoptDecoder, MeshoptSimplifier} from 'meshoptimizer';
import draco3d from 'draco3dgltf';
import sharp from 'sharp';
import {Matrix4, Matrix3, Vector3, Box3} from 'three';
const work='.asset-work',out='public/models/fleet';
fs.mkdirSync(work,{recursive:true});fs.mkdirSync(out,{recursive:true});
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({'meshopt.decoder':MeshoptDecoder,'draco3d.decoder':await draco3d.createDecoderModule(),'draco3d.encoder':await draco3d.createEncoderModule()});
await MeshoptSimplifier.ready;
const library='https://raw.githubusercontent.com/pedrosoandre/carros/main/assets/';
const specs=[
 {id:'bmw-m3-e30',name:'BMW M3 E30',length:4.36,rotation:0,author:'Martin Trafas',uid:'ac3c7013434e403e8faff87948caf422',hasInterior:false,eye:[.34,.48,-.23],tire:r=>r.material.getName().endsWith('_TIRE'),wheel:r=>Number(r.name.replace('Object_',''))>=15,paint:/PAINT/,color:[.29,.015,.022,1]},
 {id:'mercedes-benz-amg-gt',name:'Mercedes-Benz AMG GT',length:4.55,rotation:'amg',author:'Yan Carvalho',uid:'661dcab94455463784651a3ebc63cfb9',hasInterior:true,eye:[.34,.46,-.35],skip:n=>n==='Object_4',tire:r=>r.name==='Object_28',wheel:r=>['Object_13','Object_15','Object_28'].includes(r.name),paint:/^Material\.001$/,color:[.024,.21,.31,1]},
 {id:'mercedes-benz-190e-evo',name:'Mercedes-Benz 190E EVO',length:4.44,rotation:0,author:'Betier Models',uid:'15e5576f4b724f2db357165caed3492b',hasInterior:true,eye:[.33,.56,-.06],tire:r=>r.name.startsWith('TYRE_'),wheel:r=>/^(TYRE_|RIM_|IN_|Name-WheelBrake)/.test(r.name),paint:/^Car_body_color$/,color:[.025,.032,.045,1]},
 {id:'nissan-skyline-gt-r',name:'Nissan Skyline GT-R C110',length:4.46,rotation:Math.PI,author:'Martin Trafas',uid:'1a950b81ee274a0eb8014bd84ba047f5',hasInterior:false,eye:[.34,.49,-.24],skip:n=>['Shadow_Shadow_0','Inside_InsideMaterial_0'].includes(n),tire:r=>r.name.startsWith('Tire_'),wheel:r=>/^(HayashiStreet|Tire_|Brake_)/.test(r.name),paint:/^Paint$/,color:null},
 {id:'porsche-911-carrera',name:'Porsche 911 Carrera 4S',length:4.45,rotation:0,author:'Lionsharp Studios',hasInterior:false,eye:[.35,.47,-.25],skip:n=>n==='Plane_0',tire:r=>/^Cylinder/.test(r.name)&&r.material.getName()==='rubber',wheel:r=>/^Cylinder/.test(r.name),paint:/^paint$/,color:[.62,.65,.67,1],license:'CC BY-SA 4.0'},
 {id:'ferrari-458',name:'Ferrari 458 Italia',length:4.53,rotation:Math.PI,author:'vicent091036',uid:'57bf6cc56931426e87494f554df1dab6',url:'https://raw.githubusercontent.com/mrdoob/three.js/dev/examples/models/gltf/ferrari.glb',hasInterior:true,eye:[.35,.49,-.08],tire:r=>r.name==='tire',wheel:r=>r.parent.startsWith('wheel_'),paint:/^Body_Color$/,color:[.56,.008,.012,1]}
];
// Keep the source URL exactly as distributed by the attributed upstream library.
const catalogPath=work+'/carros-cars.json';
if(!fs.existsSync(catalogPath)){const r=await fetch('https://raw.githubusercontent.com/pedrosoandre/carros/main/cars.json');if(!r.ok)throw Error('Cannot fetch catalog');fs.writeFileSync(catalogPath,await r.text());}
const catalog=JSON.parse(fs.readFileSync(catalogPath)).cars;
const v=new Vector3();
function pointsBounds(points){return new Box3().setFromPoints(points);}
function clusters(points){
 const bounds=pointsBounds(points),c=pointsBounds(points).getCenter(new Vector3());
 let centers=[new Vector3(bounds.min.x,c.y,bounds.max.z),new Vector3(bounds.max.x,c.y,bounds.max.z),new Vector3(bounds.min.x,c.y,bounds.min.z),new Vector3(bounds.max.x,c.y,bounds.min.z)],groups;
 for(let iteration=0;iteration<24;iteration++){
  groups=centers.map(()=>[]);for(const p of points){let index=0,d=Infinity;centers.forEach((c,i)=>{const dd=p.distanceToSquared(c);if(dd<d){d=dd;index=i}});groups[index].push(p)}
  if(groups.some(g=>!g.length))throw Error('Empty wheel cluster');centers=groups.map(g=>pointsBounds(g).getCenter(new Vector3()));
 }
 const items=groups.map((points,i)=>({points,center:centers[i]}));
 items.sort((a,b)=>b.center.z-a.center.z);const front=items.splice(0,2).sort((a,b)=>a.center.x-b.center.x),rear=items.sort((a,b)=>a.center.x-b.center.x);return [...front,...rear];
}
function steeringCorrection(points,center){
 let xx=0,zz=0,xz=0;for(const p of points){const x=p.x-center.x,z=p.z-center.z;xx+=x*x;zz+=z*z;xz+=x*z}
 let angle=.5*Math.atan2(2*xz,xx-zz)+Math.PI/2;while(angle>Math.PI/2)angle-=Math.PI;while(angle< -Math.PI/2)angle+=Math.PI;
 return angle;
}
function getRecords(doc,spec){
 const records=[];for(const node of doc.getRoot().listNodes()){
  if(!node.getMesh()||spec.skip?.(node.getName()))continue;const world=new Matrix4().fromArray(node.getWorldMatrix()),normal=new Matrix3().getNormalMatrix(world);
  for(const primitive of node.getMesh().listPrimitives()){
   const positions=[],normals=[],tangents=[];const p=primitive.getAttribute('POSITION'),n=primitive.getAttribute('NORMAL'),t=primitive.getAttribute('TANGENT');
   for(let i=0;i<p.getCount();i++)positions.push(new Vector3().fromArray(p.getElement(i,[])).applyMatrix4(world));
   if(n)for(let i=0;i<n.getCount();i++)normals.push(new Vector3().fromArray(n.getElement(i,[])).applyMatrix3(normal).normalize());
   if(t)for(let i=0;i<t.getCount();i++){const a=t.getElement(i,[]);tangents.push([...new Vector3(...a.slice(0,3)).applyMatrix3(normal).normalize().toArray(),a[3]*(world.determinant()<0?-1:1)])}
   let indices=primitive.getIndices()?Array.from(primitive.getIndices().getArray()):Array.from({length:p.getCount()},(_,i)=>i);if(world.determinant()<0)for(let i=0;i<indices.length;i+=3)[indices[i+1],indices[i+2]]=[indices[i+2],indices[i+1]];
   records.push({name:node.getName(),parent:node.getParentNode()?.getName()||'',material:primitive.getMaterial(),positions,normals,tangents,indices,primitive});
  }
 }return records;
}
const only=process.argv.find(a=>a.startsWith('--only='))?.slice(7);if(only&&!specs.some(s=>s.id===only))throw Error('Unknown fleet id: '+only);
const manifest=only&&fs.existsSync(out+'/manifest.json')?JSON.parse(fs.readFileSync(out+'/manifest.json')).filter(m=>m.id!==only):[];
for(const spec of specs.filter(s=>!only||s.id===only)){
 const input=work+'/'+spec.id+'.glb',entry=catalog.find(c=>c.id===spec.id);
 if(!fs.existsSync(input)){const r=await fetch(spec.url||library+spec.id+'.glb');if(!r.ok)throw Error('Cannot fetch '+spec.id);fs.writeFileSync(input,Buffer.from(await r.arrayBuffer()));}
 const doc=await io.read(input),root=doc.getRoot();
 if(spec.id==='ferrari-458')for(const mesh of root.listMeshes())for(const p of mesh.listPrimitives())p.setAttribute('TEXCOORD_0',null);
 const records=getRecords(doc,spec),tireRecords=records.filter(spec.tire);let angle=spec.rotation;
 if(angle==='amg'){
  // The source is posed at an arbitrary yaw. The front axle is on the negative X side.
  const pts=tireRecords.flatMap(r=>r.positions);let centers=[[-5,1,0],[-5,1,4],[2,1,-1],[2,1,3]].map(p=>new Vector3(...p));
  for(let i=0;i<24;i++){const groups=centers.map(()=>[]);for(const p of pts){const ds=centers.map(c=>c.distanceToSquared(p));groups[ds.indexOf(Math.min(...ds))].push(p)}centers=groups.map(g=>pointsBounds(g).getCenter(new Vector3()))}
  const direction=centers[0].clone().add(centers[1]).sub(centers[2]).sub(centers[3]);angle=-Math.atan2(direction.x,direction.z);
 }
 const rot=new Matrix4().makeRotationY(angle),normalRot=new Matrix3().setFromMatrix4(rot);
 for(const r of records){r.positions.forEach(p=>p.applyMatrix4(rot));r.normals.forEach(p=>p.applyMatrix3(normalRot));r.tangents=r.tangents.map(t=>[...new Vector3(...t.slice(0,3)).applyMatrix3(normalRot).toArray(),t[3]])}
 const bounds=pointsBounds(records.flatMap(r=>r.positions)),rawWheels=clusters(tireRecords.flatMap(r=>r.positions));
 const scale=spec.length/(bounds.max.z-bounds.min.z),center=bounds.getCenter(new Vector3()),ground=Math.min(...rawWheels.map(w=>pointsBounds(w.points).min.y));
 const translate=new Vector3(-center.x*scale,-.58-ground*scale,-center.z*scale);
 for(const r of records)r.positions.forEach(p=>p.multiplyScalar(scale).add(translate));
 const wheels=clusters(tireRecords.flatMap(r=>r.positions));
 for(const w of wheels){w.correct=steeringCorrection(w.points,w.center);w.rotation=new Matrix4().makeRotationY(w.correct);w.normalRotation=new Matrix3().setFromMatrix4(w.rotation)}
 const buffer=root.listBuffers()[0]||doc.createBuffer(),groups=[doc.createNode('Chassis'),...wheels.map((_,i)=>doc.createNode('Wheel_'+i))],buckets=groups.map(()=>new Map());
 for(const r of records){
  const wheel=spec.wheel(r),pieces=new Map();
  for(let i=0;i<r.indices.length;i+=3){let k=0;if(wheel){v.copy(r.positions[r.indices[i]]).add(r.positions[r.indices[i+1]]).add(r.positions[r.indices[i+2]]).multiplyScalar(1/3);const ds=wheels.map(w=>w.center.distanceToSquared(v));k=ds.indexOf(Math.min(...ds))+1}if(!pieces.has(k))pieces.set(k,[]);pieces.get(k).push(...r.indices.slice(i,i+3));}
  for(const [k,sourceIndices] of pieces){
   const remap=new Map(),indices=[],attributes={},semantics=r.primitive.listSemantics().filter(s=>!s.startsWith('JOINTS')&&!s.startsWith('WEIGHTS'));
   for(const semantic of semantics)attributes[semantic]=[];
   for(const index of sourceIndices){if(!remap.has(index)){remap.set(index,remap.size);for(const semantic of semantics){let values;
    if(semantic==='POSITION'){const p=r.positions[index].clone();if(k)p.sub(wheels[k-1].center).applyMatrix4(wheels[k-1].rotation);values=p.toArray()}
    else if(semantic==='NORMAL'){const p=r.normals[index].clone();if(k)p.applyMatrix3(wheels[k-1].normalRotation);values=p.toArray()}
    else if(semantic==='TANGENT'){const t=r.tangents[index],p=new Vector3(...t.slice(0,3));if(k)p.applyMatrix3(wheels[k-1].normalRotation);values=[...p.toArray(),t[3]]}
    else values=r.primitive.getAttribute(semantic).getElement(index,[]);
    attributes[semantic].push(...values);
   }}indices.push(remap.get(index));}
   const primitive=doc.createPrimitive().setMaterial(r.material).setIndices(doc.createAccessor().setBuffer(buffer).setType('SCALAR').setArray(new Uint32Array(indices)));
   for(const semantic of semantics)primitive.setAttribute(semantic,doc.createAccessor().setBuffer(buffer).setType(r.primitive.getAttribute(semantic).getType()).setArray(new Float32Array(attributes[semantic])));
   const key=root.listMaterials().indexOf(r.material)+'|'+semantics.sort().join(',');if(!buckets[k].has(key))buckets[k].set(key,[]);buckets[k].get(key).push(primitive);
  }
 }
 for(let k=0;k<groups.length;k++)for(const prims of buckets[k].values()){const joined=prims.length>1?joinPrimitives(prims):prims[0];if(prims.length>1)prims.forEach(p=>p.dispose());const material=joined.getMaterial().getName();groups[k].addChild(doc.createNode((k?'WheelDetail_':'BodyDetail_')+material).setMesh(doc.createMesh(material).addPrimitive(joined)))}
 for(const scene of root.listScenes())for(const n of [...scene.listChildren()])scene.removeChild(n);
 const scene=root.listScenes()[0];for(const group of groups)scene.addChild(group);
 for(const animation of root.listAnimations())animation.dispose();
 for(const ext of [...root.listExtensionsUsed()])if(['EXT_meshopt_compression','KHR_mesh_quantization','KHR_draco_mesh_compression'].includes(ext.extensionName))ext.dispose();
 const clearcoat=doc.createExtension(KHRMaterialsClearcoat);
 for(const material of root.listMaterials()){
  if(/window/i.test(material.getName()))material.setBaseColorFactor([.16,.21,.24,.24]).setAlphaMode('BLEND').setMetallicFactor(.08).setRoughnessFactor(.16);
  if(spec.paint.test(material.getName())){if(spec.color)material.setBaseColorFactor(spec.color);material.setMetallicFactor(.63).setRoughnessFactor(.25);material.setExtension('KHR_materials_clearcoat',clearcoat.createClearcoat().setClearcoatFactor(1).setClearcoatRoughnessFactor(.18));}
  if(spec.id==='mercedes-benz-amg-gt'&&material.getName()==='Material.004')material.setBaseColorFactor([.075,.085,.095,1]).setMetallicFactor(.05).setRoughnessFactor(.72);
  if(spec.id==='mercedes-benz-amg-gt'&&material.getName()==='Material.023')material.setMetallicFactor(.03).setRoughnessFactor(.85);
  if(material.getExtension('KHR_materials_transmission')){material.setExtension('KHR_materials_transmission',null);const color=material.getBaseColorFactor();color[3]=Math.min(color[3],.28);material.setBaseColorFactor(color).setAlphaMode('BLEND').setMetallicFactor(.05).setRoughnessFactor(.15)}
  if(spec.id==='ferrari-458'&&material.getName()==='Glass_Gray')material.setBaseColorFactor([.16,.21,.24,.2]).setAlphaMode('BLEND').setMetallicFactor(.08).setRoughnessFactor(.15);
  if(spec.id==='ferrari-458'&&['Carpet','Carbon_Fiber'].includes(material.getName()))material.setBaseColorFactor([.025,.03,.035,1]).setMetallicFactor(.1).setRoughnessFactor(.8);
  if(spec.id==='mercedes-benz-amg-gt'&&material.getName()==='Material')material.setBaseColorFactor([.15,.2,.23,.19]).setMetallicFactor(.08).setRoughnessFactor(.14).setAlphaMode('BLEND');
 }
 if(spec.id==='ferrari-458')for(const mesh of root.listMeshes())for(const p of mesh.listPrimitives()){const pos=p.getAttribute('POSITION'),nor=p.getAttribute('NORMAL');if(!nor)continue;const sums=new Map();for(let i=0;i<pos.getCount();i++){const key=pos.getElement(i,[]).map(x=>x.toFixed(6)).join(','),normal=new Vector3().fromArray(nor.getElement(i,[]));if(!sums.has(key))sums.set(key,new Vector3());sums.get(key).add(normal)}for(const sum of sums.values())sum.normalize();for(let i=0;i<pos.getCount();i++){const key=pos.getElement(i,[]).map(x=>x.toFixed(6)).join(',');nor.setElement(i,sums.get(key).toArray())}}
 await doc.transform(prune(),dedup(),weld());
 const count=()=>root.listMeshes().reduce((sum,mesh)=>sum+mesh.listPrimitives().reduce((s,p)=>s+(p.getIndices()?.getCount()||p.getAttribute('POSITION').getCount())/3,0),0);
 let tri=count();if(tri>145000){const ratio=142000/tri;for(const mesh of root.listMeshes())for(const primitive of mesh.listPrimitives())simplifyPrimitive(primitive,{simplifier:MeshoptSimplifier,ratio,error:.0015});await doc.transform(prune(),dedup());tri=count()}
 for(const error of [.006,.015])if(tri>150000){for(const mesh of root.listMeshes())for(const primitive of mesh.listPrimitives())simplifyPrimitive(primitive,{simplifier:MeshoptSimplifier,ratio:140000/tri,error});await doc.transform(prune(),dedup());tri=count()}
 if(tri>150000)throw Error(spec.id+' exceeds triangle budget: '+tri);
 // Ferrari's FBX source duplicates surfaces with opposite winding. They may share
 // vertices during decimation, but final shading must keep each side's normals.
 // Rebuild crease-aware normals from the delivered faces so opposite surfaces never
 // cancel each other, and hard panel edges remain hard.
 if(spec.id==='ferrari-458'){
  for(const mesh of root.listMeshes())for(const p of mesh.listPrimitives()){
   const position=p.getAttribute('POSITION'),indices=p.getIndices().getArray(),points=Array.from({length:position.getCount()},(_,i)=>new Vector3(...position.getElement(i,[]))),keys=points.map(v=>v.toArray().map(x=>x.toFixed(6)).join(',')),adjacent=new Map(),faces=[];
   for(let i=0;i<indices.length;i+=3){const ids=[indices[i],indices[i+1],indices[i+2]],normal=points[ids[1]].clone().sub(points[ids[0]]).cross(points[ids[2]].clone().sub(points[ids[0]]));faces.push({ids,normal,unit:normal.clone().normalize()});for(const id of ids){if(!adjacent.has(keys[id]))adjacent.set(keys[id],[]);adjacent.get(keys[id]).push(normal)}}
   const positions=[],normals=[],rebuilt=[];for(const face of faces)for(const id of face.ids){const normal=new Vector3();for(const neighbor of adjacent.get(keys[id]))if(neighbor.clone().normalize().dot(face.unit)>.55)normal.add(neighbor);normal.normalize();positions.push(...points[id].toArray());normals.push(...normal.toArray());rebuilt.push(rebuilt.length)}
   p.setAttribute('POSITION',doc.createAccessor().setBuffer(buffer).setType('VEC3').setArray(new Float32Array(positions)));p.setAttribute('NORMAL',doc.createAccessor().setBuffer(buffer).setType('VEC3').setArray(new Float32Array(normals)));p.setIndices(doc.createAccessor().setBuffer(buffer).setType('SCALAR').setArray(new Uint32Array(rebuilt)));
  }
  await doc.transform(weld(),prune(),dedup());
 }

 await doc.transform(textureCompress({encoder:sharp,resize:[1024,1024],targetFormat:'webp',quality:85,effort:35}));
 const source=spec.id==='ferrari-458'?'https://sketchfab.com/models/'+spec.uid:entry.credit.sourceUrl;
 const license=spec.license||'CC BY 4.0',licenseUrl=spec.license?'https://creativecommons.org/licenses/by-sa/4.0/':'https://creativecommons.org/licenses/by/4.0/';
 root.getAsset().copyright=spec.author+' — '+license+' — '+source;
 await doc.transform(draco({method:'edgebreaker',encodeSpeed:5,decodeSpeed:5}));
 await io.write(out+'/'+spec.id+'.glb',doc);
 // Draco can discard degenerate triangles; record the actual decoded delivery count.
 const delivered=await io.read(out+'/'+spec.id+'.glb');tri=delivered.getRoot().listMeshes().reduce((sum,m)=>sum+m.listPrimitives().reduce((s,p)=>s+(p.getIndices()?.getCount()||p.getAttribute('POSITION').getCount())/3,0),0);
 const size=pointsBounds(records.flatMap(r=>r.positions)).getSize(new Vector3());
 const m={id:spec.id,name:spec.name,file:'/models/fleet/'+spec.id+'.glb',centers:wheels.map(w=>w.center.toArray().map(x=>+x.toFixed(5))),radius:+(wheels.reduce((sum,w)=>sum+pointsBounds(w.points).getSize(new Vector3()).y/2,0)/4).toFixed(5),size:[+(size.x*.9).toFixed(4),.58,+(size.z*.9).toFixed(4)],license,licenseUrl,source,author:spec.author,triangleCount:tri,bytes:fs.statSync(out+'/'+spec.id+'.glb').size,hasInterior:spec.hasInterior,cockpitEye:spec.eye,paintMaterial:spec.paint.source,changes:'Separated and straightened wheels; normalized scale and pivots; removed display ground; optimized geometry and textures; adjusted paint and glazing.'};manifest.push(m);fs.writeFileSync(out+'/manifest.json',JSON.stringify(manifest,null,2)+'\n');console.log(spec.id,tri,'triangles',m.bytes,'bytes',m.centers,'wheel yaw correction',wheels.map(w=>+(w.correct*180/Math.PI).toFixed(2)));
}
fs.writeFileSync(out+'/LICENSES.md','# Fleet model attribution\n\nThese model assets are independently licensed from the MIT game code. Brand names identify the depicted vehicles; no manufacturer endorsement is implied.\n\n'+manifest.map(m=>`## ${m.name}\n\n- Author: ${m.author}\n- Source: ${m.source}\n- License: [${m.license}](${m.licenseUrl})\n- File: [${m.id}.glb](./${m.id}.glb)\n- Changes: ${m.changes}\n${m.id==='ferrari-458'?'- Distributed by the official [Three.js examples](https://github.com/mrdoob/three.js/blob/dev/examples/webgl_materials_car.html); [license record](https://github.com/mrdoob/three.js/issues/23089).\n':'- Source distribution: [carros library](https://github.com/pedrosoandre/carros). Source license was verified using the public Sketchfab model API on 2026-10-18.\n'}${m.license.includes('SA')?'- This modified GLB is distributed under CC BY-SA 4.0.\n':''}`).join('\n'));
