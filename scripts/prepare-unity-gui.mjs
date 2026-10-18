// Reproduce the CC0 automotive controls and licensed service icons.
// Dependencies: the repository's sharp package; system unzip for Kenney's ZIP.
import fs from 'node:fs/promises'
import {execFileSync} from 'node:child_process'
import crypto from 'node:crypto'
import sharp from 'sharp'
const root='unity/ThirdParty/GUI',output='unity/MotorCoast/Assets/Resources/GUI',work='.asset-work/gui'
await fs.mkdir(output,{recursive:true});await fs.mkdir(work,{recursive:true})
const packURL='https://kenney.nl/media/pages/assets/mobile-controls/0d047b3be4-1754738457/mobile-controls-1.zip'
let zip;try{zip=await fs.readFile(work+'/mobile-controls.zip')}catch{
 const response=await fetch(packURL);if(!response.ok)throw Error(response.status);zip=Buffer.from(await response.arrayBuffer());await fs.writeFile(work+'/mobile-controls.zip',zip)
}
const source=await fs.readFile(root+'/car_controls.svg','utf8'),defs=source.match(/<defs[\s\S]*?<\/defs>/)[0]
function group(id){
 const start=source.search(new RegExp('<g\\s[^>]*\\bid="'+id+'"[^>]*>'));if(start<0)throw Error('Missing SVG group '+id)
 const text=source.slice(start);let depth=0,end=0
 for(const match of text.matchAll(/<(\/?)g\b[^>]*>/g)){depth+=match[1]?-1:1;if(!depth){end=match.index+match[0].length;break}}
 if(!end)throw Error('Unclosed SVG group');return text.slice(0,end)
}
for(const [name,id] of Object.entries({wheel:'g31235',gas:'g37164',brake:'g37281'})){
 const svg=`<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="800" height="1125" viewBox="0 0 800 1125">${defs}${group(id)}</svg>`
 await sharp(Buffer.from(svg)).trim({background:'#00000000'}).resize({width:512,height:512,fit:'inside'}).png().toFile(output+'/'+name+'.png')
}
for(const [name,path] of Object.entries({'button-circle':'Style A/Default/button_circle.png','button-wide':'Style C/Default/button_square_wide.png',left:'Style C/Default/direction_left.png',right:'Style C/Default/direction_right.png',pause:'Icons/Default/icon_pause.png',reset:'Icons/Default/icon_arrow_rotate.png',horn:'Icons/Default/icon_sound.png'}))
 await fs.writeFile(output+'/'+name+'.png',execFileSync('unzip',['-p',work+'/mobile-controls.zip','Sprites/'+path]))
for(const [name,file] of [['camera','camera'],['garage','warehouse']]){
 const svg=(await fs.readFile(root+'/'+file+'.svg','utf8')).replaceAll('currentColor','#ffffff')
 await sharp(Buffer.from(svg)).resize(128,128).png().toFile(output+'/'+name+'.png')
}
const files=[]
for(const name of (await fs.readdir(output)).filter(n=>n.endsWith('.png')).sort()){
 const data=await fs.readFile(output+'/'+name);files.push({file:'GUI/'+name,bytes:data.length,sha256:crypto.createHash('sha256').update(data).digest('hex')})
}
await fs.writeFile('unity/gui-manifest.json',JSON.stringify({sources:[{author:'looneybits',url:'https://opengameart.org/content/car-controls',license:'CC0-1.0'},{author:'Kenney',url:'https://kenney.nl/assets/mobile-controls',download:packURL,license:'CC0-1.0'},{author:'Lucide contributors',url:'https://lucide.dev',license:'ISC'}],files},null,2)+'\n')
console.log('Licensed driving GUI prepared:',files.length,'sprites')
