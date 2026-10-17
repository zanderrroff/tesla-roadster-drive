import {useEffect,useRef,useState} from 'react'
import {Icon} from './Icons'
import {inputState,clearInput,input,orbit,resetView,selectGear,steering,toggleHandbrake,useStore} from './store'
import type {Control} from './store'
export function Pedal({control,label}:{control:'forward'|'brake';label:string}) {
 const [held,setHeld]=useState(false)
 useEffect(()=>{const id=setInterval(()=>setHeld(input[control].size>0),100);return()=>clearInterval(id)},[control])
 const release=(e:React.PointerEvent<HTMLButtonElement>)=>{input[control].delete('touch'+e.pointerId);setHeld(input[control].size>0)}
 return <button aria-label={label} aria-pressed={held} className={`drive-pedal ${control} ${held?'held':''}`} onContextMenu={e=>e.preventDefault()} onPointerDown={e=>{e.preventDefault();input[control].add('touch'+e.pointerId);setHeld(true);e.currentTarget.setPointerCapture(e.pointerId)}} onPointerUp={release} onPointerCancel={release} onLostPointerCapture={release}>
  <svg viewBox="0 0 50 80" aria-hidden="true"><rect x="9" y="5" width="32" height="70" rx="6"/>{[19,30,41,52,63].map(y=><path key={y} d={`M17 ${y}h16`}/>)}</svg><span>{control==='forward'?'ГАЗ':'ТОРМОЗ / НАЗАД'}</span>
 </button>
}
function Arrow({control,label,children}:{control:Control;label:string;children:React.ReactNode}) {
 const release=(e:React.PointerEvent<HTMLButtonElement>)=>input[control].delete('touch'+e.pointerId)
 return <button className="steer-arrow" aria-label={label} onPointerDown={e=>{e.preventDefault();input[control].add('touch'+e.pointerId);e.currentTarget.setPointerCapture(e.pointerId)}} onPointerUp={release} onPointerCancel={release} onLostPointerCapture={release}>{children}</button>
}
export function SteeringWheel(){
 const ref=useRef<HTMLButtonElement>(null),pointer=useRef<number|null>(null),last=useRef(0),angle=useRef(0)
 const paint=()=>{ref.current?.style.setProperty('--steer',`${angle.current}deg`);steering.value=-angle.current/125}
 const release=(e:React.PointerEvent)=>{if(pointer.current!==e.pointerId)return;pointer.current=null;angle.current=0;paint()}
 useEffect(()=>()=>{steering.value=0},[])
 const getAngle=(e:React.PointerEvent)=>{const r=e.currentTarget.getBoundingClientRect();return Math.atan2(e.clientY-r.y-r.height/2,e.clientX-r.x-r.width/2)*180/Math.PI}
 return <button ref={ref} aria-label="Руль: вращайте пальцем" className="steering-wheel" onContextMenu={e=>e.preventDefault()} onPointerDown={e=>{if(pointer.current!==null)return;e.preventDefault();pointer.current=e.pointerId;last.current=getAngle(e);e.currentTarget.setPointerCapture(e.pointerId)}} onPointerMove={e=>{if(pointer.current!==e.pointerId)return;const next=getAngle(e);let delta=next-last.current;if(delta>180)delta-=360;if(delta< -180)delta+=360;angle.current=Math.max(-125,Math.min(125,angle.current+delta));last.current=next;paint()}} onPointerUp={release} onPointerCancel={release} onLostPointerCapture={release}>
 <svg viewBox="0 0 180 180" aria-hidden="true"><circle className="wheel-outer" cx="90" cy="90" r="72"/><circle className="wheel-inner" cx="90" cy="90" r="62"/><path className="wheel-spokes" d="M27 80L71 87L81 107L82 157H98L99 107L109 87L153 80L153 94L111 108L103 145H77L69 108L27 94Z"/><circle className="wheel-hub" cx="90" cy="92" r="25"/><path className="wheel-mark" d="M86 17h8v17h-8z"/><text x="90" y="99" textAnchor="middle">T</text></svg>
 </button>
}
export function TouchControls(){const s=useStore();return <div className="driving-controls">
 <div className="left-controls">{s.steeringMode==='wheel'?<SteeringWheel/>:<div className="arrow-controls"><Arrow control="left" label="Поворот налево"><Icon name="arrowLeft" size={34}/></Arrow><Arrow control="right" label="Поворот направо"><Icon name="arrowRight" size={34}/></Arrow></div>}<button className="steering-switch" aria-label="Сменить способ руления" onClick={()=>{clearInput();s.set({steeringMode:s.steeringMode==='wheel'?'arrows':'wheel'})}}>{s.steeringMode==='wheel'?'⇄ Стрелки':'◉ Руль'}</button></div>
 <div className="right-controls"><div className="transmission"><button className={s.autoReverse?'auto-gear selected':'auto-gear'} aria-label="Автоматический задний ход" aria-pressed={s.autoReverse} onClick={()=>s.set({autoReverse:!s.autoReverse})}>AUTO</button><div className="gear-selector" role="group" aria-label="Коробка передач">{(['D','N','R'] as const).map(g=><button key={g} aria-label={g==='D'?'Передача D — вперёд':g==='R'?'Передача R — назад':'Передача N — нейтраль'} aria-pressed={s.gear===g} className={s.gear===g?'selected':''} onClick={()=>{s.set({autoReverse:false});selectGear(g)}}>{g}</button>)}</div><button className={`handbrake ${s.handbrake?'engaged':''}`} aria-label="Ручной тормоз" aria-pressed={s.handbrake} onPointerDown={e=>{e.preventDefault();inputState.handbrake=true;s.set({handbrake:true});e.currentTarget.setPointerCapture(e.pointerId)}} onPointerUp={()=>{inputState.handbrake=false;s.set({handbrake:false})}} onPointerCancel={()=>{inputState.handbrake=false;s.set({handbrake:false})}} onLostPointerCapture={()=>{inputState.handbrake=false;s.set({handbrake:false})}}><svg viewBox="0 0 48 40" aria-hidden="true"><path d="M10 6C1 14 1 26 10 34M38 6c9 8 9 20 0 28"/><circle cx="24" cy="20" r="13"/><path d="M24 12v10M24 27v1"/></svg><span>{s.handbrake?'ВКЛЮЧЁН':'РУЧНИК'}</span></button></div><div className="pedal-pair"><Pedal control="brake" label="Тормоз"/><Pedal control="forward" label="Газ"/></div></div>
 </div>}
export function CameraGesture(){
 const points=useRef(new Map<number,{x:number;y:number}>()),pinch=useRef(0),paused=useStore(s=>s.paused)
 useEffect(()=>{points.current.clear();orbit.dragging=false},[paused])
 const release=(e:React.PointerEvent)=>{points.current.delete(e.pointerId);orbit.dragging=points.current.size>0;pinch.current=0}
 return <div className="camera-gesture" aria-label="Обзор камеры: проведите пальцем, двумя пальцами измените расстояние" onContextMenu={e=>e.preventDefault()} onDoubleClick={resetView} onPointerDown={e=>{if(paused)return;e.preventDefault();points.current.set(e.pointerId,{x:e.clientX,y:e.clientY});orbit.dragging=true;e.currentTarget.setPointerCapture(e.pointerId)}} onPointerMove={e=>{const old=points.current.get(e.pointerId);if(!old)return;const dx=e.clientX-old.x,dy=e.clientY-old.y;points.current.set(e.pointerId,{x:e.clientX,y:e.clientY});if(points.current.size===1){orbit.yaw-=dx*.005*useStore.getState().sensitivity;orbit.pitch=Math.max(.12,Math.min(1.25,orbit.pitch+dy*.0035*useStore.getState().sensitivity));orbit.manual=true}else {const [a,b]=[...points.current.values()];const distance=Math.hypot(a.x-b.x,a.y-b.y);if(pinch.current)orbit.distance=Math.max(3.8,Math.min(20,orbit.distance-(distance-pinch.current)*.025));pinch.current=distance;orbit.manual=true}}} onPointerUp={release} onPointerCancel={release} onLostPointerCapture={release} onWheel={e=>{orbit.distance=Math.max(3.8,Math.min(20,orbit.distance+e.deltaY*.008));orbit.manual=true}}/>
}
