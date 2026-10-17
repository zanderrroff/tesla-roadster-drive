import {useEffect,useMemo,useRef} from 'react'
import {Group,Mesh,MeshStandardMaterial,PlaneGeometry,CylinderGeometry,BoxGeometry,SphereGeometry,BufferGeometry,Float32BufferAttribute,Color,Vector3} from 'three'
import {mergeBufferGeometries} from 'three/examples/jsm/utils/BufferGeometryUtils'
function buildCoast(){const root=new Group(),materials:MeshStandardMaterial[]=[];const mat=(color:string,roughness=.8,metalness=0)=>{const m=new MeshStandardMaterial({color,roughness,metalness});materials.push(m);return m};const lawn=mat('#6e8050'),sand=mat('#c2ae85'),bark=mat('#766346'),leaves=mat('#416b4e'),stone=mat('#777f76'),white=mat('#ebe4d3'),orange=mat('#cf6946'),steel=mat('#344c54',.4,.5),blue=mat('#6996a5',.3,.2)
 const add=(geometry:BufferGeometry,material:MeshStandardMaterial,p:number[],rotation=[0,0,0],scale=[1,1,1])=>{const m=new Mesh(geometry,material);m.position.fromArray(p);m.rotation.set(rotation[0],rotation[1],rotation[2]);m.scale.fromArray(scale);root.add(m);return m}
 const box=(size:number[],p:number[],m:MeshStandardMaterial,r=[0,0,0])=>add(new BoxGeometry(...size as [number,number,number]),m,p,r)
 // All landscape objects stay outside the driveable apron; there are no invisible central obstacles.
 for(const side of [-1,1]){box([22,.1,340],[side*188,-.13,0],lawn);box([8,.09,340],[side*203,-.12,0],sand)}
 box([350,.12,28],[0,-.13,193],sand);box([350,.1,18],[0,-.13,-188],lawn)
 // Coastal boardwalk and thin railings behind the boundary.
 box([10,.35,380],[-211,-.15,0],white);for(let z=-180;z<=180;z+=12){box([.1,1.2,.1],[-216,.6,z],steel);box([.1,.07,12],[-216,1.2,z+6],steel)}
 const palm=(x:number,z:number,height:number,seed:number)=>{add(new CylinderGeometry(.18,.36,height,7),bark,[x,height/2,z],[.04*Math.sin(seed),0,.06*Math.cos(seed)]);add(new SphereGeometry(.65,8,6),bark,[x,height,z],[0,0,0],[1,.5,1]);for(let k=0;k<7;k++){const a=k*Math.PI*2/7+seed;const points:number[]=[],normals:number[]=[];for(let j=0;j<4;j++){const t=j/4,t2=(j+1)/4;const vertex=(v:number,side:number)=>{const w=Math.sin(v*Math.PI)*.65,rad=v*4.8;return [x+Math.cos(a)*rad+Math.sin(a)*w*side,height+.6*Math.sin(v*Math.PI)-v*v*1.2,z+Math.sin(a)*rad-Math.cos(a)*w*side]};const A=vertex(t,-1),B=vertex(t,1),C=vertex(t2,-1),D=vertex(t2,1);points.push(...A,...C,...B,...B,...C,...D)}const g=new BufferGeometry();g.setAttribute('position',new Float32BufferAttribute(points,3));g.computeVertexNormals();const leaf=add(g,leaves,[0,0,0]);leaf.material.side=2}}
 for(let i=0;i<26;i++){const side=i%2?-1:1;palm(side*(183+(i%3)*4),-150+Math.floor(i/2)*25,8+(i%4)*.7,i*1.3)}
 // A low coastal skyline sits beyond the test circuit.
 for(let i=0;i<24;i++){const x=-320+i*29,h=20+((i*17)%57),z=305+(i%3)*25;box([17,h,17],[x,h/2-1,z],i%3?white:blue);box([19,.8,19],[x,h,z],steel);for(let y=6;y<h;y+=6)box([17.2,.6,17.2],[x,y,z],steel)}
 for(let i=0;i<13;i++){const x=280+Math.sin(i)*90,z=-250+i*52;add(new SphereGeometry(1,10,6),i%2?lawn:stone,[x,-10,z],[0,i,0],[95,65+(i%3)*28,110])}
 // Paddock awnings, solar roofs and striped edge barriers provide readable scale.
 for(const x of [-110,-40,115]){box([32,.3,14],[x,5,-132],steel,[.03,0,0]);for(const dx of [-14,14])for(const dz of [-5,5])box([.25,5,.25],[x+dx,2.5,-132+dz],steel);for(let j=0;j<8;j++)box([3.4,.08,12],[x-13.3+j*3.8,5.2,-132],blue,[.03,0,0])}
 for(let x=-165;x<=165;x+=15){box([7,.55,.8],[x,.3,169],x%30?white:orange);box([7,.55,.8],[x,.3,-169],x%30?white:orange)}
 for(let i=0;i<9;i++){const x=-157+i*39;box([.15,9,.15],[x,4.5,180],steel);box([2.7,4,.03],[x+1.3,6.6,180],i%2?orange:blue)}
 // Merge by material: several hundred details become a few draw calls.
 root.updateMatrixWorld(true);for(const material of materials){const meshes=root.children.filter(o=>o instanceof Mesh&&o.material===material) as Mesh[];const gs=meshes.map(m=>{const g=m.geometry.index?m.geometry.toNonIndexed():m.geometry.clone();g.applyMatrix4(m.matrixWorld);g.deleteAttribute('uv');return g});if(!gs.length)continue;const g=mergeBufferGeometries(gs);gs.forEach(g=>g.dispose());if(g){meshes.forEach(m=>{root.remove(m);m.geometry.dispose()});const batch=new Mesh(g,material);batch.castShadow=true;batch.receiveShadow=true;root.add(batch)}}
 return {root,dispose(){root.traverse(o=>{if(o instanceof Mesh)o.geometry.dispose()});materials.forEach(m=>m.dispose())}}
}
export function Coast(){const coast=useMemo(buildCoast,[]);useEffect(()=>()=>coast.dispose(),[coast]);return <><primitive object={coast.root}/><mesh rotation={[-Math.PI/2,0,0]} position={[-560,-.6,0]} receiveShadow><planeGeometry args={[680,1800]}/><meshStandardMaterial color="#367d8d" roughness={.26} metalness={.35}/></mesh>{Array.from({length:12},(_,i)=><mesh key={i} rotation={[-Math.PI/2,0,0]} position={[-225-i*10,-.57,0]}><planeGeometry args={[.5+i*.06,900]}/><meshBasicMaterial color="#b4c9c2" transparent opacity={.12-i*.006}/></mesh>)}</>}
