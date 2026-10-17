// Shared dimensions for the visible playground, colliders and challenges.
export const CITY_LIMIT = 172
export const SPAWN: [number,number,number] = [0,.64,-100]
export const STREETS = [-150,150]
export const ROUTE: [number,number][] = [[-9,-66],[9,-42],[-9,-18],[9,6],[-9,30],[9,54],[0,88],[65,112],[120,75],[125,-50],[65,-110]]
export const PARKING: [number,number][] = [[-80,-115],[-68,-115],[-56,-115],[-44,-115]]
export const BUILDINGS = [
 {x:-115,z:-157,w:60,d:20,h:9,color:'#b3bdc1'},
 {x:-40,z:-157,w:50,d:20,h:7,color:'#a6afb6'},
 {x:120,z:-158,w:50,d:18,h:11,color:'#8c9fac'}
]
export const RAMPS = [
 {position:[87,2.05,30] as [number,number,number],size:[18,.6,22] as [number,number,number],angle:-.22},
 {position:[87,2.05,74] as [number,number,number],size:[22,.6,26] as [number,number,number],angle:.19},
 {position:[-112,1.15,88] as [number,number,number],size:[16,.6,18] as [number,number,number],angle:-.16}
]
