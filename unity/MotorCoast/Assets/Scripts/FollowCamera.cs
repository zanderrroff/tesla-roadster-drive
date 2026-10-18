using UnityEngine;
namespace MotorCoast {
[DefaultExecutionOrder(100)]
public sealed class FollowCamera : MonoBehaviour {
    public VehicleController Target;
    public Transform Person;
    public int Mode {get; private set;}
    float orbitYaw,orbitPitch=12,distance=6.7f,releaseAt;
    Vector3 velocity;bool snap=true;Camera lens;
    public void Configure(VehicleController target) { Target=target;lens=GetComponent<Camera>();snap=true;target.SetCockpit(Mode==2); }
    public void Cycle() { if(Person){orbitYaw=Person.eulerAngles.y;orbitPitch=16;snap=true;return;}Mode=(Mode+1)%3;Target.SetCockpit(Mode==2);orbitYaw=0;orbitPitch=12;snap=true; }
    public void Orbit(Vector2 delta) { orbitYaw+=delta.x*.16f;orbitPitch=Mathf.Clamp(orbitPitch-delta.y*.12f,-12,65);releaseAt=Time.unscaledTime+4; }
    public void Zoom(float delta) { distance=Mathf.Clamp(distance-delta*.012f,3.2f,13);releaseAt=Time.unscaledTime+5; }
    public void Snap()=>snap=true;
    public void FollowPerson(Transform person){Person=person;if(Target)Target.SetCockpit(false);orbitYaw=transform.eulerAngles.y;orbitPitch=16;velocity=Vector3.zero;snap=true;}
    public void FollowVehicle(){Person=null;orbitYaw=0;orbitPitch=12;if(Target)Target.SetCockpit(Mode==2);snap=true;}
    void LateUpdate() {
        if(!Target||Time.timeScale==0)return;
        if(Person){UpdatePerson();return;}
        float dt=Time.deltaTime;var body=Target.transform;
        if(Time.unscaledTime>releaseAt) { orbitYaw=Mathf.LerpAngle(orbitYaw,0,1-Mathf.Exp(-1.3f*dt));orbitPitch=Mathf.Lerp(orbitPitch,12,1-Mathf.Exp(-1.3f*dt)); }
        Vector3 position,focus;Quaternion rotation;
        if(Mode==2) {
            position=body.TransformPoint(Target.EyeLocal+new Vector3(0,.025f,.025f));
            rotation=body.rotation*Quaternion.Euler(Mathf.Clamp(-orbitPitch+12,-45,35),Mathf.Clamp(orbitYaw,-110,110),0);
            transform.SetPositionAndRotation(position,rotation);lens.nearClipPlane=.025f;lens.fieldOfView=74;snap=false;return;
        }
        lens.nearClipPlane=.15f;
        focus=body.position+Vector3.up*.6f;
        float yaw=body.eulerAngles.y+orbitYaw;
        var orbit=Quaternion.Euler(Mode==1?50:orbitPitch,yaw,0);
        position=focus+orbit*new Vector3(0,.8f,-(Mode==1?distance*1.55f:distance));
        var direction=position-focus;float range=direction.magnitude;
        if(Physics.SphereCast(focus,.22f,direction.normalized,out var hit,range,~(1<<VehicleController.VehicleLayer),QueryTriggerInteraction.Ignore))position=focus+direction.normalized*Mathf.Max(.5f,hit.distance-.15f);
        position.y=Mathf.Max(.3f,position.y);
        if(snap) { transform.position=position;velocity=Vector3.zero; }
        else transform.position=Vector3.SmoothDamp(transform.position,position,ref velocity,.12f,1000,dt);
        rotation=Quaternion.LookRotation(focus-transform.position,Vector3.up);
        transform.rotation=snap?rotation:Quaternion.Slerp(transform.rotation,rotation,1-Mathf.Exp(-14*dt));
        lens.fieldOfView=Mathf.Lerp(lens.fieldOfView,60+Mathf.Clamp(Target.Kph/35,0,7),1-Mathf.Exp(-3*dt));snap=false;
    }
    void UpdatePerson(){
        lens.nearClipPlane=.08f;var focus=Person.position+Vector3.up*1.15f;
        var direction=Quaternion.Euler(Mathf.Clamp(orbitPitch,-12,65),orbitYaw,0)*new Vector3(.25f,.15f,-Mathf.Clamp(distance*.60f,2,5));
        var position=focus+direction;
        if(Physics.SphereCast(focus,.18f,direction.normalized,out var hit,direction.magnitude,~(1<<OnFootController.PlayerLayer),QueryTriggerInteraction.Ignore))position=focus+direction.normalized*Mathf.Max(.35f,hit.distance-.1f);
        transform.position=snap?position:Vector3.SmoothDamp(transform.position,position,ref velocity,.09f,1000,Time.deltaTime);
        transform.rotation=Quaternion.LookRotation(focus-transform.position);lens.fieldOfView=62;snap=false;
    }
}
}
