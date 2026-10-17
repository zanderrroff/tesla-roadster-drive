import {useEffect,useMemo} from 'react'
import {useFrame} from '@react-three/fiber'
import {Group,Mesh,MeshStandardMaterial,MeshBasicMaterial,BufferGeometry,CanvasTexture,SphereGeometry,CylinderGeometry,TorusGeometry,PlaneGeometry,Vector3} from 'three'
import {RoundedBoxGeometry} from 'three/examples/jsm/geometries/RoundedBoxGeometry'
import {mergeBufferGeometries} from 'three/examples/jsm/utils/BufferGeometryUtils'
import {telemetry,useStore} from './store'

function buildCabin(){
 const root=new Group();root.name='Roadster interior and seated driver'
 const materials={leather:new MeshStandardMaterial({color:'#263139',roughness:.87}),insert:new MeshStandardMaterial({color:'#4b5b65',roughness:.9}),floor:new MeshStandardMaterial({color:'#182127',roughness:1}),trim:new MeshStandardMaterial({color:'#9eabb1',metalness:.72,roughness:.35}),skin:new MeshStandardMaterial({color:'#bd8a66',roughness:.85}),shirt:new MeshStandardMaterial({color:'#354e69',roughness:.92}),pants:new MeshStandardMaterial({color:'#25303c',roughness:.95}),hair:new MeshStandardMaterial({color:'#332a25',roughness:1}),eye:new MeshStandardMaterial({color:'#16202a',roughness:.5})}
 const add=(g:BufferGeometry,m:MeshStandardMaterial|MeshBasicMaterial,p:number[],scale=[1,1,1],rotation=[0,0,0],parent=root)=>{const mesh=new Mesh(g,m);mesh.position.set(...p as [number,number,number]);mesh.scale.set(...scale as [number,number,number]);mesh.rotation.set(...rotation as [number,number,number]);mesh.castShadow=false;mesh.receiveShadow=false;parent.add(mesh);return mesh}
 const box=(size:number[],p:number[],m=materials.leather,r=[0,0,0],parent=root)=>add(new RoundedBoxGeometry(size[0],size[1],size[2],1,Math.min(...size)*.18),m,p,[1,1,1],r,parent)
 const ball=(p:number[],scale:number[],m:MeshStandardMaterial,parent=root)=>add(new SphereGeometry(1,16,12),m,p,scale,[0,0,0],parent)
 const limb=(a:number[],b:number[],radius:number,m:MeshStandardMaterial)=>{const av=new Vector3(...a as [number,number,number]),bv=new Vector3(...b as [number,number,number]),delta=bv.clone().sub(av);const mesh=add(new CylinderGeometry(radius*.88,radius,delta.length(),12),m,av.add(bv).multiplyScalar(.5).toArray());mesh.quaternion.setFromUnitVectors(new Vector3(0,1,0),delta.normalize())}
 // Closed cabin tub, rear bulkhead and door cards cover the previously empty shell.
 box([1.32,.07,1.5],[0,-.35,-.03],materials.floor)
 box([1.32,.63,.07],[0,-.04,-.78],materials.leather)
 for(const x of [-.67,.67]){box([.065,.4,1.35],[x,-.07,-.03]);box([.08,.07,.53],[x*.93,-.03,-.07],materials.insert);box([.035,.025,.19],[x*.92,.09,.17],materials.trim)}
 for(const x of [-.34,.34]){
  box([.46,.11,.49],[x,-.19,-.25]);box([.31,.025,.38],[x,-.125,-.24],materials.insert)
  box([.44,.48,.1],[x,.055,-.52],materials.leather,[-.13,0,0]);box([.29,.35,.025],[x,.055,-.455],materials.insert,[-.13,0,0])
  box([.26,.16,.10],[x,.36,-.56]);for(const dx of [-.19,.19])box([.055,.3,.08],[x+dx,.05,-.43],materials.insert,[-.13,0,0])
 }
 box([1.31,.17,.30],[0,.10,.56]);box([1.23,.035,.27],[0,.20,.55],materials.insert)
 box([.19,.17,.87],[0,-.17,.06]);box([.16,.035,.36],[0,-.067,-.14],materials.insert)
 for(const x of [-.51,.48]){box([.20,.035,.025],[x,.14,.397],materials.floor);box([.20,.007,.029],[x,.14,.378],materials.trim)}
 // Centre display and compact digital instrument panel.
 const display=document.createElement('canvas');display.width=256;display.height=192;const ctx=display.getContext('2d')!,texture=new CanvasTexture(display)
 const screenMat=new MeshBasicMaterial({map:texture});add(new PlaneGeometry(.25,.19),screenMat,[0,.105,.385],[1,1,1],[0,Math.PI,0])
 box([.28,.22,.02],[0,.105,.398],materials.floor)
 const wheel=new Group();wheel.position.set(-.34,.13,.30);wheel.rotation.x=-.23;root.add(wheel)
 add(new TorusGeometry(.125,.018,8,32),materials.floor,[0,0,0],[1,1,1],[0,0,0],wheel)
 box([.18,.026,.025],[0,0,0],materials.trim,[0,0,0],wheel);box([.024,.1,.025],[0,-.045,0],materials.trim,[0,0,0],wheel);box([.07,.055,.035],[0,0,-.01],materials.leather,[0,0,0],wheel)
 // Generic seated adult driver, sized to the supplied roof and cabin opening.
 ball([-.34,-.095,-.28],[.16,.095,.12],materials.pants)
 ball([-.34,.105,-.32],[.17,.205,.105],materials.shirt)
 limb([-.34,.23,-.30],[-.34,.31,-.29],.044,materials.skin)
 const head=new Group();root.add(head)
 ball([-.34,.375,-.285],[.085,.105,.084],materials.skin,head)
 ball([-.34,.438,-.30],[.087,.05,.08],materials.hair,head)
 ball([-.34,.372,-.197],[.017,.023,.018],materials.skin,head)
 for(const x of [-.369,-.311])ball([x,.399,-.209],[.012,.008,.006],materials.eye,head)
 for(const x of [-.426,-.254])ball([x,.375,-.285],[.013,.023,.014],materials.skin,head)
 for(const side of [-1,1]){const hip=-.34+side*.085,knee=-.34+side*.10;limb([hip,-.11,-.24],[knee,-.16,.03],.061,materials.pants);limb([knee,-.16,.03],[knee,-.29,.30],.044,materials.pants);box([.09,.065,.18],[knee,-.30,.34],materials.floor)
 const shoulder=-.34+side*.145,elbow=-.34+side*.19,hand=-.34+side*.11;limb([shoulder,.20,-.29],[elbow,.02,-.02],.047,materials.shirt);limb([elbow,.02,-.02],[hand,.13,.27],.034,materials.skin);ball([hand,.13,.27],[.034,.038,.031],materials.skin)}
 // Merge static pieces by shared material: detail without one draw call per body part.
 root.updateMatrixWorld(true);const batches=new Map<string,{m:MeshStandardMaterial|MeshBasicMaterial,g:BufferGeometry[],objects:Mesh[]}>();for(const o of [...root.children]){if(!(o instanceof Mesh))continue;const m=o.material as MeshStandardMaterial|MeshBasicMaterial;let b=batches.get(m.uuid);if(!b){b={m,g:[],objects:[]};batches.set(m.uuid,b)}const g=o.geometry.index?o.geometry.toNonIndexed():o.geometry.clone();g.applyMatrix4(o.matrix);b.g.push(g);b.objects.push(o)}
 for(const b of batches.values()){const g=mergeBufferGeometries(b.g);b.g.forEach(p=>p.dispose());if(!g)continue;b.objects.forEach(o=>{root.remove(o);o.geometry.dispose()});root.add(new Mesh(g,b.m))}
 const paint=()=>{ctx.fillStyle='#101e28';ctx.fillRect(0,0,256,192);ctx.fillStyle='#e2efbd';ctx.font='bold 16px Arial';ctx.fillText('ROADSTER',18,29);ctx.fillStyle='#fff';ctx.font='52px Arial';ctx.fillText(String(Math.round(telemetry.speed*3.6)).padStart(2,'0'),18,91);ctx.font='14px Arial';ctx.fillText('km/h   '+useStore.getState().gear,104,88);ctx.strokeStyle='#4c6670';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(20,169);ctx.lineTo(70,130);ctx.lineTo(180,150);ctx.lineTo(236,112);ctx.stroke();ctx.fillStyle='#def1ab';ctx.fillRect(18,106,210,2);ctx.font='12px Arial';ctx.fillText('FREE DRIVE',18,184);texture.needsUpdate=true};paint()
 let triangles=0;root.traverse(o=>{if(o instanceof Mesh)triangles+=(o.geometry.index?.count||o.geometry.getAttribute('position').count)/3})
 return {root,wheel,head,paint,texture,triangles,dispose:()=>{root.traverse(o=>{if(o instanceof Mesh)o.geometry.dispose()});Object.values(materials).forEach(m=>m.dispose());screenMat.dispose();texture.dispose()}}
}
export function Cabin(){const cabin=useMemo(buildCabin,[]),mode=useStore(s=>s.camera);useEffect(()=>()=>cabin.dispose(),[cabin]);useEffect(()=>{cabin.head.visible=mode!==2},[mode]);let elapsed=0;useFrame((_,dt)=>{if(useStore.getState().paused)return;cabin.wheel.rotation.z=-telemetry.steeringAngle*3;elapsed+=dt;if(elapsed>.15){elapsed=0;cabin.paint()}});return <primitive object={cabin.root}/>}
