// LOLIPOP's original game-ready pine pack, CC BY 4.0, distributed by Vestige.
import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import sharp from 'sharp';
import {NodeIO,getBounds} from '@gltf-transform/core';import {ALL_EXTENSIONS} from '@gltf-transform/extensions';import {prune} from '@gltf-transform/functions';
const source='.asset-work/pine-ready',output='unity/MotorCoast/Assets/Resources/Scenery';await fs.mkdir(source,{recursive:true});await fs.mkdir(output,{recursive:true});
const revision=JSON.parse(await fs.readFile('.asset-work/vestige-tree.json')).sha;
const base='https://raw.githubusercontent.com/milnet01/Vestige/'+revision+'/assets/models/nature/gameready/pine/';
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS),manifest=[];
async function download(name){const file=source+'/'+name;try{return await fs.readFile(file)}catch{}const r=await fetch(base+name);if(!r.ok)throw Error(r.status+' '+name);const b=Buffer.from(await r.arrayBuffer());await fs.writeFile(file,b);return b;}
for(const variation of ['medium_1','large_1'])for(const lod of ['lod0','lod2']){
 const name='pine_pine_'+variation+'_'+lod+'.gltf',json=JSON.parse(await download(name));
 await Promise.all([...json.buffers,...json.images].map(f=>download(f.uri)));
 const doc=await io.read(source+'/'+name),root=doc.getRoot();
 const bounds=getBounds(root.listScenes()[0]);const holder=doc.createNode('Pine tree').setTranslation([-(bounds.min[0]+bounds.max[0])/2,-bounds.min[1],-(bounds.min[2]+bounds.max[2])/2]);
 for(const node of [...root.listScenes()[0].listChildren()]){root.listScenes()[0].removeChild(node);holder.addChild(node)}root.listScenes()[0].addChild(holder);
 for(const texture of root.listTextures()){
  const role=/normal/i.test(texture.getName())?'normal':/metallic/i.test(texture.getName())?'linear':'color';
  const uri='pine-'+texture.getName().replace(/[^A-Za-z0-9_-]/g,'_')+'-'+role+'.png';
  texture.setImage(await sharp(Buffer.from(texture.getImage())).resize({width:2048,height:2048,fit:'inside',withoutEnlargement:true}).png().toBuffer()).setMimeType('image/png').setURI(uri);
 }
 await doc.transform(prune());const file='pine-'+variation+'-'+lod;root.listBuffers().forEach((b,i)=>b.setURI(file+'-'+i+'.bin'));
 await io.write(output+'/'+file+'.gltf',doc);
 const delivered=JSON.parse(await fs.readFile(output+'/'+file+'.gltf'));const files=[];
 for(const n of [file+'.gltf',...delivered.buffers.map(x=>x.uri),...delivered.images.map(x=>x.uri)]){const data=await fs.readFile(output+'/'+n);files.push({file:'Scenery/'+n,bytes:data.length,sha256:crypto.createHash('sha256').update(data).digest('hex')})}
 const triangles=root.listMeshes().reduce((s,m)=>s+m.listPrimitives().reduce((n,p)=>n+p.getIndices().getCount()/3,0),0);
 manifest.push({file:'Scenery/'+file+'.gltf',source:'https://sketchfab.com/3d-models/e1e9c07b8e2e445c943fec660beefba2',distribution:base+name,author:'LOLIPOP',license:'CC BY 4.0',triangles,files});console.log(file,triangles,'triangles',bounds);
}
await fs.writeFile('unity/pine-manifest.json',JSON.stringify(manifest,null,2)+'\n');
const credit='\n3D pine tree models by LOLIPOP (https://sketchfab.com/lolipop_1707), CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/). Source: https://sketchfab.com/3d-models/e1e9c07b8e2e445c943fec660beefba2. Distributed by https://github.com/milnet01/Vestige; original LOD0 and LOD2 retained, pivots normalized, textures resized to 2K.\n';
const cp='unity/MotorCoast/Assets/Resources/Data/CREDITS.txt';if(!(await fs.readFile(cp,'utf8')).includes('3D pine tree models by LOLIPOP'))await fs.appendFile(cp,credit);
