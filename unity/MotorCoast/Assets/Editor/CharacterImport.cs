using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace MotorCoast.Editor {
// The original Rocketbox FBX rig and motions share their hierarchy. Keeping
// rotations and each avatar's bind translations retargets without stretching.
public static class CharacterImport {
    public static void Prepare() {
        const string source="Assets/CharacterSource",target="Assets/Resources/Characters";
        Directory.CreateDirectory(target);Directory.CreateDirectory("Assets/Resources/Motions");
        foreach(string path in Directory.GetFiles(source+"/Animations","*.fbx")) {
            var original=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            string name=Path.GetFileNameWithoutExtension(path),output="Assets/Resources/Motions/"+name+".anim";
            var clip=new AnimationClip{name=name,legacy=true,wrapMode=WrapMode.Loop,frameRate=30};
            foreach(var b in AnimationUtility.GetCurveBindings(original)) {
                if(!b.propertyName.StartsWith("m_LocalRotation"))continue;
                var binding=b;binding.path=b.path.Replace("Bip02","Bip01").Replace(' ','_');
                AnimationUtility.SetEditorCurve(clip,binding,AnimationUtility.GetEditorCurve(original,b));
            }
            var sourceRoot=AssetDatabase.LoadAssetAtPath<GameObject>(path);var sourceHip=Nodes.Find(sourceRoot.transform,"Bip01")??Nodes.Find(sourceRoot.transform,"Bip02");
            var heightBinding=EditorCurveBinding.FloatCurve(sourceHip?sourceHip.name:"Bip01",typeof(Transform),"m_LocalPosition.y");
            var heightCurve=AnimationUtility.GetEditorCurve(original,heightBinding);float origin=sourceHip?sourceHip.localPosition.y:0;
            heightCurve=heightCurve==null?AnimationCurve.Constant(0,original.length,0):new AnimationCurve(heightCurve.keys.Select(k=>new Keyframe(k.time,k.value-origin,k.inTangent,k.outTangent)).ToArray());
            heightBinding.path="Bip01";AnimationUtility.SetEditorCurve(clip,heightBinding,heightCurve);
            clip.EnsureQuaternionContinuity();var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
            if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,output);
        }
        foreach(string dir in Directory.GetDirectories(source).Where(d=>!d.EndsWith("Animations"))) {
            string id=Path.GetFileName(dir),path=dir+"/"+id+".fbx";
            if(!File.Exists(path))continue;
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!original)throw new Exception("Character import failed: "+id);
            var root=new GameObject(id);var model=UnityEngine.Object.Instantiate(original,root.transform);model.name="Rig";
            foreach(var a in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(a);
            foreach(var t in model.GetComponentsInChildren<Transform>(true))t.name=t.name.Replace("Bip02","Bip01").Replace(' ','_');
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                if(renderer.name.Contains("poly")&&!renderer.name.Contains("hipoly")){UnityEngine.Object.DestroyImmediate(renderer.gameObject);continue;}
                renderer.gameObject.SetActive(true);var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++) {
                    string name=materials[i].name,mp=dir+"/"+name+".mat";
                    var m=AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,mp);}
                    m.name=name;m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.22f);m.SetFloat("_Metallic",0);
                    var color=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+name+"_color.png");
                    bool cutout=name.Contains("opacity");
                    if(!color){color=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+name+"_opacity_color.png");cutout=color;}
                    if(!color)throw new Exception("Missing character texture: "+id+" / "+name);
                    m.SetTexture("_BaseMap",color);
                    var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+name+"_normal.png");
                    if(normal){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.7f);}
                    if(cutout){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.45f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=(int)RenderQueue.AlphaTest;}
                    EditorUtility.SetDirty(m);materials[i]=m;
                }
                renderer.sharedMaterials=materials;renderer.quality=SkinQuality.Bone4;renderer.updateWhenOffscreen=true;
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                if(materials.Length>1){
                    for(int i=0;i<materials.Length;i++){
                        var mesh=UnityEngine.Object.Instantiate(renderer.sharedMesh);mesh.name=id+"_"+i;
                        var triangles=mesh.GetTriangles(i);mesh.subMeshCount=1;mesh.SetTriangles(triangles,0);
                        string meshPath=dir+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,meshPath);
                        var part=new GameObject(materials[i].name);part.transform.SetParent(renderer.transform,false);
                        var skin=part.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=materials[i];skin.bones=renderer.bones;skin.rootBone=renderer.rootBone;skin.localBounds=renderer.localBounds;skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone4;
                    }
                    UnityEngine.Object.DestroyImmediate(renderer);
                }
            }
            var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("No character skin "+id);
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=1.76f/bounds.size.y;model.transform.localScale=Vector3.one*scale;
            model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
            var avatar=root.AddComponent<CharacterVisual>();avatar.Rig=model;avatar.Id=id;
            PrefabUtility.SaveAsPrefabAsset(root,target+"/"+id+".prefab");UnityEngine.Object.DestroyImmediate(root);
        }
        var ids=Directory.GetFiles(target,"*.prefab").Select(Path.GetFileNameWithoutExtension).OrderBy(x=>x).ToArray();
        File.WriteAllText("Assets/Resources/Data/characters.json",JsonUtility.ToJson(new CharacterCatalog{characters=ids.Select(id=>new CharacterSpec{id=id,label=id.Replace('_',' '),resource="Characters/"+id}).ToArray()},true));
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("Rocketbox avatars prepared: "+ids.Length);
    }
}
public sealed class RocketboxTextureImport:AssetPostprocessor {
    void OnPreprocessTexture() {
        if(!assetPath.StartsWith("Assets/CharacterSource/"))return;
        var t=(TextureImporter)assetImporter;t.maxTextureSize=1024;t.mipmapEnabled=true;t.anisoLevel=4;
        if(assetPath.Contains("_normal")){t.textureType=TextureImporterType.NormalMap;t.sRGBTexture=false;}
        t.alphaIsTransparency=assetPath.Contains("opacity");
        t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="iPhone",overridden=true,format=TextureImporterFormat.ASTC_6x6,maxTextureSize=1024,compressionQuality=80});
    }
}
}
