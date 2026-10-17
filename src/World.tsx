import {useMemo,useRef} from 'react'
import {useFrame,useLoader} from '@react-three/fiber'
import {useBox,useHeightfield} from '@react-three/cannon'
import {TextureLoader,Mesh,MeshStandardMaterial,Color} from 'three'
import type {BoxProps} from '@react-three/cannon'
import {useModel} from './loaders'
export function World(){
 const {scene}=useModel('/models/track-draco.glb')
 const world=useMemo(()=>{const copy=scene.clone(true);copy.traverse(o=>{if(o instanceof Mesh){o.castShadow=false;o.receiveShadow=true;}});return copy},[scene]);
 return <primitive object={world}/>
}
export function Terrain(){
 const texture=useLoader(TextureLoader,'/textures/heightmap_1024.png')
 const heights=useMemo(()=>{const image=texture.image;const c=document.createElement('canvas');c.width=image.width;c.height=image.height;const ctx=c.getContext('2d')!;ctx.drawImage(image,0,0);const {data}=ctx.getImageData(0,0,c.width,c.height);return Array.from({length:c.width},(_,x)=>Array.from({length:c.height},(_,y)=>Number(Math.max(0,data[4*(y*c.width+x)]*40/255).toPrecision(2))/4))},[texture]);
 useHeightfield(()=>({args:[heights,{elementSize:.5085}],position:[260.5,-3.3,-260],rotation:[-Math.PI/2,0,-Math.PI]}));return null
}
export function Ramp({args,...props}:BoxProps){const [ref]=useBox(()=>({type:'Static',args,...props}),useRef<Mesh>(null));return <mesh ref={ref} castShadow receiveShadow><boxGeometry args={args}/><meshStandardMaterial color="#ac694c" roughness={.8}/></mesh>}
