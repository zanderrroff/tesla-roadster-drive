import {useMemo,useRef,useEffect} from 'react'
import {useFrame,useLoader} from '@react-three/fiber'
import {useBox,useHeightfield} from '@react-three/cannon'
import {TextureLoader,Mesh,MeshStandardMaterial,CanvasTexture,RepeatWrapping,LinearEncoding,Object3D,InstancedMesh,Matrix4,Vector3,Raycaster,Color,DoubleSide,PlaneGeometry,BufferGeometry,Float32BufferAttribute} from 'three'
import type {BoxProps} from '@react-three/cannon'
import {useModel} from './loaders'
import {useStore} from './store'
export const worldSurfaces:Object3D[]=[]
function grain(){const c=document.createElement('canvas');c.width=c.height=256;const x=c.getContext('2d')!,data=x.createImageData(256,256);let seed=35;for(let i=0;i<data.data.length;i+=4){seed=(seed*1664525+1013904223)>>>0;const v=115+(seed/4294967296)*95;data.data[i]=data.data[i+1]=data.data[i+2]=v;data.data[i+3]=255}x.putImageData(data,0,0);const t=new CanvasTexture(c);t.wrapS=t.wrapT=RepeatWrapping;t.encoding=LinearEncoding;return t}
function smoothNormals(g:BufferGeometry){const p=g.getAttribute('position');if(!p)return;g.computeVertexNormals();const n=g.getAttribute('normal'),groups=new Map<string,number[]>();for(let i=0;i<p.count;i++){const key=[p.getX(i),p.getY(i),p.getZ(i)].map(v=>Math.round(v*10000)).join(',');const a=groups.get(key)||[0,0,0];a[0]+=n.getX(i);a[1]+=n.getY(i);a[2]+=n.getZ(i);groups.set(key,a)}for(let i=0;i<p.count;i++){const key=[p.getX(i),p.getY(i),p.getZ(i)].map(v=>Math.round(v*10000)).join(',');const a=groups.get(key)!;const len=Math.hypot(...a)||1;n.setXYZ(i,a[0]/len,a[1]/len,a[2]/len)}n.needsUpdate=true}
export function World(){
 const {scene}=useModel('/models/track-draco.glb'),detail=useMemo(grain,[])
 const world=useMemo(()=>{const copy=scene.clone(true);copy.traverse(o=>{if(!(o instanceof Mesh))return;o.castShadow=false;o.receiveShadow=true;const name=o.name.toLowerCase();const m=(o.material as MeshStandardMaterial).clone();o.material=m;m.envMapIntensity=.15;m.roughness=.9;
  if(/terrain|mountain/.test(name)){o.geometry=o.geometry.clone();smoothNormals(o.geometry)}
  if(/water/.test(name)){m.color.set('#51858a');m.metalness=.45;m.roughness=.2;m.envMapIntensity=.9}
  else {m.onBeforeCompile=shader=>{shader.uniforms.uSurfaceGrain={value:detail};shader.vertexShader='varying vec3 vSurfacePosition;\n'+shader.vertexShader;shader.vertexShader=shader.vertexShader.replace('#include <begin_vertex>','#include <begin_vertex>\nvSurfacePosition = (modelMatrix * vec4(position,1.0)).xyz;');shader.fragmentShader='uniform sampler2D uSurfaceGrain;\nvarying vec3 vSurfacePosition;\n'+shader.fragmentShader;shader.fragmentShader=shader.fragmentShader.replace('#include <map_fragment>',`#include <map_fragment>\nfloat surfaceGrain = texture2D(uSurfaceGrain, vSurfacePosition.xz * 1.8).r;\nfloat luminance=dot(diffuseColor.rgb,vec3(.2126,.7152,.0722));\ndiffuseColor.rgb=mix(diffuseColor.rgb,vec3(luminance)*vec3(1.16,1.05,.9),.38);\ndiffuseColor.rgb *= .85 + surfaceGrain * .28;`)};m.customProgramCacheKey=()=> 'roadster-surface-v2'}
 });copy.updateMatrixWorld(true);return copy},[scene,detail])
 useEffect(()=>{worldSurfaces.length=0;world.traverse(o=>{if(o instanceof Mesh&&!/cloud|bird|water|blade/i.test(o.name))worldSurfaces.push(o)});return()=>{worldSurfaces.length=0;detail.dispose();world.traverse(o=>{if(o instanceof Mesh){(o.material as MeshStandardMaterial).dispose();if(/terrain|mountain/i.test(o.name))o.geometry.dispose()}})}} , [world,detail])
 return <><primitive object={world}/><GroundDetails world={world}/></>
}
function GroundDetails({world}:{world:Object3D}){
 const quality=useStore(s=>s.quality),rocks=useRef<InstancedMesh>(null),plants=useRef<InstancedMesh>(null)
 const placements=useMemo(()=>{const ray=new Raycaster(),down=new Vector3(0,-1,0),items:{position:Vector3;scale:number;turn:number}[]=[];let seed=81;const random=()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296};const count=quality==='high'?180:90;world.updateMatrixWorld(true);for(let i=0;i<count*2&&items.length<count;i++){const x=-220+random()*430,z=-220+random()*455;ray.set(new Vector3(x,100,z),down);const hit=ray.intersectObject(world,true)[0];if(!hit||!/terrain/i.test(hit.object.name))continue;items.push({position:hit.point.clone(),scale:.18+random()*.42,turn:random()*Math.PI*2})}return items},[world,quality])
 useEffect(()=>{const dummy=new Object3D();placements.forEach((p,i)=>{dummy.position.copy(p.position);dummy.position.y+=p.scale*.2;dummy.rotation.set(.3,p.turn,.2);dummy.scale.set(p.scale,p.scale*.65,p.scale*.8);dummy.updateMatrix();rocks.current?.setMatrixAt(i,dummy.matrix);rocks.current?.setColorAt(i,new Color().setHSL(.09,.12,.25+(i%7)*.025));dummy.position.y=p.position.y;dummy.rotation.set(0,p.turn,0);dummy.scale.setScalar(p.scale*1.6);dummy.updateMatrix();plants.current?.setMatrixAt(i,dummy.matrix)});if(rocks.current){rocks.current.instanceMatrix.needsUpdate=true;if(rocks.current.instanceColor)rocks.current.instanceColor.needsUpdate=true}if(plants.current)plants.current.instanceMatrix.needsUpdate=true},[placements])
 const plantGeometry=useMemo(()=>{const g=new BufferGeometry();g.setAttribute('position',new Float32BufferAttribute([-.3,0,0,0,1,0,.3,0,0,0,0,-.3,0,.8,0,0,0,.3,-.2,0,-.2,0,.7,0,.2,0,.2],3));g.computeVertexNormals();return g},[])
 useEffect(()=>()=>plantGeometry.dispose(),[plantGeometry])
 return <><instancedMesh ref={rocks} args={[undefined,undefined,placements.length]} receiveShadow frustumCulled={false}><icosahedronGeometry args={[1,0]}/><meshStandardMaterial roughness={1}/></instancedMesh><instancedMesh ref={plants} args={[plantGeometry,undefined,placements.length]} frustumCulled={false}><meshStandardMaterial color="#73745b" side={DoubleSide} roughness={1}/></instancedMesh></>
}
export function Terrain(){
 const texture=useLoader(TextureLoader,'/textures/heightmap_1024.png')
 const heights=useMemo(()=>{const image=texture.image;const c=document.createElement('canvas');c.width=image.width;c.height=image.height;const ctx=c.getContext('2d')!;ctx.drawImage(image,0,0);const {data}=ctx.getImageData(0,0,c.width,c.height);const intervals=256;return Array.from({length:intervals+1},(_,x)=>Array.from({length:intervals+1},(_,y)=>{const px=Math.round(x*(c.width-1)/intervals),py=Math.round(y*(c.height-1)/intervals);return data[4*(py*c.width+px)]*10/255}))},[texture]);
 useHeightfield(()=>({args:[heights,{elementSize:.5085*(texture.image.width-1)/256}],position:[260.5,-3.3,-260],rotation:[-Math.PI/2,0,-Math.PI]}));return null
}
export function Ramp({args,...props}:BoxProps){const [ref]=useBox(()=>({type:'Static',args,...props}),useRef<Mesh>(null));return <mesh ref={ref} castShadow receiveShadow><boxGeometry args={args}/><meshStandardMaterial color="#92897c" roughness={.8}/></mesh>}
