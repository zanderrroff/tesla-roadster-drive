using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace MotorCoast.Editor {
// Real Metal/URP rendering, remote loader and the actual gameplay controller.
// Frame rate here describes the Editor host, never a physical iPhone.
[InitializeOnLoad]
public static class ExpansionPreview {
    const string Key="MotorCoast.ExpansionPreview";
    static int stage,index;static double deadline,ready;static float footReady;static string failure;static Vector3 footStart;
    static readonly List<string> report=new List<string>();
    static readonly string[] prefs={"vehicle","map","character","graphicsPreset","outfitTint"};
    static ExpansionPreview(){if(SessionState.GetBool(Key,false)){index=SessionState.GetInt(Key+"Start",0);deadline=EditorApplication.timeSinceStartup+3600;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}}
    public static void Capture(){
        Directory.CreateDirectory(Path.GetFullPath("../artifacts/preview/fleet"));Directory.CreateDirectory(Path.GetFullPath("../artifacts/preview/characters"));Directory.CreateDirectory(Path.GetFullPath("../artifacts/preview/worlds"));
        foreach(var p in prefs){SessionState.SetInt(Key+p,PlayerPrefs.GetInt(p,0));PlayerPrefs.SetInt(p,0);}PlayerPrefs.SetInt("graphicsPreset",1);
        var catalog=JsonUtility.FromJson<VehicleCatalog>(Resources.Load<TextAsset>("Data/vehicles").text);int start=Mathf.Max(0,Array.FindIndex(catalog.vehicles,c=>c.id==Environment.GetEnvironmentVariable("MC_PREVIEW_START_ID")));SessionState.SetInt(Key+"Start",start);index=start;PlayerPrefs.SetInt("vehicle",start);
        var characters=JsonUtility.FromJson<CharacterCatalog>(Resources.Load<TextAsset>("Data/characters").text);PlayerPrefs.SetInt("character",Array.FindIndex(characters.characters,c=>c.id=="Male_Adult_01"));
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Coast.unity");SessionState.SetBool(Key,true);deadline=EditorApplication.timeSinceStartup+3600;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;EditorApplication.EnterPlaymode();
    }
    static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase"))return;if(Application.isPlaying&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&failure==null)failure=message;}
    static void HideUI(){foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(c.renderMode!=RenderMode.WorldSpace)c.enabled=false;}
    static void Freeze(MotorGame game){Time.timeScale=0;game.Camera.enabled=false;game.Car.Suspend(true);HideUI();}
    static void Tick(){try{
        if(failure!=null)throw new Exception(failure);if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Expansion preview timed out");if(!Application.isPlaying)return;
        var game=UnityEngine.Object.FindFirstObjectByType<MotorGame>();if(!game||!game.Car||game.IsLoading||EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)return;
        if(stage==0){game.Resume();ready=EditorApplication.timeSinceStartup+2;stage=1;return;}
        if(stage==1){Freeze(game);RecordCar(game);NativePreview.Render(game,"fleet/"+game.Car.Spec.id,new Vector3(4,1.7f,5.5f),new Vector3(0,.25f,0),52);NativePreview.Render(game,"fleet/"+game.Car.Spec.id+"-cabin",new Vector3(.65f,.85f,-.90f),new Vector3(-.3f,.12f,.6f),70);
            if(++index<game.Catalog.vehicles.Length){stage=2;game.SelectVehicle(index);ready=EditorApplication.timeSinceStartup+.2;return;}index=0;stage=3;game.SelectVehicle(0);return;}
        if(stage==2){if(game.Car.Spec.id!=game.Catalog.vehicles[index].id)return;Freeze(game);game.Car.GetComponentInChildren<DriverRig>().Animate(20);stage=1;ready=EditorApplication.timeSinceStartup+.15;return;}
        if(stage==3){if(game.Car.Spec.id!=game.Catalog.vehicles[0].id)return;Freeze(game);var c=game.Characters.characters[index];game.Car.ChangeDriver(Resources.Load<GameObject>(c.resource));stage=4;ready=EditorApplication.timeSinceStartup+.15;return;}
        if(stage==4){Freeze(game);RecordDriver(game,game.Characters.characters[index].id);NativePreview.Render(game,"characters/"+game.Characters.characters[index].id,new Vector3(.60f,.85f,-.90f),new Vector3(-.3f,.12f,.6f),70);
            if(++index<game.Characters.characters.Length){stage=3;return;}index=0;stage=5;return;}
        if(stage==5){game.SelectMap(index);stage=6;ready=EditorApplication.timeSinceStartup+.5;return;}
        if(stage==6){if(game.MapIndex!=index)return;Freeze(game);NativePreview.Render(game,"worlds/"+index,new Vector3(190,145,-160),new Vector3(0,5,160),67);report.Add("Map "+index+": rendered "+WorldCatalog.Maps[index].Name);if(++index<WorldCatalog.Maps.Length){stage=5;return;}stage=7;game.SelectMap(0);return;}
        if(stage==7){if(game.MapIndex!=0)return;game.Resume();game.Camera.enabled=true;game.Interact();stage=8;footReady=Time.time+2.2f;ready=EditorApplication.timeSinceStartup+1;return;}
        if(stage==8){if(Time.time<footReady)return;if(!game.OnFoot)throw new Exception("Exit interaction did not activate walking: "+game.Notice);Freeze(game);var p=game.Car.transform.InverseTransformPoint(game.Person.transform.position);NativePreview.Render(game,"walking",p+new Vector3(2.4f,1.5f,3.4f),p+Vector3.up*.8f,48);report.Add("Exit/door/walking: active");stage=9;ready=EditorApplication.timeSinceStartup+.1;return;}
        if(stage==9){game.Resume();game.Car.ParkForInteraction=true;game.Car.Door.Open(true);footReady=Time.time+.6f;stage=10;return;}
        if(stage==10){if(Time.time<footReady)return;Freeze(game);NativePreview.Render(game,"roadster-door-open",new Vector3(-4,1.6f,4.8f),new Vector3(0,.25f,0),52);game.Car.Door.CloseImmediately();game.Car.ParkForInteraction=false;
            game.Pause();ShowMenu(game,"Settings");NativePreview.Render(game,"settings",new Vector3(4,2,6),Vector3.zero,52);ShowMenu(game,"Characters");stage=11;ready=EditorApplication.timeSinceStartup+1;return;}
        if(stage==11){NativePreview.Render(game,"character-selection",new Vector3(4,2,6),Vector3.zero,52);ShowMenu(game,"Maps");NativePreview.Render(game,"map-selection",new Vector3(4,2,6),Vector3.zero,52);ShowMenu(game,"Garage");NativePreview.Render(game,"expanded-garage",new Vector3(4,2,6),Vector3.zero,52);
            HideUI();game.Resume();game.Camera.enabled=false;game.Camera.transform.rotation=Quaternion.identity;game.Person.Teleport(new Vector3(-10,.04f,-80),Quaternion.identity);footStart=game.Person.transform.position;game.Input.TouchMove=Vector2.up;game.Input.TouchRun=true;footReady=Time.time+1.5f;stage=12;return;}
        if(stage==12){if(Time.time<footReady)return;if(Vector3.Distance(footStart,game.Person.transform.position)<3)throw new Exception("Sprint did not move the walking capsule");game.Input.Clear();game.Person.Jump();footStart=game.Person.transform.position;footReady=Time.time+.3f;stage=13;return;}
        if(stage==13){if(Time.time<footReady)return;if(game.Person.transform.position.y<footStart.y+.3f)throw new Exception("Jump did not leave the ground");report.Add("Walk/run/jump: simulated and passed");footReady=Time.time+1;stage=14;return;}
        if(stage==14){if(Time.time<footReady)return;game.Person.ToggleProne();footReady=Time.time+.4f;stage=15;return;}
        if(stage==15){if(Time.time<footReady)return;Freeze(game);var p=game.Car.transform.InverseTransformPoint(game.Person.transform.position);NativePreview.Render(game,"prone",p+new Vector3(2,1,2),p+Vector3.up*.4f,48);if(game.Person.GetComponent<CharacterController>().height>.65f)throw new Exception("Prone capsule did not lower");game.Person.ToggleProne();game.RecoverPerson();game.Resume();footReady=Time.time+.5f;stage=16;return;}
        if(stage==16){if(Time.time<footReady)return;game.Interact();footReady=Time.time+2.2f;stage=17;return;}
        if(stage==17){if(Time.time<footReady)return;if(game.OnFoot||!game.Car.Occupied)throw new Exception("Entering vehicle failed");report.Add("Prone and re-entry: passed");
            AssetDatabase.SaveAssets();File.WriteAllLines(Path.GetFullPath("../artifacts/expansion-preview.txt"),report);Finish(0);
        }
    }catch(Exception e){Debug.LogException(e);report.Add("FAIL: "+e);File.WriteAllLines(Path.GetFullPath("../artifacts/expansion-preview.txt"),report);Finish(1);}}
    static void ShowMenu(MotorGame game,string name){var hud=UnityEngine.Object.FindFirstObjectByType<GameHud>();foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.enabled=true;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.Camera.GetComponent<Camera>();canvas.planeDistance=.4f;}hud.SendMessage(name);Canvas.ForceUpdateCanvases();}
    static void RecordCar(MotorGame game){
        var car=game.Car;foreach(string name in new[]{"Chassis","SteeringWheel","Wheel_0","Wheel_1","Wheel_2","Wheel_3"})if(!Nodes.Find(car.transform,name))throw new Exception(car.Spec.id+" missing "+name);
        foreach(var renderer in car.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials){if(!material||!material.shader||material.shader.name.Contains("InternalError"))throw new Exception("Invalid vehicle material "+car.Spec.id);PreserveVariant(material);}
        RecordDriver(game,"male-adult-01");report.Add("Car "+car.Spec.id+": loaded; door="+car.Door.Available);
    }
    static void RecordDriver(MotorGame game,string avatar){
        var car=game.Car;var driver=car.GetComponentInChildren<DriverRig>();driver.Animate(0);float min=float.PositiveInfinity,max=float.NegativeInfinity;
        foreach(var skin in driver.GetComponentsInChildren<SkinnedMeshRenderer>()){
            var mesh=new Mesh();skin.BakeMesh(mesh,true);foreach(var vertex in mesh.vertices){var p=car.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));if(float.IsNaN(p.y))throw new Exception("Invalid character pose");min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}UnityEngine.Object.DestroyImmediate(mesh);
        }
        report.Add(car.Spec.id+" / "+avatar+" driverY="+min.ToString("F3")+".."+max.ToString("F3")+" cabin="+car.Spec.floorY.ToString("F3")+".."+car.Spec.roofY.ToString("F3"));
    }
    static void PreserveVariant(Material material){
        if(!material.shader.name.Contains("glTF"))return;
        string signature=material.shader.name+"|"+string.Join("|",material.shaderKeywords.OrderBy(x=>x))+"|"+material.renderQueue;
        string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(signature))).Replace("-","").Substring(0,20);
        const string folder="Assets/Resources/Render/VehicleVariants";Directory.CreateDirectory(folder);string path=folder+"/"+hash+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(path))return;
        var copy=new Material(material);foreach(var property in copy.GetTexturePropertyNames())copy.SetTexture(property,null);AssetDatabase.CreateAsset(copy,path);
    }
    static void Finish(int code){foreach(var p in prefs)PlayerPrefs.SetInt(p,SessionState.GetInt(Key+p,0));PlayerPrefs.Save();SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;EditorApplication.Exit(code);}
}
}
