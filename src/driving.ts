export type Gear = 'D' | 'N' | 'R'
export function canSelectGear(current:Gear,next:Gear,speed:number){return current===next||next==='N'||Math.abs(speed)<1.5}
export function driveForces(active:boolean,throttle:boolean,brake:boolean,handbrake:boolean,gear:Gear,signedSpeed:number){
 const stopped=Math.abs(signedSpeed)<.8
 let force=0
 if(active&&throttle&&!brake&&!handbrake){if(gear==='D'&&signedSpeed<58)force=-1500*Math.max(.26,1-Math.max(0,signedSpeed)/78);if(gear==='R'&&signedSpeed> -10)force=800}
 const service=active&&brake?12:0,parking=active&&handbrake?18:0
 return {force,frontBrake:Math.max(service,stopped?parking:0),rearBrake:Math.max(service,parking)}
}
// Arcade pedals: braking first; reverse requires a continuous stopped hold.
export function pedalIntent(throttle:boolean,brake:boolean,signedSpeed:number,stoppedFor:number,automatic:boolean,gear:Gear){
 if(!automatic)return {throttle,brake,gear}
 if(throttle&&brake)return {throttle:false,brake:true,gear}
 if(brake){if(signedSpeed>.65||stoppedFor<.35&&signedSpeed>-.3)return {throttle:false,brake:true,gear:'D' as Gear};return {throttle:true,brake:false,gear:'R' as Gear}}
 if(throttle){if(signedSpeed<-.65)return {throttle:false,brake:true,gear:'R' as Gear};return {throttle:true,brake:false,gear:'D' as Gear}}
 return {throttle:false,brake:false,gear}
}
