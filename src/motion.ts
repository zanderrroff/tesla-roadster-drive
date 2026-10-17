import {Quaternion,Vector3} from 'three'
export const blend=(rate:number,dt:number)=>1-Math.exp(-rate*Math.max(0,Math.min(dt,.1)))
export function smoothPose(position:Vector3,quaternion:Quaternion,target:Vector3,rotation:Quaternion,dt:number,snap:boolean,paused:boolean){
 if(paused&&!snap)return
 if(snap||position.distanceToSquared(target)>400){position.copy(target);quaternion.copy(rotation);return}
 position.lerp(target,blend(24,dt));quaternion.slerp(rotation,blend(18,dt))
}
