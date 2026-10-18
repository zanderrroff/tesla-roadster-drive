import fs from 'node:fs/promises';import sharp from 'sharp';import crypto from 'node:crypto';
const output='unity/MotorCoast/Assets/Resources/GUI',source='unity/ThirdParty/GUI';
for(const icon of ['log-out','car-front','arrow-up','chevrons-up','arrow-down','move-horizontal','smile','map','user-round','footprints']){
 const response=await fetch('https://cdn.jsdelivr.net/npm/lucide-static@0.468.0/icons/'+icon+'.svg');if(!response.ok)throw Error(icon+' '+response.status);
 const svg=await response.text();await fs.writeFile(source+'/'+icon+'.svg',svg);
 await sharp(Buffer.from(svg.replaceAll('currentColor','#ffffff'))).resize(128,128).png().toFile(output+'/'+icon+'.png');
}
const file='unity/gui-manifest.json',manifest=JSON.parse(await fs.readFile(file));
manifest.sources.push({author:'Kenney',url:'https://kenney.nl/assets/ui-pack',license:'CC0-1.0'});manifest.files=[];
for(const name of (await fs.readdir(output)).filter(x=>x.endsWith('.png')).sort()){
 const bytes=await fs.readFile(output+'/'+name);manifest.files.push({file:'GUI/'+name,bytes:bytes.length,sha256:crypto.createHash('sha256').update(bytes).digest('hex')});
}await fs.writeFile(file,JSON.stringify(manifest,null,2)+'\n');console.log(manifest.files.length,'licensed UI sprites');
