import assert from 'node:assert/strict'
import {driveForces,canSelectGear} from '../src/driving.ts'
assert.ok(driveForces(true,true,false,false,'D',0).force<0)
assert.ok(driveForces(true,true,false,false,'R',0).force>0)
assert.equal(driveForces(true,true,false,false,'N',0).force,0)
assert.equal(driveForces(true,true,true,false,'D',10).force,0)
assert.deepEqual(driveForces(true,true,false,true,'R',0),{force:0,frontBrake:180,rearBrake:180})
assert.deepEqual(driveForces(true,true,false,true,'D',20),{force:0,frontBrake:0,rearBrake:180})
assert.equal(driveForces(false,true,false,false,'D',0).force,0)
assert.equal(canSelectGear('D','R',12),false)
assert.equal(canSelectGear('R','D',-5),false)
assert.equal(canSelectGear('D','N',12),true)
assert.equal(canSelectGear('D','R',0),true)
console.log('11 drivetrain and gear safety regression assertions passed')
