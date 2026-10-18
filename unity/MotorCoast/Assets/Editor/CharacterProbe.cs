using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace MotorCoast.Editor {
public static class CharacterProbe {
    public static void Inspect() {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        using(var w=new StreamWriter("../artifacts/character-probe.txt"))
        foreach(string f in Directory.GetFiles("Assets/CharacterSource","*.fbx",SearchOption.AllDirectories)) {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(f);w.WriteLine("MODEL "+f);
            if(!model)continue;
            if(f.Contains("Male_Adult_01"))foreach(var t in model.GetComponentsInChildren<Transform>(true))w.WriteLine($"{AnimationUtility.CalculateTransformPath(t,model.transform)} pos={t.localPosition:F4} rot={t.localEulerAngles:F1} scale={t.localScale:F3}");
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))w.WriteLine($"MESH {r.name} {r.sharedMesh.vertexCount} {r.bounds} mats={string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))}");
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(f).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"))) {
                w.WriteLine($"CLIP {clip.name} length={clip.length} human={clip.humanMotion}");
                foreach(var b in AnimationUtility.GetCurveBindings(clip).Take(12))w.WriteLine(b.path+" "+b.propertyName);
            }
        }
    }
}
}
