using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace MotorCoast {
public sealed class MotorGame : MonoBehaviour {
    public readonly PlayerInput Input=new PlayerInput();
    public VehicleController Car {get;private set;}
    public FollowCamera Camera {get;private set;}
    public RenderQuality Quality {get;private set;}
    public VehicleCatalog Catalog {get;private set;}
    public Challenges Challenges {get;private set;}
    public bool IsPaused {get;private set;}=true;
    public bool SoundEnabled=>PlayerPrefs.GetInt("sound",1)==1;
    GameHud hud;CoastWorld world;VehicleAudio audioRig;bool loading,started;int selected;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Coast"&&!FindFirstObjectByType<MotorGame>())new GameObject("Motor Coast").AddComponent<MotorGame>();}
    IEnumerator Start() {
        Screen.orientation=ScreenOrientation.AutoRotation;Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;
        Screen.sleepTimeout=SleepTimeout.NeverSleep;Time.fixedDeltaTime=1f/120;Time.maximumDeltaTime=.1f;Time.timeScale=0;Input.Blocked=true;
        hud=new GameObject("Interface").AddComponent<GameHud>();hud.Build(this);
        yield return null;
        string error=null;
        try {
            Quality=gameObject.AddComponent<RenderQuality>();Quality.Apply(PlayerPrefs.GetInt("quality",1));
            Catalog=JsonUtility.FromJson<VehicleCatalog>(Resources.Load<TextAsset>("Data/vehicles").text);
            world=new GameObject("Coastal driving park").AddComponent<CoastWorld>();world.Build();
            var lens=new GameObject("Main Camera",typeof(UnityEngine.Camera),typeof(AudioListener));lens.tag="MainCamera";
            var camera=lens.GetComponent<UnityEngine.Camera>();camera.farClipPlane=2000;camera.allowHDR=true;camera.allowMSAA=true;camera.backgroundColor=new Color(.5f,.7f,.8f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.None;
            Camera=lens.AddComponent<FollowCamera>();Challenges=gameObject.AddComponent<Challenges>();Challenges.World=world;
            AudioListener.volume=SoundEnabled?.85f:0;AudioListener.pause=true;
            hud.SetArrows(PlayerPrefs.GetInt("steeringArrows",0)==1);
        }catch(Exception e){error=e.Message;Debug.LogException(e);}
        if(error!=null){hud.Error(error);yield break;}
        yield return LoadVehicle(Mathf.Clamp(PlayerPrefs.GetInt("vehicle",0),0,Catalog.vehicles.Length-1));
    }
    IEnumerator LoadVehicle(int index) {
        if(loading)yield break;loading=true;Input.Clear();Input.Blocked=true;IsPaused=true;Time.timeScale=0;AudioListener.pause=true;
        selected=index;var spec=Catalog.vehicles[index];hud.Loading("Загрузка "+spec.label+"…");yield return null;
        var request=Resources.LoadAsync<GameObject>(Quality.Level==2?spec.highResource:spec.resource);yield return request;
        var driverRequest=Resources.LoadAsync<GameObject>("Driver/driver");yield return driverRequest;
        string error=null;GameObject next=null;
        try {
            if(!request.asset||!driverRequest.asset)throw new InvalidOperationException("Модель не импортирована в Unity. Выполните Prepare project.");
            next=new GameObject(spec.label,typeof(Rigidbody),typeof(VehicleController));next.SetActive(false);
            var model=Instantiate((GameObject)request.asset);var controller=next.GetComponent<VehicleController>();controller.Configure(spec,model,(GameObject)driverRequest.asset,Input);
            if(Car)Destroy(Car.gameObject);Car=controller;next.SetActive(true);Car.Suspend(true);
            audioRig=next.AddComponent<VehicleAudio>();audioRig.Configure(Car);
            Camera.Configure(Car);Camera.transform.SetPositionAndRotation(Car.transform.position+new Vector3(4,2.0f,6.3f),Quaternion.LookRotation(new Vector3(-4,-1.6f,-6.3f)));
            Challenges.Car=Car;Challenges.ResetRun();PlayerPrefs.SetInt("vehicle",index);PlayerPrefs.Save();
        }catch(Exception e){error=e.Message;Debug.LogException(e);if(next)Destroy(next);}
        loading=false;if(error!=null){hud.Error(error);yield break;}
        hud.Menu(!started);yield return null;yield return Resources.UnloadUnusedAssets();
    }
    public void SelectVehicle(int index){if(!loading)StartCoroutine(LoadVehicle(index));}
    public void Resume(){if(!Car||loading)return;started=true;IsPaused=false;Time.timeScale=1;Input.Clear();Input.Blocked=false;Car.Suspend(false);AudioListener.pause=false;hud.Driving();Camera.Snap();}
    public void Pause(bool garage=false){if(loading)return;IsPaused=true;Time.timeScale=0;Input.Clear();Input.Blocked=true;if(Car)Car.Suspend(true);AudioListener.pause=true;if(Car)hud.Menu(!started,garage);}
    public void ResetCar(){if(!Car)return;Car.ResetToStart();Camera.Snap();Challenges.ResetRun();}
    public void Horn(){if(!IsPaused&&audioRig)audioRig.Horn();}
    public void Retry(){if(Catalog!=null && Catalog.vehicles!=null && Catalog.vehicles.Length>0)SelectVehicle(selected);else UnityEngine.SceneManagement.SceneManager.LoadScene(0);}
    public void ChangeQuality(int level){Quality.Apply(level);if(Car&&Car.Spec.id=="roadster"&&!loading)StartCoroutine(LoadVehicle(selected));}
    public void ToggleSound(){PlayerPrefs.SetInt("sound",SoundEnabled?0:1);PlayerPrefs.Save();AudioListener.volume=SoundEnabled?.85f:0;}
    void Update(){if(loading||!Car)return;if(UnityEngine.Input.GetKeyDown(KeyCode.Escape)){if(IsPaused)Resume();else Pause();}if(IsPaused)return;
        if(UnityEngine.Input.GetKeyDown(KeyCode.C))Camera.Cycle();if(UnityEngine.Input.GetKeyDown(KeyCode.R))ResetCar();if(UnityEngine.Input.GetKeyDown(KeyCode.G))Pause(true);if(UnityEngine.Input.GetKeyDown(KeyCode.H))Horn();
        if(!UnityEngine.Input.touchSupported&&UnityEngine.Input.mouseScrollDelta.y!=0)Camera.Zoom(UnityEngine.Input.mouseScrollDelta.y*16);
    }
    void OnApplicationFocus(bool focus){if(!focus)Pause();}
    void OnApplicationPause(bool pause){if(pause)Pause();}
    void OnDestroy(){Time.timeScale=1;AudioListener.pause=false;Screen.sleepTimeout=SleepTimeout.SystemSetting;}
}
}
