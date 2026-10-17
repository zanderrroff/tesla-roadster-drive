#pragma once
#ifdef __cplusplus
extern "C" {
#endif
struct MCInput { float steer, throttle, brake, handbrake, speed; int requestedGear; };
struct MCState { float steer, throttle; int gear; };
struct MCOutput { float steerDegrees, driveTorque, brakeTorque, rearBrakeTorque, rearGrip; int gear; };
void MCReset(MCState* state);
void MCStep(MCState* state, const MCInput* input, float dt, float power, MCOutput* output);
#ifdef __cplusplus
}
#endif
