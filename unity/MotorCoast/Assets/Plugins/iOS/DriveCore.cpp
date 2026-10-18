#include "DriveCore.h"
#include <algorithm>
#include <cmath>
namespace {
float finite(float x) { return std::isfinite(x) ? x : 0.f; }
float clamp(float x, float lo, float hi) { return std::max(lo, std::min(hi, finite(x))); }
float damp(float current, float target, float rate, float dt) {
    return target + (current - target) * std::exp(-rate * dt);
}
}
extern "C" void MCReset(MCState* s) { if (s) *s = {0.f, 0.f, 1}; }
extern "C" void MCStep(MCState* s, const MCInput* i, float dt, float power, MCOutput* o) {
    if (!s || !i || !o) return;
    *o = {0.f, 0.f, 0.f, 0.f, 1.f, s->gear};
    dt = clamp(dt, 0.f, .05f);
    if (dt == 0.f) return; // Pause never accumulates input or advances steering.
    float speed = finite(i->speed), absoluteSpeed = std::abs(speed);
    float brake = clamp(i->brake, 0.f, 1.f), handbrake = clamp(i->handbrake, 0.f, 1.f);
    int requested = std::max(-1, std::min(1, i->requestedGear));
    // Reversing direction at speed first brakes to a stop. Never apply reverse torque at speed.
    if (requested != s->gear) {
        if (absoluteSpeed < .65f) s->gear = requested;
        else brake = 1.f;
    }
    float rawSteer = clamp(i->steer, -1.f, 1.f);
    // Fine control around centre without reducing full-lock parking manoeuvres.
    float desiredSteer = rawSteer * (.45f + .55f * rawSteer * rawSteer);
    s->steer = damp(finite(s->steer), desiredSteer, absoluteSpeed > 20.f ? 4.5f : 8.f, dt);
    float throttle = brake > .05f || handbrake > .5f || s->gear == 0 ? 0.f : clamp(i->throttle, 0.f, 1.f);
    s->throttle = damp(finite(s->throttle), throttle, throttle == 0.f ? 25.f : 6.f, dt);
    // Bicycle-model steering envelope for a 2.7 m wheelbase and 0.95 g.
    // C# scales curvature to each actual wheelbase before applying Ackermann.
    // No high-speed angle floor: 9 degrees at 140 km/h would demand over 8 g.
    constexpr float radiansToDegrees = 57.295779513f;
    float maxSteer = std::min(34.f, std::atan(9.3195f * 2.7f / std::max(1.f, absoluteSpeed * absoluteSpeed)) * radiansToDegrees);
    o->steerDegrees = s->steer * maxSteer;
    float limit = s->gear < 0 ? 12.f : 68.f;
    float torqueFade = clamp((limit - absoluteSpeed) / 12.f, 0.f, 1.f);
    // Total wheel torque, split by C# over the four WheelColliders.
    o->driveTorque = brake > .05f || handbrake > .5f ? 0.f : s->throttle * 2200.f * clamp(power, .4f, 2.f) * torqueFade * s->gear;
    o->brakeTorque = 1750.f * brake + (throttle < .01f && absoluteSpeed > .2f ? 24.f : 0.f);
    o->rearBrakeTorque = o->brakeTorque + 3200.f * handbrake;
    o->rearGrip = 1.f - .58f * handbrake;
    o->gear = s->gear;
}
