using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MotorCoast.Editor {
public static class BuildMac {
    [MenuItem("Motor Coast/3. Build Mac app")]
    public static void Export() {
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneOSX)
            throw new BuildFailedException("Run Tools/build-mac.sh to select macOS first.");
        const string scene="Assets/Scenes/Coast.unity";
        if(!File.Exists(scene)||!Resources.Load<RenderPipelineAsset>("Render/Standard"))
            throw new BuildFailedException("Prepare the Motor Coast project before building.");
        // Editor play/preview can replace the active quality level's pipeline.
        // Re-register BOTH runtime presets before URP strips build resources.
        var quality=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var levels=quality.FindProperty("m_QualitySettings");
        for(int i=0;i<levels.arraySize;i++) {
            var pipeline=Resources.Load<UniversalRenderPipelineAsset>("Render/"+(i==levels.arraySize-1?"Ultra":"Standard"));
            if(!pipeline)throw new BuildFailedException("A runtime render preset is missing.");
            levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline").objectReferenceValue=pipeline;
        }
        quality.ApplyModifiedPropertiesWithoutUndo();
        QualitySettings.SetQualityLevel(0,false);
        GraphicsSettings.defaultRenderPipeline=Resources.Load<UniversalRenderPipelineAsset>("Render/Standard");
        PlayerSettings.companyName="Motor Coast";PlayerSettings.productName="Motor Coast";
        PlayerSettings.bundleVersion="2.1.0";PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"app.zanderrroff.motorcoast.mac");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone,ManagedStrippingLevel.Disabled);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{GraphicsDeviceType.Metal});
        EditorUserBuildSettings.SetPlatformSettings("OSXUniversal","Architecture","x64");
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.macRetinaSupport=false;PlayerSettings.runInBackground=false;
        PlayerSettings.usePlayerLog=true;PlayerSettings.allowFullscreenSwitch=true;
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Brand/AppIcon.png");
        if(icon)PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,new[]{icon});
        const string plugin="Assets/Plugins/macOS/libMotorCoastNative.dylib";
        if(!File.Exists(plugin))throw new BuildFailedException("Build the C++ plugin first.");
        AssetDatabase.ImportAsset(plugin,ImportAssetOptions.ForceSynchronousImport);
        var importer=(PluginImporter)AssetImporter.GetAtPath(plugin);
        importer.SetCompatibleWithAnyPlatform(false);importer.SetCompatibleWithEditor(true);
        importer.SetEditorData("OS","OSX");importer.SetEditorData("CPU","AnyCPU");
        importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX,true);
        importer.SetPlatformData(BuildTarget.StandaloneOSX,"CPU","AnyCPU");importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        string path=Environment.GetEnvironmentVariable("MOTORCOAST_MAC_OUTPUT")??Path.GetFullPath("../Build/macOS/Motor Coast.app");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        // Fast block compression keeps local rebuilds practical on Intel Air.
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneOSX,locationPathName=path,options=BuildOptions.CompressWithLz4});
        if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("macOS build failed: "+report.summary.result);
        File.WriteAllText(Path.GetFullPath("../artifacts/mac-build.txt"),$"Unity {Application.unityVersion}\nmacOS / Mono / native C++ driving / Metal\nSize: {report.summary.totalSize}\nSeconds: {report.summary.totalTime.TotalSeconds:0.0}\nApp: {path}\n");
        Debug.Log("Motor Coast macOS app ready: "+path);
    }
}
}
