using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace MotorCoast {
[RequireComponent(typeof(Rigidbody))]
public sealed class VehicleController : MonoBehaviour {
    public const int VehicleLayer=8;
    public VehicleSpec Spec {get; private set;}
    public Rigidbody Body {get; private set;}
    public float Speed => Vector3.Dot(Body.linearVelocity,transform.forward);
    public float Kph => Body.linearVelocity.magnitude*3.6f;
    public float Slip {get; private set;}
    public int Grounded {get; private set;}
    public int Gear => output.gear;
    public float Throttle => state.throttle;
    public Vector3 EyeLocal => driver.EyeLocal;
    public event Action<float> Impact;
    public PlayerInput Controls;
    readonly WheelCollider[] wheels=new WheelCollider[4];
    readonly Transform[] visuals=new Transform[4];
    readonly Quaternion[] rest=new Quaternion[4];
    DriverRig driver; DriveState state; DriveOutput output;
    Vector3 spawn; float flippedTime,wheelbase; bool suspended;
    public void Configure(VehicleSpec spec,GameObject model,GameObject driverPrefab,PlayerInput controls) {
        Spec=spec;Controls=controls;wheelbase=Mathf.Abs((spec.wheelCenters[0].z+spec.wheelCenters[1].z-spec.wheelCenters[2].z-spec.wheelCenters[3].z)*.5f);spawn=new Vector3(0,spec.wheelRadius-Mathf.Min(spec.wheelCenters[0].y,spec.wheelCenters[2].y)+.008f,-80);Body=GetComponent<Rigidbody>();
        Body.mass=spec.mass;Body.centerOfMass=new Vector3(0,-.14f,-.10f);
        Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        Body.solverIterations=12;Body.solverVelocityIterations=6;Body.linearDamping=.025f;Body.angularDamping=.45f;
        Body.maxAngularVelocity=6;Body.maxDepenetrationVelocity=3;
        var hull=gameObject.AddComponent<BoxCollider>();hull.size=spec.colliderSize;hull.center=new Vector3(0,-.035f,0);
        var roof=gameObject.AddComponent<BoxCollider>();roof.size=new Vector3(spec.colliderSize.x*.70f,.33f,spec.colliderSize.z*.40f);roof.center=new Vector3(0,.30f,-.2f);
        model.transform.SetParent(transform,false);model.transform.localPosition=Vector3.zero;
        var chassis=Nodes.Find(model.transform,"Chassis");
        if(!chassis)throw new InvalidOperationException("Model has no Chassis: "+spec.id);
        for(int i=0;i<4;i++) {
            visuals[i]=Nodes.Find(model.transform,"Wheel_"+i);
            if(!visuals[i])throw new InvalidOperationException("Model has no wheel: "+i);
            rest[i]=visuals[i].localRotation;
            var go=new GameObject("Suspension_"+i);go.transform.SetParent(transform,false);go.transform.localPosition=spec.wheelCenters[i]+Vector3.up*.11f;
            var wc=go.AddComponent<WheelCollider>();wheels[i]=wc;wc.radius=spec.wheelRadius;wc.mass=25;
            wc.suspensionDistance=.22f;wc.wheelDampingRate=1.6f;wc.forceAppPointDistance=.15f;
            wc.suspensionSpring=new JointSpring { spring=35000,damper=5000,targetPosition=.5f };
            wc.forwardFriction=new WheelFrictionCurve { extremumSlip=.35f,extremumValue=1,asymptoteSlip=.8f,asymptoteValue=.75f,stiffness=1.6f };
            wc.sidewaysFriction=new WheelFrictionCurve { extremumSlip=.22f,extremumValue=1,asymptoteSlip=.55f,asymptoteValue=.78f,stiffness=1.32f };
            wc.ConfigureVehicleSubsteps(8,8,4);
        }
        Transform steering=Nodes.Find(chassis,"SteeringWheel");
        if(!spec.hasInterior) steering=CabinBuilder.Build(chassis,spec);
        if(!steering)throw new InvalidOperationException("Steering wheel missing: "+spec.id);
        var character=new GameObject("Driver");character.transform.SetParent(chassis,false);Instantiate(driverPrefab,character.transform,false);
        driver=character.AddComponent<DriverRig>();driver.Configure(character.transform,steering,spec,transform);
        foreach(var r in model.GetComponentsInChildren<Renderer>()) {
            r.receiveShadows=true;r.shadowCastingMode=ShadowCastingMode.On;
            foreach(var m in r.sharedMaterials) if(m.renderQueue>=3000) { r.shadowCastingMode=ShadowCastingMode.Off;break; }
        }
        Nodes.Layer(gameObject,VehicleLayer);DriveNative.MCReset(ref state);ResetToStart();
    }
    void FixedUpdate() {
        if(Spec==null||suspended||Time.timeScale==0)return;
        var input=Controls.Sample(Speed);DriveNative.MCStep(ref state,ref input,Time.fixedDeltaTime,Spec.power,out output);
        Grounded=0;Slip=0;
        float centreAngle=Mathf.Atan(Mathf.Tan(output.steerDegrees*Mathf.Deg2Rad)*wheelbase/2.7f)*Mathf.Rad2Deg;
        for(int i=0;i<4;i++) {
            var wc=wheels[i];bool front=i<2;
            wc.steerAngle=front?Ackermann(centreAngle,wheelbase,Spec.wheelCenters[i].x):0;
            wc.motorTorque=output.driveTorque*(front?.15f:.35f);
            wc.brakeTorque=front?output.brakeTorque:output.rearBrakeTorque;
            var grip=wc.sidewaysFriction;grip.stiffness=1.32f*(front?1:output.rearGrip);wc.sidewaysFriction=grip;
            if(wc.GetGroundHit(out var hit)) {
                Grounded++;Slip=Mathf.Max(Slip,Mathf.Abs(hit.sidewaysSlip));
                // Reduce wheelspin and release a locking service brake. Rear
                // handbrake remains deliberately exempt from ABS for drifting.
                if(Mathf.Abs(hit.forwardSlip)>.3f)wc.motorTorque*=Mathf.Clamp01(.3f/Mathf.Abs(hit.forwardSlip));
                if(Mathf.Abs(Speed)>5&&Mathf.Abs(hit.forwardSlip)>.45f&&(front||input.handbrake<.1f))
                    wc.brakeTorque*=Mathf.Clamp(.45f/Mathf.Abs(hit.forwardSlip),.25f,1);
            }
        }
        AntiRoll(0,1);AntiRoll(2,3);
        if(Grounded>1)Body.AddForce(-transform.up*Mathf.Min(Body.linearVelocity.sqrMagnitude*.5f,Body.mass*4f));
        flippedTime=Vector3.Dot(transform.up,Vector3.up)<.15f && Kph<5 ? flippedTime+Time.fixedDeltaTime : 0;
        if(transform.position.y < -12 || transform.position.sqrMagnitude>2500000 || flippedTime>3)ResetToStart();
    }
    public static float Ackermann(float centreDegrees,float wheelbase,float lateralPosition) {
        if(Mathf.Abs(centreDegrees)<.001f)return 0;
        float sign=Mathf.Sign(centreDegrees),radius=wheelbase/Mathf.Tan(Mathf.Abs(centreDegrees)*Mathf.Deg2Rad);
        return sign*Mathf.Atan(wheelbase/Mathf.Max(.5f,radius-sign*lateralPosition))*Mathf.Rad2Deg;
    }
    void AntiRoll(int left,int right) {
        bool a=wheels[left].GetGroundHit(out var ha),b=wheels[right].GetGroundHit(out var hb);
        float ta=a?(-wheels[left].transform.InverseTransformPoint(ha.point).y-wheels[left].radius)/wheels[left].suspensionDistance:1;
        float tb=b?(-wheels[right].transform.InverseTransformPoint(hb.point).y-wheels[right].radius)/wheels[right].suspensionDistance:1;
        float force=Mathf.Clamp(ta-tb,-1,1)*3800;
        if(a)Body.AddForceAtPosition(-transform.up*force,wheels[left].transform.position);
        if(b)Body.AddForceAtPosition(transform.up*force,wheels[right].transform.position);
    }
    void LateUpdate() {
        if(Spec==null||suspended||Time.timeScale==0)return;
        for(int i=0;i<4;i++) {
            wheels[i].GetWorldPose(out var position,out var rotation);
            // GetWorldPose is sampled in the physics frame; use interpolated chassis for smooth render motion.
            var localPosition=Quaternion.Inverse(Body.rotation)*(position-Body.position);
            var localRotation=Quaternion.Inverse(Body.rotation)*rotation;
            visuals[i].position=transform.TransformPoint(localPosition);
            visuals[i].rotation=transform.rotation*localRotation*rest[i];
        }
        driver.Animate(output.steerDegrees);
    }
    public void SetCockpit(bool value)=>driver.Cockpit(value);
    public void Suspend(bool value) { suspended=value;Controls.Clear(); }
    public void ResetToStart() {
        Controls.Clear();Controls.RequestedGear=1;DriveNative.MCReset(ref state);output=default;
        Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;
        Body.position=spawn;Body.rotation=Quaternion.identity;
        transform.SetPositionAndRotation(spawn,Quaternion.identity);flippedTime=0;
        foreach(var w in wheels) { w.motorTorque=0;w.brakeTorque=0;w.steerAngle=0; }
        Body.WakeUp();
    }
    void OnCollisionEnter(Collision c) { if(c.relativeVelocity.magnitude>1.5f)Impact?.Invoke(c.relativeVelocity.magnitude); }
}
}
