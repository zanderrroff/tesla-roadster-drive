using System.Collections.Generic;
using UnityEngine;
namespace MotorCoast {
public static class CabinBuilder {
    public static Transform Build(Transform chassis,VehicleSpec spec) {
        var root=new GameObject("Coachwork interior").transform;root.SetParent(chassis,false);root.localPosition=spec.shellOffset;
        var black=Geometry.Lit("Charcoal leather",new Color(.025f,.031f,.039f),0,.22f);
        var cream=Geometry.Lit("Warm leather",new Color(.41f,.36f,.29f),0,.25f);
        var insert=Geometry.Lit("Perforated leather",new Color(.52f,.46f,.36f),0,.16f);
        var metal=Geometry.Lit("Satin aluminium",new Color(.55f,.61f,.64f),.85f,.65f);
        var carbon=Geometry.Lit("Carbon trim",new Color(.035f,.045f,.05f),.35f,.55f);
        var led=Geometry.Lit("Ambient lighting",new Color(.20f,.45f,.52f),.1f,.6f);
        led.EnableKeyword("_EMISSION");led.SetColor("_EmissionColor",new Color(.08f,.3f,.38f));
        void Box(Vector3 size,Vector3 p,Material m,float tilt=0) {
            var b=Geometry.RoundedBox(root,size,p,m,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.32f);b.transform.localRotation=Quaternion.Euler(tilt,0,0);
        }
        Box(new Vector3(1.34f,.07f,1.53f),new Vector3(0,-.405f,-.01f),black);
        Box(new Vector3(1.34f,.64f,.075f),new Vector3(0,-.035f,-.78f),black);
        foreach(float x in new[]{-.34f,.34f}) {
            Box(new Vector3(.47f,.075f,.53f),new Vector3(x,-.334f,-.23f),black);
            Box(new Vector3(.31f,.022f,.43f),new Vector3(x,-.296f,-.23f),insert);
            Box(new Vector3(.45f,.60f,.12f),new Vector3(x,0,-.52f),cream,-8.59f);
            Box(new Vector3(.29f,.46f,.025f),new Vector3(x,-.015f,-.44f),insert,-8.59f);
            Box(new Vector3(.25f,.17f,.12f),new Vector3(x,.355f,-.55f),black);
            Box(new Vector3(.18f,.11f,.025f),new Vector3(x,.355f,-.477f),cream);
            foreach(float side in new[]{-1f,1f}) {
                Box(new Vector3(.075f,.46f,.14f),new Vector3(x+side*.19f,-.045f,-.41f),cream,-7.4f);
                Box(new Vector3(.06f,.085f,.43f),new Vector3(x+side*.19f,-.29f,-.23f),cream);
                for(int k=0;k<4;k++)Box(new Vector3(.002f,.045f,.002f),new Vector3(x+side*.15f,-.06f+k*.07f,-.329f),insert,-7.4f);
            }
        }
        foreach(float s in new[]{-1f,1f}) {
            Box(new Vector3(.065f,.42f,1.33f),new Vector3(s*.66f,-.07f,-.035f),black);
            Box(new Vector3(.07f,.16f,.85f),new Vector3(s*.621f,-.025f,-.03f),cream);
            Box(new Vector3(.07f,.006f,1.10f),new Vector3(s*.624f,.12f,.02f),led);
            Box(new Vector3(.085f,.055f,.54f),new Vector3(s*.60f,-.045f,-.09f),black);
            Box(new Vector3(.02f,.025f,.14f),new Vector3(s*.574f,.07f,.2f),metal);
        }
        Box(new Vector3(1.30f,.15f,.34f),new Vector3(0,.12f,.57f),black);
        Box(new Vector3(1.27f,.04f,.28f),new Vector3(0,.207f,.55f),carbon);
        Box(new Vector3(1.17f,.006f,.02f),new Vector3(0,.11f,.39f),led);
        Box(new Vector3(.20f,.16f,.84f),new Vector3(0,-.20f,.03f),black);
        Box(new Vector3(.18f,.045f,.28f),new Vector3(0,-.10f,-.21f),cream);
        Box(new Vector3(.16f,.025f,.33f),new Vector3(0,-.106f,.16f),carbon);
        Box(new Vector3(.35f,.205f,.014f),new Vector3(0,.108f,.365f),black);
        var display=Geometry.RoundedBox(root,new Vector3(.325f,.18f,.002f),new Vector3(0,.108f,.354f),carbon,.004f);
        for(int i=0;i<4;i++)Box(new Vector3(.035f,.002f,.001f),new Vector3(-.11f+i*.06f,.06f,.352f),led);
        foreach(float x in new[]{-.52f,.52f})for(int i=0;i<5;i++)Box(new Vector3(.16f,.002f,.01f),new Vector3(x,.14f+i*.007f,.38f),metal);
        Geometry.Combine(root);
        var wheel=new GameObject("SteeringWheel").transform;wheel.SetParent(chassis,false);wheel.localPosition=spec.wheelPosition;wheel.localRotation=Quaternion.Euler(spec.wheelTilt,0,0);
        Geometry.MeshObject("Leather rim",wheel,Geometry.Torus(spec.steeringRadius,.014f,56,12),black);
        Geometry.RoundedBox(wheel,new Vector3(.20f,.024f,.026f),new Vector3(0,-.01f,0),metal,.008f);
        Geometry.RoundedBox(wheel,new Vector3(.025f,.1f,.024f),new Vector3(0,-.055f,0),metal,.006f);
        Geometry.RoundedBox(wheel,new Vector3(.075f,.052f,.037f),new Vector3(0,0,-.009f),black,.015f);
        return wheel;
    }
}
public static class Geometry {
    static void Remove(Object o){if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
    public static Material Lit(string name,Color color,float metallic=0,float smoothness=.3f) {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);return m;
    }
    public static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material) {
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=material;return o;
    }
    public static GameObject Box(string name,Transform parent,Vector3 size,Vector3 position,Material material,bool collide=false) {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;
        if(!collide){o.GetComponent<Collider>().enabled=false;Remove(o.GetComponent<Collider>());}return o;
    }
    public static GameObject RoundedBox(Transform parent,Vector3 size,Vector3 position,Material material,float radius) {
        const int n=4;var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        Vector3[] faces={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        var half=size*.5f;var inner=half-Vector3.one*radius;
        foreach(var normal in faces) {
            var u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up).normalized;var v=Vector3.Cross(normal,u);int start=vertices.Count;
            for(int y=0;y<=n;y++)for(int x=0;x<=n;x++) {
                var p=Vector3.Scale(normal+u*(x*2f/n-1)+v*(y*2f/n-1),half);
                var q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                var outward=(p-q).normalized;vertices.Add(q+outward*radius);normals.Add(outward);uv.Add(new Vector2(x/(float)n,y/(float)n));
            }
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){int a=start+y*(n+1)+x;indices.AddRange(new[]{a,a+1,a+n+2,a,a+n+2,a+n+1});}
        }
        var mesh=new Mesh{name="Beveled upholstery"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
        var o=MeshObject(material.name,parent,mesh,material);o.transform.localPosition=position;return o;
    }
    public static Mesh Torus(float major,float minor,int rings,int sides) {
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var ix=new List<int>();
        for(int i=0;i<=rings;i++)for(int j=0;j<=sides;j++) { float a=i*2*Mathf.PI/rings,b=j*2*Mathf.PI/sides;var normal=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));v.Add(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*major+normal*minor);n.Add(normal);uv.Add(new Vector2(i/(float)rings,j/(float)sides)); }
        for(int i=0;i<rings;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j;ix.AddRange(new[]{a,a+sides+1,a+1,a+1,a+sides+1,a+sides+2});}
        var mesh=new Mesh{name="Steering rim"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(ix,0);mesh.RecalculateBounds();return mesh;
    }
    public static void Combine(Transform root) {
        var groups=new Dictionary<Material,List<CombineInstance>>();var sources=new List<GameObject>();
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>()) {
            if(mf.transform==root)continue;var r=mf.GetComponent<MeshRenderer>();if(!r||r.sharedMaterials.Length!=1)continue;
            var mat=r.sharedMaterial;if(!groups.TryGetValue(mat,out var list))groups[mat]=list=new List<CombineInstance>();
            list.Add(new CombineInstance{mesh=mf.sharedMesh,transform=root.worldToLocalMatrix*mf.transform.localToWorldMatrix});sources.Add(mf.gameObject);
        }
        foreach(var item in groups){var mesh=new Mesh{name=item.Key.name+" batch",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(item.Value.ToArray(),true,true);MeshObject(mesh.name,root,mesh,item.Key);}
        foreach(var source in sources){source.SetActive(false);Remove(source);}
    }
}
}
