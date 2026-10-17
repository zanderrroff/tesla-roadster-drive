import create from 'zustand'
export type Control='forward'|'backward'|'left'|'right'|'brake'|'handbrake'
export const input: Record<Control,Set<string>>={forward:new Set(),backward:new Set(),left:new Set(),right:new Set(),brake:new Set(),handbrake:new Set()}
export const down=(c:Control)=>input[c].size>0
export const clearInput=()=>Object.values(input).forEach(s=>s.clear())
const saved=(()=>{try{return JSON.parse(localStorage.getItem('roadster-settings')||'{}')}catch{return {}}})()
export type Quality='mobile'|'high'
interface State {started:boolean;paused:boolean;loaded:boolean;camera:number;sound:boolean;quality:Quality;reset:number;about:boolean;set:(patch:Partial<State>)=>void}
export const useStore=create<State>(set=>({started:false,paused:false,loaded:false,camera:0,sound:saved.sound??true,quality:saved.quality==='high'?'high':'mobile',reset:0,about:false,set:patch=>set(state=>({...state,...patch}))}))
export const telemetry={speed:0,distance:0,position:[0,0,0],fps:0}
export function pause(){clearInput();useStore.getState().set({paused:true})}
export function resetCar(){clearInput();useStore.getState().set({reset:useStore.getState().reset+1})}
useStore.subscribe(s=>{try{localStorage.setItem('roadster-settings',JSON.stringify({sound:s.sound,quality:s.quality}))}catch{}})
