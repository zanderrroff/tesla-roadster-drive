using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace MotorCoast.Tests {
public sealed class VehicleTests {
    static VehicleCatalog Catalog()=>JsonUtility.FromJson<VehicleCatalog>(Resources.Load<TextAsset>("Data/vehicles").text);
    [Test] public void NativeAbiMatchesCpp() {
        Assert.AreEqual(24,Marshal.SizeOf<DriveInput>());Assert.AreEqual(12,Marshal.SizeOf<DriveState>());Assert.AreEqual(24,Marshal.SizeOf<DriveOutput>());
        DriveState s=default;DriveNative.MCReset(ref s);var input=new DriveInput{throttle=1,requestedGear=1};
        DriveNative.MCStep(ref s,ref input,1f/120,1,out var output);Assert.Greater(output.driveTorque,0);
    }
    [Test] public void CatalogContainsSevenCompleteCars() {
        var cars=Catalog().vehicles;Assert.AreEqual(7,cars.Length);Assert.AreEqual(7,cars.Select(c=>c.id).Distinct().Count());
        foreach(var c in cars) {Assert.AreEqual(4,c.wheelCenters.Length);Assert.Greater(c.wheelRadius,.25f);Assert.Less(c.wheelRadius,.4f);Assert.IsNotNull(Resources.Load<GameObject>(c.resource));}
    }
    [TestCase("roadster"),TestCase("bmw-m3-e30"),TestCase("mercedes-benz-amg-gt"),TestCase("mercedes-benz-190e-evo"),TestCase("nissan-skyline-gt-r"),TestCase("porsche-911-carrera"),TestCase("ferrari-458")]
    public void EveryDriverFitsCabinAndFollowsSteering(string id) {
        QualitySettings.skinWeights=SkinWeights.FourBones;
        foreach(var spec in Catalog().vehicles.Where(s=>s.id==id)) {
            var root=new GameObject("Vehicle fixture",typeof(Rigidbody),typeof(VehicleController));
            try {
                var car=root.GetComponent<VehicleController>();car.Configure(spec,Object.Instantiate(Resources.Load<GameObject>(spec.resource)),Resources.Load<GameObject>("Driver/driver"),new PlayerInput());
                var driver=root.GetComponentInChildren<DriverRig>();Assert.IsNotNull(driver);
                var steering=Nodes.Find(root.transform,"SteeringWheel");Assert.IsNotNull(steering);var neutral=steering.localRotation;
                foreach(float angle in new[]{-34f,0,34f,0}) {
                    driver.Animate(angle);float min=float.PositiveInfinity,max=float.NegativeInfinity;
                    foreach(var r in driver.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                        // Compensate renderer scale before TransformPoint; the
                        // default BakeMesh already bakes it into the vertices.
                        var mesh=new Mesh();r.BakeMesh(mesh,true);
                        var source=r.sharedMesh.vertices;var weights=r.sharedMesh.boneWeights;var bind=r.sharedMesh.bindposes;var bones=r.bones;var baked=mesh.vertices;
                        for(int n=0;n<source.Length;n+=127){var w=weights[n];var v=source[n];
                            var expected=bones[w.boneIndex0].TransformPoint(bind[w.boneIndex0].MultiplyPoint3x4(v))*w.weight0+bones[w.boneIndex1].TransformPoint(bind[w.boneIndex1].MultiplyPoint3x4(v))*w.weight1+bones[w.boneIndex2].TransformPoint(bind[w.boneIndex2].MultiplyPoint3x4(v))*w.weight2+bones[w.boneIndex3].TransformPoint(bind[w.boneIndex3].MultiplyPoint3x4(v))*w.weight3;
                            Assert.Less(Vector3.Distance(expected,r.transform.TransformPoint(baked[n])),.002f,spec.id+" skinning validation");
                        }
                        foreach(var v in mesh.vertices){var p=root.transform.InverseTransformPoint(r.transform.TransformPoint(v));Assert.IsFalse(float.IsNaN(p.y),spec.id);min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}
                        Object.DestroyImmediate(mesh);
                    }
                    Debug.Log(spec.id+" driver Y: "+min+" .. "+max+"; cabin: "+spec.floorY+" .. "+spec.roofY);
                    Assert.GreaterOrEqual(min,spec.floorY-.008f,spec.id+" feet/floor");Assert.LessOrEqual(max,spec.roofY+.004f,spec.id+" head/roof");
                    foreach(string side in new[]{"L","R"}){
                        var hand=Nodes.Find(driver.transform,"Bip01_"+side+"_Hand");float sign=side=="L"?-1:1;
                        var target=steering.TransformPoint(DriverRig.GripWristLocal(sign,spec.steeringRadius,spec.driverScale));
                        Assert.Less(Vector3.Distance(hand.position,target),.008f,spec.id+" wrist anchor");
                        var finger=Nodes.Find(driver.transform,"Bip01_"+side+"_Finger21");var contact=steering.InverseTransformPoint(finger.position);
                        float distance=Mathf.Sqrt(Mathf.Pow(new Vector2(contact.x,contact.y).magnitude-spec.steeringRadius,2)+contact.z*contact.z);
                        Assert.Less(distance,.045f,spec.id+" middle finger must wrap the rim rather than float over a spoke");
                    }
                }
                Assert.Less(Quaternion.Angle(neutral,steering.localRotation),.001f,spec.id+" steering drift");
            }finally{Object.DestroyImmediate(root);}
        }
    }
    [Test] public void AckermannWheelsShareTurningCentre() {
        foreach(float angle in new[]{-30f,-4f,4f,30f}) {
            float right=VehicleController.Ackermann(angle,2.7f,.78f),left=VehicleController.Ackermann(angle,2.7f,-.78f);
            float rightCentre=2.7f/Mathf.Tan(right*Mathf.Deg2Rad)+.78f,leftCentre=2.7f/Mathf.Tan(left*Mathf.Deg2Rad)-.78f;
            Assert.AreEqual(rightCentre,leftCentre,.001f);
            Assert.AreEqual(Mathf.Sign(angle),Mathf.Sign(right));
        }
    }
    [UnityTest] public IEnumerator SuddenHighSpeedSteeringFollowsAnArcWithoutRolling() {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        yield return new EnterPlayMode();
        var previous=Physics.simulationMode;var oldStep=Time.fixedDeltaTime;var oldTime=Time.timeScale;
        Physics.simulationMode=SimulationMode.Script;Time.fixedDeltaTime=1f/120;Time.timeScale=1;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-2,0);ground.transform.localScale=new Vector3(2000,4,2000);
        var root=new GameObject("Fast corner fixture",typeof(Rigidbody),typeof(VehicleController));
        try {
            var car=root.GetComponent<VehicleController>();var controls=new PlayerInput();
            car.Configure(Catalog().vehicles[0],Object.Instantiate(Resources.Load<GameObject>("Vehicles/roadster")),Resources.Load<GameObject>("Driver/driver"),controls);
            foreach(float entrySpeed in new[]{30f,45f}) {
                car.ResetToStart();Physics.SyncTransforms();
                for(int i=0;i<360;i++){car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);}
                // Accelerate the tyres and body together; assigning chassis
                // velocity alone leaves four stationary wheels dragging on asphalt.
                controls.TouchGas=1;
                for(int i=0;i<7200&&car.Speed<entrySpeed;i++){car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);}
                Assert.GreaterOrEqual(car.Speed,entrySpeed);
                controls.TouchGas=0;
                var start=car.Body.position;float maxYaw=0,maxRoll=0;
                controls.TouchSteer=1;
                for(int i=0;i<360;i++) {
                    car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);
                    maxYaw=Mathf.Max(maxYaw,Mathf.Abs(car.Body.angularVelocity.y));
                    maxRoll=Mathf.Max(maxRoll,Vector3.Angle(car.Body.rotation*Vector3.up,Vector3.up));
                    Assert.Greater(car.Body.position.y,.3f,"Suspension may not penetrate the flat road");
                }
                float heading=Mathf.DeltaAngle(0,car.Body.rotation.eulerAngles.y);
                Debug.Log($"Corner at {entrySpeed*3.6f:0} km/h: heading {heading:0.0}, peak yaw {maxYaw:0.00} rad/s, roll {maxRoll:0.0} deg");
                Assert.Greater(heading,8);Assert.Less(heading,100);Assert.Less(maxYaw,.85f);Assert.Less(maxRoll,18);
                Assert.Greater(car.Body.position.x-start.x,4,"A turn must produce a physical curved trajectory");
                controls.TouchSteer=-1;var before=car.Body.rotation;
                car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);
                Assert.Less(Quaternion.Angle(before,car.Body.rotation),1,"Opposite input may not instantly change heading");
                for(int i=0;i<360;i++){car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);}
                Assert.Greater(Vector3.Dot(car.Body.rotation*Vector3.up,Vector3.up),.95f,"Countersteering must keep the chassis upright");
            }
        }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(ground);Physics.simulationMode=previous;Time.fixedDeltaTime=oldStep;Time.timeScale=oldTime;}
    }
    [UnityTearDown] public IEnumerator ReturnToEditor(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator CarSettlesOnGroundAndResetClearsMomentum() {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        yield return new EnterPlayMode();
        var previous=Physics.simulationMode;var oldTime=Time.timeScale;var oldStep=Time.fixedDeltaTime;Physics.simulationMode=SimulationMode.Script;Time.timeScale=1;Time.fixedDeltaTime=1f/120;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-2,0);ground.transform.localScale=new Vector3(2000,4,2000);
        var root=new GameObject("Physics fixture",typeof(Rigidbody),typeof(VehicleController));
        try {
            var spec=Catalog().vehicles[0];var car=root.GetComponent<VehicleController>();var controls=new PlayerInput();
            car.Configure(spec,Object.Instantiate(Resources.Load<GameObject>(spec.resource)),Resources.Load<GameObject>("Driver/driver"),controls);Physics.SyncTransforms();
            float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
            for(int i=0;i<480;i++){car.SendMessage("FixedUpdate");Physics.Simulate(1f/120);if(i>=240){minimum=Mathf.Min(minimum,car.Body.position.y);maximum=Mathf.Max(maximum,car.Body.position.y);}}
            Assert.Greater(car.Body.position.y,.35f);Assert.Less(car.Body.position.y,1.0f);Assert.Less(Mathf.Abs(car.Body.linearVelocity.y),.08f);
            Assert.Less(maximum-minimum,.01f,"Settled suspension must not oscillate");
            controls.TouchGas=1;
            for(int i=0;i<360;i++){car.SendMessage("FixedUpdate");Physics.Simulate(1f/120);}
            Assert.Greater(car.Kph,10);
            var wheel=Nodes.Find(root.transform,"Wheel_0");car.SendMessage("LateUpdate");var wheelRotation=wheel.rotation;
            for(int i=0;i<12;i++){car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);}
            car.SendMessage("LateUpdate");Assert.Greater(Quaternion.Angle(wheelRotation,wheel.rotation),1,"Visible wheel must roll");
            controls.TouchGas=0;controls.TouchBrake=1;
            for(int i=0;i<600;i++){car.SendMessage("FixedUpdate");Physics.Simulate(1f/120);}
            Assert.Less(car.Kph,2);car.ResetToStart();Assert.Less(car.Body.linearVelocity.sqrMagnitude,.00001f);Assert.Less(car.Body.angularVelocity.sqrMagnitude,.00001f);
            controls.RequestedGear=-1;controls.TouchGas=1;
            for(int i=0;i<360;i++){car.SendMessage("FixedUpdate");Physics.Simulate(Time.fixedDeltaTime);}
            Assert.Less(car.Speed,-2,"Reverse must move backwards");
            controls.TouchGas=0;car.SendMessage("FixedUpdate");
            var suspension=root.GetComponentsInChildren<WheelCollider>();float coastingFront=suspension[0].brakeTorque,coastingRear=suspension[2].brakeTorque;
            controls.TouchHandbrake=1;car.SendMessage("FixedUpdate");
            Assert.Greater(suspension[2].brakeTorque-coastingRear,3000,"Handbrake must lock the rear axle");
            Assert.AreEqual(coastingFront,suspension[0].brakeTorque,"Handbrake must preserve front-wheel coasting drag");
            car.Suspend(true);Assert.AreEqual(0,controls.TouchGas);Assert.AreEqual(0,controls.TouchHandbrake);
            Time.timeScale=0;var position=root.transform.position;car.SendMessage("FixedUpdate");car.SendMessage("LateUpdate");Assert.AreEqual(position,root.transform.position,"Pause must not move the car");
        }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(ground);Physics.simulationMode=previous;Time.timeScale=oldTime;Time.fixedDeltaTime=oldStep;}
    }
}
}
