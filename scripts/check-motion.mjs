import assert from 'node:assert/strict'
import {Vector3,Quaternion} from 'three'
import {smoothPose} from '../src/motion.ts'
const position=new Vector3(1,2,3),rotation=new Quaternion(),target=new Vector3(3,4,5),q=new Quaternion().setFromAxisAngle(new Vector3(0,1,0),1)
const frozen=[...position.toArray(),...rotation.toArray()]
for(let i=0;i<120;i++)smoothPose(position,rotation,target,q,1/60,false,true)
assert.deepEqual([...position.toArray(),...rotation.toArray()],frozen,'Pause freezes the visible pose exactly')
function run(rate){const p=new Vector3(),r=new Quaternion();for(let i=0;i<rate;i++)smoothPose(p,r,target,q,1/rate,false,false);return p}
assert.ok(run(30).distanceTo(run(120))<1e-6,'Smoothing is frame-rate independent')
let rawVariation=0,filteredVariation=0,lastRaw=0,lastFiltered=0;const p=new Vector3(),r=new Quaternion()
for(let i=0;i<240;i++){const noisyY=.52+(i%2?.012:-.012);smoothPose(p,r,new Vector3(0,noisyY,0),rotation,1/60,i===0,false);if(i>30){rawVariation+=Math.abs(noisyY-lastRaw);filteredVariation+=Math.abs(p.y-lastFiltered)}lastRaw=noisyY;lastFiltered=p.y}
assert.ok(filteredVariation<rawVariation*.25,'Filter suppresses high frequency suspension/worker noise')
smoothPose(p,r,new Vector3(0,1,100),rotation,1/60,true,false);assert.equal(p.z,100,'Reset snaps without travelling across the map')
console.log({pauseFrozen:true,frameIndependent:true,jitterRatio:filteredVariation/rawVariation,reset:true})
