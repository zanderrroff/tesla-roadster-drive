import assert from 'node:assert/strict';
import {ALL_EXTENSIONS} from '@gltf-transform/extensions';
import {NodeIO} from '@gltf-transform/core';
import dimensions from '../src/vehicle-dimensions.json' with {type:'json'};
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS);
for(const quality of ['mobile','high']){
 const d=await io.read('public/models/roadster-'+quality+'.glb');
 const nodes=d.getRoot().listNodes();
 for(const n of ['Chassis','Wheel_0','Wheel_1','Wheel_2','Wheel_3'])assert(nodes.some(x=>x.getName()===n),'Missing '+n);
 let triangles=0;
 for(const m of d.getRoot().listMeshes())for(const p of m.listPrimitives()){
  const a=p.getAttribute('POSITION');assert(a&&a.getCount()>0);
  assert([...a.getArray()].every(Number.isFinite),'Invalid geometry');
  const ix=p.getIndices();assert(ix);assert([...ix.getArray()].every(i=>i<a.getCount()),'Invalid index');
  triangles+=ix.getCount()/3;
 }
 if(quality==='mobile')assert(triangles<=150000,'Mobile geometry exceeds budget');
 console.log(quality,triangles,'triangles, valid geometry and four separate wheels');
}
assert(dimensions.centers[0][0]<0&&dimensions.centers[1][0]>0);
assert(dimensions.centers[0][2]>0&&dimensions.centers[2][2]<0);
assert(dimensions.radius>.2&&dimensions.radius<.5);
