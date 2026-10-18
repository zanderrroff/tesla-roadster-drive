#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
namespace MotorCoast.Editor {
public static class IOSPostprocess {
    [PostProcessBuild(100)]
    public static void Apply(BuildTarget target,string path) {
        if(target!=BuildTarget.iOS)return;
        var plistPath=Path.Combine(path,"Info.plist");var plist=new PlistDocument();plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption",false);plist.root.SetString("CFBundleDisplayName","Motor Coast");plist.WriteToFile(plistPath);
        var privacy=Path.Combine(path,"PrivacyInfo.xcprivacy");
        var manifest=new PlistDocument();if(File.Exists(privacy))manifest.ReadFromFile(privacy);
        manifest.root.SetBoolean("NSPrivacyTracking",false);
        if(!manifest.root.values.ContainsKey("NSPrivacyCollectedDataTypes"))manifest.root.CreateArray("NSPrivacyCollectedDataTypes");
        var apis=manifest.root.values.ContainsKey("NSPrivacyAccessedAPITypes")?manifest.root["NSPrivacyAccessedAPITypes"].AsArray():manifest.root.CreateArray("NSPrivacyAccessedAPITypes");
        var defaults=apis.values.Select(v=>v.AsDict()).FirstOrDefault(v=>v["NSPrivacyAccessedAPIType"].AsString()=="NSPrivacyAccessedAPICategoryUserDefaults");
        if(defaults==null){defaults=apis.AddDict();defaults.SetString("NSPrivacyAccessedAPIType","NSPrivacyAccessedAPICategoryUserDefaults");defaults.CreateArray("NSPrivacyAccessedAPITypeReasons");}
        var reasons=defaults["NSPrivacyAccessedAPITypeReasons"].AsArray();if(!reasons.values.Any(v=>v.AsString()=="CA92.1"))reasons.AddString("CA92.1");
        manifest.WriteToFile(privacy);
        var pbx=PBXProject.GetPBXProjectPath(path);var project=new PBXProject();project.ReadFromFile(pbx);
        var app=project.GetUnityMainTargetGuid();var framework=project.GetUnityFrameworkTargetGuid();var file=project.FindFileGuidByProjectPath("PrivacyInfo.xcprivacy");if(string.IsNullOrEmpty(file))file=project.AddFile("PrivacyInfo.xcprivacy","PrivacyInfo.xcprivacy",PBXSourceTree.Source);project.AddFileToBuild(app,file);
        foreach(var t in new[]{app,framework}) {project.SetBuildProperty(t,"IPHONEOS_DEPLOYMENT_TARGET","16.0");project.SetBuildProperty(t,"ENABLE_BITCODE","NO");}
        project.WriteToFile(pbx);
        // Unity's own privacy manifests are retained. This declaration covers our PlayerPrefs only.
    }
}
}
#endif
