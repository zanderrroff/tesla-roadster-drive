import create from 'zustand'
import {SPAWN} from './playground-layout'
import {canSelectGear} from './driving'
import type {Gear} from './driving'
export type Control = 'forward' | 'left' | 'right' | 'brake'
export const input: Record<Control, Set<string>> = {forward:new Set(),left:new Set(),right:new Set(),brake:new Set()}
export const inputState={handbrake:false,horn:false}
export const steering = {value:0}
export const orbit = {yaw:0,pitch:.38,distance:7.8,dragging:false,manual:false}
export const down = (c:Control) => input[c].size > 0
export const clearInput = () => {Object.values(input).forEach(s=>s.clear());steering.value=0;orbit.dragging=false;inputState.handbrake=false;inputState.horn=false}
const saved=(()=>{try{return JSON.parse(localStorage.getItem('roadster-settings')||'{}')}catch{return {}}})()
export type Quality='mobile'|'high'|'ultra'
interface State {
 spawn:[number,number,number];started:boolean;paused:boolean;loaded:boolean;camera:number;sound:boolean;quality:Quality;
 reset:number;about:boolean;gear:Gear;handbrake:boolean;steeringMode:'wheel'|'arrows';hint:string;vehicle:string;garage:boolean;volume:number;assist:boolean;timeOfDay:'day'|'sunset';sensitivity:number;autoReverse:boolean;
 set:(patch:Partial<State>)=>void
}
export const useStore=create<State>(set=>({spawn:SPAWN,started:false,paused:false,loaded:false,camera:0,sound:saved.sound??true,
 quality:['high','ultra'].includes(saved.quality)?saved.quality:'mobile',reset:0,about:false,gear:'D',handbrake:false,
 steeringMode:saved.schema===14&&saved.steeringMode==='wheel'?'wheel':'arrows',hint:'',vehicle:saved.vehicle||'roadster',garage:false,volume:saved.volume??.65,assist:saved.assist??true,timeOfDay:saved.timeOfDay==='day'?'day':'sunset',sensitivity:saved.sensitivity??1,autoReverse:true,set:patch=>set(s=>({...s,...patch}))}))
export const telemetry={speed:0,signedSpeed:0,distance:0,position:[0,0,0],fps:0,dpr:1,heading:0,lateralSpeed:0,height:0,groundClearance:0,wheelRotation:0,steeringAngle:0,renderPosition:[0,0,0],cameraPosition:[0,0,0],cameraQuaternion:[0,0,0,1],wheelQuaternions:[] as number[][],throttle:0,braking:false,slip:0,impact:0,rpm:900,renderCalls:0,renderTriangles:0}
export function pause(){clearInput();useStore.getState().set({paused:true,handbrake:false})}
export function resetView(){orbit.yaw=0;orbit.pitch=useStore.getState().camera===1? .95:.38;orbit.distance=useStore.getState().camera===1?15:7.8;orbit.manual=false}
export function resetCar(){clearInput();resetView();useStore.getState().set({reset:useStore.getState().reset+1,gear:'D',handbrake:false,hint:''})}
export function selectGear(gear:Gear){const s=useStore.getState();if(!canSelectGear(s.gear,gear,telemetry.speed)){s.set({hint:'Остановитесь, чтобы переключить направление'});return false}input.forward.clear();s.set({gear,hint:gear==='R'?'Задний ход: удерживайте педаль газа':gear==='N'?'Нейтральная передача':''});return true}
export function toggleHandbrake(){const s=useStore.getState();s.set({handbrake:!s.handbrake,hint:s.handbrake?'Ручник отпущен':'Ручник включён — нажмите ещё раз, чтобы отпустить'})}
useStore.subscribe(s=>{try{localStorage.setItem('roadster-settings',JSON.stringify({schema:14,sound:s.sound,quality:s.quality,steeringMode:s.steeringMode,vehicle:s.vehicle,volume:s.volume,assist:s.assist,timeOfDay:s.timeOfDay,sensitivity:s.sensitivity}))}catch{}})
