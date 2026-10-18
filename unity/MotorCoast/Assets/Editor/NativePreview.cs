using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MotorCoast.Editor {
// Runs the real player in the Editor and renders its Metal/URP cameras to PNG.
// This verifies visuals on the build host, not iPhone performance or touch input.
[InitializeOnLoad]
public static class NativePreview {
    const string Pending="MotorCoast.NativePreview";
    static double deadline,readyAt;
    static int stage;
    static bool resumed;static string runtimeFailure;
    static NativePreview(){if(SessionState.GetBool(Pending,false)){deadline=EditorApplication.timeSinceStartup+900;EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog;}}
    static void OnLog(string message,string trace,LogType type){
        // Unity 6000.3's editor-only Quick Search can fail on an empty index.
        // It is unrelated to the player; every player/render/UI exception fails QA.
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))return;
        if(Application.isPlaying&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&runtimeFailure==null)runtimeFailure=message;
    }
    public static void Capture(){
        Directory.CreateDirectory(Path.GetFullPath("../artifacts/preview"));
        SessionState.SetInt(Pending+"Vehicle",PlayerPrefs.GetInt("vehicle",0));
        SessionState.SetInt(Pending+"Quality",PlayerPrefs.GetInt("quality",1));
        PlayerPrefs.SetInt("vehicle",0);PlayerPrefs.SetInt("quality",2);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Coast.unity");
        SessionState.SetBool(Pending,true);deadline=EditorApplication.timeSinceStartup+900;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
        EditorApplication.EnterPlaymode();
    }
    static void Tick(){
        try {
            if(runtimeFailure!=null)throw new InvalidOperationException("Runtime preview failed: "+runtimeFailure);
            if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Native preview did not finish loading");
            if(!EditorApplication.isPlaying)return;
            var game=UnityEngine.Object.FindFirstObjectByType<MotorGame>();if(!game||!game.Car)return;
            if(!resumed){game.Resume();if(game.IsPaused)return;readyAt=EditorApplication.timeSinceStartup+5;resumed=true;return;}
            if(EditorApplication.timeSinceStartup<readyAt||ShaderUtil.anythingCompiling)return;
            if(stage==0){
                game.Input.Clear();Time.timeScale=0;game.Camera.enabled=false;
                var hud=UnityEngine.Object.FindFirstObjectByType<GameHud>();hud.SetTouchLayout(true);var canvas=hud.GetComponent<Canvas>();
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.Camera.GetComponent<Camera>();canvas.planeDistance=.4f;Canvas.ForceUpdateCanvases();
                Render(game,"driving-controls",new Vector3(0,2,-6.5f),new Vector3(0,.4f,1),62);
                hud.SetTouchLayout(false);Render(game,"desktop-controls",new Vector3(0,2,-6.5f),new Vector3(0,.4f,1),62);
                hud.SetTouchLayout(true);canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(c.renderMode!=RenderMode.WorldSpace)c.enabled=false;
                Render(game,"roadster-exterior",new Vector3(4,1.8f,5.5f),new Vector3(0,.25f,0),52);
                Render(game,"roadster-cabin",new Vector3(.8f,.85f,-.9f),new Vector3(-.3f,.15f,.7f),65);
                Render(game,"driving-park",new Vector3(95,125,-55),new Vector3(0,0,100),65);
                game.Car.SetCockpit(true);Render(game,"roadster-cockpit",game.Car.EyeLocal+new Vector3(0,.025f,.025f),game.Car.EyeLocal+new Vector3(0,-.2f,2),78);game.Car.SetCockpit(false);
                game.Car.GetComponentInChildren<DriverRig>().Animate(-34);game.Car.SetCockpit(true);
                stage=1;readyAt=EditorApplication.timeSinceStartup+.3;return;
            }
            if(stage>=1&&stage<=3){
                var driver=game.Car.GetComponentInChildren<DriverRig>();float angle=new[]{-34f,0f,34f}[stage-1];
                var wheel=Nodes.Find(game.Car.transform,"SteeringWheel");var centre=game.Car.transform.InverseTransformPoint(wheel.position);
                Render(game,"roadster-grip-"+angle,centre+new Vector3(.12f,.18f,-.35f),centre,55);
                // Skinning updates once per player frame. Separate poses across
                // frames so captures show the same skin that the player sees.
                if(stage<3){driver.Animate(stage==1?0:34);stage++;readyAt=EditorApplication.timeSinceStartup+.3;return;}
                driver.Animate(0);game.Car.SetCockpit(false);stage=4;resumed=false;game.SelectVehicle(1);return;
            }
            if(game.Car.Spec.id==game.Catalog.vehicles[1].id){
                Time.timeScale=0;game.Camera.enabled=false;
                foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(c.renderMode!=RenderMode.WorldSpace)c.enabled=false;
                Render(game,"bmw-exterior",new Vector3(4,1.8f,5.5f),new Vector3(0,.25f,0),52);
                Render(game,"bmw-cabin",new Vector3(.6f,.7f,-.8f),new Vector3(-.3f,.1f,.6f),65);
                File.WriteAllText(Path.GetFullPath("../artifacts/preview/host.txt"),"Unity "+Application.unityVersion+"\n"+SystemInfo.operatingSystem+"\n"+SystemInfo.processorType+"\n"+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+"\nEditor render at 1280 x 720; this is not a physical iPhone test.\n");
                Finish(0);
            }
        }catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void Render(MotorGame game,string name,Vector3 offset,Vector3 lookAt,float fov){
        var camera=game.Camera.GetComponent<Camera>();var car=game.Car.transform;
        camera.transform.SetPositionAndRotation(car.TransformPoint(offset),Quaternion.LookRotation(car.TransformPoint(lookAt)-car.TransformPoint(offset),Vector3.up));
        camera.fieldOfView=fov;camera.nearClipPlane=.025f;camera.aspect=16f/9;
        var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
        ShaderUtil.allowAsyncCompilation=false;Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera,request);
        var resolved=RenderTexture.GetTemporary(1280,720,0,RenderTextureFormat.ARGB32);Graphics.Blit(target,resolved);
        var previous=RenderTexture.active;RenderTexture.active=resolved;
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
        File.WriteAllBytes(Path.GetFullPath("../artifacts/preview/"+name+".png"),pixels.EncodeToPNG());
        RenderTexture.active=previous;RenderTexture.ReleaseTemporary(resolved);UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("Native preview captured: "+name);
    }
    static void Finish(int code){PlayerPrefs.SetInt("vehicle",SessionState.GetInt(Pending+"Vehicle",0));PlayerPrefs.SetInt("quality",SessionState.GetInt(Pending+"Quality",1));PlayerPrefs.Save();SessionState.SetBool(Pending,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;EditorApplication.Exit(code);}
}
}
