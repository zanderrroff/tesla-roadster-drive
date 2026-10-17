import fleet from '../public/models/fleet/manifest.json'
import roadsterDimensions from './vehicle-dimensions.json'
export interface VehicleSpec {id:string;name:string;brand:string;className:string;file:string;centers:number[][];radius:number;size:[number,number,number];power:number;electric:boolean;color:string;description:string;cockpitEye?:number[];hasInterior?:boolean}
const presets:Record<string,Partial<VehicleSpec>>={
 "bmw-m3-e30":{brand:"BMW",name:"M3 E30",className:"Спортивное купе",power:.85,color:"#b32d38",description:"Классика 80-х · лёгкое купе"},
 "mercedes-benz-amg-gt":{brand:"MERCEDES-BENZ",name:"AMG GT",className:"Гран-туризмо",power:1.1,color:"#28718d",description:"Мощное купе · длинный капот"},
 "mercedes-benz-190e-evo":{brand:"MERCEDES-BENZ",name:"190E Evolution",className:"Спортивный седан",power:.8,color:"#273439",description:"Классический седан · точные повороты"},
 "nissan-skyline-gt-r":{brand:"NISSAN",name:"Skyline GT-R C110",className:"Японская классика",power:.78,color:"#00bddc",description:"Японское купе · плавная тяга"},
 "porsche-911-carrera":{brand:"PORSCHE",name:"911 Carrera 4S",className:"Спорткар",power:1.05,color:"#ced2d5",description:"Компактный спорткар · быстрые повороты"},
 "ferrari-458":{brand:"FERRARI",name:"458 Italia",className:"Суперкар",power:1.15,color:"#b72e26",description:"Суперкар · высокая тяга"}
}
export const VEHICLES:VehicleSpec[]=[{id:'roadster',name:'Roadster',brand:'TESLA',className:'Электрический спорткар',file:'',...roadsterDimensions,size:[1.72,.58,3.85],power:1,electric:true,color:'#bb253a',description:'Мгновенная тяга · полный привод',hasInterior:false},...fleet.map(v=>({...v,...presets[v.id],size:v.size as [number,number,number],electric:false} as VehicleSpec))]
export const vehicleById=(id:string)=>VEHICLES.find(v=>v.id===id)||VEHICLES[0]
