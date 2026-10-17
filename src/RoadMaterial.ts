import {useEffect, useMemo} from 'react'
import {useLoader, useThree} from '@react-three/fiber'
import {LinearEncoding, MeshStandardMaterial, RepeatWrapping, sRGBEncoding, TextureLoader, Vector2} from 'three'
import {useStore} from './store'

export function useRoadMaterial() {
  const quality = useStore(s => s.quality)
  const gl = useThree(s => s.gl)
  const level = quality === 'mobile' ? 'mobile' : 'high'
  const sources = useLoader(TextureLoader, ['color', 'normal', 'arm'].map(t => `/textures/asphalt/${t}-${level}.webp`))
  const result = useMemo(() => {
    const maps = sources.map((source, index) => {
      const texture = source.clone()
      texture.wrapS = texture.wrapT = RepeatWrapping
      texture.repeat.set(346 / 3, 346 / 3)
      texture.encoding = index === 0 ? sRGBEncoding : LinearEncoding
      texture.anisotropy = Math.min(gl.capabilities.getMaxAnisotropy(), quality === 'mobile' ? 4 : 8)
      texture.needsUpdate = true
      return texture
    })
    // Construct the material here: older R3F versions auto-mark ALL JSX texture
    // properties as sRGB, corrupting linear normal/roughness/occlusion channels.
    const material = new MeshStandardMaterial({
      map: maps[0], normalMap: maps[1], normalScale: new Vector2(.55, .55),
      roughnessMap: maps[2], roughness: 1, aoMap: maps[2], aoMapIntensity: .65,
      metalness: 0, envMapIntensity: .65,
    })
    return {material, dispose() {maps.forEach(t => t.dispose()); material.dispose()}}
  }, [sources, gl, quality])
  useEffect(() => () => result.dispose(), [result])
  return result.material
}
