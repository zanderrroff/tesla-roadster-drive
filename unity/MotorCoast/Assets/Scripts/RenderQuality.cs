using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MotorCoast {
public sealed class RenderQuality : MonoBehaviour {
    public int Level {get;private set;}
    public bool Ultra=>Level==1;
    public static int SavedLevel=>PlayerPrefs.HasKey("graphicsPreset")?PlayerPrefs.GetInt("graphicsPreset",0):(PlayerPrefs.GetInt("quality",1)==2?1:0);
    public float MeasuredFps {get;private set;}
    public string ResolutionLabel=>pipeline?Mathf.RoundToInt(Screen.width*pipeline.renderScale)+" × "+Mathf.RoundToInt(Screen.height*pipeline.renderScale):"—";
    UniversalRenderPipelineAsset pipeline;Volume volume;float frameTime,overload,recovery,baseScale;int observedFrames;
    public void Apply(int level) {
        Level=Mathf.Clamp(level,0,1);PlayerPrefs.SetInt("graphicsPreset",Level);PlayerPrefs.Save();
        QualitySettings.SetQualityLevel(Level*(QualitySettings.names.Length-1),false);
        QualitySettings.skinWeights=SkinWeights.FourBones;QualitySettings.globalTextureMipmapLimit=0;
        var source=Resources.Load<UniversalRenderPipelineAsset>("Render/"+(Ultra?"Ultra":"Standard"));
        if(!source)throw new System.InvalidOperationException("Run Motor Coast / Prepare project before playing.");
        var previous=pipeline;pipeline=Instantiate(source);QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
        if(previous)Destroy(previous);
        float pixelCap=Ultra?2560:1920;baseScale=Mathf.Clamp(pixelCap/Mathf.Max(Screen.width,Screen.height),.55f,1);pipeline.renderScale=baseScale;
        QualitySettings.vSyncCount=0;QualitySettings.lodBias=Ultra?1.6f:1;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
        Application.targetFrameRate=60;overload=recovery=0;observedFrames=0;frameTime=1f/60;MeasuredFps=0;
        if(!volume){var o=new GameObject("Photographic grade",typeof(Volume));volume=o.GetComponent<Volume>();volume.isGlobal=true;volume.priority=1;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tone=volume.profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var color=volume.profile.Add<ColorAdjustments>(true);color.postExposure.Override(-.15f);color.contrast.Override(4);color.saturation.Override(-3);
            var bloom=volume.profile.Add<Bloom>(true);bloom.intensity.Override(.12f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.6f);
        }
        if(volume.profile.TryGet<Bloom>(out var b)){b.active=Ultra;b.intensity.Override(.15f);}
    }
    void Update() {
        if(Time.timeScale==0||!pipeline)return;
        frameTime=Mathf.Lerp(frameTime,Time.unscaledDeltaTime,.025f);observedFrames++;if(observedFrames>45)MeasuredFps=1/Mathf.Max(.001f,frameTime);
        float budget=1f/60;
        overload=frameTime>budget*1.2f?overload+Time.unscaledDeltaTime:Mathf.Max(0,overload-Time.unscaledDeltaTime);
        recovery=frameTime<budget*1.04f?recovery+Time.unscaledDeltaTime:0;
        if(recovery>12){pipeline.renderScale=Mathf.Min(baseScale,pipeline.renderScale+.03f);recovery=0;}
        if(overload>3){pipeline.renderScale=Mathf.Max(baseScale*.72f,pipeline.renderScale-.06f);overload=0;}
    }
    void OnEnable(){Application.lowMemory+=OnLowMemory;}void OnDisable(){Application.lowMemory-=OnLowMemory;}
    void OnLowMemory(){if(Level>0)Apply(Level-1);Resources.UnloadUnusedAssets();}
}
}
