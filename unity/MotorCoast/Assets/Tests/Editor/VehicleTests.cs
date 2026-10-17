using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
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
    [Test] public void EveryDriverFitsCabinAndFollowsSteering() {
        foreach(var spec in Catalog().vehicles) {
            var root=new GameObject("Vehicle fixture",typeof(Rigidbody),typeof(VehicleController));
            try {
                var car=root.GetComponent<VehicleController>();car.Configure(spec,Object.Instantiate(Resources.Load<GameObject>(spec.resource)),Resources.Load<GameObject>("Driver/driver"),new PlayerInput());
                var driver=root.GetComponentInChildren<DriverRig>();Assert.IsNotNull(driver);
                var steering=Nodes.Find(root.transform,"SteeringWheel");Assert.IsNotNull(steering);var neutral=steering.localRotation;
                foreach(float angle in new[]{-34f,0,34f,0}) {
                    driver.Animate(angle);float min=float.PositiveInfinity,max=float.NegativeInfinity;
                    foreach(var r in driver.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                        var mesh=new Mesh();r.BakeMesh(mesh);
                        foreach(var v in mesh.vertices){var p=root.transform.InverseTransformPoint(r.transform.TransformPoint(v));Assert.IsFalse(float.IsNaN(p.y),spec.id);min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}
                        Object.DestroyImmediate(mesh);
                    }
                    Assert.GreaterOrEqual(min,spec.floorY-.008f,spec.id+" feet/floor");Assert.LessOrEqual(max,spec.roofY+.004f,spec.id+" head/roof");
                    foreach(string side in new[]{"L","R"}){
                        var hand=Nodes.Find(driver.transform,"Bip01_"+side+"_Hand");float sign=side=="L"?-1:1;
                        var target=steering.TransformPoint(new Vector3(sign*spec.steeringRadius*1.0595f,spec.steeringRadius*.0532f,-.1137f*spec.driverScale));
                        Assert.Less(Vector3.Distance(hand.position,target),.008f,spec.id+" hand grip");
                    }
                }
                Assert.Less(Quaternion.Angle(neutral,steering.localRotation),.001f,spec.id+" steering drift");
            }finally{Object.DestroyImmediate(root);}
        }
    }
    [Test] public void CarSettlesOnGroundAndResetClearsMomentum() {
        var previous=Physics.simulationMode;var oldTime=Time.timeScale;Physics.simulationMode=SimulationMode.Script;Time.timeScale=1;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-2,0);ground.transform.localScale=new Vector3(2000,4,2000);
        var root=new GameObject("Physics fixture",typeof(Rigidbody),typeof(VehicleController));
        try {
            var spec=Catalog().vehicles[0];var car=root.GetComponent<VehicleController>();var controls=new PlayerInput();
            car.Configure(spec,Object.Instantiate(Resources.Load<GameObject>(spec.resource)),Resources.Load<GameObject>("Driver/driver"),controls);Physics.SyncTransforms();
            for(int i=0;i<480;i++){car.SendMessage("FixedUpdate");Physics.Simulate(1f/120);}
            Assert.Greater(root.transform.position.y,.35f);Assert.Less(root.transform.position.y,1.0f);Assert.Less(Mathf.Abs(car.Body.linearVelocity.y),.08f);
            controls.TouchGas=1;
            for(int i=0;i<360;i++){car.SendMessage("FixedUpdate");Physics.Simulate(1f/120);}
            Assert.Greater(car.Kph,10);controls.TouchGas=0;controls.TouchBrake=1;
            for(int i=0;i<600;i++){car.SendMessage("FixedUpdate");Physics.Simulate(1f/120);}
            Assert.Less(car.Kph,2);car.Reset();Assert.Less(car.Body.linearVelocity.sqrMagnitude,.00001f);Assert.Less(car.Body.angularVelocity.sqrMagnitude,.00001f);
        }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(ground);Physics.simulationMode=previous;Time.timeScale=oldTime;}
    }
}
}
