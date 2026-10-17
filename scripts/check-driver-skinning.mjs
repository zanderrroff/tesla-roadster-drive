import assert from 'node:assert/strict';
import { Box3, Group, Matrix4, Vector3, Vector4 } from 'three';
import { clone } from 'three/examples/jsm/utils/SkeletonUtils.js';
import { loadThreeGeometry } from './load-three-geometry.mjs';

const { scene: source } = await loadThreeGeometry('public/models/driver.glb');
const driver = clone(source);
driver.position.set(.34, -.19, -.265);
driver.scale.setScalar(.74);
const chassis = new Group();
chassis.add(driver);
const meshes = [];
driver.traverse(node => { if (node.isSkinnedMesh) meshes.push(node); });
assert.equal(meshes.length, 3, 'body, face and hair are preserved');
const sourceBones = new Set();
source.traverse(node => { if (node.isBone) sourceBones.add(node); });
for (const mesh of meshes) {
  assert.equal(mesh.bindMode, 'attached');
  assert.ok(mesh.skeleton.bones.every(bone => !sourceBones.has(bone)), 'clone owns its bones');
  assert.ok(mesh.bindMatrix.equals(new Matrix4()), 'source GLTF uses identity bind matrix');
}

const point = new Vector3(), gpuPoint = new Vector4(), base = new Vector4(), weighted = new Vector4();
const skinWeights = new Vector4(), skinIndices = new Vector4();
const boneMatrix = new Matrix4(), inverseChassis = new Matrix4();
let maximumShaderDifference = 0;
function sample() {
  chassis.updateMatrixWorld(true);
  inverseChassis.copy(chassis.matrixWorld).invert();
  const output = {};
  for (const mesh of meshes) {
    mesh.skeleton.update();
    const bounds = new Box3();
    const position = mesh.geometry.attributes.position;
    const ids = mesh.geometry.attributes.skinIndex, weights = mesh.geometry.attributes.skinWeight;
    for (let index = 0; index < position.count; index++) {
      point.fromBufferAttribute(position, index);
      mesh.boneTransform(index, point);
      point.applyMatrix4(mesh.matrixWorld).applyMatrix4(inverseChassis);
      assert.ok(point.toArray().every(Number.isFinite), 'all skinned vertices stay finite');
      bounds.expandByPoint(point);
      // Also follow the shader's homogeneous-weight path with the actual Float32
      // bone matrices, catching weight/translation mistakes hidden by Vector3.
      base.set(position.getX(index), position.getY(index), position.getZ(index), 1).applyMatrix4(mesh.bindMatrix);
      weighted.set(0, 0, 0, 0);
      skinWeights.fromBufferAttribute(weights, index);
      skinIndices.fromBufferAttribute(ids, index);
      for (let component = 0; component < 4; component++) {
        const weight = skinWeights.getComponent(component);
        if (!weight) continue;
        const joint = skinIndices.getComponent(component);
        boneMatrix.fromArray(mesh.skeleton.boneMatrices, joint * 16);
        gpuPoint.copy(base).applyMatrix4(boneMatrix).multiplyScalar(weight);
        weighted.add(gpuPoint);
      }
      weighted.applyMatrix4(mesh.bindMatrixInverse);
      gpuPoint.set(weighted.x, weighted.y, weighted.z, 1).applyMatrix4(mesh.matrixWorld).applyMatrix4(inverseChassis);
      maximumShaderDifference = Math.max(maximumShaderDifference,
        Math.hypot(gpuPoint.x - point.x, gpuPoint.y - point.y, gpuPoint.z - point.z));
    }
    output[mesh.material.name] = { min: bounds.min.toArray(), max: bounds.max.toArray() };
  }
  return output;
}
const reference = sample();
let maximumChassisLocalDrift = 0;
for (const pose of [
  { position: [12, 3, -8], rotation: [.3, 1.1, -.12], scale: [1, 1, 1] },
  { position: [-170, 8, 170], rotation: [-.24, 2.2, .4], scale: [1.8, 1.8, 1.8] },
  { position: [75, -.5, 150], rotation: [.7, -.9, .12], scale: [.7, 1.25, 1.5] },
  { position: [0, 0, 0], rotation: [0, 0, 0], scale: [1, 1, 1] },
]) {
  chassis.position.fromArray(pose.position);
  chassis.rotation.set(...pose.rotation);
  chassis.scale.fromArray(pose.scale);
  const current = sample();
  for (const name of Object.keys(reference)) for (const bound of ['min', 'max']) for (let axis = 0; axis < 3; axis++) {
    maximumChassisLocalDrift = Math.max(maximumChassisLocalDrift, Math.abs(current[name][bound][axis] - reference[name][bound][axis]));
  }
}
assert.ok(maximumChassisLocalDrift < 1e-5, 'driver does not sink or detach as its chassis moves, rotates or scales');
assert.ok(maximumShaderDifference < 1e-4, `GPU Float32 skinning agrees with Three boneTransform: ${maximumShaderDifference}`);
assert.ok(reference.m002_body.min[1] > -.370, 'feet stay above the cabin floor');
assert.ok(reference.m002_opacity.max[1] < .54, 'head stays below the Roadster roof');
const bonePosition = name => driver.getObjectByName(name).getWorldPosition(new Vector3()).toArray();
const wheel = new Group();
wheel.position.set(.34, .11, .11);
wheel.rotation.x = -.12;
wheel.updateMatrixWorld(true);
const gripAnchors = Object.fromEntries(['L', 'R'].map(side => [side,
  wheel.worldToLocal(new Vector3().fromArray(bonePosition(`Bip01_${side}_Hand`))).toArray(),
]));
console.log(JSON.stringify({
  driverYRange: [reference.m002_body.min[1], reference.m002_opacity.max[1]],
  pelvis: bonePosition('Bip01_Pelvis'),
  eyes: [bonePosition('Bip01_LEye'), bonePosition('Bip01_REye')],
  head: reference.m002_head,
  body: reference.m002_body,
  gripAnchorsInWheelSpace: gripAnchors,
  maximumChassisLocalDrift,
  maximumShaderDifference,
  cloneAndSkinningPassed: true,
}, null, 2));
