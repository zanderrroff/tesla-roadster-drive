using UnityEngine;
namespace MotorCoast {
public sealed class DriverRig : MonoBehaviour {
    Transform wheel, driver; Quaternion neutral; float radius, scale,gripAngle;
    Arm[] arms; Renderer[] headRenderers;
    struct Arm { public Transform upper,lower,hand; public Quaternion upperRest,lowerRest,handRest,handInWheel; public float sign; }
    public Vector3 EyeLocal {get; private set;}
    public void Configure(Transform model,Transform column,VehicleSpec spec,Transform chassis) {
        driver=model; wheel=column; neutral=column.localRotation.normalized; radius=spec.steeringRadius;scale=spec.driverScale;
        model.localPosition=spec.driverPosition; model.localScale=Vector3.one*scale;
        var avatar=model.GetComponentInChildren<CharacterVisual>();if(avatar)avatar.SeatPose();
        var head=Nodes.Find(model,"Bip01_Head");var spine=Nodes.Find(model,"Bip01_Spine");
        if(head && spine && spec.torsoLean!=0) {
            var rotation=head.rotation;
            spine.rotation=Quaternion.AngleAxis(spec.torsoLean*Mathf.Rad2Deg,chassis.right)*spine.rotation;
            head.rotation=rotation;
        }
        if(spec.fitLegs||avatar) foreach(string side in new[]{"L","R"}) {
            var thigh=Nodes.Find(model,"Bip01_"+side+"_Thigh");var calf=Nodes.Find(model,"Bip01_"+side+"_Calf");var foot=Nodes.Find(model,"Bip01_"+side+"_Foot");
            var rotation=foot.rotation;var target=chassis.InverseTransformPoint(foot.position);
            target.y=spec.floorY+.103f*scale+.034f;target.z=spec.fitLegs?spec.pedalZ:spec.wheelPosition.z+.22f;
            Reach(thigh,calf,foot,chassis.TransformPoint(target),chassis.up);foot.rotation=rotation;
        }
        if(avatar)for(int attempt=0;attempt<2;attempt++){
            float minimum=float.PositiveInfinity;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
                var baked=new Mesh();skin.BakeMesh(baked,true);foreach(var p in baked.vertices)minimum=Mathf.Min(minimum,chassis.InverseTransformPoint(skin.transform.TransformPoint(p)).y);
                if(Application.isPlaying)Destroy(baked);else DestroyImmediate(baked);
            }
            float correction=spec.floorY+.008f-minimum;if(correction<=.001f)break;
            foreach(string side in new[]{"L","R"}){var thigh=Nodes.Find(model,"Bip01_"+side+"_Thigh");var calf=Nodes.Find(model,"Bip01_"+side+"_Calf");var foot=Nodes.Find(model,"Bip01_"+side+"_Foot");var rotation=foot.rotation;Reach(thigh,calf,foot,foot.position+chassis.up*correction,chassis.up);foot.rotation=rotation;}
        }
        if(avatar){
            float bottom=float.PositiveInfinity,top=float.NegativeInfinity;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh,true);foreach(var p in mesh.vertices){float y=chassis.InverseTransformPoint(skin.transform.TransformPoint(p)).y;bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            // Small roadsters can put the pedal just beyond an avatar's leg
            // reach. Use remaining seat/headroom instead of burying the shoe.
            float lift=Mathf.Min(Mathf.Max(0,spec.floorY+.006f-bottom),Mathf.Max(0,spec.roofY-.004f-top));model.position+=chassis.up*lift;
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
        EyeLocal=chassis.InverseTransformPoint(le&&re?(le.position+re.position)*.5f:head.position+chassis.forward*.08f);
        var heads=new System.Collections.Generic.List<Renderer>();
        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        foreach(var r in model.GetComponentsInChildren<Renderer>()) {
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;
            foreach(var m in r.sharedMaterials) if(m.name.Contains("_head")||m.name.Contains("_opacity")) { heads.Add(r);break; }
        }
        headRenderers=heads.ToArray();Animate(0);
    }
    public void Cockpit(bool enabled) { if(headRenderers!=null) foreach(var r in headRenderers) r.enabled=!enabled; }
    public void Animate(float roadAngle) {
        if(arms==null)return;
        // Positive road-wheel yaw turns right; the rim turns clockwise from the driver view.
        float rimAngle=Mathf.Clamp(-roadAngle*13.5f,-450,450);
        wheel.localRotation=neutral*Quaternion.AngleAxis(rimAngle,Vector3.forward);
        // The column has a real 13.5:1 steering ratio. Hands release/regrip around
        // the upper half of the rim instead of twisting the wrists several turns.
        gripAngle=Mathf.Sin(rimAngle*Mathf.Deg2Rad*.75f)*45.8f;
        foreach(var a in arms) { a.upper.localRotation=a.upperRest;a.lower.localRotation=a.lowerRest;a.hand.localRotation=a.handRest; }
        foreach(var a in arms) {
            var target=WristTarget(a.sign);
            Reach(a.upper,a.lower,a.hand,target,null);a.hand.rotation=wheel.parent.rotation*neutral*Quaternion.AngleAxis(gripAngle,Vector3.forward)*a.handInWheel;
        }
    }
    public Vector3 WristTarget(float sign)=>wheel.position+wheel.parent.rotation*neutral*Quaternion.AngleAxis(gripAngle,Vector3.forward)*Vector3.Scale(GripWristLocal(sign,radius,scale),wheel.lossyScale);
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
