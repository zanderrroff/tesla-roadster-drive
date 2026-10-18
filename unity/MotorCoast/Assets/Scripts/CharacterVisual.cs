using System;
using System.Collections.Generic;
using UnityEngine;
namespace MotorCoast {
[Serializable]public sealed class CharacterSpec {public string id,label,resource;}
[Serializable]public sealed class CharacterCatalog {public CharacterSpec[] characters;}
[DefaultExecutionOrder(30)]
public sealed class CharacterVisual:MonoBehaviour {
    public GameObject Rig;public string Id;
    public bool Preview,Animations=true,Seated,Transition;
    public float Speed;public bool Crouching,Crawling,Airborne,Climbing;
    public string Emote;
    Animation animationPlayer;string current;float previewTime;
    Transform pelvis,head,rootBone;float rootHeight;Vector3 modelOrigin;Quaternion modelRotation;
    readonly List<Material> customMaterials=new List<Material>();
    readonly Dictionary<Material,Texture> originalTextures=new Dictionary<Material,Texture>();readonly List<Texture2D> tintedTextures=new List<Texture2D>();
    public static readonly string[] MotionNames={"Idle","Walk","Run","Crouch","Dance","Clap","Cheer","Laugh","Wave","SitDown","StandUp","WalkStart","WalkStop","RunStart","RunStop","FemaleDance","FemaleIdle"};
    public static readonly Color[] OutfitColors={Color.white,new Color(.26f,.40f,.65f),new Color(.40f,.47f,.32f),new Color(.65f,.24f,.19f),new Color(.30f,.28f,.35f),new Color(.72f,.61f,.40f)};
    void Awake(){Initialize();}
    public void Initialize(){
        if(animationPlayer||!Rig)return;
        pelvis=Nodes.Find(Rig.transform,"Bip01_Pelvis");head=Nodes.Find(Rig.transform,"Bip01_Head");
        rootBone=Nodes.Find(Rig.transform,"Bip01");rootHeight=rootBone?rootBone.localPosition.y:0;
        modelOrigin=Rig.transform.localPosition;modelRotation=Rig.transform.localRotation;
        animationPlayer=Rig.GetComponent<Animation>();if(!animationPlayer)animationPlayer=Rig.AddComponent<Animation>();animationPlayer.playAutomatically=false;
        foreach(string name in MotionNames){var clip=Resources.Load<AnimationClip>("Motions/"+name);if(clip)animationPlayer.AddClip(clip,name);}
        SetTint(PlayerPrefs.GetInt("outfitTint",0));
    }
    public void SetTint(int index) {
        foreach(var texture in tintedTextures)if(texture)Remove(texture);tintedTextures.Clear();
        foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>()) {
            var materials=r.sharedMaterials;
            for(int i=0;i<materials.Length;i++)if(!customMaterials.Contains(materials[i])){materials[i]=new Material(materials[i]);customMaterials.Add(materials[i]);}
            r.sharedMaterials=materials;
            foreach(var m in materials){
                if(!customMaterials.Contains(m))customMaterials.Add(m);
                if(!m.name.Contains("body"))continue;
                if(!originalTextures.TryGetValue(m,out var original)){original=m.GetTexture("_BaseMap");originalTextures[m]=original;}
                m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",original);if(index==0||!original)continue;
                var rt=RenderTexture.GetTemporary(original.width,original.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(original,rt);var previous=RenderTexture.active;RenderTexture.active=rt;
                var texture=new Texture2D(original.width,original.height,TextureFormat.RGBA32,true,false);texture.ReadPixels(new Rect(0,0,original.width,original.height),0,0);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
                var pixels=texture.GetPixels32();var tint=OutfitColors[Mathf.Clamp(index,0,OutfitColors.Length-1)];
                for(int p=0;p<pixels.Length;p++){
                    var c=pixels[p];bool skin=c.r>c.g*1.08f&&c.r>c.b*1.16f&&c.g>c.b*.91f;
                    if(!skin){c.r=(byte)(c.r*tint.r);c.g=(byte)(c.g*tint.g);c.b=(byte)(c.b*tint.b);pixels[p]=c;}
                }
                texture.SetPixels32(pixels);texture.Apply(true,true);texture.anisoLevel=4;tintedTextures.Add(texture);m.SetTexture("_BaseMap",texture);
            }
        }
    }
    void Update(){
        if(Seated||Transition||!animationPlayer)return;
        string next=Preview?(Id.StartsWith("Female")?"FemaleDance":"Dance"):Emote??(Crouching||Crawling?"Crouch":Speed>3?"Run":Speed>.15f?"Walk":Id.StartsWith("Female")?"FemaleIdle":"Idle");
        if(Preview){previewTime+=Time.unscaledDeltaTime;var c=Resources.Load<AnimationClip>("Motions/"+(Animations?next:"Idle"));if(c)c.SampleAnimation(Rig,Animations?previewTime%c.length:0);return;}
        if(Time.timeScale==0)return;
        if(!Animations){animationPlayer.Stop();var clip=Resources.Load<AnimationClip>("Motions/"+(Crouching||Crawling?"Crouch":"Idle"));if(clip)clip.SampleAnimation(Rig,0);current=null;return;}
        if(next!=current){animationPlayer.CrossFade(next,.18f);current=next;}
        if(current=="Walk")animationPlayer[current].speed=Mathf.Clamp(Speed/1.45f,.4f,1.8f);
        if(current=="Run")animationPlayer[current].speed=Mathf.Clamp(Speed/4f,.7f,1.5f);
    }
    void LateUpdate(){
        if(Seated||Transition||!Rig||(!Preview&&Time.timeScale==0))return;
        if(rootBone){var p=rootBone.localPosition;p.y+=rootHeight;rootBone.localPosition=p;}
        Rig.transform.SetLocalPositionAndRotation(modelOrigin,modelRotation);
        if(Crawling){
            Resources.Load<AnimationClip>("Motions/Idle")?.SampleAnimation(Rig,0);
            var pivot=transform.InverseTransformPoint(pelvis.position);Rig.transform.localRotation=Quaternion.Euler(82,0,0)*modelRotation;
            Rig.transform.localPosition+=new Vector3(0,.43f,-.12f)-transform.InverseTransformPoint(pelvis.position);
            float phase=Animations?Mathf.Sin(Time.time*5)*Mathf.Clamp01(Speed)*.12f:0;
            PoseLimb("L",true,new Vector3(-.24f,.11f,.53f+phase),Vector3.up);
            PoseLimb("R",true,new Vector3(.24f,.11f,.53f-phase),Vector3.up);
            PoseLimb("L",false,new Vector3(-.17f,.10f,-.88f-phase),Vector3.up);
            PoseLimb("R",false,new Vector3(.17f,.10f,-.88f+phase),Vector3.up);
            if(head)head.rotation=Quaternion.AngleAxis(-65,transform.right)*head.rotation;
        }else if(Airborne||Climbing){
            PoseLimb("L",false,new Vector3(-.15f,.30f,-.10f),transform.forward);
            PoseLimb("R",false,new Vector3(.15f,.24f,-.12f),transform.forward);
            if(Climbing){PoseLimb("L",true,new Vector3(-.24f,1.1f,.42f),-transform.forward);PoseLimb("R",true,new Vector3(.24f,1.1f,.42f),-transform.forward);}
        }
    }
    void PoseLimb(string side,bool arm,Vector3 target,Vector3 pole){
        var a=Nodes.Find(Rig.transform,"Bip01_"+side+(arm?"_UpperArm":"_Thigh"));
        var b=Nodes.Find(Rig.transform,"Bip01_"+side+(arm?"_Forearm":"_Calf"));
        var end=Nodes.Find(Rig.transform,"Bip01_"+side+(arm?"_Hand":"_Foot"));
        if(a&&b&&end)DriverRig.Reach(a,b,end,transform.TransformPoint(target),pole);
    }
    public void SeatPose(){
        Initialize();Seated=true;Preview=false;animationPlayer.Stop();
        var clip=Resources.Load<AnimationClip>("Motions/SitDown");if(clip)clip.SampleAnimation(Rig,clip.length-.03f);
        Rig.transform.localPosition-=transform.InverseTransformPoint(pelvis.position);
    }
    public Vector3 TransitionPose(bool entering,float time){
        Initialize();Transition=true;animationPlayer.Stop();current=null;
        Rig.transform.SetLocalPositionAndRotation(modelOrigin,modelRotation);
        var clip=Resources.Load<AnimationClip>("Motions/"+(entering?"SitDown":"StandUp"));
        if(clip)clip.SampleAnimation(Rig,Mathf.Clamp01(time)*(clip.length-.01f));
        if(rootBone){var p=rootBone.localPosition;p.y+=rootHeight;rootBone.localPosition=p;}
        return pelvis.position-transform.position;
    }
    public void EndTransition(){Transition=false;current=null;transform.localScale=Vector3.one;Rig.transform.SetLocalPositionAndRotation(modelOrigin,modelRotation);}
    public void SetHeadVisible(bool visible){
        foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>())if(r.name.Contains("head")||r.name.Contains("opacity"))r.enabled=visible;
    }
    static void Remove(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    void OnDestroy(){foreach(var m in customMaterials)if(m)Remove(m);foreach(var t in tintedTextures)if(t)Remove(t);}
}
}
