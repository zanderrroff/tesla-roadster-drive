import assert from 'node:assert/strict'
import fs from 'node:fs'
import {Group, Vector3, Box3, Quaternion} from 'three'
import {clone} from 'three/examples/jsm/utils/SkeletonUtils.js'
import {loadThreeGeometry} from './load-three-geometry.mjs'
import {fitDriver, driverSteeringRig, steeringRig, steeringWheelAngle} from '../src/driver-rig.ts'

const layouts=JSON.parse(fs.readFileSync('src/cabin-layouts.json'))
const fleet=JSON.parse(fs.readFileSync('public/models/fleet/manifest.json'))
const {scene:source}=await loadThreeGeometry('public/models/driver.glb')
assert.ok(steeringWheelAngle(.48)<0 && steeringWheelAngle(-.48)>0)
for(const [id,layout] of Object.entries(layouts)) {
  const spec=fleet.find(v=>v.id===id), wheelSpec=layout.wheel||spec.steeringWheel
  assert.ok(wheelSpec,`${id}: steering geometry has metadata`)
  const chassis=new Group(),driver=clone(source),wheel=new Group()
  fitDriver(driver,layout.driver)
  wheel.position.fromArray(wheelSpec.center)
  if(wheelSpec.quaternion)wheel.quaternion.fromArray(wheelSpec.quaternion)
  else wheel.rotation.set(...wheelSpec.rotation)
  chassis.add(driver,wheel);chassis.updateMatrixWorld(true)
  const rotate=steeringRig(wheel),animate=driverSteeringRig(driver,wheel,wheelSpec.radius)
  let maxGripError=0,minY=Infinity,maxY=-Infinity
  const points=[]
  for(const roadAngle of [-.48,-.24,0,.24,.48,0]) {
    rotate(steeringWheelAngle(roadAngle));animate();chassis.updateMatrixWorld(true)
    for(const [side,sign] of [['L',1],['R',-1]]) {
      const hand=driver.getObjectByName(`Bip01_${side}_Hand`).getWorldPosition(new Vector3())
      const target=wheel.localToWorld(new Vector3(sign*wheelSpec.radius*1.0595,wheelSpec.radius*.0532,-.1137*layout.driver.scale))
      maxGripError=Math.max(maxGripError,hand.distanceTo(target))
    }
    const bounds=new Box3()
    driver.traverse(mesh=>{if(!mesh.isSkinnedMesh)return;mesh.skeleton.update();for(let i=0;i<mesh.geometry.attributes.position.count;i++){
      const p=new Vector3().fromBufferAttribute(mesh.geometry.attributes.position,i)
      mesh.boneTransform(i,p);p.applyMatrix4(mesh.matrixWorld);bounds.expandByPoint(p)
      assert.ok(p.toArray().every(Number.isFinite),`${id}: finite skinned vertices`)
    }})
    minY=Math.min(minY,bounds.min.y);maxY=Math.max(maxY,bounds.max.y)
    points.push(driver.getObjectByName('Bip01_L_Hand').getWorldPosition(new Vector3()))
  }
  console.log(id,JSON.stringify({maxGripError,minY,maxY,floor:layout.floorY,roof:layout.roofY}))
  assert.ok(maxGripError<.008,`${id}: wrists reach wheel through full steering range, error ${maxGripError}`)
  assert.ok(minY>=layout.floorY-.005,`${id}: no driver below floor`)
  assert.ok(maxY<=layout.roofY,`${id}: head clears roof`)
  assert.ok(points[0].distanceTo(points[4])>.08,`${id}: hands actually follow steering`)
  assert.ok(points[2].distanceTo(points[5])<1e-6,`${id}: no incremental pose drift`)
  // Steering pivots keep their center and authored tilt through both directions.
  const neutral=new Quaternion().copy(wheel.quaternion),origin=wheel.position.clone()
  rotate(.8);assert.ok(wheel.position.equals(origin));rotate(0)
  assert.ok(wheel.quaternion.angleTo(neutral)<1e-7)
}
