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
    public CharacterCatalog Characters {get;private set;}
    public OnFootController Person {get;private set;}
    public bool OnFoot {get;private set;}
    public string Notice {get;private set;}="";float noticeUntil;
    public bool IsPaused {get;private set;}=true;
    public bool IsLoading=>loading;
    public bool SoundEnabled=>PlayerPrefs.GetInt("sound",1)==1;
    public bool ShowFps=>PlayerPrefs.GetInt("showFps",0)==1;
    public bool AnimationsEnabled=>PlayerPrefs.GetInt("animations",1)==1;
    GameHud hud;CoastWorld world;VehicleAudio audioRig;bool loading,started,interacting;int selected;Coroutine interaction;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Coast"&&!FindFirstObjectByType<MotorGame>())new GameObject("Motor Coast").AddComponent<MotorGame>();}
    IEnumerator Start() {
        Screen.orientation=ScreenOrientation.AutoRotation;Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;
        Screen.sleepTimeout=SleepTimeout.NeverSleep;Time.fixedDeltaTime=1f/120;Time.maximumDeltaTime=.1f;Time.timeScale=0;Input.Blocked=true;
        hud=new GameObject("Interface").AddComponent<GameHud>();hud.Build(this);
        yield return null;
        string error=null;
        try {
            Quality=gameObject.AddComponent<RenderQuality>();Quality.Apply(RenderQuality.SavedLevel);
            Catalog=JsonUtility.FromJson<VehicleCatalog>(Resources.Load<TextAsset>("Data/vehicles").text);
            Characters=JsonUtility.FromJson<CharacterCatalog>(Resources.Load<TextAsset>("Data/characters").text);
            world=new GameObject("Coastal driving park").AddComponent<CoastWorld>();world.Build(PlayerPrefs.GetInt("map",0));
            var lens=new GameObject("Main Camera",typeof(UnityEngine.Camera),typeof(AudioListener));lens.tag="MainCamera";
            var camera=lens.GetComponent<UnityEngine.Camera>();camera.farClipPlane=2000;camera.allowHDR=true;camera.allowMSAA=true;camera.backgroundColor=new Color(.5f,.7f,.8f);
            camera.cullingMask=~(1<<10);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.None;
            Camera=lens.AddComponent<FollowCamera>();Challenges=gameObject.AddComponent<Challenges>();Challenges.World=world;
            AudioListener.volume=SoundEnabled?.85f:0;AudioListener.pause=true;
            hud.SetArrows(PlayerPrefs.GetInt("steeringArrows",0)==1);
        }catch(Exception e){error=e.Message;Debug.LogException(e);}
        if(error!=null){hud.Error(error);yield break;}
        yield return LoadVehicle(Mathf.Clamp(PlayerPrefs.GetInt("vehicle",0),0,Catalog.vehicles.Length-1));
    }
    IEnumerator LoadVehicle(int index) {
        CancelInteraction();
        if(loading)yield break;loading=true;Input.Clear();Input.Blocked=true;IsPaused=true;Time.timeScale=0;AudioListener.pause=true;
        selected=index;var spec=Catalog.vehicles[index];hud.Loading("Загрузка "+spec.label+"…");yield return null;
        GameObject loadedModel=null;string modelError=null;
        if(string.IsNullOrEmpty(spec.modelUrl)){
            var request=Resources.LoadAsync<GameObject>(Quality.Ultra?spec.highResource:spec.resource);yield return request;
            if(request.asset)loadedModel=Instantiate((GameObject)request.asset);
        }else{
            var task=RemoteVehicle.Load(spec,Quality.Ultra,hud.LoadingProgress);while(!task.IsCompleted)yield return null;
            if(task.IsFaulted)modelError=task.Exception.GetBaseException().Message;else loadedModel=task.Result;
        }
        int characterIndex=Mathf.Clamp(PlayerPrefs.GetInt("character",0),0,Characters.characters.Length-1);
        var driverRequest=Resources.LoadAsync<GameObject>(Characters.characters[characterIndex].resource);yield return driverRequest;
        string error=null;GameObject next=null;
        try {
            if(!loadedModel||!driverRequest.asset)throw new InvalidOperationException(modelError??"Модель не импортирована в Unity. Выполните Prepare project.");
            next=new GameObject(spec.label,typeof(Rigidbody),typeof(VehicleController));next.SetActive(false);
            var controller=next.GetComponent<VehicleController>();controller.Configure(spec,loadedModel,(GameObject)driverRequest.asset,Input);
            if(Car)Destroy(Car.gameObject);Car=controller;next.SetActive(true);Car.Suspend(true);
            if(!Person){Person=new GameObject("Player",typeof(CharacterController)).AddComponent<OnFootController>();Person.Configure(this,(GameObject)driverRequest.asset);}
            else Person.ReplaceVisual((GameObject)driverRequest.asset);
            Person.gameObject.SetActive(false);OnFoot=false;
            audioRig=next.AddComponent<VehicleAudio>();audioRig.Configure(Car);
            Camera.Configure(Car);Camera.FollowVehicle();Camera.transform.SetPositionAndRotation(Car.transform.position+new Vector3(4,2.0f,6.3f),Quaternion.LookRotation(new Vector3(-4,-1.6f,-6.3f)));
            Challenges.Car=Car;Challenges.ResetRun();PlayerPrefs.SetInt("vehicle",index);PlayerPrefs.Save();
        }catch(Exception e){error=e.Message;Debug.LogException(e);if(next)Destroy(next);else if(loadedModel)Destroy(loadedModel);}
        loading=false;if(error!=null){hud.Error(error);yield break;}
        hud.Menu(!started);yield return null;yield return Resources.UnloadUnusedAssets();
    }
    public void SelectVehicle(int index){if(!loading)StartCoroutine(LoadVehicle(index));}
    public void Resume(){if(!Car||loading)return;started=true;IsPaused=false;Time.timeScale=1;Input.Clear();Input.Blocked=interacting;Car.Suspend(false);AudioListener.pause=false;hud.Driving();Camera.Snap();}
    public void Pause(bool garage=false){if(loading)return;IsPaused=true;Time.timeScale=0;Input.Clear();Input.Blocked=true;if(Person)Person.CancelInput();if(Car)Car.Suspend(true);AudioListener.pause=true;if(Car)hud.Menu(!started,garage);}
    public void ResetCar(){if(!Car)return;Car.ResetToStart();Camera.Snap();Challenges.ResetRun();}
    public void Horn(){if(!IsPaused&&audioRig)audioRig.Horn();}
    public void Retry(){if(Catalog!=null && Catalog.vehicles!=null && Catalog.vehicles.Length>0)SelectVehicle(selected);else UnityEngine.SceneManagement.SceneManager.LoadScene(0);}
    public void ChangeQuality(int level){if(loading||Quality.Level==level)return;Quality.Apply(level);if(Car&&(Car.Spec.id=="roadster"||!string.IsNullOrEmpty(Car.Spec.modelUrl)))StartCoroutine(LoadVehicle(selected));}
    public void ToggleSound(){PlayerPrefs.SetInt("sound",SoundEnabled?0:1);PlayerPrefs.Save();AudioListener.volume=SoundEnabled?.85f:0;}
    public void ToggleFps(){PlayerPrefs.SetInt("showFps",ShowFps?0:1);PlayerPrefs.Save();}
    public void ToggleAnimations(){PlayerPrefs.SetInt("animations",AnimationsEnabled?0:1);PlayerPrefs.Save();}
    public void Notify(string text){Notice=text;noticeUntil=Time.unscaledTime+3;}
    public void Interact(){
        if(IsPaused||loading||interacting||!Car)return;
        if(!OnFoot){
            if(Car.Kph>1.5f){Notify("Остановитесь, чтобы выйти");return;}
            if(Vector3.Dot(Car.transform.up,Vector3.up)<.7f){Notify("Сначала восстановите автомобиль");return;}
            if(!OnFootController.FindExit(Car,out var feet)){Notify("У дверей недостаточно места");return;}
            interaction=StartCoroutine(ExitVehicle(feet));return;
        }else{
            if(Vector3.Distance(Person.transform.position,Car.transform.position)>3.5f){Notify("Подойдите к автомобилю");return;}
            if(Car.Kph>1.5f||Person.Climbing){Notify("Дождитесь остановки");return;}
            interaction=StartCoroutine(EnterVehicle());return;
        }
    }
    void CancelInteraction(){if(interaction!=null)StopCoroutine(interaction);interaction=null;interacting=false;if(Car){Car.ParkForInteraction=false;Car.Door.CloseImmediately();}if(Person&&Person.Visual.Transition){Person.Visual.EndTransition();if(OnFoot)RecoverPerson();}}
    IEnumerator ExitVehicle(Vector3 feet){
        interacting=true;Input.Clear();Input.Blocked=true;Car.ParkForInteraction=true;Car.Door.Open(true);yield return new WaitForSeconds(.45f);
        if(!OnFootController.FindExit(Car,out feet)){Car.Door.Open(false);Car.ParkForInteraction=false;interacting=false;Input.Blocked=IsPaused;Notify("Проход оказался занят");yield break;}
        OnFoot=true;Car.SetOccupied(false);Person.gameObject.SetActive(true);Person.Teleport(feet,Quaternion.Euler(0,Car.transform.eulerAngles.y,0));Camera.FollowPerson(Person.transform);Challenges.enabled=false;hud.Driving();
        yield return SeatTransition(false,feet);
        Car.Door.Open(false);Car.ParkForInteraction=false;interacting=false;Input.Blocked=IsPaused;
    }
    IEnumerator EnterVehicle(){
        interacting=true;Input.Clear();Input.Blocked=true;Car.ParkForInteraction=true;Car.Door.Open(true);yield return new WaitForSeconds(.45f);
        yield return SeatTransition(true,Person.transform.position);
        Person.gameObject.SetActive(false);OnFoot=false;Car.SetOccupied(true);Camera.FollowVehicle();Challenges.enabled=true;hud.Driving();
        yield return new WaitForSeconds(.25f);Car.Door.Open(false);Car.ParkForInteraction=false;interacting=false;Input.Blocked=IsPaused;
    }
    IEnumerator SeatTransition(bool entering,Vector3 feet){
        // Collisions are checked before interaction. Only the short traversal
        // through the open doorway temporarily disables the walking capsule.
        var rotation=Quaternion.Euler(0,Car.transform.eulerAngles.y,0);
        if(!AnimationsEnabled){Person.Teleport(feet,rotation);yield break;}
        Person.BeginSeatTransition();Person.transform.rotation=rotation;
        for(float elapsed=0;elapsed<.85f;elapsed+=Time.deltaTime){
            float t=Mathf.Clamp01(elapsed/.85f),outside=entering?1-t:t;
            Person.Visual.transform.localScale=Vector3.one*Mathf.Lerp(Car.Spec.driverScale,1,outside);
            var offset=Person.Visual.TransitionPose(entering,t);
            var seated=Car.transform.TransformPoint(Car.Spec.driverPosition)-offset;
            Person.transform.position=Vector3.Lerp(seated,feet,Mathf.SmoothStep(0,1,outside));
            yield return null;
        }
        Person.EndSeatTransition(feet,rotation);
    }
    public void RecoverPerson(){if(OnFootController.FindExit(Car,out var feet))Person.Teleport(feet,Car.transform.rotation);else{Car.ResetToStart();Person.Teleport(Car.transform.position+Vector3.left*2,Quaternion.identity);}}
    public void SelectCharacter(int index){if(!loading)StartCoroutine(ChangeCharacter(index));}
    IEnumerator ChangeCharacter(int index){
        loading=true;hud.Loading("Подготовка персонажа…");var request=Resources.LoadAsync<GameObject>(Characters.characters[index].resource);yield return request;
        if(!request.asset){loading=false;hud.Error("Персонаж не найден");yield break;}
        PlayerPrefs.SetInt("character",index);PlayerPrefs.Save();Car.ChangeDriver((GameObject)request.asset);Person.ReplaceVisual((GameObject)request.asset);loading=false;hud.Menu();
        yield return Resources.UnloadUnusedAssets();
    }
    public void SetOutfit(int index){PlayerPrefs.SetInt("outfitTint",index);PlayerPrefs.Save();foreach(var c in FindObjectsByType<CharacterVisual>(FindObjectsInactive.Include,FindObjectsSortMode.None))c.SetTint(index);}
    public int MapIndex=>world?world.MapIndex:0;
    public void SelectMap(int index){if(!loading)StartCoroutine(ChangeMap(index));}
    IEnumerator ChangeMap(int index){
        CancelInteraction();
        loading=true;hud.Loading("Загрузка: "+WorldCatalog.Maps[index].Name+"…");yield return null;
        if(Person)Person.gameObject.SetActive(false);OnFoot=false;Car.SetOccupied(true);Camera.FollowVehicle();
        world.gameObject.SetActive(false);Destroy(world.gameObject);yield return null;yield return Resources.UnloadUnusedAssets();
        world=new GameObject(WorldCatalog.Maps[index].Name).AddComponent<CoastWorld>();world.Build(index);Challenges.World=world;Challenges.enabled=true;Challenges.ResetRun();Car.ResetToStart();Physics.SyncTransforms();
        PlayerPrefs.SetInt("map",index);PlayerPrefs.Save();loading=false;hud.Menu();
    }
    void Update(){if(Time.unscaledTime>noticeUntil)Notice="";if(loading||!Car)return;if(UnityEngine.Input.GetKeyDown(KeyCode.Escape)){if(IsPaused)Resume();else Pause();}if(IsPaused)return;
        if(UnityEngine.Input.GetKeyDown(KeyCode.F)&&OnFoot)hud.Emotes();if(UnityEngine.Input.GetKeyDown(KeyCode.E))Interact();if(UnityEngine.Input.GetKeyDown(KeyCode.C)&&!OnFoot)Camera.Cycle();if(UnityEngine.Input.GetKeyDown(KeyCode.R)){if(OnFoot)RecoverPerson();else ResetCar();}if(UnityEngine.Input.GetKeyDown(KeyCode.G))Pause(true);if(UnityEngine.Input.GetKeyDown(KeyCode.H)&&!OnFoot)Horn();
        if(!UnityEngine.Input.touchSupported&&UnityEngine.Input.mouseScrollDelta.y!=0)Camera.Zoom(UnityEngine.Input.mouseScrollDelta.y*16);
    }
    void OnApplicationFocus(bool focus){if(!focus)Pause();}
    void OnApplicationPause(bool pause){if(pause)Pause();}
    void OnDestroy(){Time.timeScale=1;AudioListener.pause=false;Screen.sleepTimeout=SleepTimeout.SystemSetting;}
}
}
