import fs from 'node:fs/promises';
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { dequantize, dedup, prune } from '@gltf-transform/functions';
import { MeshoptDecoder } from 'meshoptimizer';
import { Quaternion, Vector3 } from 'three';

// Microsoft Rocketbox Male Adult 01 (MIT), converted to glTF by CodeLove.
// The exported rig is seated by default, Y up / +Z forward, pelvis at origin.
const source = 'https://models.codelove.in/models/male-adult-01.glb';
const licenseSource = 'https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/master/LICENSE.md';
await fs.mkdir('.asset-work', { recursive: true });
const input = '.asset-work/driver-original.glb';
try { await fs.access(input); } catch {
  const response = await fetch(source);
  if (!response.ok) throw new Error(`Driver download failed: ${response.status}`);
  await fs.writeFile(input, new Uint8Array(await response.arrayBuffer()));
}
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS)
  .registerDependencies({ 'meshopt.decoder': MeshoptDecoder });
await MeshoptDecoder.ready;
const document = await io.read(input);
const root = document.getRoot();
const nodes = new Map(root.listNodes().map(node => [node.getName(), node]));
const bone = name => {
  const node = nodes.get(name);
  if (!node) throw new Error(`Missing driver bone: ${name}`);
  return node;
};
const worldPosition = name => new Vector3().fromArray(bone(name).getWorldTranslation());
const worldRotation = name => new Quaternion().fromArray(bone(name).getWorldRotation());
const setWorldRotation = (name, rotation) => {
  const node = bone(name), parent = node.getParentNode();
  const inverseParent = parent ? new Quaternion().fromArray(parent.getWorldRotation()).invert() : new Quaternion();
  node.setRotation(inverseParent.multiply(rotation).normalize().toArray());
};
const aimAt = (name, childName, target) => {
  const position = worldPosition(name);
  const current = worldPosition(childName).sub(position).normalize();
  const wanted = target.clone().sub(position).normalize();
  setWorldRotation(name, new Quaternion().setFromUnitVectors(current, wanted).multiply(worldRotation(name)));
};
const originalPose = Object.fromEntries(root.listNodes().filter(n => n.getName().startsWith('Bip01')).map(n => [n.getName(), {
  translation: n.getTranslation(), rotation: n.getRotation(),
}]));
const hip = worldPosition('Bip01');
const headRotation = worldRotation('Bip01 Head');
const feet = ['Bip01 L Foot', 'Bip01 R Foot'].map(worldRotation);
const lean = new Quaternion().setFromAxisAngle(new Vector3(1, 0, 0), -.22);
setWorldRotation('Bip01', lean.multiply(worldRotation('Bip01')));
setWorldRotation('Bip01 Head', headRotation);

for (const [side, sign, index] of [['L', 1, 0], ['R', -1, 1]]) {
  const target = (x, y, z) => hip.clone().add(new Vector3(x * sign, y, z));
  aimAt(`Bip01 ${side} Thigh`, `Bip01 ${side} Calf`, target(.11, -.025, .40));
  aimAt(`Bip01 ${side} Calf`, `Bip01 ${side} Foot`, target(.13, -.12, .80));
  setWorldRotation(`Bip01 ${side} Foot`, feet[index]);
  aimAt(`Bip01 ${side} UpperArm`, `Bip01 ${side} Forearm`, target(.23, .40, .17));
  aimAt(`Bip01 ${side} Forearm`, `Bip01 ${side} Hand`, target(.145, .39, .58));
  // A relaxed grip; each authored finger chain bends around a steering rim.
  for (let finger = 1; finger <= 4; finger++) {
    for (const suffix of ['', '1', '2']) {
      const name = `Bip01 ${side} Finger${finger}${suffix}`;
      const n = bone(name);
      const q = new Quaternion().fromArray(n.getRotation());
      q.multiply(new Quaternion().setFromAxisAngle(new Vector3(0, 0, 1), suffix ? .32 : .20));
      n.setRotation(q.toArray());
    }
  }
}
const sceneRoot = bone('RootNode');
sceneRoot.setTranslation(hip.clone().negate().toArray());
for (const animation of [...root.listAnimations()]) animation.dispose();
// Decode meshopt once at preparation time. The runtime needs no mesh decoder.
for (const extension of [...root.listExtensionsUsed()]) {
  if (extension.extensionName === 'EXT_meshopt_compression') extension.dispose();
}
await document.transform(dequantize(), dedup(), prune());
const copyright = 'Copyright (c) 2020 Microsoft. MIT License. Microsoft Rocketbox Male Adult 01; glTF conversion by CodeLove; seated pose prepared for Roadster Drive.';
Object.assign(root.getAsset(), { copyright });
const triangleCount = root.listMeshes().reduce((sum, mesh) => sum + mesh.listPrimitives().reduce((s, p) => s + (p.getIndices()?.getCount() ?? p.getAttribute('POSITION').getCount()) / 3, 0), 0);
if (triangleCount > 20000) throw new Error('Driver exceeds triangle budget');
await io.write('public/models/driver.glb', document);
const licenseResponse = await fetch(licenseSource);
if (!licenseResponse.ok) throw new Error('Unable to retain Rocketbox license');
const licenseText = await licenseResponse.text();
await fs.writeFile('.asset-work/driver-LICENSE.txt', licenseText);
const info = {
  title: 'Microsoft Rocketbox — Male Adult 01',
  author: 'Microsoft / Rocketbox (original avatar creation team led by Markus Wojcik)',
  source: 'https://github.com/microsoft/Microsoft-Rocketbox/tree/master/Assets/Avatars/Adults/Male_Adult_01',
  downloadSource: source,
  conversionCredit: 'CodeLove, https://models.codelove.in/models/male-adult-01/',
  license: 'MIT', licenseSource, licenseText,
  modifications: 'Decoded meshopt geometry and quantization, preserved all 7440 triangles and 1024px face/body textures plus normal maps, posed the original skeleton sitting with bent knees and hands toward a steering wheel. Removed animation tracks.',
  triangles: triangleCount,
  bytes: (await fs.stat('public/models/driver.glb')).size,
  axes: { up: '+Y', forward: '+Z', units: 'metres', pivot: 'pelvis' },
  suggestedPlacement: { position: [.34, -.19, -.265], scale: [.74, .74, .74], rotation: [0, 0, 0] },
  steeringWheel: { position: [.34, .11, .11], radius: .125, note: 'At this placement the knuckles are around z .09 and fingertips curl to z .12; do not push the wheel to z .28 because the upper arm and forearm together are .557 m.' },
  materialNames: root.listMaterials().map(m => m.getName()),
  cockpit: 'Hide meshes with material m002_head and m002_opacity in first person; keep m002_body visible for arms/hands. Never mutate the cached shared GLTF; clone with SkeletonUtils.clone.',
  bonePositions: Object.fromEntries(root.listNodes().filter(n => /Bip01$|Pelvis|Spine|Neck|Head$|Thigh|Calf|Foot|UpperArm|Forearm|Hand$|Eye$/.test(n.getName())).map(n => [n.getName(), n.getWorldTranslation()])),
  seatedPose: Object.fromEntries(root.listNodes().filter(n => n.getName().startsWith('Bip01')).map(n => [n.getName(), { translation: n.getTranslation(), rotation: n.getRotation() }])),
  originalPose,
};
await fs.writeFile('.asset-work/driver-info.json', JSON.stringify(info, null, 2));
console.log(JSON.stringify({ triangles: triangleCount, bytes: info.bytes, bones: root.listSkins()[0].listJoints().length, positions: info.bonePositions }, null, 2));
