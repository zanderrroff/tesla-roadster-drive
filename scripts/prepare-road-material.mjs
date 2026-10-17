import fs from 'node:fs/promises'
import sharp from 'sharp'
import {createHash} from 'node:crypto'

// Poly Haven Asphalt 02, Rob Tuytel, CC0. Keep measured 3 m texture scale.
const files = await (await fetch('https://api.polyhaven.com/files/asphalt_02')).json()
const directory = 'public/textures/asphalt'
await fs.mkdir(directory, {recursive: true})
const provenance = []
for (const [type, key] of [['color', 'Diffuse'], ['normal', 'nor_gl'], ['arm', 'arm']]) {
  const source = files[key]['2k'].jpg
  const response = await fetch(source.url)
  if (!response.ok) throw Error(`Texture download failed: ${response.status}`)
  const original = Buffer.from(await response.arrayBuffer())
  if (createHash('md5').update(original).digest('hex') !== source.md5) throw Error('Source checksum mismatch')
  for (const [name, size] of [['mobile', 1024], ['high', 2048]]) {
    const output = `${directory}/${type}-${name}.webp`
    await sharp(original).resize(size, size).webp({quality: type === 'normal' ? 95 : 87, effort: 5}).toFile(output)
    provenance.push({file: output.replace('public', ''), size, source: source.url, bytes: (await fs.stat(output)).size})
  }
}
await fs.writeFile(`${directory}/source.json`, JSON.stringify({
  name: 'Asphalt 02', author: 'Rob Tuytel', source: 'https://polyhaven.com/a/asphalt_02',
  license: 'CC0', physicalWidthMetres: 3,
  channels: {color: 'sRGB', normal: 'OpenGL tangent-space, linear', arm: 'R occlusion / G roughness / B metalness, linear'},
  modifications: 'Resized and compressed to WebP at two resolutions. No geometric displacement; visual and collision floor remain aligned.',
  files: provenance,
}, null, 2) + '\n')
console.log(provenance)
