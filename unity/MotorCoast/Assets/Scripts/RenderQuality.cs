using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MotorCoast {
public sealed class RenderQuality : MonoBehaviour {
    public int Level {get;private set;}
    public float MeasuredFps {get;private set;}
    public string ResolutionLabel=>pipeline?Mathf.RoundToInt(Screen.width*pipeline.renderScale)+" × "+Mathf.RoundToInt(Screen.height*pipeline.renderScale):"—";
    UniversalRenderPipelineAsset pipeline;Volume volume;float frameTime,overload,baseScale;int observedFrames;
    public void Apply(int level) {
        Level=Mathf.Clamp(level,0,2);PlayerPrefs.SetInt("quality",Level);PlayerPrefs.Save();
        var source=Resources.Load<UniversalRenderPipelineAsset>("Render/"+new[]{"Performance","Balanced","Detail"}[Level]);
        if(!source)throw new System.InvalidOperationException("Run Motor Coast / Prepare project before playing.");
        var previous=pipeline;pipeline=Instantiate(source);QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
        if(previous)Destroy(previous);
        float pixelCap=new[]{1500,1920,2560}[Level];baseScale=Mathf.Clamp(pixelCap/Mathf.Max(Screen.width,Screen.height),.55f,1);pipeline.renderScale=baseScale;
        QualitySettings.vSyncCount=0;QualitySettings.lodBias=new[]{.7f,1f,1.5f}[Level];QualitySettings.anisotropicFiltering=AnisotropicFiltering.Enable;
        Application.targetFrameRate=Level==2?30:60;overload=0;
        if(!volume){var o=new GameObject("Photographic grade",typeof(Volume));volume=o.GetComponent<Volume>();volume.isGlobal=true;volume.priority=1;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tone=volume.profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var color=volume.profile.Add<ColorAdjustments>(true);color.postExposure.Override(-.15f);color.contrast.Override(4);color.saturation.Override(-3);
            var bloom=volume.profile.Add<Bloom>(true);bloom.intensity.Override(.12f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.6f);
        }
        if(volume.profile.TryGet<Bloom>(out var b))b.active=Level>0;
    }
    void Update() {
        if(Time.timeScale==0||!pipeline)return;
        frameTime=Mathf.Lerp(frameTime,Time.unscaledDeltaTime,.025f);observedFrames++;if(observedFrames>45)MeasuredFps=1/Mathf.Max(.001f,frameTime);
        float budget=Level==2?1f/30:1f/60;
        overload=frameTime>budget*1.2f?overload+Time.unscaledDeltaTime:Mathf.Max(0,overload-Time.unscaledDeltaTime);
        if(overload>3){pipeline.renderScale=Mathf.Max(baseScale*.72f,pipeline.renderScale-.06f);overload=0;}
    }
    void OnEnable(){Application.lowMemory+=OnLowMemory;}void OnDisable(){Application.lowMemory-=OnLowMemory;}
    void OnLowMemory(){if(Level>0)Apply(Level-1);Resources.UnloadUnusedAssets();}
}
}
