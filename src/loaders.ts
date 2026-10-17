import {useLoader} from '@react-three/fiber'
import {GLTFLoader} from 'three/examples/jsm/loaders/GLTFLoader'
import {DRACOLoader} from 'three/examples/jsm/loaders/DRACOLoader'
const draco=new DRACOLoader().setDecoderPath('/draco/')
export function useModel(url:string){return useLoader(GLTFLoader,url,loader=>(loader as GLTFLoader).setDRACOLoader(draco))}
