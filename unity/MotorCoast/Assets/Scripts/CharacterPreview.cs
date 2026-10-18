using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
namespace MotorCoast {
public sealed class CharacterPreview:MonoBehaviour {
    GameObject stage;Camera lens;CharacterVisual person;RenderTexture texture;Light studioLight,previousSun;
    public void Show(string resource,bool animate){
        if(!stage){
            stage=new GameObject("Character selection studio");stage.transform.position=new Vector3(0,-5000,0);
            var camera=new GameObject("Portrait camera",typeof(Camera));camera.transform.SetParent(stage.transform,false);camera.transform.localPosition=new Vector3(0,1.02f,3.3f);camera.transform.localRotation=Quaternion.Euler(0,180,0);
            lens=camera.GetComponent<Camera>();lens.fieldOfView=35;lens.nearClipPlane=.1f;lens.farClipPlane=10;lens.cullingMask=1<<10;lens.clearFlags=CameraClearFlags.SolidColor;lens.backgroundColor=new Color(.025f,.041f,.048f);
            texture=new RenderTexture(420,620,24,RenderTextureFormat.ARGB32){antiAliasing=2,name="Character portrait"};texture.Create();lens.targetTexture=texture;GetComponent<RawImage>().texture=texture;
            var light=new GameObject("Portrait softbox",typeof(Light));light.transform.SetParent(stage.transform,false);light.transform.localRotation=Quaternion.Euler(25,-155,0);var l=light.GetComponent<Light>();studioLight=l;l.type=LightType.Directional;l.intensity=2.1f;l.color=new Color(.92f,.95f,1);l.cullingMask=1<<10;
        }
        if(person){person.gameObject.SetActive(false);Destroy(person.gameObject);}
        person=Instantiate(Resources.Load<GameObject>(resource),stage.transform,false).GetComponent<CharacterVisual>();person.Preview=true;person.Animations=animate;Nodes.Layer(person.gameObject,10);
    }
    void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeginPortrait;RenderPipelineManager.endCameraRendering+=EndPortrait;}
    void OnDisable(){RenderPipelineManager.beginCameraRendering-=BeginPortrait;RenderPipelineManager.endCameraRendering-=EndPortrait;if(RenderSettings.sun==studioLight&&previousSun)RenderSettings.sun=previousSun;}
    void BeginPortrait(ScriptableRenderContext context,Camera camera){if(camera==lens&&studioLight){previousSun=RenderSettings.sun;RenderSettings.sun=studioLight;}}
    void EndPortrait(ScriptableRenderContext context,Camera camera){if(camera==lens&&previousSun)RenderSettings.sun=previousSun;}
    void OnDestroy(){if(lens)lens.enabled=false;if(stage)Destroy(stage);if(texture){texture.Release();Destroy(texture);}}
}
}
