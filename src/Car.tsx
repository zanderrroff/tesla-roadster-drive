import {useMemo,useRef,useEffect,createRef} from 'react'
import {useFrame,useThree} from '@react-three/fiber'
import {useBox,useCompoundBody,useRaycastVehicle} from '@react-three/cannon'
import {Group,Mesh,Vector3,Quaternion,MathUtils,MeshStandardMaterial,MeshPhysicalMaterial,CanvasTexture,Raycaster,PerspectiveCamera} from 'three'
import {useModel} from './loaders'
import {down,telemetry,useStore,steering,orbit} from './store'
import {driveForces,pedalIntent} from './driving'
import {worldSurfaces} from './World'
import {vehicleById} from './vehicles'
import {SPAWN} from './playground-layout'
import {CHASSIS,wheelOptions} from './vehicle-physics'
import {Cabin} from './Cabin'
import {smoothPose,blend} from './motion'
const start=SPAWN, rotation:[number,number,number]=[0,0,0]
const pos=new Vector3(),q=new Quaternion(),offset=new Vector3(),target=new Vector3(),forward=new Vector3(),velocityVector=new Vector3(),look=new Vector3(),rayDirection=new Vector3()
function Wheel({body,radius}:{body:React.RefObject<Group>;radius:number}) {
 useCompoundBody(()=>({mass:30,type:'Kinematic',collisionFilterGroup:0,shapes:[{type:'Cylinder',args:[radius,radius,.24,12],rotation:[0,0,-Math.PI/2]}]}),body)
 return <group ref={body}/>
}
function shadowTexture(){const c=document.createElement('canvas');c.width=c.height=64;const x=c.getContext('2d')!;const g=x.createRadialGradient(32,32,8,32,32,31);g.addColorStop(0,'rgba(12,18,20,.45)');g.addColorStop(.55,'rgba(12,18,20,.25)');g.addColorStop(1,'rgba(12,18,20,0)');x.fillStyle=g;x.fillRect(0,0,64,64);return new CanvasTexture(c)}
export function Car(){
 const id=useStore(s=>s.vehicle),spec=vehicleById(id),dimensions=spec
 const quality=useStore(s=>s.quality),reset=useStore(s=>s.reset),cameraMode=useStore(s=>s.camera)
 const camera=useThree(s=>s.camera) as PerspectiveCamera,viewport=useThree(s=>s.size),started=useStore(s=>s.started),garage=useStore(s=>s.garage)
 useEffect(()=>{if(!started||garage)camera.setViewOffset(viewport.width,viewport.height,-viewport.width*.17,0,viewport.width,viewport.height);else camera.clearViewOffset()},[camera,viewport,started,garage])
 useEffect(()=>{camera.fov=cameraMode===2?70:55;camera.near=cameraMode===2?.025:.1;camera.updateProjectionMatrix()},[camera,cameraMode])
 const {scene}=useModel(spec.id==='roadster'?'/models/roadster-'+(quality==='mobile'?'mobile':'high')+'.glb':spec.file)
 const brakeMaterials=useRef<MeshStandardMaterial[]>([])
 const models=useMemo(()=>{brakeMaterials.current=[];const clone=scene.clone(true);clone.traverse(o=>{if(!(o instanceof Mesh))return;o.castShadow=quality!=='mobile';o.receiveShadow=false;const original=o.material as MeshStandardMaterial;let m=original.clone();
  if(o.name==='Body_Material.001_0'){m=new MeshPhysicalMaterial({color:original.color,metalness:.65,roughness:.23,clearcoat:1,clearcoatRoughness:.22,envMapIntensity:1.25})}
  if(/^(Disc|Maxs)/.test(o.name)){m.color.set('#aeb8c1');m.metalness=.82;m.roughness=.28}
  if(o.name.startsWith('Tires')){m.roughness=.95;m.metalness=.05}
  if(o.name.startsWith('Glass')){m.color.set('#829d9f');m.transparent=true;m.opacity=o.name.includes('.015')?.4:.2;m.depthWrite=false;m.roughness=.13;m.metalness=.08;m.envMapIntensity=.8}
  if(o.name.includes('side Light_Material.016')){m.emissive.set('#ff1810');m.emissiveIntensity=.35;brakeMaterials.current.push(m)}
  if(o.name.includes('Headlight_Material.004')){m.emissive.set('#fff6d8');m.emissiveIntensity=.8}
  if(m.transparent)m.depthWrite=false;
  o.material=m;
 });return [clone.getObjectByName('Chassis')!,...Array.from({length:4},(_,i)=>clone.getObjectByName('Wheel_'+i)!)] as Group[]},[scene,quality])
 const [body,bodyApi]=useBox(()=>({mass:CHASSIS.mass,args:spec.size,position:start,rotation,allowSleep:false,onCollide:e=>{if(e.contact.impactVelocity>2)telemetry.impact=performance.now()},linearDamping:CHASSIS.linearDamping,angularDamping:CHASSIS.angularDamping}),useRef<Group>(null))
 const wheels=useMemo(()=>Array.from({length:4},()=>createRef<Group>()),[])
 const [,api]=useRaycastVehicle(()=>({chassisBody:body,wheels,wheelInfos:wheelOptions(dimensions.centers,dimensions.radius)}))
 const velocity=useRef([0,0,0]),steer=useRef(0),previous=useRef(new Vector3(...start)),fresh=useRef(true),shadow=useRef<Mesh>(null),ray=useRef(new Raycaster()),rayTime=useRef(0),cameraLimit=useRef(30)
 const visual=useRef<Group>(null),visualWheels=useMemo(()=>Array.from({length:4},()=>createRef<Group>()),[])
 const followOffset=useRef(new Vector3()),invQ=useRef(new Quaternion()),wheelQ=useRef(new Quaternion()),wheelP=useRef(new Vector3())
 const lastMode=useRef(-1),lastPreview=useRef(false)
 const overturned=useRef(0),stoppedFor=useRef(0),throttle=useRef(0),yawVelocity=useRef([0,0,0])
 const groundY=useRef(0)
 const blob=useMemo(shadowTexture,[])
 useEffect(()=>()=>blob.dispose(),[blob])
 useEffect(()=>bodyApi.angularVelocity.subscribe(v=>{yawVelocity.current=v}),[bodyApi])
 useEffect(()=>bodyApi.velocity.subscribe(v=>{velocity.current=v;telemetry.speed=Math.hypot(...v)}),[bodyApi])
 useEffect(()=>{bodyApi.position.set(...useStore.getState().spawn);bodyApi.rotation.set(...rotation);bodyApi.velocity.set(0,0,0);bodyApi.angularVelocity.set(0,0,0);steer.current=0;fresh.current=true;previous.current.set(...useStore.getState().spawn)},[reset,bodyApi])
 useEffect(()=>{useStore.getState().set({loaded:true})},[])
 useFrame((_,dt)=>{
  if(!body.current||!visual.current)return;const s=useStore.getState(),active=s.started&&!s.paused,delta=Math.min(dt,.1)
  if(s.paused&&!fresh.current&&!s.garage)return
  body.current.getWorldPosition(pos);body.current.getWorldQuaternion(q);telemetry.position=[pos.x,pos.y,pos.z]
  forward.set(0,0,1).applyQuaternion(q);const signed=forward.dot(velocityVector.set(...velocity.current as [number,number,number]));telemetry.signedSpeed=signed;telemetry.heading=Math.atan2(forward.x,forward.z);telemetry.lateralSpeed=velocityVector.x*forward.z-velocityVector.z*forward.x;telemetry.height=pos.y;telemetry.wheelRotation+=signed*delta/dimensions.radius;if(import.meta.env.DEV){telemetry.wheelQuaternions=wheels.map(w=>w.current?.getWorldQuaternion(new Quaternion()).toArray()||[]);(window as unknown as {roadsterTelemetry:typeof telemetry}).roadsterTelemetry=telemetry}
  const direction=Math.max(-1,Math.min(1,steering.value+(down('left')?1:0)-(down('right')?1:0)))
  steer.current=MathUtils.damp(steer.current,active?direction*.48/(1+telemetry.speed*.045):0,direction===0?9:6.5,delta)
  stoppedFor.current=down('brake')&&Math.abs(signed)<.65?stoppedFor.current+delta:0
  const intent=pedalIntent(down('forward'),down('brake'),signed,stoppedFor.current,s.autoReverse,s.gear)
  if(active&&intent.gear!==s.gear)s.set({gear:intent.gear})
  throttle.current=MathUtils.damp(throttle.current,active&&intent.throttle&&!s.handbrake?1:0,4.5,delta)
  const f=driveForces(active,intent.throttle,intent.brake,s.handbrake,intent.gear,signed)
  f.force*=spec.power*throttle.current;telemetry.throttle=throttle.current;telemetry.braking=intent.brake
  if(active&&s.assist&&!s.handbrake&&telemetry.groundClearance<.85&&telemetry.speed>3){const lateral=telemetry.lateralSpeed;bodyApi.applyForce([-lateral*180*forward.z,0,lateral*180*forward.x],[0,0,0]);if(Math.abs(yawVelocity.current[1])>1.1)bodyApi.angularVelocity.set(yawVelocity.current[0],MathUtils.damp(yawVelocity.current[1],Math.sign(yawVelocity.current[1])*1.1,3,delta),yawVelocity.current[2])}
  telemetry.steeringAngle=steer.current
  for(let i=0;i<4;i++){api.applyEngineForce(f.force,i);api.setBrake(i<2?f.frontBrake:f.rearBrake,i);if(i<2)api.setSteeringValue(steer.current,i)}
  for(const m of brakeMaterials.current)m.emissiveIntensity=intent.brake||s.handbrake?2:.35
  if(active){overturned.current=new Vector3(0,1,0).applyQuaternion(q).y<.1?overturned.current+dt:0;if(overturned.current>2.5){overturned.current=0;useStore.getState().set({reset:s.reset+1,hint:'Машина восстановлена после переворота'})}telemetry.distance+=Math.min(pos.distanceTo(previous.current),10);if(pos.y< -12||Math.abs(pos.x)>185||Math.abs(pos.z)>185)useStore.getState().set({reset:s.reset+1})}previous.current.copy(pos)
  const snap=fresh.current,previewing=!s.started||s.garage,modeChanged=lastMode.current!==s.camera
  smoothPose(visual.current.position,visual.current.quaternion,pos,q,delta,snap,s.paused)
  invQ.current.copy(q).invert()
  for(let i=0;i<4;i++){const proxy=wheels[i].current,view=visualWheels[i].current;if(!proxy||!view)continue
   if(previewing){wheelP.current.fromArray(dimensions.centers[i]);wheelQ.current.identity()}else{proxy.getWorldPosition(wheelP.current).sub(pos).applyQuaternion(invQ.current);proxy.getWorldQuaternion(wheelQ.current).premultiply(invQ.current)}
   if(snap){view.position.copy(wheelP.current);view.quaternion.copy(wheelQ.current)}else{view.position.lerp(wheelP.current,blend(32,delta));view.quaternion.slerp(wheelQ.current,blend(40,delta))}
  }
  const displayPos=visual.current.position,displayQ=visual.current.quaternion
  forward.set(0,0,1).applyQuaternion(displayQ)
  const preview=!s.started||s.garage;if(!preview&&lastPreview.current){camera.clearViewOffset();lastPreview.current=false}const viewYaw=preview?-.8:orbit.yaw,viewPitch=preview?.22:orbit.pitch,viewDistance=preview?6.1:orbit.distance
  const heading=Math.atan2(forward.x,forward.z),angle=heading+Math.PI+viewYaw,flat=viewDistance*Math.cos(viewPitch)
  offset.set(Math.sin(angle)*flat,Math.sin(viewPitch)*viewDistance,Math.cos(angle)*flat);look.copy(displayPos);look.y+=.45
  rayTime.current+=dt
  if(rayTime.current>.10){rayTime.current=0;rayDirection.copy(offset).normalize();ray.current.set(look,rayDirection);ray.current.far=orbit.distance;const hit=ray.current.intersectObjects(worldSurfaces,false)[0];cameraLimit.current=hit?Math.max(1.5,hit.distance-.35):orbit.distance
   ray.current.set(wheelP.current.set(displayPos.x,displayPos.y+1,displayPos.z),rayDirection.set(0,-1,0));ray.current.far=30;const ground=ray.current.intersectObjects(worldSurfaces,false)[0];telemetry.groundClearance=ground?pos.y-ground.point.y:30;if(shadow.current)shadow.current.visible=!!ground&&telemetry.groundClearance<5;if(ground)groundY.current=ground.point.y+.025
  }
  if(shadow.current){shadow.current.position.set(displayPos.x,groundY.current,displayPos.z);shadow.current.rotation.set(-Math.PI/2,0,-heading)}
  if(s.camera===2){
   camera.position.copy(wheelP.current.fromArray(spec.hasInterior?(spec.cockpitEye||[.34,.4,-.3]):[.34,.37,-.4]).applyQuaternion(displayQ).add(displayPos))
   const yaw=Math.max(-1.3,Math.min(1.3,orbit.yaw));rayDirection.set(Math.sin(yaw),(.38-orbit.pitch)*.7-.035,Math.cos(yaw)).normalize().applyQuaternion(displayQ)
   camera.up.set(0,1,0);camera.lookAt(target.copy(camera.position).add(rayDirection))
  }else{
   offset.setLength(preview?viewDistance:Math.min(orbit.distance,cameraLimit.current))
   if(snap||modeChanged)followOffset.current.copy(offset);else followOffset.current.lerp(offset,blend(orbit.dragging?18:8,delta))
   camera.position.copy(look).add(followOffset.current);camera.up.set(0,1,0);camera.lookAt(look);if(preview!==lastPreview.current){if(preview)camera.setViewOffset(innerWidth,innerHeight,-innerWidth*.17,0,innerWidth,innerHeight);else camera.clearViewOffset();lastPreview.current=preview}
  }
  telemetry.slip=Math.abs(telemetry.lateralSpeed);telemetry.renderPosition=displayPos.toArray();telemetry.cameraPosition=camera.position.toArray();telemetry.cameraQuaternion=camera.quaternion.toArray()
  const desiredFov=s.camera===2?70:55+Math.min(9,telemetry.speed*.22);if(Math.abs(camera.fov-desiredFov)>.01){camera.fov=MathUtils.damp(camera.fov,desiredFov,3,delta);camera.updateProjectionMatrix()}
  fresh.current=false;lastMode.current=s.camera

 })
 return <><group ref={body}/>{wheels.map((w,i)=><Wheel key={i} body={w} radius={dimensions.radius}/>)}
 <group ref={visual}><primitive object={models[0]}/><Cabin shell={!spec.hasInterior} vehicleId={id}/>{quality==='mobile'&&<group><mesh castShadow position={[0,-.06,0]}><boxGeometry args={[1.65,.38,3.85]}/><meshBasicMaterial colorWrite={false} depthWrite={false}/></mesh><mesh castShadow position={[0,.29,-.15]}><boxGeometry args={[1.15,.35,1.3]}/><meshBasicMaterial colorWrite={false} depthWrite={false}/></mesh></group>}{visualWheels.map((w,i)=><group key={i} ref={w}><primitive object={models[i+1]}/></group>)}</group>
 <mesh ref={shadow} rotation={[-Math.PI/2,0,0]} renderOrder={1}><planeGeometry args={[3.5,5.6]}/><meshBasicMaterial map={blob} transparent depthWrite={false} polygonOffset polygonOffsetFactor={-2}/></mesh></>
}
