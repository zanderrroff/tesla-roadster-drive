using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace MotorCoast {
// Opt-in validation inside the built Mac player, never active during normal play.
public sealed class DesktopSmokeTest : MonoBehaviour {
    public static bool Active {get;private set;}
    string output,failure;MotorGame game;readonly List<float> frames=new List<float>();
    readonly Dictionary<string,int> saved=new Dictionary<string,int>();
    [Serializable] sealed class Report {
        public bool passed;public string error,unity,os,cpu,gpu,graphics,renderResolution,downloadedVehicle;
        public int vehicles,characters,maps,frameCount;public float averageFps,p95FrameMs,peakKph,driveDistance;
    }
    readonly Report report=new Report();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot(){
        if(!DesktopPlatform.IsMac)return;
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-motorcoast-smoke");
        if(i<0||i+1>=args.Length)return;
        Active=true;var test=new GameObject("Mac player validation").AddComponent<DesktopSmokeTest>();
        test.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(test.output);
        foreach(var key in new[]{"vehicle","graphicsPreset","map","character","showFps","macFirstRun"}){
            test.saved[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):-1;PlayerPrefs.SetInt(key,key=="showFps"?1:0);
        }
        Application.runInBackground=true;
    }
    void OnEnable(){Application.logMessageReceived+=OnLog;}
    void OnDisable(){Application.logMessageReceived-=OnLog;}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=message;}
    IEnumerator Start(){
        var exercise=Exercise();
        while(true){
            object next=null;bool more=false;
            try{if(failure!=null)throw new InvalidOperationException(failure);more=exercise.MoveNext();if(more)next=exercise.Current;}
            catch(Exception error){failure=error.ToString();break;}
            if(!more)break;yield return next;
        }
        report.passed=failure==null;report.error=failure;report.unity=Application.unityVersion;
        report.os=SystemInfo.operatingSystem;report.cpu=SystemInfo.processorType;report.gpu=SystemInfo.graphicsDeviceName;report.graphics=SystemInfo.graphicsDeviceType.ToString();
        if(frames.Count>0){float sum=0;foreach(float dt in frames)sum+=dt;frames.Sort();report.frameCount=frames.Count;report.averageFps=frames.Count/sum;report.p95FrameMs=frames[Mathf.Min(frames.Count-1,Mathf.FloorToInt(frames.Count*.95f))]*1000;}
        foreach(var item in saved){if(item.Value<0)PlayerPrefs.DeleteKey(item.Key);else PlayerPrefs.SetInt(item.Key,item.Value);}PlayerPrefs.Save();
        File.WriteAllText(Path.Combine(output,"mac-player-report.json"),JsonUtility.ToJson(report,true));
        Debug.Log("MOTOR COAST MAC SMOKE "+(report.passed?"PASS":"FAIL")+": "+report.error);
        Application.Quit(report.passed?0:1);
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    IEnumerator Exercise(){
        float deadline=Time.realtimeSinceStartup+240;
        while(!(game=FindFirstObjectByType<MotorGame>())||!game.Car||game.IsLoading){Require(Time.realtimeSinceStartup<deadline,"Initial load timed out");yield return null;}
        report.vehicles=game.Catalog.vehicles.Length;report.characters=game.Characters.characters.Length;report.maps=WorldCatalog.Maps.Length;
        Require(report.vehicles==97&&report.characters==50&&report.maps==10,"Catalog differs from iPhone release");
        Require(game.Car.GetComponent<Rigidbody>()!=null,"Native vehicle did not initialize");
        AudioListener.volume=0;game.Resume();yield return new WaitForSeconds(3);
        ScreenCapture.CaptureScreenshot(Path.Combine(output,"mac-native-standard.png"));
        var start=game.Car.transform.position;game.Input.TouchGas=1;yield return new WaitForSeconds(4);
        report.peakKph=game.Car.Kph;report.driveDistance=Vector3.Distance(start,game.Car.transform.position);
        Require(report.peakKph>12&&report.driveDistance>5,"C++ acceleration/vehicle movement failed");
        float heading=game.Car.transform.eulerAngles.y;game.Input.TouchSteer=.45f;game.Input.TouchGas=.35f;yield return new WaitForSeconds(2);
        Require(Mathf.Abs(Mathf.DeltaAngle(heading,game.Car.transform.eulerAngles.y))>3,"Steering failed");
        game.Input.Clear();game.Input.TouchBrake=1;yield return new WaitForSeconds(5);Require(game.Car.Kph<4,"Braking failed");
        game.ResetCar();game.Input.Clear();game.Input.RequestedGear=-1;game.Input.TouchGas=1;yield return new WaitForSeconds(2);
        Require(game.Car.Gear==-1&&game.Car.Kph>3,"Reverse failed");
        game.Input.Clear();game.ResetCar();game.Input.RequestedGear=1;yield return new WaitForSeconds(1);
        game.Pause();var paused=game.Car.transform.position;yield return new WaitForSecondsRealtime(1);
        Require(Time.timeScale==0&&Vector3.Distance(paused,game.Car.transform.position)<.01f,"Pause moved the vehicle");
        game.Resume();game.Interact();yield return new WaitForSeconds(3);Require(game.OnFoot,"Exit failed");
        var feet=game.Person.transform.position;game.Input.TouchMove=Vector2.up;yield return new WaitForSeconds(2);
        Require(Vector3.Distance(feet,game.Person.transform.position)>1,"Walking failed");game.Input.Clear();
        game.RecoverPerson();game.Interact();yield return new WaitForSeconds(3);Require(!game.OnFoot,"Re-entry failed");
        game.Pause();game.SelectVehicle(7);deadline=Time.realtimeSinceStartup+150;
        while(game.IsLoading){Require(Time.realtimeSinceStartup<deadline,"Optional vehicle download timed out");yield return null;}
        Require(game.Car.Spec.id==game.Catalog.vehicles[7].id,"Optional glTF vehicle did not load");
        foreach(var renderer in game.Car.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
            Require(material&&material.shader&&material.shader.isSupported&&material.shader.name!="Hidden/InternalErrorShader","Optional vehicle shader unavailable");
        report.downloadedVehicle=game.Car.Spec.id;game.Resume();yield return new WaitForSeconds(2);
        ScreenCapture.CaptureScreenshot(Path.Combine(output,"mac-native-downloaded-car.png"));yield return new WaitForSeconds(1);
        game.Pause();game.SelectVehicle(0);deadline=Time.realtimeSinceStartup+120;
        while(game.IsLoading){Require(Time.realtimeSinceStartup<deadline,"Tesla reload timed out");yield return null;}
        game.SelectMap(2);while(game.IsLoading)yield return null;game.Resume();yield return new WaitForSeconds(2);
        ScreenCapture.CaptureScreenshot(Path.Combine(output,"mac-native-alpine.png"));yield return new WaitForSeconds(1);
        game.Pause();game.SelectMap(0);while(game.IsLoading)yield return null;
        game.Pause();game.ChangeQuality(1);deadline=Time.realtimeSinceStartup+120;
        while(game.IsLoading){Require(Time.realtimeSinceStartup<deadline,"Ultra load timed out");yield return null;}
        game.Resume();yield return new WaitForSeconds(3);Require(game.Quality.Ultra,"Ultra did not apply");
        ScreenCapture.CaptureScreenshot(Path.Combine(output,"mac-native-ultra.png"));yield return new WaitForSeconds(1);
        game.Pause();game.ChangeQuality(0);deadline=Time.realtimeSinceStartup+120;
        while(game.IsLoading){Require(Time.realtimeSinceStartup<deadline,"Standard reload timed out");yield return null;}
        game.Resume();yield return new WaitForSeconds(3);game.Input.TouchGas=.25f;
        float until=Time.realtimeSinceStartup+20;
        while(Time.realtimeSinceStartup<until){frames.Add(Time.unscaledDeltaTime);yield return null;}
        game.Input.Clear();report.renderResolution=game.Quality.ResolutionLabel;
        game.ResetCar();yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(output,"mac-native-final.png"));
        game.Pause();yield return new WaitForSecondsRealtime(1);
        ScreenCapture.CaptureScreenshot(Path.Combine(output,"mac-native-menu.png"));yield return new WaitForSecondsRealtime(1);
    }
}
}
