import {useMemo,useRef,useEffect,useLayoutEffect} from 'react'
import {useBox} from '@react-three/cannon'
import {Mesh,Group,MeshStandardMaterial,MeshBasicMaterial,Float32BufferAttribute,BufferGeometry,CanvasTexture,RepeatWrapping,Object3D,InstancedMesh,Color} from 'three'
import {mergeBufferGeometries} from 'three/examples/jsm/utils/BufferGeometryUtils'
import type {BoxProps} from '@react-three/cannon'
import {BUILDINGS,STREETS,RAMPS,PARKING} from './playground-layout'
export const worldSurfaces:Object3D[]=[]
function surfaceTexture(kind:'asphalt'|'concrete'|'windows') {
 const c=document.createElement('canvas');c.width=c.height=256;const ctx=c.getContext('2d')!;let seed=121
 const random=()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296}
 ctx.fillStyle=kind==='asphalt'?'#63666a':kind==='concrete'?'#b7b9b8':'#8d9da5';ctx.fillRect(0,0,256,256)
 if(kind==='windows'){for(let y=8;y<256;y+=32)for(let x=7;x<256;x+=32){ctx.fillStyle=random()>.3?'#344e5b':'#536976';ctx.fillRect(x,y,20,23);ctx.fillStyle='#bdcbd0';ctx.fillRect(x,y,20,2);ctx.fillStyle='#8799a1';ctx.fillRect(x+9,y,1,23)}}
 else {for(let i=0;i<10000;i++){const v=kind==='asphalt'?70+random()*65:150+random()*55;ctx.fillStyle=`rgba(${v},${v},${v},.3)`;ctx.fillRect(random()*256,random()*256,1,1)}if(kind==='concrete'){ctx.strokeStyle='#93999b';ctx.lineWidth=1;ctx.strokeRect(0,0,256,256)}}
 const t=new CanvasTexture(c);t.wrapS=t.wrapT=RepeatWrapping;t.anisotropy=4;return t
}
function Surface({x,z,w,d,color,map}:{x:number;z:number;w:number;d:number;color?:string;map?:CanvasTexture}){return <mesh rotation={[-Math.PI/2,0,0]} position={[x,.008,z]} receiveShadow><planeGeometry args={[w,d]}/><meshStandardMaterial color={color||'#fff'} map={map} roughness={.92} polygonOffset polygonOffsetFactor={-1}/></mesh>}
function Solid({color='#acb3b7',...props}:BoxProps&{color?:string}){const [ref]=useBox(()=>({type:'Static',...props}),useRef<Mesh>(null));useEffect(()=>{const o=ref.current;if(o)worldSurfaces.push(o);return()=>{if(o){const i=worldSurfaces.indexOf(o);if(i>=0)worldSurfaces.splice(i,1)}}},[ref]);return <mesh ref={ref} receiveShadow castShadow><boxGeometry args={props.args}/><meshStandardMaterial color={color} roughness={.8}/></mesh>}
function RoadMarks(){const transforms=useMemo(()=>{const rows:{x:number;z:number;w:number;d:number;color:string}[]=[];for(const axis of STREETS)for(let t=-155;t<=155;t+=10){if(STREETS.some(a=>Math.abs(t-a)<12))continue;rows.push({x:axis,z:t,w:.16,d:4,color:'#e8dc9e'},{x:t,z:axis,w:4,d:.16,color:'#e8dc9e'})}for(const [x,z]of PARKING){rows.push({x:x-3,z,w:.12,d:10,color:'#f3f1df'},{x:x+3,z,w:.12,d:10,color:'#f3f1df'},{x,z:z+5,w:6,d:.12,color:'#f3f1df'})}for(const z of STREETS)for(const x of STREETS)for(let k=-6;k<7;k+=2){rows.push({x:x+k,z:z+10,w:.9,d:3,color:'#dddcd4'},{x:x+k,z:z-10,w:.9,d:3,color:'#dddcd4'})}return rows},[]),ref=useRef<InstancedMesh>(null)
 useEffect(()=>{const o=new Object3D();transforms.forEach((p,i)=>{o.position.set(p.x,.018,p.z);o.rotation.x=-Math.PI/2;o.scale.set(p.w,p.d,1);o.updateMatrix();ref.current!.setMatrixAt(i,o.matrix);ref.current!.setColorAt(i,new Color(p.color))});ref.current!.instanceMatrix.needsUpdate=true;if(ref.current!.instanceColor)ref.current!.instanceColor.needsUpdate=true},[transforms])
 return <instancedMesh ref={ref} args={[undefined,undefined,transforms.length]} frustumCulled={false}><planeGeometry/><meshBasicMaterial polygonOffset polygonOffsetFactor={-2}/></instancedMesh>
}
export function World(){
 const staticRoot=useRef<Group>(null)
 useStaticBatch(staticRoot)
 const ground=useRef<Mesh>(null),asphalt=useMemo(()=>{const t=surfaceTexture('asphalt');t.repeat.set(85,85);return t},[]),concrete=useMemo(()=>{const t=surfaceTexture('concrete');t.repeat.set(100,100);return t},[])
 const facades=useMemo(()=>BUILDINGS.map(b=>{const t=surfaceTexture('windows');t.repeat.set(Math.max(1,b.w/12),b.h/12);return t}),[])
 useEffect(()=>{if(ground.current)worldSurfaces.push(ground.current);return()=>{worldSurfaces.length=0;asphalt.dispose();concrete.dispose();facades.forEach(t=>t.dispose())}},[asphalt,concrete,facades])
 return <group ref={staticRoot}><mesh ref={ground} rotation={[-Math.PI/2,0,0]} receiveShadow><planeGeometry args={[346,346]}/><meshStandardMaterial map={asphalt} color="#c6ced0" roughness={.95}/></mesh>
 <mesh rotation={[-Math.PI/2,0,0]} position={[0,-.02,0]}><planeGeometry args={[2000,2000]}/><meshStandardMaterial color="#899b82" roughness={1}/></mesh><PlaygroundMarks/><ZoneSigns/>
 <RoadMarks/>
 {BUILDINGS.map((b,i)=><group key={i}><mesh position={[b.x,b.h/2,b.z]} receiveShadow><boxGeometry args={[b.w+.02,b.h,b.d+.02]}/><meshStandardMaterial map={facades[i]} color={b.color} roughness={.5} metalness={.12}/></mesh><mesh position={[b.x,b.h+.3,b.z]}><boxGeometry args={[b.w+1,.6,b.d+1]}/><meshStandardMaterial color="#bdc5c6"/></mesh><mesh position={[b.x,1.2,b.z]}><boxGeometry args={[b.w+.15,2.4,b.d+.15]}/><meshStandardMaterial color="#68737a"/></mesh></group>)}
 {[-1,1].map(side=><group key={side}>{Array.from({length:7},(_,i)=><group key={i} position={[side*166,0,-144+i*48]}><mesh position={[0,4,0]}><cylinderGeometry args={[.09,.14,8,6]}/><meshStandardMaterial color="#53636a"/></mesh><mesh position={[-side*.7,8,0]}><boxGeometry args={[1.6,.15,.6]}/><meshStandardMaterial color="#e9edf0" emissive="#e3efeb" emissiveIntensity={.2}/></mesh></group>)}</group>)}
 </group>
}
export function Terrain(){useBox(()=>({type:'Static',args:[2000,4,2000],position:[0,-2,0],material:{friction:.6,restitution:0}}));return <>{BUILDINGS.map((b,i)=><BuildingCollider key={i} b={b}/>)}{RAMPS.map((r,i)=><Solid key={'r'+i} args={r.size} position={r.position} rotation={[r.angle,0,0]} color="#747d83"/>)}<Solid args={[348,1.4,1]} position={[0,.7,174]}/><Solid args={[348,1.4,1]} position={[0,.7,-174]}/><Solid args={[1,1.4,348]} position={[174,.7,0]}/><Solid args={[1,1.4,348]} position={[-174,.7,0]}/></>}
function BuildingCollider({b}:{b:typeof BUILDINGS[number]}){useBox(()=>({type:'Static',args:[b.w,b.h,b.d],position:[b.x,b.h/2,b.z]}));return null}
export function Ramp(props:BoxProps){return <Solid {...props}/>}

function PlaygroundMarks(){return <>
 {[24,26,40,42].map((r,i)=><mesh key={r} position={[-72,.02,12]} rotation={[-Math.PI/2,0,0]}><ringGeometry args={[r,r+.18,96]}/><meshBasicMaterial color={i%2?'#b8ad77':'#323a40'} transparent opacity={.75}/></mesh>)}
 {Array.from({length:15},(_,i)=><mesh key={'t'+i} position={[-72+Math.sin(i*2)*2,.023,12+Math.cos(i*2)*2]} rotation={[-Math.PI/2,0,i*.3]}><ringGeometry args={[29+i*.6,29.08+i*.6,96,1,0,3.6]}/><meshBasicMaterial color="#252c31" transparent opacity={.35}/></mesh>)}
 {Array.from({length:8},(_,i)=><group key={'cone'+i} position={[i%2?9:-9,0,-72+i*23]}><mesh position={[0,.35,0]}><coneGeometry args={[.27,.7,10]}/><meshStandardMaterial color="#e99145"/></mesh><mesh position={[0,.04,0]}><boxGeometry args={[.55,.08,.55]}/><meshStandardMaterial color="#303b43"/></mesh><mesh position={[0,.4,0]}><cylinderGeometry args={[.09,.13,.12,10]}/><meshStandardMaterial color="#f6f1da"/></mesh></group>)}
 {[-1,1].map(side=><group key={side}>{Array.from({length:23},(_,i)=><mesh key={i} position={[side*169,.22,-165+i*15]}><boxGeometry args={[1,.44,7]}/><meshStandardMaterial color={i%2?'#dfddd1':'#c35243'}/></mesh>)}</group>)}
 <Surface x={87} z={3} w={18} d={.4} color="#e6ce70"/>
 </>}

function ZoneSigns(){const textures=useMemo(()=>['DRIFT ZONE','JUMP ARENA','SLALOM RUN'].map((label,i)=>{const c=document.createElement('canvas');c.width=512;c.height=128;const x=c.getContext('2d')!;x.fillStyle='#172d39';x.fillRect(0,0,512,128);x.fillStyle=['#def294','#ffbb79','#98d4eb'][i];x.fillRect(0,0,12,128);x.font='bold 46px Arial';x.textAlign='center';x.fillText(label,262,80);return new CanvasTexture(c)}),[]);useEffect(()=>()=>textures.forEach(t=>t.dispose()),[textures]);return <>{[[-72,65],[87,102],[0,112]].map(([x,z],i)=><group key={i} position={[x,0,z]}><mesh position={[0,6,0]}><boxGeometry args={[22,5,.4]}/><meshStandardMaterial map={textures[i]} roughness={.8}/></mesh>{[-8,8].map(a=><mesh key={a} position={[a,2.2,0]}><cylinderGeometry args={[.15,.15,4.4,6]}/><meshStandardMaterial color="#64727a"/></mesh>)}</group>)}</>}

// Merge static, untextured decorations by material properties, preserving vertex colours.
// Instanced road markings and textured surfaces retain their existing optimized paths.
function useStaticBatch(root:React.RefObject<Group>){useEffect(()=>{if(!root.current)return;const parent=root.current;parent.updateMatrixWorld(true);const buckets=new Map<string,{material:MeshStandardMaterial|MeshBasicMaterial;geometries:BufferGeometry[];sources:Mesh[]}>();parent.traverse(o=>{if(!(o instanceof Mesh)||o instanceof InstancedMesh||Array.isArray(o.material))return;const m=o.material;if(!(m instanceof MeshStandardMaterial||m instanceof MeshBasicMaterial)||m.map)return;const key=[m.type,m.transparent,m.opacity,m.side,m.polygonOffset,m.polygonOffsetFactor].join(':');let bucket=buckets.get(key);if(!bucket){const mat=m.clone();mat.color.set('#ffffff');mat.vertexColors=true;bucket={material:mat,geometries:[],sources:[]};buckets.set(key,bucket)}const g=o.geometry.index?o.geometry.toNonIndexed():o.geometry.clone();g.applyMatrix4(o.matrixWorld);for(const name of Object.keys(g.attributes))if(!['position','normal'].includes(name))g.deleteAttribute(name);const count=g.getAttribute('position').count,colors=new Float32Array(count*3);for(let i=0;i<count;i++)m.color.toArray(colors,i*3);g.setAttribute('color',new Float32BufferAttribute(colors,3));bucket.geometries.push(g);bucket.sources.push(o)});const merged:Mesh[]=[];for(const b of buckets.values()){const geometry=mergeBufferGeometries(b.geometries);b.geometries.forEach(g=>g.dispose());if(!geometry){b.material.dispose();continue}const mesh=new Mesh(geometry,b.material);mesh.receiveShadow=true;parent.add(mesh);merged.push(mesh);b.sources.forEach(o=>o.visible=false)}return()=>{merged.forEach(m=>{parent.remove(m);m.geometry.dispose();(m.material as MeshStandardMaterial).dispose()});for(const b of buckets.values())b.sources.forEach(o=>o.visible=true)}},[])}
