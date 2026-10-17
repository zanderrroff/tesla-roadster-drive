import {useEffect,useMemo,useRef} from 'react'
import {useFrame,useLoader,useThree} from '@react-three/fiber'
import {Loader,DataTexture,PMREMGenerator,Vector3,DirectionalLight,ACESFilmicToneMapping,sRGBEncoding,PCFSoftShadowMap} from 'three'
import {RGBELoader} from 'three/examples/jsm/loaders/RGBELoader'
import {Sky} from 'three/examples/jsm/objects/Sky'
import {telemetry,useStore} from './store'
export function Environment(){
 const {gl,scene}=useThree(),hdr=useLoader(RGBELoader as any,'/textures/dikhololo_night_1k.hdr') as DataTexture
 const quality=useStore(s=>s.quality),light=useRef<DirectionalLight>(null)
 const basis=useMemo(()=>{const direction=new Vector3(35,55,25).normalize(),right=new Vector3(0,1,0).cross(direction).normalize(),up=direction.clone().cross(right).normalize();return {direction,right,up,target:new Vector3(),source:new Vector3(),sun:new Vector3(35,55,25)}},[])
 const sky=useMemo(()=>{const s=new Sky();s.scale.setScalar(450);s.material.uniforms.turbidity.value=5;s.material.uniforms.rayleigh.value=1.5;s.material.uniforms.mieCoefficient.value=.004;s.material.uniforms.mieDirectionalG.value=.8;s.material.uniforms.sunPosition.value.set(1,.5,.4);return s},[])
 useEffect(()=>{const pmrem=new PMREMGenerator(gl);const env=pmrem.fromEquirectangular(hdr);scene.environment=env.texture;gl.toneMapping=ACESFilmicToneMapping;gl.toneMappingExposure=1.05;gl.outputEncoding=sRGBEncoding;gl.shadowMap.type=PCFSoftShadowMap;pmrem.dispose();return()=>{scene.environment=null;env.dispose()}},[gl,scene,hdr])
 useEffect(()=>()=>{sky.geometry.dispose();sky.material.dispose()},[sky])
 useFrame(()=>{if(!light.current||useStore.getState().paused)return;const p=basis.source.fromArray(telemetry.renderPosition),step=32/(quality==='high'?2048:1024),r=Math.round(p.dot(basis.right)/step)*step,u=Math.round(p.dot(basis.up)/step)*step,d=p.dot(basis.direction);basis.target.copy(basis.right).multiplyScalar(r).addScaledVector(basis.up,u).addScaledVector(basis.direction,d);light.current.target.position.copy(basis.target);light.current.position.copy(basis.target).add(basis.sun);light.current.target.updateMatrixWorld()})
 return <><primitive object={sky}/><hemisphereLight args={['#d9e9f6','#879393',.8]}/><directionalLight ref={light} intensity={1.5} color="#fff3d8" castShadow shadow-mapSize={quality==='high'?[2048,2048]:[1024,1024]} shadow-bias={-.0004} shadow-normalBias={.012} shadow-radius={2} shadow-camera-left={-16} shadow-camera-right={16} shadow-camera-top={16} shadow-camera-bottom={-16} shadow-camera-near={1} shadow-camera-far={140}/></>
}
export function AdaptiveResolution(){
 const {setDpr}=useThree(),quality=useStore(s=>s.quality),frames=useRef(0),elapsed=useRef(0),current=useRef(quality==='high'?1.25:1)
 useEffect(()=>{setDpr(current.current)},[setDpr])
 useFrame((_,d)=>{if(useStore.getState().paused)return;frames.current++;elapsed.current+=d;if(elapsed.current<6)return;const fps=frames.current/elapsed.current;telemetry.fps=fps;const limit=quality==='high'?1.5:1.25;let next=current.current;if(fps<26)next=Math.max(quality==='high'?1:.75,next-.15);else if(fps>52)next=Math.min(limit,next+.1);if(next!==current.current){current.current=next;setDpr(next)}telemetry.dpr=current.current;elapsed.current=0;frames.current=0})
 return null
}
