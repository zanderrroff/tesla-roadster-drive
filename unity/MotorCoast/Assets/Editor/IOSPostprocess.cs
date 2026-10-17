#if UNITY_IOS
using System.IO;
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
        File.WriteAllText(privacy,"<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"><plist version=\"1.0\"><dict><key>NSPrivacyTracking</key><false/><key>NSPrivacyCollectedDataTypes</key><array/><key>NSPrivacyAccessedAPITypes</key><array><dict><key>NSPrivacyAccessedAPIType</key><string>NSPrivacyAccessedAPICategoryUserDefaults</string><key>NSPrivacyAccessedAPITypeReasons</key><array><string>CA92.1</string></array></dict></array></dict></plist>");
        var pbx=PBXProject.GetPBXProjectPath(path);var project=new PBXProject();project.ReadFromFile(pbx);
        var app=project.GetUnityMainTargetGuid();var framework=project.GetUnityFrameworkTargetGuid();var file=project.AddFile("PrivacyInfo.xcprivacy","PrivacyInfo.xcprivacy",PBXSourceTree.Source);project.AddFileToBuild(app,file);
        foreach(var t in new[]{app,framework}) {project.SetBuildProperty(t,"IPHONEOS_DEPLOYMENT_TARGET","16.0");project.SetBuildProperty(t,"ENABLE_BITCODE","NO");}
        project.WriteToFile(pbx);
        // Unity's own privacy manifests are retained. This declaration covers our PlayerPrefs only.
    }
}
}
#endif
