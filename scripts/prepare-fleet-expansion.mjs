// Quality-gated conversion of attributed upstream passenger cars. The raw GLBs
// remain in the download cache; reproducible derivatives are distributed as
// optional content so a hundred cabins never occupy RAM at the same time.
import fs from 'node:fs/promises';import crypto from 'node:crypto';import sharp from 'sharp';
import {NodeIO} from '@gltf-transform/core';import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
import {dequantize,prune,dedup,weld,simplify,joinPrimitives,textureCompress} from '@gltf-transform/functions';
import {MeshoptDecoder,MeshoptSimplifier} from 'meshoptimizer';import draco from 'draco3dgltf';
import {Vector3,Matrix3,Matrix4,Quaternion} from 'three';
import {pointsBounds,clusters,getRecords,connectedParts} from './lib/fleet-geometry.mjs';
import {repairMaterials} from './lib/fleet-materials.mjs';
const root='unity/artifacts/expanded-content';await fs.mkdir(root,{recursive:true});
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({'meshopt.decoder':MeshoptDecoder,'draco3d.decoder':await draco.createDecoderModule()});await MeshoptSimplifier.ready;
const inputs=JSON.parse(await fs.readFile('.asset-work/expanded-candidates.json')).filter(x=>x.accepted);
const existing=JSON.parse(await fs.readFile('unity/MotorCoast/Assets/Resources/Data/vehicles.json')).vehicles.filter(c=>c.resource);
const retry=process.argv.includes('--retry');let previous=retry?JSON.parse(await fs.readFile('unity/fleet-expansion-manifest.json')):null;
const seenSources=new Set();const results=(previous?previous.vehicles:[]).filter(v=>{const id=v.source.match(/[a-f0-9]{32}$/)?.[0];if(v.highTriangles<8000||seenSources.has(id))return false;seenSources.add(id);return true}),rejected=[],wheelPattern=/tyre|tire|reifen|pneu|rim|felge|wheel|brakedisc|brake_disc/i,steerPattern=/steer|lenkrad|volante|driving_wheel/i;
const v3=a=>({x:-a.x,y:a.y,z:a.z});
// Verified from front/rear renders; some source groups called "front" contain
// both bumpers, so their names cannot establish the longitudinal direction.
const reverseSource=new Set(['chevrolet-suburban','nissan-silvia-s15','vw-voyage','alfa-romeo-4c','bmw-e39','land-rover-range-rover','porsche-718']);
for(const item of inputs){
 if(results.length>=100-existing.length)break;
 const car=item.car;const sourceId=car.credit.sourceUrl.match(/[a-f0-9]{32}$/)?.[0];if(seenSources.has(sourceId))continue;if(existing.some(c=>c.id===car.id)||results.some(c=>c.spec.id===car.id))continue;
 try{
  const doc=await io.read('.asset-work/expanded-cars/'+car.id+'.glb');await doc.transform(dequantize());const r=doc.getRoot();
  let records=getRecords(doc,{});if(records.some(x=>x.positions.length>1500000))throw Error('Mesh exceeds preparation budget');
  let tires=records.filter(x=>/tyre|tire|reifen|pneu/i.test(x.name+' '+x.material?.getName())&&!steerPattern.test(x.name));
  const namedWheels=records.filter(x=>wheelPattern.test(x.name+' '+x.parent+' '+x.material?.getName())&&!/steer|arch|pedal|driving_wheel|interior/i.test(x.name+' '+x.parent));
  const viable=group=>{if(!group.length)return false;const dims=pointsBounds(group.flatMap(x=>x.positions)).getSize(new Vector3()).toArray().sort((a,b)=>a-b);return dims[2]/dims[1]>1.3&&dims[1]/Math.max(dims[0],.0001)>1.5};
  if(!viable(tires)&&viable(namedWheels))tires=namedWheels;
  if(!tires.length)throw Error('Tyres have no reliable source selection');
  let tb=pointsBounds(tires.flatMap(x=>x.positions)),size=tb.getSize(new Vector3());
  if(size.y>Math.min(size.x,size.z)){
   const upAxis=size.x<size.z?'x':'z',body=pointsBounds(records.flatMap(x=>x.positions)).getCenter(new Vector3());
   const up=new Vector3();up[upAxis]=body[upAxis]>=tb.getCenter(new Vector3())[upAxis]?1:-1;
   const q=new Quaternion().setFromUnitVectors(up,new Vector3(0,1,0));
   for(const rec of records){rec.positions.forEach(p=>p.applyQuaternion(q));rec.normals.forEach(p=>p.applyQuaternion(q));rec.tangents=rec.tangents.map(t=>[...new Vector3(...t.slice(0,3)).applyQuaternion(q).toArray(),t[3]])}
   tb=pointsBounds(tires.flatMap(x=>x.positions));size=tb.getSize(new Vector3());
  }
  let angle=size.x>size.z?Math.PI/2:0;
  function rotate(a){const rot=new Matrix4().makeRotationY(a),n=new Matrix3().setFromMatrix4(rot);for(const rec of records){rec.positions.forEach(p=>p.applyMatrix4(rot));rec.normals.forEach(p=>p.applyMatrix3(n));rec.tangents=rec.tangents.map(t=>[...new Vector3(...t.slice(0,3)).applyMatrix3(n).toArray(),t[3]])}}
  rotate(angle);tb=pointsBounds(tires.flatMap(x=>x.positions));
  const front=records.filter(x=>/front|bonnet|headlight|(?:^|[_ -])(?:FL|FR|LF|RF)(?:[_ -]|$)/i.test(x.name+' '+x.parent)&&!steerPattern.test(x.name));
  if(front.length){let z=front.reduce((s,x)=>s+pointsBounds(x.positions).getCenter(new Vector3()).z,0)/front.length;if(z<tb.getCenter(new Vector3()).z)rotate(Math.PI);}
  if(reverseSource.has(car.id))rotate(Math.PI);
  let wheels=clusters(tires.flatMap(x=>x.positions));let radius=wheels.reduce((s,w)=>s+pointsBounds(w.points).getSize(new Vector3()).y*.5,0)/4;
  let factor=radius>.24&&radius<.49?1:(car.category==='suv'||car.category==='pickup'?.38:.33)/radius;
  let centre=pointsBounds(wheels.map(w=>w.center)).getCenter(new Vector3()),ground=Math.min(...wheels.map(w=>pointsBounds(w.points).min.y));
  const translation=new Vector3(-centre.x*factor,-.58-ground*factor,-centre.z*factor);
  for(const rec of records)rec.positions.forEach(p=>p.multiplyScalar(factor).add(translation));
  wheels=clusters(tires.flatMap(x=>x.positions));radius*=factor;
  const axle=pointsBounds(wheels.map(w=>w.center)).getSize(new Vector3());
  if(axle.x<1.1||axle.x>2.3||axle.z<1.8||axle.z>4.1||axle.y>.25||radius<.25||radius>.47)throw Error('Implausible wheel geometry '+JSON.stringify(axle));
  records=records.filter(rec=>{const b=pointsBounds(rec.positions),s=b.getSize(new Vector3());return !(s.y<.04&&(s.x>axle.x*1.5||s.z>axle.z*1.7));});
  let bodyBounds=pointsBounds(records.flatMap(x=>x.positions));size=bodyBounds.getSize(new Vector3());
  if(size.x>2.6||size.z>7||size.y>3||size.y<.85)throw Error('Body includes unsupported scenery or dimensions');
  const steering=[];
  function eligible(b){const s=b.getSize(new Vector3()),c=b.getCenter(new Vector3());return s.x<.65&&s.y<.65&&s.z<.8&&Math.abs(c.x)<.85&&Math.abs(c.z)<1.4&&c.y>-.25;}
  for(const rec of records){
   if(!steerPattern.test(rec.name+' '+rec.parent+' '+rec.material?.getName())||/steeringbox|column|pedal/i.test(rec.name))continue;
   const bounds=pointsBounds(rec.positions);
   if(eligible(bounds))steering.push({rec,bounds,positions:rec.positions,indices:rec.indices});
   else for(const part of connectedParts(rec))if(eligible(part.bounds))steering.push({rec,bounds:part.bounds,positions:[...new Set(part.indices)].map(i=>rec.positions[i]),indices:part.indices});
  }
  let rims=steering.filter(rec=>{const s=rec.bounds.getSize(new Vector3());return s.x>.25&&s.x<.58&&s.y>.17;});
  if(!rims.length){
   for(const rec of records){
    const bound=pointsBounds(rec.positions);if(bound.max.y<-.1||bound.min.y>bodyBounds.max.y-.25||bound.max.z<-.25||bound.min.z>.9)continue;
    if(wheelPattern.test(rec.name)&&!steerPattern.test(rec.name))continue;
    for(const part of connectedParts(rec)){
     const b=part.bounds,s=b.getSize(new Vector3()),c=b.getCenter(new Vector3());
     if(s.x<.27||s.x>.54||s.y<.17||s.y>.52||s.z>.40||Math.abs(c.x)<.18||Math.abs(c.x)>.68||c.z<-.65||c.z>1||c.y<-.2||c.y>bodyBounds.max.y-.22)continue;
     const positions=[...new Set(part.indices)].map(i=>rec.positions[i]);if(positions.length<100)continue;
     let yy=0,yz=0;for(const p of positions){yy+=(p.y-c.y)**2;yz+=(p.y-c.y)*(p.z-c.z);}const slope=yz/Math.max(yy,1e-6);
     let residual=0,outer=0,corners=0;for(const p of positions){residual+=(p.z-c.z-slope*(p.y-c.y))**2;const radial=Math.sqrt((p.x-c.x)**2+(1+slope*slope)*(p.y-c.y)**2);if(radial>s.x*.36)outer++;if(radial>s.x*.565)corners++;}
     const aspect=s.y*Math.sqrt(1+slope*slope)/s.x;
     if(Math.sqrt(residual/positions.length)>.045||outer/positions.length<.25||corners/positions.length>.02||aspect<.78||aspect>1.25)continue;
     const candidate={rec,bounds:b,positions,indices:part.indices};steering.push(candidate);rims.push(candidate);
    }
   }
  }
  if(!rims.length)throw Error('Steering rim cannot be separated safely');
  const rim=rims.sort((a,b)=>b.positions.length-a.positions.length)[0],sb=pointsBounds(rim.positions),wheelCentre=sb.getCenter(new Vector3()),rimRadius=sb.getSize(new Vector3()).x/2;
  let yy=0,yz=0;for(const p of rim.positions){yy+=(p.y-wheelCentre.y)**2;yz+=(p.y-wheelCentre.y)*(p.z-wheelCentre.z);}
  const axis=new Vector3(0,-yz/Math.max(yy,.0001),1).normalize(),rotation=new Quaternion().setFromUnitVectors(new Vector3(0,0,1),axis),inverse=rotation.clone().invert();
  const steeringSet=new Map();for(const x of steering.filter(x=>x.bounds.getCenter(new Vector3()).distanceTo(wheelCentre)<.4)){if(!steeringSet.has(x.rec))steeringSet.set(x.rec,new Set());for(const i of x.indices)steeringSet.get(x.rec).add(i);}
  const doors=records.filter(rec=>{
   if(!/door|porte|tuer/i.test(rec.name+' '+rec.parent))return false;
   const b=pointsBounds(rec.positions),c=b.getCenter(new Vector3());return c.x*wheelCentre.x>0&&Math.abs(c.x)>.4&&Math.abs(c.z-(wheelCentre.z-.35))<.6&&b.getSize(new Vector3()).x<size.x*.55;
  });
  const doorSet=new Set(doors),doorBounds=doors.length?pointsBounds(doors.flatMap(x=>x.positions)):null;
  const hinge=doorBounds?new Vector3(wheelCentre.x>0?doorBounds.max.x:doorBounds.min.x,wheelCentre.y-.1,doorBounds.max.z):new Vector3();
  const buffer=r.listBuffers()[0]||doc.createBuffer();const groups=[doc.createNode('Chassis'),...wheels.map((w,i)=>doc.createNode('Wheel_'+i)),doc.createNode('SteeringWheel').setTranslation(wheelCentre.toArray()).setRotation(rotation.toArray()),doc.createNode('Door_Driver').setTranslation(hinge.toArray())];
  const buckets=groups.map(()=>new Map());
  for(const rec of records){
   const name=rec.name+' '+rec.parent+' '+rec.material?.getName();
   const wheel=wheelPattern.test(name)&&!steerPattern.test(name)&&!/arch|pedal|interior|steer|rubberstrip/i.test(name);
   const pieces=new Map();
   for(let i=0;i<rec.indices.length;i+=3){
    let k=steeringSet.get(rec)?.has(rec.indices[i])?5:doorSet.has(rec)?6:0;
    if(k===0&&wheel){const p=rec.positions[rec.indices[i]].clone().add(rec.positions[rec.indices[i+1]]).add(rec.positions[rec.indices[i+2]]).multiplyScalar(1/3);let nearest=0,d=Infinity;wheels.forEach((w,j)=>{const distance=w.center.distanceToSquared(p);if(distance<d){d=distance;nearest=j}});if(d<(radius*1.5)**2)k=nearest+1;}
    if(!pieces.has(k))pieces.set(k,[]);pieces.get(k).push(...rec.indices.slice(i,i+3));
   }
   for(const [group,sourceIndices] of pieces){
    const remap=new Map(),indices=[],attributes={};const semantics=rec.primitive.listSemantics().filter(x=>!x.startsWith('JOINTS')&&!x.startsWith('WEIGHTS'));
    for(const semantic of semantics)attributes[semantic]=[];
    for(const index of sourceIndices){if(!remap.has(index)){remap.set(index,remap.size);for(const semantic of semantics){let value;
     if(semantic==='POSITION'){const p=rec.positions[index].clone();if(group===5)p.sub(wheelCentre).applyQuaternion(inverse);else if(group===6)p.sub(hinge);else if(group>0)p.sub(wheels[group-1].center);value=p.toArray();}
     else if(semantic==='NORMAL'){const p=rec.normals[index].clone();if(group===5)p.applyQuaternion(inverse);value=p.toArray();}
     else if(semantic==='TANGENT'){const t=rec.tangents[index],p=new Vector3(...t.slice(0,3));if(group===5)p.applyQuaternion(inverse);value=[...p.toArray(),t[3]];}
     else value=rec.primitive.getAttribute(semantic).getElement(index,[]);attributes[semantic].push(...value);
    }}indices.push(remap.get(index));}
    const p=doc.createPrimitive().setMaterial(rec.material).setIndices(doc.createAccessor().setBuffer(buffer).setType('SCALAR').setArray(new Uint32Array(indices)));
    for(const semantic of semantics)p.setAttribute(semantic,doc.createAccessor().setBuffer(buffer).setType(rec.primitive.getAttribute(semantic).getType()).setArray(new Float32Array(attributes[semantic])));
    const key=r.listMaterials().indexOf(rec.material)+'|'+semantics.sort().join(',');if(!buckets[group].has(key))buckets[group].set(key,[]);buckets[group].get(key).push(p);
   }
  }
  for(let k=1;k<5;k++)if(!buckets[k].size)throw Error('Missing moving wheel '+k);
  for(let k=0;k<groups.length;k++)for(const primitives of buckets[k].values()){
   const p=primitives.length>1?joinPrimitives(primitives):primitives[0];if(primitives.length>1)primitives.forEach(x=>x.dispose());groups[k].addChild(doc.createNode('Part_'+k+'_'+p.getMaterial().getName()).setMesh(doc.createMesh().addPrimitive(p)));
  }
  for(const s of r.listScenes())for(const node of [...s.listChildren()])s.removeChild(node);
  const scene=r.listScenes()[0];groups.slice(0,5).forEach(g=>scene.addChild(g));groups[0].addChild(groups[5]);if(doors.length)groups[0].addChild(groups[6]);
  r.listAnimations().forEach(a=>a.dispose());
  for(const ext of [...r.listExtensionsUsed()])if(['EXT_meshopt_compression','KHR_mesh_quantization','KHR_draco_mesh_compression'].includes(ext.extensionName))ext.dispose();
  for(const m of r.listMaterials()){
   if(/glass|window|windshield|windscreen/i.test(m.getName()))m.setAlphaMode('BLEND').setBaseColorFactor([.16,.22,.24,.23]).setMetallicFactor(.06).setRoughnessFactor(.13);
   if(m.getExtension('KHR_materials_transmission'))m.setExtension('KHR_materials_transmission',null);
  }
  repairMaterials(doc,car.id);
  await doc.transform(prune(),dedup(),weld());
  // PNG is used where alpha is needed and for linear normal/metallic maps.
  for(const texture of r.listTextures()){const png=await sharp(Buffer.from(texture.getImage())).resize({width:2048,height:2048,fit:'inside',withoutEnlargement:true}).png().toBuffer();texture.setImage(png).setMimeType('image/png');}
  let triangles=()=>r.listMeshes().reduce((n,m)=>n+m.listPrimitives().reduce((s,p)=>s+p.getIndices().getCount()/3,0),0);
  if(triangles()>350000)await doc.transform(simplify({simplifier:MeshoptSimplifier,ratio:340000/triangles(),error:.005,lockBorder:true}),prune());
  if(triangles()<8000)throw Error('Insufficient exterior/cabin detail for this release');
  const credit=car.credit;doc.getRoot().getAsset().copyright=credit.author+' — '+credit.license+' — '+credit.sourceUrl;
  const hi=root+'/'+car.id+'-ultra.glb';await io.write(hi,doc);const highTriangles=triangles();
  if(triangles()>145000)await doc.transform(simplify({simplifier:MeshoptSimplifier,ratio:140000/triangles(),error:.012,lockBorder:true}),prune());
  if(triangles()>150000)await doc.transform(simplify({simplifier:MeshoptSimplifier,ratio:130000/triangles(),error:.025,lockBorder:false}),prune());
  if(triangles()>150000)throw Error('Standard triangle budget exceeded');
  const low=root+'/'+car.id+'.glb';await io.write(low,doc);
  const high=await fs.readFile(hi),standard=await fs.readFile(low);const floor=MathfFloor();
  function MathfFloor(){return Math.max(-.43,wheelCentre.y-.58)}
  const roof=bodyBounds.max.y-.035,seatY=wheelCentre.y-.33,scale=Math.min(1.04,Math.max(.72,(roof-seatY-.04)/.85));
  const spec={id:car.id,label:car.brand+' '+car.model,resource:'',highResource:'',electric:car.brand==='Tesla'||/e-tron/i.test(car.id),hasInterior:true,fitLegs:true,power:car.category==='sports'?1.2:car.category==='suv'?.95:1,mass:car.category==='suv'?1900:1450,wheelRadius:radius,wheelCenters:wheels.map(w=>v3(w.center)),colliderSize:{x:Math.min(2.25,size.x*.94),y:.46,z:size.z*.94},floorY:floor,roofY:roof,driverPosition:v3(new Vector3(wheelCentre.x,seatY,wheelCentre.z-.43*scale)),driverScale:scale,torsoLean:0,pedalZ:wheelCentre.z+.23,wheelPosition:v3(wheelCentre),wheelTilt:Math.atan2(-axis.y,axis.z)*180/Math.PI,steeringRadius:rimRadius,shellOffset:{x:0,y:0,z:0},modelUrl:'https://github.com/zanderrroff/tesla-roadster-drive/releases/download/motorcoast-content-v2.1/'+car.id+'.glb',highModelUrl:'https://github.com/zanderrroff/tesla-roadster-drive/releases/download/motorcoast-content-v2.1/'+car.id+'-ultra.glb',modelSha256:crypto.createHash('sha256').update(standard).digest('hex'),highModelSha256:crypto.createHash('sha256').update(high).digest('hex'),downloadBytes:standard.length,highDownloadBytes:high.length};
  if(high.equals(standard)){spec.highModelUrl=spec.modelUrl;spec.highModelSha256=spec.modelSha256;spec.highDownloadBytes=spec.downloadBytes;await fs.unlink(hi);}
  seenSources.add(sourceId);results.push({spec,credit,sourceLicense:item.sourceLicense,source:credit.sourceUrl,standardTriangles:triangles(),highTriangles,doors:doors.length>0,changes:'Metric conversion, separate rotating wheels and steering; original cabin retained; optional driver door; texture conversion and mobile geometry reduction.'});console.log('READY',car.id,triangles(),highTriangles,doors.length?'door':'no door',standard.length,JSON.stringify(spec.driverPosition));
 }catch(error){rejected.push({id:car.id,error:error.message});console.log('REJECT',car.id,error.message);}
 await fs.writeFile('unity/fleet-expansion-manifest.json',JSON.stringify({vehicles:results,rejected},null,2)+'\n');
}
console.log('Prepared',results.length,'new cars;',rejected.length,'rejected');
