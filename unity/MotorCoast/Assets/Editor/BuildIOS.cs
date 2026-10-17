using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MotorCoast.Editor {
public static class BuildIOS {
    const string Scene="Assets/Scenes/Coast.unity";
    [MenuItem("Motor Coast/1. Prepare project")]
    public static void Prepare() {
        Directory.CreateDirectory("Assets/Resources/Render");Directory.CreateDirectory("Assets/Scenes");
        string[] names={"Performance","Balanced","Detail"};
        for(int q=0;q<3;q++) {
            string path="Assets/Resources/Render/"+names[q]+".asset";
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if(!pipeline) {
                var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();renderer.name=names[q]+" Renderer";renderer.renderingMode=RenderingMode.Forward;renderer.depthPrimingMode=DepthPrimingMode.Disabled;
                renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(renderer,"Assets/Resources/Render/"+names[q]+"Renderer.asset");
                ResourceReloader.ReloadAllNullIn(renderer,UniversalRenderPipelineAsset.packagePath);
                pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,path);
            }
            pipeline.supportsHDR=q>0;pipeline.msaaSampleCount=q==0?2:4;pipeline.renderScale=1;
            var settings=new SerializedObject(pipeline);settings.FindProperty("m_MainLightShadowsSupported").boolValue=true;settings.FindProperty("m_SoftShadowsSupported").boolValue=q>0;
            settings.FindProperty("m_MainLightRenderingMode").intValue=(int)LightRenderingMode.PerPixel;settings.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)LightRenderingMode.Disabled;
            pipeline.mainLightShadowmapResolution=q==2?4096:2048;pipeline.shadowDistance=new[]{55f,90f,140f}[q];pipeline.shadowCascadeCount=q==2?4:2;
            pipeline.shadowDepthBias=.08f;pipeline.shadowNormalBias=.22f;pipeline.supportsCameraDepthTexture=false;pipeline.supportsCameraOpaqueTexture=false;
            pipeline.useSRPBatcher=true;pipeline.supportsDynamicBatching=false;settings.FindProperty("m_ReflectionProbeBlending").boolValue=q>0;settings.FindProperty("m_ReflectionProbeBoxProjection").boolValue=q>0;settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }
        GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/Render/Balanced.asset");QualitySettings.renderPipeline=GraphicsSettings.defaultRenderPipeline;
        PlayerSettings.companyName="Motor Coast";PlayerSettings.productName="Motor Coast";PlayerSettings.bundleVersion="2.0.0";PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.runInBackground=false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"app.zanderrroff.motorcoast");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.iOS,Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS,ManagedStrippingLevel.Minimal);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{GraphicsDeviceType.Metal});
        PlayerSettings.iOS.targetOSVersionString="16.0";PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneOnly;
        PlayerSettings.iOS.appleDeveloperTeamID=Environment.GetEnvironmentVariable("APPLE_TEAM_ID")??"U8QHYG2G45";PlayerSettings.iOS.appleEnableAutomaticSigning=true;
        PlayerSettings.iOS.buildNumber=Environment.GetEnvironmentVariable("BUILD_NUMBER")??"1";
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToLandscapeLeft=PlayerSettings.allowedAutorotateToLandscapeRight=true;PlayerSettings.allowedAutorotateToPortrait=PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Brand/AppIcon.png");if(icon)PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(string name in new[]{"Universal Render Pipeline/Lit","MotorCoast/CoastWater","Skybox/Panoramic","UI/Default","UI/Default Font"}) {
            var shader=Shader.Find(name);if(!shader)throw new BuildFailedException("Shader missing: "+name);
            bool present=false;for(int n=0;n<shaders.arraySize;n++)if(shaders.GetArrayElementAtIndex(n).objectReferenceValue==shader)present=true;
            if(!present){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;}
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        foreach(var file in Directory.GetFiles("Assets/Plugins/iOS"))if(!file.EndsWith(".meta")) {
            var importer=AssetImporter.GetAtPath(file) as PluginImporter;if(!importer)continue;importer.SetCompatibleWithAnyPlatform(false);importer.SetCompatibleWithEditor(false);importer.SetCompatibleWithPlatform(BuildTarget.iOS,true);importer.SaveAndReimport();
        }
        foreach(var file in Directory.GetFiles("Assets/Plugins/macOS","*.dylib")) {
            var importer=AssetImporter.GetAtPath(file) as PluginImporter;if(!importer)continue;importer.SetCompatibleWithAnyPlatform(false);importer.SetCompatibleWithEditor(true);importer.SetEditorData("OS","OSX");importer.SetEditorData("CPU","AnyCPU");importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        // Re-import after choosing URP so glTFast generates URP materials, never Built-in shaders.
        foreach(var file in Directory.GetFiles("Assets/Resources","*.gltf",SearchOption.AllDirectories))AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        var catalog=JsonUtility.FromJson<VehicleCatalog>(Resources.Load<TextAsset>("Data/vehicles").text);
        foreach(var spec in catalog.vehicles)foreach(var resource in new[]{spec.resource,spec.highResource}) {
            var model=Resources.Load<GameObject>(resource);if(!model||!Nodes.Find(model.transform,"Chassis"))throw new BuildFailedException("Invalid imported car: "+resource);
            for(int i=0;i<4;i++)if(!Nodes.Find(model.transform,"Wheel_"+i))throw new BuildFailedException("Missing wheel "+i+": "+resource);
        }
        if(!Resources.Load<GameObject>("Driver/driver"))throw new BuildFailedException("Driver was not imported");
        if(!File.Exists(Scene)){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(scene,Scene);}
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};AssetDatabase.SaveAssets();
        Debug.Log("Motor Coast: Unity project prepared (URP / Metal / IL2CPP).");
    }
    [MenuItem("Motor Coast/2. Export iPhone project")]
    public static void Export() {
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.iOS)throw new BuildFailedException("Switch to iOS first, or run Tools/export-ios.sh (-buildTarget iOS).");
        Prepare();string path=Environment.GetEnvironmentVariable("MOTORCOAST_XCODE_OUTPUT")??Path.GetFullPath("../Build/iOS");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},target=BuildTarget.iOS,locationPathName=path,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Unity iOS export failed: "+report.summary.result);
        var summary=new { version=Application.unityVersion,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,target="iOS",backend="IL2CPP",graphics="Metal" };
        File.WriteAllText(Path.Combine(path,"motorcoast-build.txt"),$"Unity {summary.version}\nNative iOS / IL2CPP / Metal\nSize: {summary.bytes}\nExport seconds: {summary.seconds:0.0}\n");
        var scripts=Path.Combine(path,"ci_scripts");Directory.CreateDirectory(scripts);
        File.Copy(Path.GetFullPath("../Tools/ci_post_clone.sh"),Path.Combine(scripts,"ci_post_clone.sh"),true);
        Debug.Log("Xcode export ready: "+path);
    }
}
public sealed class AssetSettings:AssetPostprocessor {
    void OnPreprocessTexture(){
        if(!assetPath.StartsWith("Assets/Resources/")&&!assetPath.StartsWith("Assets/Brand/"))return;
        var t=(TextureImporter)assetImporter;t.mipmapEnabled=!assetPath.Contains("Brand/");t.isReadable=false;t.anisoLevel=8;t.maxTextureSize=2048;
        t.sRGBTexture=!(assetPath.Contains("-linear-")||assetPath.Contains("-normal")||assetPath.Contains("-metal-smooth")||assetPath.Contains("-ao"));
        if(assetPath.Contains("-normal"))t.textureType=TextureImporterType.NormalMap;
        if(assetPath.EndsWith(".hdr")){t.textureCompression=TextureImporterCompression.Uncompressed;return;}
        if(assetPath.Contains("Brand/")){t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.Uncompressed;return;}
        t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="iPhone",overridden=true,format=TextureImporterFormat.ASTC_6x6,maxTextureSize=2048,compressionQuality=80});
    }
    void OnPreprocessAudio(){if(!assetPath.StartsWith("Assets/Resources/Audio/"))return;var importer=(AudioImporter)assetImporter;var s=importer.defaultSampleSettings;s.loadType=AudioClipLoadType.CompressedInMemory;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.8f;s.preloadAudioData=true;importer.defaultSampleSettings=s;}
}
}
