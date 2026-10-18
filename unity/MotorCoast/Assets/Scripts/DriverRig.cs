using UnityEngine;
namespace MotorCoast {
public sealed class DriverRig : MonoBehaviour {
    Transform wheel, driver; Quaternion neutral; float radius, scale;
    Arm[] arms; Renderer[] headRenderers;
    struct Arm { public Transform upper,lower,hand; public Quaternion upperRest,lowerRest,handRest,handInWheel; public float sign; }
    public Vector3 EyeLocal {get; private set;}
    public void Configure(Transform model,Transform column,VehicleSpec spec,Transform chassis) {
        driver=model; wheel=column; neutral=column.localRotation.normalized; radius=spec.steeringRadius;scale=spec.driverScale;
        model.localPosition=spec.driverPosition; model.localScale=Vector3.one*scale;
        var head=Nodes.Find(model,"Bip01_Head");var spine=Nodes.Find(model,"Bip01_Spine");
        if(head && spine && spec.torsoLean!=0) {
            var rotation=head.rotation;
            spine.rotation=Quaternion.AngleAxis(spec.torsoLean*Mathf.Rad2Deg,chassis.right)*spine.rotation;
            head.rotation=rotation;
        }
        if(spec.fitLegs) foreach(string side in new[]{"L","R"}) {
            var thigh=Nodes.Find(model,"Bip01_"+side+"_Thigh");var calf=Nodes.Find(model,"Bip01_"+side+"_Calf");var foot=Nodes.Find(model,"Bip01_"+side+"_Foot");
            var rotation=foot.rotation;var target=chassis.InverseTransformPoint(foot.position);
            target.y=spec.floorY+.103f*scale+.034f;target.z=spec.pedalZ;
            Reach(thigh,calf,foot,chassis.TransformPoint(target),chassis.up);foot.rotation=rotation;
        }
        arms=new Arm[2];int i=0;
        foreach(string side in new[]{"L","R"}) {
            var upper=Nodes.Find(model,"Bip01_"+side+"_UpperArm");var lower=Nodes.Find(model,"Bip01_"+side+"_Forearm");var hand=Nodes.Find(model,"Bip01_"+side+"_Hand");
            arms[i]=new Arm { upper=upper,lower=lower,hand=hand,upperRest=upper.localRotation,lowerRest=lower.localRotation,handRest=hand.localRotation,
                handInWheel=Quaternion.LookRotation((i==0?-1:1)*Vector3.up,(i==0?1:-1)*Vector3.right),sign=i==0?-1:1 };i++;
        }
        // The imported relaxed pose leaves fingers open. Close each original
        // finger chain around the rim, keeping the thumb on the upper/inner edge.
        foreach(string side in new[]{"L","R"})for(int finger=1;finger<=4;finger++) {
            foreach(string suffix in new[]{"","1","2"}) {
                var joint=Nodes.Find(model,"Bip01_"+side+"_Finger"+finger+suffix);
                if(joint)joint.localRotation*=Quaternion.AngleAxis(suffix==""?-7:suffix=="1"?-28:-18,Vector3.forward);
            }
        }
        var le=Nodes.Find(model,"Bip01_LEye");var re=Nodes.Find(model,"Bip01_REye");
        EyeLocal=chassis.InverseTransformPoint((le.position+re.position)*.5f);
        var heads=new System.Collections.Generic.List<Renderer>();
        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        foreach(var r in model.GetComponentsInChildren<Renderer>()) {
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;
            foreach(var m in r.sharedMaterials) if(m.name.Contains("m002_head")||m.name.Contains("m002_opacity")) { heads.Add(r);break; }
        }
        headRenderers=heads.ToArray();Animate(0);
    }
    public void Cockpit(bool enabled) { if(headRenderers!=null) foreach(var r in headRenderers) r.enabled=!enabled; }
    public void Animate(float roadAngle) {
        if(arms==null)return;
        // Positive road-wheel yaw turns right; the rim turns clockwise from the driver view.
        wheel.localRotation=neutral*Quaternion.AngleAxis(Mathf.Clamp(-roadAngle*1.65f,-45.8f,45.8f),Vector3.forward);
        foreach(var a in arms) { a.upper.localRotation=a.upperRest;a.lower.localRotation=a.lowerRest;a.hand.localRotation=a.handRest; }
        foreach(var a in arms) {
            var target=wheel.TransformPoint(GripWristLocal(a.sign,radius,scale));
            Reach(a.upper,a.lower,a.hand,target,null);a.hand.rotation=wheel.rotation*a.handInWheel;
        }
    }
    public static Vector3 GripWristLocal(float sign,float rimRadius,float driverScale)=>new Vector3(sign*(rimRadius+.014f),rimRadius*.08f,-.095f*driverScale);
    static void Aim(Transform bone,Transform child,Vector3 target) {
        bone.rotation=Quaternion.FromToRotation(child.position-bone.position,target-bone.position)*bone.rotation;
    }
    public static void Reach(Transform upper,Transform lower,Transform end,Vector3 target,Vector3? bendDirection) {
        var origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
        var delta=target-origin;float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.00001f,a+b-.00001f);var direction=delta.normalized;
        var pole=bendDirection??(lower.position-origin);pole-=direction*Vector3.Dot(pole,direction);
        if(pole.sqrMagnitude<.00000001f)pole=Vector3.Cross(direction,Vector3.right);
        pole.Normalize();float along=(a*a-b*b+length*length)/(2*length),bend=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Aim(upper,lower,origin+direction*along+pole*bend);Aim(lower,end,origin+direction*length);
    }
}
}
