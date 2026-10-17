import fs from 'node:fs/promises';
import { MeshBasicMaterial } from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

// Exercise the installed Three loader and its complete skeleton/bind-matrix path.
// Only material loading is replaced, so Node needs no DOM/image decoder.
export async function loadThreeGeometry(path) {
  const loader = new GLTFLoader();
  loader.register(parser => ({
    name: 'geometry-only-validation',
    loadMaterial(index) {
      const material = new MeshBasicMaterial();
      material.name = parser.json.materials[index]?.name || '';
      return Promise.resolve(material);
    },
  }));
  const bytes = await fs.readFile(path);
  const data = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
  return new Promise((resolve, reject) => loader.parse(data, '', resolve, reject));
}
