using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
namespace MotorCoast.Tests {
public sealed class ExplorationTests {
    [Test] public void EveryNewAvatarClearsTheRoadsterFloorAndRoof(){
        var catalog=JsonUtility.FromJson<CharacterCatalog>(Resources.Load<TextAsset>("Data/characters").text);
        var spec=JsonUtility.FromJson<VehicleCatalog>(Resources.Load<TextAsset>("Data/vehicles").text).vehicles[0];
        var root=new GameObject("Avatar fit fixture",typeof(Rigidbody),typeof(VehicleController));
        try{
            var car=root.GetComponent<VehicleController>();car.Configure(spec,Object.Instantiate(Resources.Load<GameObject>(spec.resource)),Resources.Load<GameObject>(catalog.characters[0].resource),new PlayerInput());
            for(int i=0;i<4;i++)Assert.Less(Vector3.Distance(Nodes.Find(root.transform,"Wheel_"+i).position,root.transform.TransformPoint(spec.wheelCenters[i])),.001f,"Wheels must be placed before the first physics frame");
            foreach(var c in catalog.characters){
                car.ChangeDriver(Resources.Load<GameObject>(c.resource));var driver=root.GetComponentInChildren<DriverRig>();driver.Animate(20);
                foreach(var skin in driver.GetComponentsInChildren<SkinnedMeshRenderer>()){
                    var mesh=new Mesh();try{skin.BakeMesh(mesh,true);foreach(var vertex in mesh.vertices){float y=root.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex)).y;Assert.GreaterOrEqual(y,spec.floorY-.008f,c.id+" feet");Assert.LessOrEqual(y,spec.roofY+.004f,c.id+" head");}}finally{Object.DestroyImmediate(mesh);}
                }
            }
        }finally{Object.DestroyImmediate(root);}
    }
    [Test] public void FiftyDistinctAvatarsHaveCompleteAnimatedRigs(){
        var catalog=JsonUtility.FromJson<CharacterCatalog>(Resources.Load<TextAsset>("Data/characters").text);
        Assert.AreEqual(50,catalog.characters.Length);Assert.AreEqual(50,catalog.characters.Select(c=>c.id).Distinct().Count());
        foreach(var c in catalog.characters){
            var prefab=Resources.Load<GameObject>(c.resource);Assert.IsNotNull(prefab,c.id);
            foreach(string bone in new[]{"Bip01_Pelvis","Bip01_Head","Bip01_L_Hand","Bip01_R_Hand","Bip01_L_Foot","Bip01_R_Foot"})Assert.IsNotNull(Nodes.Find(prefab.transform,bone),c.id+" "+bone);
            foreach(var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>()){Assert.Greater(skin.sharedMesh.vertexCount,100,c.id);Assert.IsTrue(skin.sharedMaterials.All(m=>m&&m.GetTexture("_BaseMap")),c.id+" textured skin");}
        }
        foreach(var name in CharacterVisual.MotionNames){var clip=Resources.Load<AnimationClip>("Motions/"+name);Assert.IsNotNull(clip,name);Assert.Greater(clip.length,.1f,name);}
    }
    [Test] public void QualityMigrationKeepsExactlyTwoPresets(){
        bool existed=PlayerPrefs.HasKey("graphicsPreset");int preset=PlayerPrefs.GetInt("graphicsPreset"),old=PlayerPrefs.GetInt("quality",1);
        try{PlayerPrefs.DeleteKey("graphicsPreset");foreach(int previous in new[]{0,1,2}){PlayerPrefs.SetInt("quality",previous);Assert.AreEqual(previous==2?1:0,RenderQuality.SavedLevel);}PlayerPrefs.SetInt("graphicsPreset",0);PlayerPrefs.SetInt("quality",2);Assert.AreEqual(0,RenderQuality.SavedLevel);}
        finally{PlayerPrefs.SetInt("quality",old);if(existed)PlayerPrefs.SetInt("graphicsPreset",preset);else PlayerPrefs.DeleteKey("graphicsPreset");}
        Assert.IsNotNull(Resources.Load("Render/Standard"));Assert.IsNotNull(Resources.Load("Render/Ultra"));
    }
    [Test] public void RoomChecksRejectLowCeilingsAndWalls(){
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(20,1,20);
        var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=new Vector3(0,1.2f,0);roof.transform.localScale=new Vector3(3,.2f,3);
        try{Physics.SyncTransforms();Assert.IsFalse(OnFootController.HasRoom(new Vector3(0,.03f,0),1.76f,~0));Assert.IsTrue(OnFootController.HasRoom(new Vector3(0,.03f,0),.65f,~0));Assert.IsTrue(OnFootController.HasRoom(new Vector3(4,.03f,0),1.76f,~0));}
        finally{Object.DestroyImmediate(floor);Object.DestroyImmediate(roof);}
    }
    [TestCase(false),TestCase(true)] public void ClimbingRequiresClearSpaceAboveTheLedge(bool blocked){
        var game=new GameObject("Climb game fixture").AddComponent<MotorGame>();var root=new GameObject("Climber",typeof(CharacterController));
        var ledge=GameObject.CreatePrimitive(PrimitiveType.Cube);ledge.transform.position=new Vector3(0,.4f,1.5f);ledge.transform.localScale=new Vector3(3,.8f,2);
        var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(0,1.8f,1.5f);ceiling.transform.localScale=new Vector3(3,.2f,2);ceiling.SetActive(blocked);
        try{
            var person=root.AddComponent<OnFootController>();person.Configure(game,Resources.Load<GameObject>("Characters/Male_Adult_01"));Physics.SyncTransforms();
            person.SendMessage("TryClimb");Assert.AreEqual(!blocked,person.Climbing,"Climb must require a reachable landing with standing clearance");
        }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(ledge);Object.DestroyImmediate(ceiling);Object.DestroyImmediate(game.gameObject);}
    }
    [Test] public void FootJoystickAndSprintCannotStickAfterLosingFocus(){
        var go=new GameObject("Joystick",typeof(RectTransform));var events=new GameObject("Events",typeof(EventSystem));var input=new PlayerInput();
        try{var pad=go.AddComponent<MovePad>();pad.Input=input;var first=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=10,position=new Vector2(110,60)};pad.OnPointerDown(first);Assert.Greater(input.TouchMove.sqrMagnitude,.01f);pad.OnPointerUp(new PointerEventData(events.GetComponent<EventSystem>()){pointerId=20});Assert.Greater(input.TouchMove.sqrMagnitude,.01f);pad.OnCancel(first);Assert.AreEqual(Vector2.zero,input.TouchMove);input.TouchRun=true;input.TouchMove=Vector2.one;input.Clear();Assert.IsFalse(input.TouchRun);Assert.AreEqual(Vector2.zero,input.TouchMove);}
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(events);}
    }
    [Test] public void TenMapsHaveDifferentSeedsAndTraversalLayouts(){
        Assert.AreEqual(10,WorldCatalog.Maps.Length);Assert.AreEqual(10,WorldCatalog.Maps.Select(m=>m.Seed).Distinct().Count());Assert.GreaterOrEqual(WorldCatalog.Maps.Select(m=>m.Layout).Distinct().Count(),6);
        var root=new GameObject("Height fixture");try{foreach(var spec in WorldCatalog.Maps.Skip(1)){var world=new ExpeditionWorld(root.transform,spec);Assert.AreEqual(0,world.Height(0,-80),"Spawn must be level");Assert.IsFalse(float.IsNaN(world.Height(400,400)));}}finally{Object.DestroyImmediate(root);}
    }
}
}
