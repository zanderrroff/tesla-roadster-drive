// Run after door separation. Standard uses 1K textures, Ultra keeps 2K originals.
// glTFast creates runtime textures, so Unity's asset mip limit cannot save their RAM.
import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import sharp from 'sharp';
import {NodeIO} from '@gltf-transform/core';
import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
const file='unity/fleet-expansion-manifest.json';
const manifest=JSON.parse(await fs.readFile(file));
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS);
const dir='unity/artifacts/expanded-content/';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
for(const item of manifest.vehicles){
 if(item.standardTextureSize===1024)continue;
 const s=item.spec, low=dir+s.id+'.glb',high=dir+s.id+'-ultra.glb';
 if(s.modelUrl===s.highModelUrl){await fs.copyFile(low,high);s.highModelUrl=s.modelUrl.replace(/\.glb$/,'-ultra.glb');}
 const doc=await io.read(low);
 for(const t of doc.getRoot().listTextures()){
  t.setImage(await sharp(Buffer.from(t.getImage())).resize({width:1024,height:1024,fit:'inside',withoutEnlargement:true}).png().toBuffer()).setMimeType('image/png');
 }
 await io.write(low,doc);const a=await fs.readFile(low),b=await fs.readFile(high);
 s.modelSha256=hash(a);s.downloadBytes=a.length;s.highModelSha256=hash(b);s.highDownloadBytes=b.length;
 if(a.equals(b)){s.highModelUrl=s.modelUrl;await fs.unlink(high);}
 item.standardTextureSize=1024;item.highTextureSize=2048;
 item.changes+=' Standard textures limited to 1K; Ultra textures up to 2K.';
 await fs.writeFile(file,JSON.stringify(manifest,null,2)+'\n');console.log(s.id,a.length,b.length);
}
