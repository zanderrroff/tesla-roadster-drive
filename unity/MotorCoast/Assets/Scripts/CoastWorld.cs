using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MotorCoast {
public sealed class CoastWorld : MonoBehaviour {
    public readonly List<Vector3> Slalom=new List<Vector3>();
    readonly System.Random random=new System.Random(3218);
    Material asphalt,stone,grass,white,orange,metal,leaf;
    public int MapIndex {get;private set;}
    public void Build(int mapIndex=0) {
        using var resources=RuntimeResources.Begin(gameObject);
        MapIndex=Mathf.Clamp(mapIndex,0,WorldCatalog.Maps.Length-1);
        asphalt=Geometry.Lit("Photogrammetry asphalt",Color.white,0,.12f);
        asphalt.SetTexture("_BaseMap",Resources.Load<Texture2D>("Textures/asphalt-color"));asphalt.SetTexture("_BumpMap",Resources.Load<Texture2D>("Textures/asphalt-normal"));asphalt.EnableKeyword("_NORMALMAP");asphalt.SetFloat("_BumpScale",.55f);
        asphalt.SetTexture("_MetallicGlossMap",Resources.Load<Texture2D>("Textures/asphalt-metal-smooth"));asphalt.EnableKeyword("_METALLICSPECGLOSSMAP");asphalt.SetFloat("_Smoothness",1);
        asphalt.SetTexture("_OcclusionMap",Resources.Load<Texture2D>("Textures/asphalt-ao"));asphalt.EnableKeyword("_OCCLUSIONMAP");asphalt.SetFloat("_OcclusionStrength",.7f);asphalt.SetTextureScale("_BaseMap",new Vector2(153,153));
        stone=ExpeditionWorld.Surface("concrete",new Color(.88f,.89f,.86f));grass=ExpeditionWorld.Surface("grass",Color.white,1);
        white=Geometry.Lit("Warm white markings",new Color(.78f,.78f,.70f),0,.2f);orange=Geometry.Lit("Safety orange",new Color(.85f,.20f,.045f),0,.22f);metal=Geometry.Lit("Dark metal",new Color(.12f,.16f,.17f),.7f,.45f);leaf=Geometry.Lit("Palm foliage",new Color(.12f,.22f,.075f),0,.16f);
        if(MapIndex>0){new ExpeditionWorld(transform,WorldCatalog.Maps[MapIndex]).Build();Ramp(new Vector3(-32,0,35),12,24,2.4f,0);Ramp(new Vector3(-32,0,92),16,28,2.4f,180);SetupLight();return;}
        Geometry.Box("Continuous collision foundation",transform,new Vector3(660,4,660),new Vector3(0,-2,0),grass,true);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Drift and stunt apron";ground.transform.SetParent(transform,false);ground.transform.localScale=new Vector3(46,1,46);ground.transform.localPosition=new Vector3(0,.008f,0);ground.GetComponent<Renderer>().sharedMaterial=asphalt;ground.GetComponent<Collider>().enabled=false;Destroy(ground.GetComponent<Collider>());
        // Flat painted lines have no collision. The entire apron remains freely drivable.
        Line(new Vector3(-208,.018f,-208),new Vector3(208,.018f,-208),.15f,white);
        Line(new Vector3(-208,.018f,208),new Vector3(208,.018f,208),.15f,white);
        Line(new Vector3(-208,.018f,-208),new Vector3(-208,.018f,208),.15f,white);
        Line(new Vector3(208,.018f,-208),new Vector3(208,.018f,208),.15f,white);
        foreach(float r in new[]{26f,44f})Circle(new Vector3(-90,.02f,-15),r,.15f,white);
        for(int i=0;i<19;i++)Line(new Vector3(-1,.02f,-170+i*18),new Vector3(-1,.02f,-161+i*18),.12f,white);
        for(int i=0;i<9;i++) {
            var p=new Vector3(88+(i%2==0?-7:7),0,-135+i*19);Slalom.Add(p);Cone(p);Circle(p+Vector3.up*.025f,3,.08f,orange);
        }
        Ramp(new Vector3(-125,0,105),18,28,3.6f,0);Ramp(new Vector3(-70,0,140),14,24,2.4f,180);
        Ramp(new Vector3(120,0,100),20,34,5,0);Ramp(new Vector3(120,0,168),24,36,5,180);
        Ramp(new Vector3(-155,0,-115),12,22,2.2f,90);
        // Open pit lane and shaded pavilion. Obstacles stay clear of the starting lane.
        var garage=new GameObject("Pit pavilion").transform;garage.SetParent(transform,false);
        Geometry.Box("Pit slab",garage,new Vector3(60,.12f,20),new Vector3(-60,.06f,-194),stone,true);
        Geometry.Box("Pavilion roof",garage,new Vector3(64,.25f,22),new Vector3(-60,6.5f,-194),metal,true);
        foreach(float x in new[]{-88f,-60f,-32f})foreach(float z in new[]{-203f,-185f})Geometry.Box("Support",garage,new Vector3(.28f,6.5f,.28f),new Vector3(x,3.25f,z),metal,true);
        for(int i=0;i<7;i++) {
            float x=-86+i*9;Line(new Vector3(x,.14f,-202),new Vector3(x,.14f,-187),.12f,white);
        }
        for(int i=0;i<60;i++) {
            float a=i*Mathf.PI*2/60;float r=248+Next()*25;Palm(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),5+Next()*4);
        }
        var cliff=Resources.Load<GameObject>("Scenery/coastal-cliff");
        if(!cliff)throw new System.InvalidOperationException("Coastal cliff resource missing");
        for(int i=0;i<8;i++) {
            var rock=Instantiate(cliff,transform,false);rock.name="Photogrammetry coastline";
            rock.transform.localPosition=new Vector3(-300-Next()*14,-3,-300+i*85);
            rock.transform.localRotation=Quaternion.Euler(0,85+(Next()-.5f)*18,0);
            rock.transform.localScale=Vector3.one*(.94f+Next()*.14f);
        }
        var ocean=GameObject.CreatePrimitive(PrimitiveType.Plane);ocean.name="Coastal water";ocean.transform.SetParent(transform,false);ocean.transform.localPosition=new Vector3(0,-.38f,0);ocean.transform.localScale=new Vector3(220,1,220);ocean.GetComponent<Renderer>().sharedMaterial=RuntimeResources.Own(new Material(Shader.Find("MotorCoast/CoastWater")));ocean.GetComponent<Collider>().enabled=false;Destroy(ocean.GetComponent<Collider>());
        SetupLight();
        // Unity static batching is invoked once. Dynamic vehicle/character are outside this root.
        foreach(var r in GetComponentsInChildren<MeshRenderer>())r.gameObject.isStatic=true;
        StaticBatchingUtility.Combine(gameObject);
    }
    float Next()=>(float)random.NextDouble();
    void Line(Vector3 a,Vector3 b,float width,Material m) {
        var midpoint=(a+b)*.5f;var go=Geometry.Box("Road marking",transform,new Vector3(width,.008f,(b-a).magnitude),midpoint,m);go.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
    }
    void Circle(Vector3 center,float radius,float width,Material m) {
        const int segments=128;var vs=new Vector3[(segments+1)*2];var ix=new int[segments*6];
        for(int i=0;i<=segments;i++) { float a=i*2*Mathf.PI/segments;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));vs[i*2]=center+d*(radius-width/2);vs[i*2+1]=center+d*(radius+width/2);if(i<segments){int n=i*6,k=i*2;ix[n]=k;ix[n+1]=k+2;ix[n+2]=k+1;ix[n+3]=k+1;ix[n+4]=k+2;ix[n+5]=k+3;} }
        var mesh=new Mesh{name="Painted drift circle"};mesh.vertices=vs;mesh.triangles=ix;mesh.RecalculateNormals();Geometry.MeshObject(mesh.name,transform,mesh,m);
    }
    void Cone(Vector3 p) {
        Geometry.Box("Cone base",transform,new Vector3(.7f,.08f,.7f),p+Vector3.up*.04f,metal);
        const int n=20;var vs=new List<Vector3>();var ix=new List<int>();
        for(int i=0;i<n;i++){float a=i*2*Mathf.PI/n,b=(i+1)*2*Mathf.PI/n;int k=vs.Count;vs.Add(p+new Vector3(Mathf.Cos(a)*.24f,.08f,Mathf.Sin(a)*.24f));vs.Add(p+Vector3.up*.85f);vs.Add(p+new Vector3(Mathf.Cos(b)*.24f,.08f,Mathf.Sin(b)*.24f));ix.AddRange(new[]{k,k+1,k+2});}
        var mesh=new Mesh{name="Slalom cone"};mesh.SetVertices(vs);mesh.SetTriangles(ix,0);mesh.RecalculateNormals();Geometry.MeshObject(mesh.name,transform,mesh,orange);
    }
    void Ramp(Vector3 position,float width,float length,float height,float yaw) {
        var parent=new GameObject("Ramp").transform;parent.SetParent(transform,false);parent.localPosition=position;parent.localRotation=Quaternion.Euler(0,yaw,0);
        var v=new[]{new Vector3(-width/2,0,-length/2),new Vector3(width/2,0,-length/2),new Vector3(-width/2,height,length/2),new Vector3(width/2,height,length/2),new Vector3(-width/2,0,length/2),new Vector3(width/2,0,length/2)};
        var mesh=new Mesh{name="Shared ramp surface and collider"};mesh.vertices=v;mesh.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5,0,4,2,1,3,5,0,1,4,1,5,4};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1),new Vector2(0,1),new Vector2(1,1)};mesh.RecalculateNormals();
        // Separate face normals prevent a wedge from shading like a rounded lump;
        // physical metre UVs keep the same concrete grain on every ramp size.
        var flat=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        foreach(int index in mesh.triangles){var p=v[index];flat.Add(p);uv.Add(new Vector2(p.x/3,(p.z+p.y)/3));indices.Add(indices.Count);}
        mesh.Clear();mesh.SetVertices(flat);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
        var o=Geometry.MeshObject("Concrete ramp",parent,mesh,stone);o.AddComponent<MeshCollider>().sharedMesh=mesh;
        foreach(float x in new[]{-width/2+.3f,width/2-.3f}) { var stripe=Geometry.Box("Ramp edge",parent,new Vector3(.14f,.012f,Mathf.Sqrt(length*length+height*height)),new Vector3(x,height*.5f+.01f,0),orange);stripe.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(height,length)*Mathf.Rad2Deg,0,0); }
    }
    void Palm(Vector3 p,float height) {
        var root=new GameObject("Palm").transform;root.SetParent(transform,false);root.localPosition=p;
        var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.transform.SetParent(root,false);trunk.transform.localPosition=Vector3.up*height*.5f;trunk.transform.localScale=new Vector3(.42f,height*.5f,.42f);trunk.GetComponent<Renderer>().sharedMaterial=stone;Destroy(trunk.GetComponent<Collider>());
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int l=0;l<9;l++){float a=l*2*Mathf.PI/9;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,d)*.28f;for(int s=0;s<5;s++){float t=s/4f;var point=Vector3.up*height+d*t*3+Vector3.up*Mathf.Sin(t*Mathf.PI)*.8f-Vector3.up*t*t*1.2f;vertices.Add(point-side*(1-t));vertices.Add(point+side*(1-t));if(s<4){int k=l*10+s*2;triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3,k+1,k+2,k,k+3,k+2,k+1});}}}
        var mesh=new Mesh{name="Palm fronds"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();Geometry.MeshObject(mesh.name,root,mesh,leaf);
    }
    void SetupLight() {
        var sun=new GameObject("Afternoon sun",typeof(Light));sun.transform.SetParent(transform,false);sun.transform.rotation=Quaternion.Euler(WorldCatalog.Maps[MapIndex].Sun,-35,0);var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.91f,.79f);light.intensity=2.1f;light.shadows=LightShadows.Soft;light.shadowBias=.08f;light.shadowNormalBias=.22f;light.shadowStrength=.85f;RenderSettings.sun=light;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.28f,.36f,.43f);RenderSettings.ambientEquatorColor=new Color(.19f,.21f,.22f);RenderSettings.ambientGroundColor=new Color(.09f,.08f,.06f);
        var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.24f,.28f,.32f));ambient.AddDirectionalLight(Vector3.up,new Color(.45f,.58f,.72f),.45f);RenderSettings.ambientProbe=ambient;
        var sky=RuntimeResources.Own(new Material(Shader.Find("Skybox/Panoramic")));sky.SetTexture("_MainTex",Resources.Load<Texture2D>("Textures/coast-sky"));sky.SetFloat("_Exposure",.8f);sky.SetFloat("_Rotation",75);RenderSettings.skybox=sky;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=MapIndex==0?.0007f:.0012f;RenderSettings.fogColor=new Color(.64f,.75f,.80f);
        var probe=new GameObject("Static sky reflection",typeof(ReflectionProbe));probe.transform.SetParent(transform,false);probe.transform.position=new Vector3(0,3,-80);var reflection=probe.GetComponent<ReflectionProbe>();reflection.mode=ReflectionProbeMode.Realtime;reflection.refreshMode=ReflectionProbeRefreshMode.ViaScripting;reflection.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;reflection.resolution=256;reflection.size=Vector3.one*2000;reflection.cullingMask=~(1<<VehicleController.VehicleLayer);reflection.clearFlags=ReflectionProbeClearFlags.Skybox;reflection.intensity=.8f;reflection.RenderProbe();
    }
}
}
