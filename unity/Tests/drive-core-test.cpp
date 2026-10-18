#include "../MotorCoast/Assets/Plugins/iOS/DriveCore.h"
#include <cassert>
#include <cmath>
#include <iostream>
#include <limits>
MCOutput run(float dt, int count, MCState& state, MCInput in) {
    MCOutput out{}; for(int i=0;i<count;i++) MCStep(&state,&in,dt,1,&out); return out;
}
int main() {
    MCState a{},b{}; MCReset(&a); MCReset(&b);
    MCInput input{1,1,0,0,18,1};
    auto x=run(1.f/60,60,a,input), y=run(1.f/120,120,b,input);
    assert(std::abs(x.steerDegrees-y.steerDegrees)<.0001);
    assert(std::abs(x.driveTorque-y.driveTorque)<.01);
    MCState stopped{}; MCReset(&stopped); auto low=run(1.f/60,60,stopped,{1,1,0,0,0,1});
    assert(x.steerDegrees<low.steerDegrees*.7f && x.steerDegrees>3);
    // Full steering steps at motorway speed stay inside a plausible lateral
    // acceleration envelope, are continuous, and still allow parking lock.
    assert(low.steerDegrees>33.f);
    for(float speed : {15.f, 25.f, 40.f, 60.f}) {
        MCState corner{}; MCReset(&corner);
        auto turn=run(1.f/120,240,corner,{1,0,0,0,speed,1});
        float acceleration=speed*speed*std::tan(turn.steerDegrees/57.295779513f)/2.7f;
        assert(acceleration<=9.33f && acceleration>8.9f);
        float previous=turn.steerDegrees;
        turn=run(1.f/120,1,corner,{-1,0,0,0,speed,1});
        assert(turn.steerDegrees>0 && turn.steerDegrees<previous);
    }
    input.brake=1; MCStep(&a,&input,1.f/120,1,&x);
    assert(x.driveTorque==0 && x.brakeTorque>=1750);
    input={0,1,0,1,10,1}; MCStep(&a,&input,1.f/120,1,&x);
    assert(x.rearBrakeTorque>=3200 && x.rearGrip<.5 && x.driveTorque==0);
    input={0,1,0,0,20,-1}; MCStep(&a,&input,1.f/120,1,&x);
    assert(x.gear==1 && x.brakeTorque>=1750 && x.driveTorque==0);
    input.speed=.2; x=run(1.f/120,120,a,input);
    assert(x.gear==-1 && x.driveTorque<0);
    auto before=a; MCStep(&a,&input,0,1,&x);
    assert(a.steer==before.steer && a.throttle==before.throttle && a.gear==before.gear);
    input={-1,1,0,0,100,1}; x=run(1.f/60,200,b,input);
    assert(x.driveTorque==0 && x.steerDegrees>=-34 && x.steerDegrees<=34);
    input={NAN,INFINITY,-INFINITY,NAN,NAN,1}; MCStep(&a,&input,NAN,1,&x);
    assert(std::isfinite(x.driveTorque));
    MCStep(&a,&input,1.f/120,1,&x);assert(std::isfinite(x.driveTorque) && std::isfinite(x.steerDegrees));
    MCReset(&a); assert(a.gear==1 && a.steer==0 && a.throttle==0);
    std::cout<<"DriveCore: frame-rate invariance, braking, handbrake, safe reverse, pause and limits PASS\n";
}
