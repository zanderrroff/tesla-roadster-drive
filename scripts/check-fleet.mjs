import assert from 'node:assert/strict'
import fs from 'node:fs'
import {NodeIO} from '@gltf-transform/core'
import {ALL_EXTENSIONS} from '@gltf-transform/extensions'
import draco from 'draco3dgltf'
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({'draco3d.decoder':await draco.createDecoderModule()})
const cars=JSON.parse(fs.readFileSync(new URL('../public/models/fleet/manifest.json',import.meta.url)))
for(const car of cars){const doc=await io.read('public'+car.file),root=doc.getRoot();assert.equal(root.listScenes()[0].listChildren().length,5);for(const name of ['Chassis','Wheel_0','Wheel_1','Wheel_2','Wheel_3'])assert.ok(root.listNodes().some(n=>n.getName()===name),car.id+name);assert.ok(car.source.startsWith('https://'));assert.ok(car.license.startsWith('CC BY'));let triangles=0;for(const mesh of root.listMeshes())for(const p of mesh.listPrimitives()){const position=p.getAttribute('POSITION');for(const a of p.listAttributes())assert.ok(Array.from(a.getArray()).every(Number.isFinite));const idx=p.getIndices()?.getArray();if(idx)assert.ok(Array.from(idx).every(n=>n>=0&&n<position.getCount()));triangles+=(idx?.length||position.getCount())/3}assert.ok(triangles<=150000,car.id+' triangle budget');assert.ok(car.centers[0][2]>0&&car.centers[2][2]<0);assert.ok(car.radius>.25&&car.radius<.45);console.log(car.id,triangles,'triangles; valid wheel groups, attributes, indices and license provenance')}
const driver=await io.read('public/models/driver.glb');assert.ok(driver.getRoot().listSkins().length>0);assert.ok(fs.readFileSync('public/models/driver-LICENSE.txt','utf8').includes('Copyright (c) 2020 Microsoft'));console.log('Rigged driver and MIT attribution present')
