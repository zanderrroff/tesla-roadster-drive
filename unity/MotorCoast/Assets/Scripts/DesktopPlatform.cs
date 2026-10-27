using System;
using UnityEngine;
namespace MotorCoast {
public static class DesktopPlatform {
    public static bool IsMac=>Application.platform==RuntimePlatform.OSXPlayer;
    public static bool LowPowerMac=>IsMac&&SystemInfo.graphicsDeviceName.IndexOf("Intel",StringComparison.OrdinalIgnoreCase)>=0;
    public static void ToggleFullscreen(){
        if(Screen.fullScreen)Screen.SetResolution(1280,720,FullScreenMode.Windowed);
        else Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.height,FullScreenMode.FullScreenWindow);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize(){
        if(!IsMac)return;
        if(!PlayerPrefs.HasKey("macFirstRun")){
            PlayerPrefs.SetInt("graphicsPreset",0);PlayerPrefs.SetInt("showFps",1);
            PlayerPrefs.SetInt("macFirstRun",1);PlayerPrefs.Save();
        }
    }
}
}
