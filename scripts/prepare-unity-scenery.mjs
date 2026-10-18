// Poly Haven CC0 photogrammetry, reduced for a distant mobile coastline.
import fs from 'node:fs/promises'
import path from 'node:path'
import crypto from 'node:crypto'
import sharp from 'sharp'
import {NodeIO,getBounds} from '@gltf-transform/core'
import {weld,simplify,prune} from '@gltf-transform/functions'
import {MeshoptSimplifier} from 'meshoptimizer'
import {BufferGeometry,BufferAttribute} from 'three'
const source='.asset-work/coastal-cliff',output='unity/MotorCoast/Assets/Resources/Scenery'
const headers={'User-Agent':'MotorCoast/1.0 (Unity asset pipeline)'}
await fs.mkdir(source,{recursive:true});await fs.mkdir(output,{recursive:true})
const files=await (await fetch('https://api.polyhaven.com/files/coastal_cliff_01',{headers})).json()
const asset=files.gltf['2k'].gltf
for(const [name,file] of Object.entries({'coastal-cliff.gltf':asset,...asset.include})){
 const target=path.join(source,name);await fs.mkdir(path.dirname(target),{recursive:true})
 let bytes;try{bytes=await fs.readFile(target)}catch{}
 if(!bytes||crypto.createHash('md5').update(bytes).digest('hex')!==file.md5){const response=await fetch(file.url,{headers});if(!response.ok)throw Error(response.status+' '+file.url);bytes=Buffer.from(await response.arrayBuffer());await fs.writeFile(target,bytes)}
 if(crypto.createHash('md5').update(bytes).digest('hex')!==file.md5)throw Error('Asset checksum mismatch: '+name)
}
const io=new NodeIO(),doc=await io.read(source+'/coastal-cliff.gltf'),root=doc.getRoot()
let before=0;for(const m of root.listMeshes())for(const p of m.listPrimitives())before+=p.getIndices().getCount()/3
await MeshoptSimplifier.ready
await doc.transform(weld(),simplify({simplifier:MeshoptSimplifier,ratio:12000/before,error:.025,lockBorder:true}),prune())
const bounds=getBounds(root.listScenes()[0]),center=[(bounds.min[0]+bounds.max[0])/2,bounds.min[1],(bounds.min[2]+bounds.max[2])/2]
const parent=doc.createNode('CoastalCliff').setTranslation(center.map(x=>-x))
for(const node of [...root.listScenes()[0].listChildren()]){root.listScenes()[0].removeChild(node);parent.addChild(node)}root.listScenes()[0].addChild(parent)
const roles=new Map();for(const m of root.listMaterials()){if(m.getBaseColorTexture())roles.set(m.getBaseColorTexture(),'color');if(m.getNormalTexture())roles.set(m.getNormalTexture(),'normal')}
let n=0;for(const t of root.listTextures())t.setImage(await sharp(Buffer.from(t.getImage())).png().toBuffer()).setMimeType('image/png').setURI('coastal-cliff-'+(roles.get(t)||'linear')+'-'+(n++)+'.png')
for(const m of root.listMeshes())for(const p of m.listPrimitives()){
 const g=new BufferGeometry();for(const [name,key,size] of [['position','POSITION',3],['normal','NORMAL',3],['uv','TEXCOORD_0',2]])g.setAttribute(name,new BufferAttribute(p.getAttribute(key).getArray(),size));g.setIndex(new BufferAttribute(p.getIndices().getArray(),1));g.computeTangents();
 p.setAttribute('TANGENT',doc.createAccessor('Coastal tangent frame').setType('VEC4').setArray(new Float32Array(g.attributes.tangent.array)).setBuffer(p.getAttribute('POSITION').getBuffer()));g.dispose()
}
root.listBuffers().forEach((b,i)=>b.setURI('coastal-cliff-'+i+'.bin'))
await io.write(output+'/coastal-cliff.gltf',doc)
const json=JSON.parse(await fs.readFile(output+'/coastal-cliff.gltf')),entries=[]
for(const name of ['coastal-cliff.gltf',...json.buffers.map(b=>b.uri),...json.images.map(i=>i.uri)]){const b=await fs.readFile(output+'/'+name);entries.push({file:'Scenery/'+name,bytes:b.length,sha256:crypto.createHash('sha256').update(b).digest('hex')})}
let triangles=0;for(const m of root.listMeshes())for(const p of m.listPrimitives())triangles+=p.getIndices().getCount()/3
await fs.writeFile('unity/scenery-manifest.json',JSON.stringify([{source:'https://polyhaven.com/a/coastal_cliff_01',authors:'Rico Cilliers, Rob Tuytel',license:'CC0',file:'Scenery/coastal-cliff.gltf',triangles,originalTriangles:before,files:entries}],null,2)+'\n')
const credit='Coastal Cliff 01 — Rico Cilliers, Rob Tuytel / Poly Haven, CC0. https://polyhaven.com/a/coastal_cliff_01; mesh simplified for mobile, 2K textures.\n'
const creditFile='unity/MotorCoast/Assets/Resources/Data/CREDITS.txt';if(!(await fs.readFile(creditFile,'utf8')).includes('Coastal Cliff 01'))await fs.appendFile(creditFile,'\n'+credit)
console.log(JSON.stringify({sourceTriangles:before,mobileTriangles:triangles,bounds,bytes:entries.reduce((n,f)=>n+f.bytes,0)}))
