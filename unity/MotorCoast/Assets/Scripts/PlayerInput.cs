using UnityEngine;
namespace MotorCoast {
public sealed class PlayerInput {
    public float TouchSteer, TouchGas, TouchBrake, TouchHandbrake;
    public int RequestedGear=1;
    public bool Blocked, TouchLeft, TouchRight;
    public void Clear() { TouchSteer=TouchGas=TouchBrake=TouchHandbrake=0; TouchLeft=TouchRight=false; }
    public DriveInput Sample(float speed) {
        if(Blocked) return new DriveInput { requestedGear=RequestedGear, speed=speed };
        float left=Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0;
        float right=Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0;
        bool forward=Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow);
        bool back=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow);
        float throttle=TouchGas, brake=TouchBrake;
        if(forward&&back){brake=1;throttle=0;}
        else if(forward) { RequestedGear=1; throttle=1; }
        else if(back) { if(speed>.65f) brake=1; else { RequestedGear=-1; throttle=1; } }
        return new DriveInput { steer=Mathf.Clamp(TouchSteer+right-left+(TouchRight?1:0)-(TouchLeft?1:0),-1,1), throttle=throttle, brake=brake,
            handbrake=Mathf.Max(TouchHandbrake,Input.GetKey(KeyCode.Space)?1:0), requestedGear=RequestedGear,speed=speed };
    }
}
}
