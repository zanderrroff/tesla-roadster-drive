import {useMemo,useRef,useEffect,createRef} from 'react'
import {useFrame} from '@react-three/fiber'
import {useBox,useCompoundBody,useRaycastVehicle} from '@react-three/cannon'
import {Group,Mesh,Vector3,Quaternion,MathUtils,MeshStandardMaterial} from 'three'
import {useModel} from './loaders'
import {down,telemetry,useStore} from './store'
import dimensions from './vehicle-dimensions.json'
const start:[number,number,number]=[-110,1.4,220], rotation:[number,number,number]=[0,Math.PI/2+.35,0]
const pos=new Vector3(),q=new Quaternion(),offset=new Vector3(),target=new Vector3(),forward=new Vector3()
function Wheel({body,model}:{body:React.RefObject<Group>;model:Group}){
 useCompoundBody(()=>({mass:30,type:'Kinematic',collisionFilterGroup:0,shapes:[{type:'Cylinder',args:[dimensions.radius,dimensions.radius,.24,12],rotation:[0,0,-Math.PI/2]}]}),body)
 return <group ref={body}><primitive object={model}/></group>
}
export function Car(){
 const quality=useStore(s=>s.quality),reset=useStore(s=>s.reset)
 const {scene}=useModel('/models/roadster-'+quality+'.glb')
 const models=useMemo(()=>{const clone=scene.clone(true);clone.traverse(o=>{if(o instanceof Mesh){o.castShadow=true;o.receiveShadow=false}});return [clone.getObjectByName('Chassis')!,...Array.from({length:4},(_,i)=>clone.getObjectByName('Wheel_'+i)!)] as Group[]},[scene])
 const [body,bodyApi]=useBox(()=>({mass:650,args:[1.82,.8,4.18],position:start,rotation,allowSleep:false,linearDamping:.06,angularDamping:.4}),useRef<Group>(null))
 const wheels=useMemo(()=>Array.from({length:4},()=>createRef<Group>()),[])
 const [,api]=useRaycastVehicle(()=>({chassisBody:body,wheels,wheelInfos:dimensions.centers.map((v,i)=>({chassisConnectionPointLocal:[v[0],v[1]+.18,v[2]],isFrontWheel:i<2,axleLocal:[-1,0,0],directionLocal:[0,-1,0],radius:dimensions.radius,suspensionRestLength:.18,suspensionStiffness:38,dampingRelaxation:3,dampingCompression:4.4,frictionSlip:2.1,rollInfluence:.04,maxSuspensionForce:100000,customSlidingRotationalSpeed:-.01,useCustomSlidingRotationalSpeed:true}))}))
 const velocity=useRef([0,0,0]); const steer=useRef(0);const previous=useRef(new Vector3(...start));const fresh=useRef(true)
 useEffect(()=>bodyApi.velocity.subscribe(v=>{velocity.current=v;telemetry.speed=Math.hypot(...v)}),[bodyApi])
 useEffect(()=>{bodyApi.position.set(...start);bodyApi.rotation.set(...rotation);bodyApi.velocity.set(0,0,0);bodyApi.angularVelocity.set(0,0,0);steer.current=0;fresh.current=true;previous.current.set(...start)},[reset,bodyApi])
 useEffect(()=>{useStore.getState().set({loaded:true})},[])
 useFrame(({camera},dt)=>{
  if(!body.current)return;const s=useStore.getState(),active=s.started&&!s.paused;const delta=Math.min(dt,.05)
  body.current.getWorldPosition(pos);body.current.getWorldQuaternion(q);telemetry.position=[pos.x,pos.y,pos.z]
  forward.set(0,0,1).applyQuaternion(q);const signed=forward.dot(new Vector3(...velocity.current));
  steer.current=MathUtils.damp(steer.current,active?((down('left')?1:0)-(down('right')?1:0))*.43/(1+telemetry.speed*.022):0,9,delta)
  for(let i=0;i<4;i++){let force=0;if(active&&down('forward')&&signed<58)force=-1900;if(active&&down('backward')&&signed>-12)force=1100;api.applyEngineForce(force,i);api.setBrake(active?(down('brake')?70:down('handbrake')&&i>=2?95:0):0,i);if(i<2)api.setSteeringValue(steer.current,i)}
  if(active){telemetry.distance+=Math.min(pos.distanceTo(previous.current),10);if(pos.y< -12||Math.abs(pos.x)>270||Math.abs(pos.z)>270)useStore.getState().set({reset:s.reset+1})}previous.current.copy(pos)
  offset.set(0,s.camera?15:3.3,s.camera?-13:-7.4).applyQuaternion(q);offset.y=Math.max(offset.y,s.camera?12:2.8);target.copy(pos).add(offset)
  if(fresh.current){camera.position.copy(target);fresh.current=false}else camera.position.lerp(target,1-Math.exp(-4*delta))
  camera.lookAt(pos.x,pos.y+.4,pos.z)
 });
 return <><group ref={body}><primitive object={models[0]}/></group>{wheels.map((w,i)=><Wheel key={i} body={w} model={models[i+1]}/>)}</>
}
