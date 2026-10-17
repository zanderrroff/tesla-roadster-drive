import {useEffect} from 'react'
import {down,inputState,telemetry,useStore} from './store'
import {vehicleById} from './vehicles'
let context:AudioContext|undefined,master:GainNode|undefined,engine:AudioBufferSourceNode|undefined,tire:AudioBufferSourceNode|undefined,ambience:AudioBufferSourceNode|undefined
let motor:OscillatorNode|undefined,motor2:OscillatorNode|undefined,horn:OscillatorNode|undefined
let engineGain:GainNode,tireGain:GainNode,ambientGain:GainNode,motorGain:GainNode,hornGain:GainNode,windGain:GainNode
let crashBuffer:AudioBuffer|undefined,lastImpact=0,loading=false
const smooth=(p:AudioParam,v:number,rate=.12)=>p.setTargetAtTime(v,context!.currentTime,rate)
export async function startAudio(){try{
 context??=new AudioContext();await context.resume();if(master)return
 master=context.createGain();master.gain.value=0
 const compressor=context.createDynamicsCompressor();compressor.threshold.value=-14;compressor.ratio.value=4;master.connect(compressor);compressor.connect(context.destination)
 const channel=()=>{const g=context!.createGain();g.gain.value=0;g.connect(master!);return g}
 engineGain=channel();tireGain=channel();ambientGain=channel();motorGain=channel();hornGain=channel();windGain=channel()
 motor=context.createOscillator();motor.type='sine';motor.connect(motorGain);motor.start()
 motor2=context.createOscillator();motor2.type='triangle';const quiet=context.createGain();quiet.gain.value=.18;motor2.connect(quiet);quiet.connect(motorGain);motor2.start()
 horn=context.createOscillator();horn.type='triangle';horn.frequency.value=350;horn.connect(hornGain);horn.start()
 const noise=context.createBuffer(1,context.sampleRate*2,context.sampleRate),samples=noise.getChannelData(0);let n=0;for(let i=0;i<samples.length;i++){n=(n+(Math.random()*2-1)*.03)/1.02;samples[i]=n}
 const wind=context.createBufferSource();wind.buffer=noise;wind.loop=true;const low=context.createBiquadFilter();low.type='lowpass';low.frequency.value=700;wind.connect(low);low.connect(windGain);wind.start()
 if(!loading){loading=true;const load=async(file:string,g:GainNode)=>{try{const r=await fetch('/sounds/v14/'+file);if(!r.ok)return;const buffer=await context!.decodeAudioData(await r.arrayBuffer());const source=context!.createBufferSource();source.buffer=buffer;source.loop=true;source.connect(g);source.start();return source}catch{return}}
 void load('engine-loop.wav',engineGain).then(s=>engine=s);void load('tire-loop.wav',tireGain).then(s=>tire=s);void load('ambience.wav',ambientGain).then(s=>ambience=s)
 void fetch('/sounds/v14/impact.wav').then(r=>r.ok?r.arrayBuffer():Promise.reject()).then(b=>context!.decodeAudioData(b)).then(b=>crashBuffer=b).catch(()=>{})}
 }catch{}}
export function Sound(){useEffect(()=>{const timer=setInterval(()=>{if(!context||!master)return;const s=useStore.getState(),active=s.started&&!s.paused&&s.sound&&!document.hidden
 smooth(master.gain,active?s.volume:0,.06);if(!active)return
 const electric=vehicleById(s.vehicle).electric,throttle=telemetry.throttle,speed=telemetry.speed,slip=Math.min(1,Math.max(0,(Math.abs(telemetry.lateralSpeed)-1.3)/7)),rpm=900+((speed*105+throttle*700)%4200)
 telemetry.rpm=electric?speed*180:rpm
 smooth(engineGain.gain,electric?0:.14+throttle*.24);if(engine)smooth(engine.playbackRate,.65+rpm/3800,.09)
 smooth(motorGain.gain,electric?.007+throttle*.025+Math.min(.018,speed*.0006):0)
 if(motor)smooth(motor.frequency,95+speed*13+throttle*30);if(motor2)smooth(motor2.frequency,190+speed*26)
 smooth(tireGain.gain,telemetry.groundClearance<.9?slip*.24:0,.08);if(tire)smooth(tire.playbackRate,.8+speed*.009)
 smooth(windGain.gain,Math.min(.45,speed*.007));smooth(ambientGain.gain,.07);smooth(hornGain.gain,inputState.horn?.09:0,.02)
 if(telemetry.impact>lastImpact&&crashBuffer){const hit=context.createBufferSource(),g=context.createGain();hit.buffer=crashBuffer;g.gain.value=.25;hit.connect(g);g.connect(master);hit.start();hit.onended=()=>{hit.disconnect();g.disconnect()};lastImpact=telemetry.impact}
 },50);return()=>clearInterval(timer)},[]);return null}
