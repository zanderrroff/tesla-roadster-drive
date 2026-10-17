import create from 'zustand'
import {canSelectGear} from './driving'
import type {Gear} from './driving'
export type Control = 'forward' | 'left' | 'right' | 'brake'
export const input: Record<Control, Set<string>> = {forward:new Set(),left:new Set(),right:new Set(),brake:new Set()}
export const steering = {value:0}
export const orbit = {yaw:0,pitch:.38,distance:7.8,dragging:false,manual:false}
export const down = (c:Control) => input[c].size > 0
export const clearInput = () => {Object.values(input).forEach(s=>s.clear());steering.value=0;orbit.dragging=false}
const saved=(()=>{try{return JSON.parse(localStorage.getItem('roadster-settings')||'{}')}catch{return {}}})()
export type Quality='mobile'|'high'
interface State {
 started:boolean;paused:boolean;loaded:boolean;camera:number;sound:boolean;quality:Quality;
 reset:number;about:boolean;gear:Gear;handbrake:boolean;steeringMode:'wheel'|'arrows';hint:string;
 set:(patch:Partial<State>)=>void
}
export const useStore=create<State>(set=>({started:false,paused:false,loaded:false,camera:0,sound:saved.sound??true,
 quality:saved.quality==='high'?'high':'mobile',reset:0,about:false,gear:'D',handbrake:false,
 steeringMode:saved.steeringMode==='arrows'?'arrows':'wheel',hint:'',set:patch=>set(s=>({...s,...patch}))}))
export const telemetry={speed:0,signedSpeed:0,distance:0,position:[0,0,0],fps:0,dpr:1}
export function pause(){clearInput();useStore.getState().set({paused:true})}
export function resetView(){orbit.yaw=0;orbit.pitch=useStore.getState().camera? .95:.38;orbit.distance=useStore.getState().camera?15:7.8;orbit.manual=false}
export function resetCar(){clearInput();resetView();useStore.getState().set({reset:useStore.getState().reset+1,gear:'D',handbrake:false,hint:''})}
export function selectGear(gear:Gear){const s=useStore.getState();if(!canSelectGear(s.gear,gear,telemetry.speed)){s.set({hint:'Остановитесь, чтобы переключить направление'});return false}input.forward.clear();s.set({gear,hint:gear==='R'?'Задний ход: удерживайте педаль газа':gear==='N'?'Нейтральная передача':''});return true}
export function toggleHandbrake(){const s=useStore.getState();s.set({handbrake:!s.handbrake,hint:s.handbrake?'Ручник отпущен':'Ручник включён — нажмите ещё раз, чтобы отпустить'})}
useStore.subscribe(s=>{try{localStorage.setItem('roadster-settings',JSON.stringify({sound:s.sound,quality:s.quality,steeringMode:s.steeringMode}))}catch{}})
