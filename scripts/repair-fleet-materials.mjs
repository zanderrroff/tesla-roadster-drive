import fs from 'node:fs/promises';import crypto from 'node:crypto';
import {NodeIO} from '@gltf-transform/core';import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
import {repairMaterials} from './lib/fleet-materials.mjs';
const file='unity/fleet-expansion-manifest.json',manifest=JSON.parse(await fs.readFile(file)),io=new NodeIO().registerExtensions(ALL_EXTENSIONS);
for(const item of manifest.vehicles){
 if(item.materialRepair===1)continue;
 const s=item.spec;
 for(const url of new Set([s.modelUrl,s.highModelUrl])){
  const path='unity/artifacts/expanded-content/'+url.split('/').at(-1),doc=await io.read(path);
  if(!repairMaterials(doc,s.id))continue;
  await io.write(path,doc);const bytes=await fs.readFile(path),hash=crypto.createHash('sha256').update(bytes).digest('hex');
  if(url===s.modelUrl){s.modelSha256=hash;s.downloadBytes=bytes.length;}if(url===s.highModelUrl){s.highModelSha256=hash;s.highDownloadBytes=bytes.length;}
  console.log('Material repair',s.id,path);
 }
 item.materialRepair=1;
 await fs.writeFile(file,JSON.stringify(manifest,null,2)+'\n');
}
