import {useMemo,useRef,useEffect,createRef} from 'react'
import {useFrame,useThree} from '@react-three/fiber'
import {useBox,useCompoundBody,useRaycastVehicle} from '@react-three/cannon'
import {Group,Mesh,Vector3,Quaternion,MathUtils,MeshStandardMaterial,MeshPhysicalMaterial,CanvasTexture,Raycaster,PerspectiveCamera} from 'three'
import {useModel} from './loaders'
import {down,telemetry,useStore,steering,orbit} from './store'
import {driveForces} from './driving'
import {worldSurfaces} from './World'
import dimensions from './vehicle-dimensions.json'
import {SPAWN} from './playground-layout'
import {CHASSIS,wheelOptions} from './vehicle-physics'
import {Cabin} from './Cabin'
import {smoothPose,blend} from './motion'
const start=SPAWN, rotation:[number,number,number]=[0,0,0]
const pos=new Vector3(),q=new Quaternion(),offset=new Vector3(),target=new Vector3(),forward=new Vector3(),velocityVector=new Vector3(),look=new Vector3(),rayDirection=new Vector3()
function Wheel({body}:{body:React.RefObject<Group>}) {
 useCompoundBody(()=>({mass:30,type:'Kinematic',collisionFilterGroup:0,shapes:[{type:'Cylinder',args:[dimensions.radius,dimensions.radius,.24,12],rotation:[0,0,-Math.PI/2]}]}),body)
 return <group ref={body}/>
}
function shadowTexture(){const c=document.createElement('canvas');c.width=c.height=64;const x=c.getContext('2d')!;const g=x.createRadialGradient(32,32,8,32,32,31);g.addColorStop(0,'rgba(12,18,20,.45)');g.addColorStop(.55,'rgba(12,18,20,.25)');g.addColorStop(1,'rgba(12,18,20,0)');x.fillStyle=g;x.fillRect(0,0,64,64);return new CanvasTexture(c)}
export function Car(){
 const quality=useStore(s=>s.quality),reset=useStore(s=>s.reset),cameraMode=useStore(s=>s.camera)
 const camera=useThree(s=>s.camera) as PerspectiveCamera
 useEffect(()=>{camera.fov=cameraMode===2?70:55;camera.near=cameraMode===2?.025:.1;camera.updateProjectionMatrix()},[camera,cameraMode])
 const {scene}=useModel('/models/roadster-'+quality+'.glb')
 const brakeMaterials=useRef<MeshStandardMaterial[]>([])
 const models=useMemo(()=>{brakeMaterials.current=[];const clone=scene.clone(true);clone.traverse(o=>{if(!(o instanceof Mesh))return;o.castShadow=quality==='high';o.receiveShadow=false;const original=o.material as MeshStandardMaterial;let m=original.clone();
  if(o.name==='Body_Material.001_0'){m=new MeshPhysicalMaterial({color:original.color,metalness:.65,roughness:.23,clearcoat:1,clearcoatRoughness:.22,envMapIntensity:1.25})}
  if(/^(Disc|Maxs)/.test(o.name)){m.color.set('#aeb8c1');m.metalness=.82;m.roughness=.28}
  if(o.name.startsWith('Tires')){m.roughness=.95;m.metalness=.05}
  if(o.name.startsWith('Glass')){m.color.set('#829d9f');m.transparent=true;m.opacity=o.name.includes('.015')?.4:.2;m.depthWrite=false;m.roughness=.13;m.metalness=.08;m.envMapIntensity=.8}
  if(o.name.includes('side Light_Material.016')){m.emissive.set('#ff1810');m.emissiveIntensity=.35;brakeMaterials.current.push(m)}
  if(o.name.includes('Headlight_Material.004')){m.emissive.set('#fff6d8');m.emissiveIntensity=.8}
  o.material=m;
 });return [clone.getObjectByName('Chassis')!,...Array.from({length:4},(_,i)=>clone.getObjectByName('Wheel_'+i)!)] as Group[]},[scene,quality])
 const [body,bodyApi]=useBox(()=>({mass:CHASSIS.mass,args:CHASSIS.size,position:start,rotation,allowSleep:false,linearDamping:CHASSIS.linearDamping,angularDamping:CHASSIS.angularDamping}),useRef<Group>(null))
 const wheels=useMemo(()=>Array.from({length:4},()=>createRef<Group>()),[])
 const [,api]=useRaycastVehicle(()=>({chassisBody:body,wheels,wheelInfos:wheelOptions(dimensions.centers,dimensions.radius)}))
 const velocity=useRef([0,0,0]),steer=useRef(0),previous=useRef(new Vector3(...start)),fresh=useRef(true),shadow=useRef<Mesh>(null),ray=useRef(new Raycaster()),rayTime=useRef(0),cameraLimit=useRef(30)
 const visual=useRef<Group>(null),visualWheels=useMemo(()=>Array.from({length:4},()=>createRef<Group>()),[])
 const followOffset=useRef(new Vector3()),invQ=useRef(new Quaternion()),wheelQ=useRef(new Quaternion()),wheelP=useRef(new Vector3())
 const lastMode=useRef(-1)
 const overturned=useRef(0)
 const groundY=useRef(0)
 const blob=useMemo(shadowTexture,[])
 useEffect(()=>()=>blob.dispose(),[blob])
 useEffect(()=>bodyApi.velocity.subscribe(v=>{velocity.current=v;telemetry.speed=Math.hypot(...v)}),[bodyApi])
 useEffect(()=>{bodyApi.position.set(...useStore.getState().spawn);bodyApi.rotation.set(...rotation);bodyApi.velocity.set(0,0,0);bodyApi.angularVelocity.set(0,0,0);steer.current=0;fresh.current=true;previous.current.set(...useStore.getState().spawn)},[reset,bodyApi])
 useEffect(()=>{useStore.getState().set({loaded:true})},[])
 useFrame(({camera},dt)=>{
  if(!body.current||!visual.current)return;const s=useStore.getState(),active=s.started&&!s.paused,delta=Math.min(dt,.1)
  if(s.paused&&!fresh.current)return
  body.current.getWorldPosition(pos);body.current.getWorldQuaternion(q);telemetry.position=[pos.x,pos.y,pos.z]
  forward.set(0,0,1).applyQuaternion(q);const signed=forward.dot(velocityVector.set(...velocity.current as [number,number,number]));telemetry.signedSpeed=signed;telemetry.heading=Math.atan2(forward.x,forward.z);telemetry.lateralSpeed=velocityVector.x*forward.z-velocityVector.z*forward.x;telemetry.height=pos.y;telemetry.wheelRotation+=signed*delta/dimensions.radius;if(import.meta.env.DEV){telemetry.wheelQuaternions=wheels.map(w=>w.current?.getWorldQuaternion(new Quaternion()).toArray()||[]);(window as unknown as {roadsterTelemetry:typeof telemetry}).roadsterTelemetry=telemetry}
  const direction=Math.max(-1,Math.min(1,steering.value+(down('left')?1:0)-(down('right')?1:0)))
  steer.current=MathUtils.damp(steer.current,active?direction*.5/(1+telemetry.speed*.024):0,10,delta)
  const f=driveForces(active,down('forward'),down('brake'),s.handbrake,s.gear,signed)
  telemetry.steeringAngle=steer.current
  for(let i=0;i<4;i++){api.applyEngineForce(f.force,i);api.setBrake(i<2?f.frontBrake:f.rearBrake,i);if(i<2)api.setSteeringValue(steer.current,i)}
  for(const m of brakeMaterials.current)m.emissiveIntensity=down('brake')||s.handbrake?2:.35
  if(active){overturned.current=new Vector3(0,1,0).applyQuaternion(q).y<.1?overturned.current+dt:0;if(overturned.current>2.5){overturned.current=0;useStore.getState().set({reset:s.reset+1,hint:'Машина восстановлена после переворота'})}telemetry.distance+=Math.min(pos.distanceTo(previous.current),10);if(pos.y< -12||Math.abs(pos.x)>185||Math.abs(pos.z)>185)useStore.getState().set({reset:s.reset+1})}previous.current.copy(pos)
  const snap=fresh.current,modeChanged=lastMode.current!==s.camera
  smoothPose(visual.current.position,visual.current.quaternion,pos,q,delta,snap,s.paused)
  invQ.current.copy(q).invert()
  for(let i=0;i<4;i++){const proxy=wheels[i].current,view=visualWheels[i].current;if(!proxy||!view)continue
   proxy.getWorldPosition(wheelP.current).sub(pos).applyQuaternion(invQ.current)
   proxy.getWorldQuaternion(wheelQ.current).premultiply(invQ.current)
   if(snap){view.position.copy(wheelP.current);view.quaternion.copy(wheelQ.current)}else{view.position.lerp(wheelP.current,blend(32,delta));view.quaternion.slerp(wheelQ.current,blend(40,delta))}
  }
  const displayPos=visual.current.position,displayQ=visual.current.quaternion
  forward.set(0,0,1).applyQuaternion(displayQ)
  const heading=Math.atan2(forward.x,forward.z),angle=heading+Math.PI+orbit.yaw,flat=orbit.distance*Math.cos(orbit.pitch)
  offset.set(Math.sin(angle)*flat,Math.sin(orbit.pitch)*orbit.distance,Math.cos(angle)*flat);look.copy(displayPos);look.y+=.45
  rayTime.current+=dt
  if(rayTime.current>.10){rayTime.current=0;rayDirection.copy(offset).normalize();ray.current.set(look,rayDirection);ray.current.far=orbit.distance;const hit=ray.current.intersectObjects(worldSurfaces,false)[0];cameraLimit.current=hit?Math.max(1.5,hit.distance-.35):orbit.distance
   ray.current.set(wheelP.current.set(displayPos.x,displayPos.y+1,displayPos.z),rayDirection.set(0,-1,0));ray.current.far=30;const ground=ray.current.intersectObjects(worldSurfaces,false)[0];telemetry.groundClearance=ground?pos.y-ground.point.y:30;if(shadow.current)shadow.current.visible=!!ground&&telemetry.groundClearance<5;if(ground)groundY.current=ground.point.y+.025
  }
  if(shadow.current){shadow.current.position.set(displayPos.x,groundY.current,displayPos.z);shadow.current.rotation.set(-Math.PI/2,0,-heading)}
  if(s.camera===2){
   camera.position.copy(wheelP.current.set(-.34,.37,-.23).applyQuaternion(displayQ).add(displayPos))
   const yaw=Math.max(-1.3,Math.min(1.3,orbit.yaw));rayDirection.set(Math.sin(yaw),(.38-orbit.pitch)*.7,Math.cos(yaw)).normalize().applyQuaternion(displayQ)
   camera.up.set(0,1,0);camera.lookAt(target.copy(camera.position).add(rayDirection))
  }else{
   offset.setLength(Math.min(orbit.distance,cameraLimit.current))
   if(snap||modeChanged)followOffset.current.copy(offset);else followOffset.current.lerp(offset,blend(orbit.dragging?18:8,delta))
   camera.position.copy(look).add(followOffset.current);camera.up.set(0,1,0);camera.lookAt(look)
  }
  telemetry.renderPosition=displayPos.toArray();telemetry.cameraPosition=camera.position.toArray();telemetry.cameraQuaternion=camera.quaternion.toArray()
  fresh.current=false;lastMode.current=s.camera

 })
 return <><group ref={body}/>{wheels.map((w,i)=><Wheel key={i} body={w}/>)}
 <group ref={visual}><primitive object={models[0]}/><Cabin/>{quality==='mobile'&&<group><mesh castShadow position={[0,-.06,0]}><boxGeometry args={[1.65,.38,3.85]}/><meshBasicMaterial colorWrite={false} depthWrite={false}/></mesh><mesh castShadow position={[0,.29,-.15]}><boxGeometry args={[1.15,.35,1.3]}/><meshBasicMaterial colorWrite={false} depthWrite={false}/></mesh></group>}{visualWheels.map((w,i)=><group key={i} ref={w}><primitive object={models[i+1]}/></group>)}</group>
 <mesh ref={shadow} rotation={[-Math.PI/2,0,0]} renderOrder={1}><planeGeometry args={[3.5,5.6]}/><meshBasicMaterial map={blob} transparent depthWrite={false} polygonOffset polygonOffsetFactor={-2}/></mesh></>
}
