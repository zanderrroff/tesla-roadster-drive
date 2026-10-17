import {useMemo,useRef,useEffect,createRef} from 'react'
import {useFrame} from '@react-three/fiber'
import {useBox,useCompoundBody,useRaycastVehicle} from '@react-three/cannon'
import {Group,Mesh,Vector3,Quaternion,MathUtils,MeshStandardMaterial,MeshPhysicalMaterial,CanvasTexture,Raycaster} from 'three'
import {useModel} from './loaders'
import {down,telemetry,useStore,steering,orbit} from './store'
import {driveForces} from './driving'
import {worldSurfaces} from './World'
import dimensions from './vehicle-dimensions.json'
const start:[number,number,number]=[-110,1.4,220], rotation:[number,number,number]=[0,Math.PI/2+.35,0]
const pos=new Vector3(),q=new Quaternion(),offset=new Vector3(),target=new Vector3(),forward=new Vector3(),velocityVector=new Vector3(),look=new Vector3(),rayDirection=new Vector3()
function Wheel({body,model}:{body:React.RefObject<Group>;model:Group}) {
 useCompoundBody(()=>({mass:30,type:'Kinematic',collisionFilterGroup:0,shapes:[{type:'Cylinder',args:[dimensions.radius,dimensions.radius,.24,12],rotation:[0,0,-Math.PI/2]}]}),body)
 return <group ref={body}><primitive object={model}/></group>
}
function shadowTexture(){const c=document.createElement('canvas');c.width=c.height=64;const x=c.getContext('2d')!;const g=x.createRadialGradient(32,32,8,32,32,31);g.addColorStop(0,'rgba(12,18,20,.45)');g.addColorStop(.55,'rgba(12,18,20,.25)');g.addColorStop(1,'rgba(12,18,20,0)');x.fillStyle=g;x.fillRect(0,0,64,64);return new CanvasTexture(c)}
export function Car(){
 const quality=useStore(s=>s.quality),reset=useStore(s=>s.reset)
 const {scene}=useModel('/models/roadster-'+quality+'.glb')
 const brakeMaterials=useRef<MeshStandardMaterial[]>([])
 const models=useMemo(()=>{brakeMaterials.current=[];const clone=scene.clone(true);clone.traverse(o=>{if(!(o instanceof Mesh))return;o.castShadow=true;o.receiveShadow=false;const original=o.material as MeshStandardMaterial;let m=original.clone();
  if(o.name==='Body_Material.001_0'){m=new MeshPhysicalMaterial({color:original.color,metalness:.65,roughness:.23,clearcoat:1,clearcoatRoughness:.12,envMapIntensity:1.25})}
  if(o.name.startsWith('Tires')){m.roughness=.95;m.metalness=.05}
  if(o.name.startsWith('Glass')){m.color.set('#182630');m.transparent=false;m.opacity=1;m.roughness=.12;m.metalness=.45;m.envMapIntensity=1.1}
  if(o.name.includes('side Light_Material.016')){m.emissive.set('#ff1810');m.emissiveIntensity=.35;brakeMaterials.current.push(m)}
  if(o.name.includes('Headlight_Material.004')){m.emissive.set('#fff6d8');m.emissiveIntensity=.8}
  o.material=m;
 });return [clone.getObjectByName('Chassis')!,...Array.from({length:4},(_,i)=>clone.getObjectByName('Wheel_'+i)!)] as Group[]},[scene])
 const [body,bodyApi]=useBox(()=>({mass:650,args:[1.82,.8,4.18],position:start,rotation,allowSleep:false,linearDamping:.06,angularDamping:.4}),useRef<Group>(null))
 const wheels=useMemo(()=>Array.from({length:4},()=>createRef<Group>()),[])
 const [,api]=useRaycastVehicle(()=>({chassisBody:body,wheels,wheelInfos:dimensions.centers.map((v,i)=>({chassisConnectionPointLocal:[v[0],v[1]+.18,v[2]],isFrontWheel:i<2,axleLocal:[-1,0,0],directionLocal:[0,-1,0],radius:dimensions.radius,suspensionRestLength:.18,suspensionStiffness:38,dampingRelaxation:3,dampingCompression:4.4,frictionSlip:2.1,rollInfluence:.04,maxSuspensionForce:100000,customSlidingRotationalSpeed:-.01,useCustomSlidingRotationalSpeed:true}))}))
 const velocity=useRef([0,0,0]),steer=useRef(0),previous=useRef(new Vector3(...start)),fresh=useRef(true),shadow=useRef<Mesh>(null),ray=useRef(new Raycaster()),rayTime=useRef(0),cameraLimit=useRef(30)
 const groundY=useRef(0)
 const blob=useMemo(shadowTexture,[])
 useEffect(()=>()=>blob.dispose(),[blob])
 useEffect(()=>bodyApi.velocity.subscribe(v=>{velocity.current=v;telemetry.speed=Math.hypot(...v)}),[bodyApi])
 useEffect(()=>{bodyApi.position.set(...start);bodyApi.rotation.set(...rotation);bodyApi.velocity.set(0,0,0);bodyApi.angularVelocity.set(0,0,0);steer.current=0;fresh.current=true;previous.current.set(...start)},[reset,bodyApi])
 useEffect(()=>{useStore.getState().set({loaded:true})},[])
 useFrame(({camera},dt)=>{
  if(!body.current)return;const s=useStore.getState(),active=s.started&&!s.paused,delta=Math.min(dt,.05)
  body.current.getWorldPosition(pos);body.current.getWorldQuaternion(q);telemetry.position=[pos.x,pos.y,pos.z]
  forward.set(0,0,1).applyQuaternion(q);const signed=forward.dot(velocityVector.set(...velocity.current as [number,number,number]));telemetry.signedSpeed=signed
  const direction=Math.max(-1,Math.min(1,steering.value+(down('left')?1:0)-(down('right')?1:0)))
  steer.current=MathUtils.damp(steer.current,active?direction*.5/(1+telemetry.speed*.024):0,10,delta)
  const f=driveForces(active,down('forward'),down('brake'),s.handbrake,s.gear,signed)
  for(let i=0;i<4;i++){api.applyEngineForce(f.force,i);api.setBrake(i<2?f.frontBrake:f.rearBrake,i);if(i<2)api.setSteeringValue(steer.current,i)}
  for(const m of brakeMaterials.current)m.emissiveIntensity=down('brake')||s.handbrake?2:.35
  if(active){telemetry.distance+=Math.min(pos.distanceTo(previous.current),10);if(pos.y< -12||Math.abs(pos.x)>270||Math.abs(pos.z)>270)useStore.getState().set({reset:s.reset+1})}previous.current.copy(pos)
  const heading=Math.atan2(forward.x,forward.z),angle=heading+Math.PI+orbit.yaw,flat=orbit.distance*Math.cos(orbit.pitch)
  offset.set(Math.sin(angle)*flat,Math.sin(orbit.pitch)*orbit.distance,Math.cos(angle)*flat);look.copy(pos);look.y+=.45
  rayTime.current+=dt
  if(rayTime.current>.15){rayTime.current=0;rayDirection.copy(offset).normalize();ray.current.set(look,rayDirection);ray.current.far=orbit.distance;const hit=ray.current.intersectObjects(worldSurfaces,false)[0];cameraLimit.current=hit?Math.max(1.5,hit.distance-.35):orbit.distance
   if(shadow.current){ray.current.set(new Vector3(pos.x,pos.y+1,pos.z),new Vector3(0,-1,0));ray.current.far=6;const ground=ray.current.intersectObjects(worldSurfaces,false)[0];shadow.current.visible=!!ground;if(ground){groundY.current=ground.point.y+.035}}
  }
  if(shadow.current){shadow.current.position.set(pos.x,groundY.current,pos.z);shadow.current.rotation.set(-Math.PI/2,0,-heading)}
  offset.setLength(Math.min(orbit.distance,cameraLimit.current));target.copy(look).add(offset)
  if(fresh.current){camera.position.copy(target);fresh.current=false}else camera.position.lerp(target,1-Math.exp(-(orbit.dragging?18:7)*delta))
  camera.lookAt(look)
 })
 return <><group ref={body}><primitive object={models[0]}/></group>{wheels.map((w,i)=><Wheel key={i} body={w} model={models[i+1]}/>) }
 <mesh ref={shadow} rotation={[-Math.PI/2,0,0]} renderOrder={1}><planeGeometry args={[3.5,5.6]}/><meshBasicMaterial map={blob} transparent depthWrite={false} polygonOffset polygonOffsetFactor={-2}/></mesh></>
}
