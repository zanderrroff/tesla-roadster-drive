export type Gear = 'D' | 'N' | 'R'
export function canSelectGear(current: Gear, next: Gear, speed: number) {
  return current === next || next === 'N' || Math.abs(speed) < 1.5
}
export function driveForces(active: boolean, throttle: boolean, brake: boolean, handbrake: boolean, gear: Gear, signedSpeed: number) {
  const stopped = Math.abs(signedSpeed) < .8
  let force = 0
  if (active && throttle && !brake && !handbrake) {
    if (gear === 'D' && signedSpeed < 58) force = -1900
    if (gear === 'R' && signedSpeed > -10) force = 1150
  }
  const service = active && brake ? 12 : 0
  const parking = active && handbrake ? 18 : 0
  return { force, frontBrake: Math.max(service, stopped ? parking : 0), rearBrake: Math.max(service, parking) }
}
