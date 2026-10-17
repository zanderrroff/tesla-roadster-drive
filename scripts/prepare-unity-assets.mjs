/** Assets are imported offline by glTFast; no network/decompression during play. */
import fs from 'node:fs/promises'
import path from 'node:path'
import crypto from 'node:crypto'
import sharp from 'sharp'
import {NodeIO} from '@gltf-transform/core'
import {prune} from '@gltf-transform/functions'
import {BufferGeometry,BufferAttribute,Vector3} from 'three'
import {ALL_EXTENSIONS} from '@gltf-transform/extensions'
import draco3d from 'draco3dgltf'
import {MeshoptDecoder} from 'meshoptimizer'
const dest='unity/MotorCoast/Assets/Resources'
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({'draco3d.decoder':await draco3d.createDecoderModule(),'meshopt.decoder':MeshoptDecoder})
const fleet=JSON.parse(await fs.readFile('public/models/fleet/manifest.json'))
const layouts=JSON.parse(await fs.readFile('src/cabin-layouts.json'))
const tesla=JSON.parse(await fs.readFile('src/vehicle-dimensions.json'))
const manifest=[]
await fs.mkdir(dest+'/Vehicles',{recursive:true})
async function convert(source,name){
 const doc=await io.read(source),root=doc.getRoot()
 const roles=new Map();
 for(const m of root.listMaterials()){if(m.getBaseColorTexture())roles.set(m.getBaseColorTexture(),'color');if(m.getEmissiveTexture())roles.set(m.getEmissiveTexture(),'color');if(m.getNormalTexture())roles.set(m.getNormalTexture(),'normal');}
 let ti=0;const base=path.basename(name);
 for(const texture of root.listTextures()){
  const buffer=await sharp(Buffer.from(texture.getImage())).png().toBuffer()
  texture.setImage(buffer).setMimeType('image/png').setURI(`${base}-${roles.get(texture)||'linear'}-${ti++}.png`)
 }
 // Split materials into renderers so cockpit mode hides the head only, and glass
 // never disables the shadow of the opaque body sharing its original mesh.
 for(const node of [...root.listNodes()]) {
  const mesh=node.getMesh();if(!mesh||mesh.listPrimitives().length<2)continue
  mesh.listPrimitives().forEach((p,index)=>{
   const part=doc.createNode(node.getName()+'_part_'+index).setMesh(doc.createMesh(mesh.getName()+'_part_'+index).addPrimitive(p.clone()))
   if(node.getSkin())part.setSkin(node.getSkin())
   node.addChild(part)
  })
  node.setMesh(null);node.setSkin(null)
 }
 // Store tangent frames for normal mapped meshes instead of leaving them to platform importers.
 for(const mesh of root.listMeshes())for(const p of mesh.listPrimitives()) {
  if(p.getAttribute('TANGENT')||!p.getMaterial()?.getNormalTexture())continue
  const position=p.getAttribute('POSITION'),normal=p.getAttribute('NORMAL'),uv=p.getAttribute('TEXCOORD_0'),indices=p.getIndices()
  if(!normal||!uv||!indices)throw Error('Normal mapped primitive lacks UV/normal/index data')
  const g=new BufferGeometry();g.setAttribute('position',new BufferAttribute(position.getArray(),3));g.setAttribute('normal',new BufferAttribute(normal.getArray(),3));g.setAttribute('uv',new BufferAttribute(uv.getArray(),2));g.setIndex(new BufferAttribute(indices.getArray(),1));g.computeTangents()
  const tangent=g.attributes.tangent.array,n=new Vector3(),t=new Vector3()
  for(let i=0;i<position.getCount();i++){n.fromArray(normal.getArray(),i*3);t.fromArray(tangent,i*4);if(t.lengthSq()<1e-10){t.crossVectors(n,Math.abs(n.y)<.8?new Vector3(0,1,0):new Vector3(1,0,0)).normalize();tangent.set([t.x,t.y,t.z,1],i*4)}}
  p.setAttribute('TANGENT',doc.createAccessor('Offline tangent frame').setType('VEC4').setArray(new Float32Array(tangent)).setBuffer(position.getBuffer()));g.dispose()
 }
 for(const m of root.listMaterials()) {const e=m.getExtension('KHR_materials_emissive_strength');if(e&&m.getEmissiveFactor().every(v=>v===0))m.setExtension('KHR_materials_emissive_strength',null)}
 await doc.transform(prune())
 for(const ext of root.listExtensionsUsed())if(['KHR_draco_mesh_compression','EXT_meshopt_compression','EXT_texture_webp'].includes(ext.extensionName))ext.dispose()
 for(const node of root.listNodes())node.setName(node.getName().replaceAll(' ','_'))
 let triangles=0
 for(const mesh of root.listMeshes())for(const p of mesh.listPrimitives())triangles+=(p.getIndices()?.getCount()??p.getAttribute('POSITION').getCount())/3
 root.listBuffers().forEach((b,i)=>b.setURI(`${base}-data-${i}.bin`));
 const output=dest+'/'+name+'.gltf';await fs.mkdir(path.dirname(output),{recursive:true});await io.write(output,doc)
 const json=JSON.parse(await fs.readFile(output));const relatives=[path.basename(output),...json.buffers.map(b=>b.uri),...(json.images||[]).map(i=>i.uri)];
 const files=[];for(const relative of relatives){const b=await fs.readFile(path.dirname(output)+'/'+relative);files.push({file:path.dirname(name)+'/'+relative,bytes:b.length,sha256:crypto.createHash('sha256').update(b).digest('hex')});}
 const bytes={length:files.reduce((n,f)=>n+f.bytes,0)};manifest.push({source,file:name+'.gltf',bytes:bytes.length,triangles,files});
 try{await fs.unlink(dest+'/'+name+'.glb')}catch(e){if(e.code!=='ENOENT')throw e;}
 console.log(name,triangles,'triangles',bytes.length,'bytes')
}
await convert('public/models/roadster-mobile.glb','Vehicles/roadster')
await convert('public/models/roadster-high.glb','Vehicles/roadster-high')
for(const car of fleet)await convert('public'+car.file,'Vehicles/'+car.id)
await convert('public/models/driver.glb','Driver/driver')
const vec=a=>({x:-a[0],y:a[1],z:a[2]}) // glTFast mirrors X to Unity's left-handed frame.
const native=[]
for(const car of [{id:'roadster',name:'Tesla Roadster',...tesla,size:[1.72,.58,3.85],power:1.15,electric:true},...fleet]){
 const l=layouts[car.id],wheel=l.wheel||car.steeringWheel
 native.push({id:car.id,label:car.name,resource:'Vehicles/'+car.id,highResource:car.id==='roadster'?'Vehicles/roadster-high':'Vehicles/'+car.id,electric:!!car.electric,power:car.power||1,mass:car.id==='roadster'?1500:1420,wheelCenters:car.centers.map(vec),wheelRadius:car.radius,colliderSize:{x:car.size[0]*.94,y:.46,z:car.size[2]*.95},hasInterior:!!car.hasInterior,shellOffset:vec(l.shellOffset||[0,0,0]),driverPosition:vec(l.driver.position),driverScale:l.driver.scale,torsoLean:l.driver.torsoLean,floorY:l.floorY,roofY:l.roofY,fitLegs:l.driver.pedalZ!==undefined,pedalZ:l.driver.pedalZ||0,wheelPosition:vec(wheel.center),wheelTilt:(wheel.rotation?.[0]||0)*180/Math.PI,steeringRadius:wheel.radius})
}
await fs.mkdir(dest+'/Data',{recursive:true})
await fs.writeFile(dest+'/Data/vehicles.json',JSON.stringify({vehicles:native},null,2)+'\n')
// Only these two directories are generated model outputs. Remove stale generated resources.
const expected=new Set(manifest.flatMap(m=>m.files.map(f=>f.file)))
for(const folder of ['Vehicles','Driver'])for(const entry of await fs.readdir(dest+'/'+folder)){
 const relative=folder+'/'+entry;if(!/\.(gltf|bin|png)$/.test(entry)||expected.has(relative))continue
 await fs.unlink(dest+'/'+relative);try{await fs.unlink(dest+'/'+relative+'.meta')}catch(e){if(e.code!=='ENOENT')throw e}
}
await fs.writeFile('unity/asset-manifest.json',JSON.stringify(manifest,null,2)+'\n')
await fs.mkdir(dest+'/Textures',{recursive:true})
await sharp('public/textures/asphalt/color-high.webp').png().toFile(dest+'/Textures/asphalt-color.png')
await sharp('public/textures/asphalt/normal-high.webp').png().toFile(dest+'/Textures/asphalt-normal.png')
const {data,info}=await sharp('public/textures/asphalt/arm-high.webp').removeAlpha().raw().toBuffer({resolveWithObject:true})
const metallic=Buffer.alloc(info.width*info.height*4),occlusion=Buffer.alloc(info.width*info.height)
for(let p=0;p<info.width*info.height;p++){metallic[p*4+3]=255-data[p*info.channels+1];occlusion[p]=data[p*info.channels]}
await sharp(metallic,{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(dest+'/Textures/asphalt-metal-smooth.png')
await sharp(occlusion,{raw:{width:info.width,height:info.height,channels:1}}).png().toFile(dest+'/Textures/asphalt-ao.png')
await fs.copyFile('public/textures/coast-sky-1k.hdr',dest+'/Textures/coast-sky.hdr')
await fs.mkdir(dest+'/Audio',{recursive:true})
for(const name of ['engine-loop','tire-loop','impact','ambience'])await fs.copyFile('public/sounds/v14/'+name+'.wav',dest+'/Audio/'+name+'.wav')
let credits=await fs.readFile('public/CREDITS.txt','utf8')
credits+='\nAsphalt 02 — Rob Tuytel / Poly Haven, CC0. https://polyhaven.com/a/asphalt_02\nUnity conversion: decoded meshes, PNG texture conversion, mirrored coordinates on import; steering and driver rig corrections. Porsche derivative retains CC BY-SA 4.0.\n'
await fs.writeFile(dest+'/Data/CREDITS.txt',credits)
await fs.copyFile('public/models/driver-LICENSE.txt',dest+'/Data/Driver-LICENSE.txt')
console.log('Unity assets prepared; original source and licenses preserved.')
