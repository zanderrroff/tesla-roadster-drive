import {Object3D, Quaternion, Vector3} from 'three'

const zAxis = new Vector3(0, 0, 1)
export const steeringWheelAngle = (roadWheelAngle: number) => Math.max(-.8, Math.min(.8, -roadWheelAngle * 1.65))

/** Keep an authored steering column's tilt; rotate only around its local axle. */
export function steeringRig(wheel: Object3D) {
  const neutral = wheel.quaternion.clone().normalize()
  const turn = new Quaternion()
  return (angle: number) => {
    wheel.quaternion.copy(neutral).multiply(turn.setFromAxisAngle(zAxis, angle))
    wheel.updateWorldMatrix(true, true)
  }
}

function setWorldQuaternion(bone: Object3D, rotation: Quaternion) {
  const parent = bone.parent?.getWorldQuaternion(new Quaternion()) || new Quaternion()
  bone.quaternion.copy(parent.invert().multiply(rotation)).normalize()
  bone.updateWorldMatrix(false, true)
}

export interface DriverFit {position: number[]; scale: number; torsoLean: number; floorY?: number; pedalZ?: number}
export function fitDriver(driver: Object3D, fit: DriverFit) {
  driver.position.fromArray(fit.position)
  driver.scale.setScalar(fit.scale)
  driver.updateMatrixWorld(true)
  const spine = driver.getObjectByName('Bip01_Spine')
  const head = driver.getObjectByName('Bip01_Head')
  if (spine && head && fit.torsoLean) {
    const headRotation = head.getWorldQuaternion(new Quaternion())
    const lean = new Quaternion().setFromAxisAngle(new Vector3(1, 0, 0), fit.torsoLean)
    setWorldQuaternion(spine, lean.multiply(spine.getWorldQuaternion(new Quaternion())))
    setWorldQuaternion(head, headRotation)
  }
  if (fit.floorY !== undefined && fit.pedalZ !== undefined) {
    for (const side of ['L', 'R']) {
      const thigh = driver.getObjectByName(`Bip01_${side}_Thigh`)!
      const calf = driver.getObjectByName(`Bip01_${side}_Calf`)!
      const foot = driver.getObjectByName(`Bip01_${side}_Foot`)!
      const rotation = foot.getWorldQuaternion(new Quaternion())
      const target = foot.getWorldPosition(new Vector3())
      target.y = fit.floorY + .103 * fit.scale + .034
      target.z = fit.pedalZ
      reach(thigh, calf, foot, target, new Vector3(0, 1, 0))
      setWorldQuaternion(foot, rotation)
    }
  }
}

function aim(bone: Object3D, child: Object3D, target: Vector3) {
  const origin = bone.getWorldPosition(new Vector3())
  const current = child.getWorldPosition(new Vector3()).sub(origin).normalize()
  const wanted = target.clone().sub(origin).normalize()
  const rotation = new Quaternion().setFromUnitVectors(current, wanted)
  setWorldQuaternion(bone, rotation.multiply(bone.getWorldQuaternion(new Quaternion())))
}

/** Analytic two-bone IK: preserve limb lengths and bend toward the authored elbow. */
export function reach(upper: Object3D, lower: Object3D, hand: Object3D, target: Vector3, bendDirection?: Vector3) {
  const shoulder = upper.getWorldPosition(new Vector3())
  const elbow = lower.getWorldPosition(new Vector3())
  const wrist = hand.getWorldPosition(new Vector3())
  const a = shoulder.distanceTo(elbow), b = elbow.distanceTo(wrist)
  const direction = target.clone().sub(shoulder)
  const distance = Math.max(Math.abs(a - b) + 1e-5, Math.min(a + b - 1e-5, direction.length()))
  direction.normalize()
  const pole = bendDirection?.clone() || elbow.clone().sub(shoulder)
  pole.addScaledVector(direction, -pole.dot(direction))
  if (pole.lengthSq() < 1e-8) pole.set(0, -1, 0).addScaledVector(direction, direction.y)
  pole.normalize()
  const along = (a * a - b * b + distance * distance) / (2 * distance)
  const bend = Math.sqrt(Math.max(0, a * a - along * along))
  const joint = shoulder.clone().addScaledVector(direction, along).addScaledVector(pole, bend)
  const endpoint = shoulder.clone().addScaledVector(direction, distance)
  aim(upper, lower, joint)
  aim(lower, hand, endpoint)
}

/** No separate world-space driver transform: the complete rig stays a chassis child. */
export function driverSteeringRig(driver: Object3D, wheel: Object3D, radius: number) {
  driver.updateWorldMatrix(true, true)
  wheel.updateWorldMatrix(true, true)
  const wheelRotation = wheel.getWorldQuaternion(new Quaternion())
  const arms = (['L', 'R'] as const).map((side, index) => {
    const upper = driver.getObjectByName(`Bip01_${side}_UpperArm`)
      || driver.getObjectByName(`Bip01 ${side} UpperArm`)
    const lower = driver.getObjectByName(`Bip01_${side}_Forearm`)
      || driver.getObjectByName(`Bip01 ${side} Forearm`)
    const hand = driver.getObjectByName(`Bip01_${side}_Hand`)
      || driver.getObjectByName(`Bip01 ${side} Hand`)
    if (!upper || !lower || !hand) throw new Error(`Driver arm missing: ${side}`)
    return {
      upper, lower, hand, sign: index === 0 ? 1 : -1,
      upperRest: upper.quaternion.clone(), lowerRest: lower.quaternion.clone(),
      handRest: hand.quaternion.clone(),
      handInWheel: wheelRotation.clone().invert().multiply(hand.getWorldQuaternion(new Quaternion())),
    }
  })
  const target = new Vector3(), handRotation = new Quaternion()
  return () => {
    // Always solve from the same seated pose. Incremental rotations cause drift.
    for (const arm of arms) {
      arm.upper.quaternion.copy(arm.upperRest)
      arm.lower.quaternion.copy(arm.lowerRest)
      arm.hand.quaternion.copy(arm.handRest)
    }
    driver.updateWorldMatrix(true, true)
    for (const arm of arms) {
      target.set(arm.sign * radius * 1.0595, radius * .0532, -.1137 * driver.scale.x)
      wheel.localToWorld(target)
      reach(arm.upper, arm.lower, arm.hand, target)
      handRotation.copy(wheel.getWorldQuaternion(new Quaternion())).multiply(arm.handInWheel)
      setWorldQuaternion(arm.hand, handRotation)
    }
  }
}
