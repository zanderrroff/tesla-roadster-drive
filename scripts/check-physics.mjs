import assert from 'node:assert/strict'
import fs from 'node:fs'
import {World,Body,Vec3,Box,RaycastVehicle} from 'cannon-es'
import {CHASSIS,wheelOptions} from '../src/vehicle-physics.ts'
import {RAMPS,SPAWN} from '../src/playground-layout.ts'
const d=JSON.parse(fs.readFileSync(new URL('../src/vehicle-dimensions.json',import.meta.url)))
function setup(x=SPAWN[0],z=SPAWN[2]){const world=new World({gravity:new Vec3(0,-9.81,0)});const plane=new Body({mass:0,shape:new Box(new Vec3(1000,2,1000)),position:new Vec3(0,-2,0)});world.addBody(plane);for(const r of RAMPS){const b=new Body({mass:0,shape:new Box(new Vec3(...r.size.map(n=>n/2))),position:new Vec3(...r.position)});b.quaternion.setFromEuler(r.angle,0,0);b.aabbNeedsUpdate=true;world.addBody(b)}const body=new Body({mass:CHASSIS.mass,shape:new Box(new Vec3(...CHASSIS.size.map(n=>n/2))),position:new Vec3(x,.64,z),linearDamping:CHASSIS.linearDamping,angularDamping:CHASSIS.angularDamping});const car=new RaycastVehicle({chassisBody:body,indexForwardAxis:2,indexRightAxis:0,indexUpAxis:1});for(const o of wheelOptions(d.centers,d.radius))car.addWheel({...o,chassisConnectionPointLocal:new Vec3(...o.chassisConnectionPointLocal),axleLocal:new Vec3(...o.axleLocal),directionLocal:new Vec3(...o.directionLocal)});car.addToWorld(world);return {world,body,car}}
const a=setup();for(let i=0;i<240;i++)a.world.step(1/120)
assert.ok(a.body.position.y>.45&&a.body.position.y<.7,'Stable ride height above floor')
for(let i=0;i<360;i++){for(let w=0;w<4;w++)a.car.applyEngineForce(-1900,w);a.world.step(1/120);assert.ok(a.body.position.y>.3,'No ground penetration on acceleration')}
assert.ok(a.body.position.z>SPAWN[2]+15,'Forward movement')
assert.ok(Math.abs(a.car.wheelInfos[0].rotation)>10,'Wheel rotation follows movement')
for(let i=0;i<600;i++){for(let w=0;w<4;w++){a.car.applyEngineForce(0,w);a.car.setBrake(12,w)}a.world.step(1/120)}
assert.ok(a.body.velocity.length()<.5,'Brakes stop vehicle');assert.ok(a.body.quaternion.vmult(new Vec3(0,1,0)).y>.95,'Braking must not flip car')
const before=a.body.position.z;for(let i=0;i<240;i++){for(let w=0;w<4;w++){a.car.setBrake(0,w);a.car.applyEngineForce(850,w)}a.world.step(1/120)}assert.ok(a.body.position.z<before-5,'Reverse movement')
const b=setup(87,-12);let highest=0;for(let i=0;i<600;i++){for(let w=0;w<4;w++)b.car.applyEngineForce(i<480?-1900:0,w);b.world.step(1/120);highest=Math.max(highest,b.body.position.y);assert.ok(b.body.position.y>-.05,'Ramp/landing stays above floor')}
assert.ok(highest>4,'Ramp launches car');console.log(JSON.stringify({stableRide:true,forward:true,reverse:true,braking:true,wheelRotation:true,rampPeak:highest}))

const turning=setup(-72,-90);let maxSlip=0;for(let i=0;i<600;i++){for(let w=0;w<4;w++){turning.car.applyEngineForce(i<320?-1900:0,w);turning.car.setBrake(i>=320&&i<390&&w>=2?18:0,w);if(w<2)turning.car.setSteeringValue(i>220?.26:0,w)}turning.world.step(1/120);const forward=turning.body.quaternion.vmult(new Vec3(0,0,1));maxSlip=Math.max(maxSlip,Math.abs(turning.body.velocity.x*forward.z-turning.body.velocity.z*forward.x));assert.ok(turning.body.position.y>0,'Turning remains above ground')}console.log({driftLateralSpeed:maxSlip});assert.ok(maxSlip>2,'Handbrake turn generates measurable drift')
