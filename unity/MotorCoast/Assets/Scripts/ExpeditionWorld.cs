using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MotorCoast {
// Original layouts using CC0 photogrammetry surfaces. Only the selected map is
// resident. Terrain is divided into bounded meshes for frustum/shadow culling.
public sealed class ExpeditionWorld {
    readonly Transform root;readonly WorldSpec spec;readonly System.Random random;
    readonly List<List<Vector3>> roadPaths=new List<List<Vector3>>();
    readonly Dictionary<Vector2Int,List<(Vector3 a,Vector3 b)>> roadGrid=new Dictionary<Vector2Int,List<(Vector3,Vector3)>>();
    Material terrain,road,concrete,marking,glass,steel;readonly List<Material> facades=new List<Material>();
    public ExpeditionWorld(Transform parent,WorldSpec map){root=parent;spec=map;random=new System.Random(map.Seed);}
    public static Material Surface(string name,Color tint,float tile=1){
        var template=Resources.Load<Material>("Render/RuntimeAsphalt");var m=template?RuntimeResources.Own(new Material(template)):Geometry.Lit(name,tint);
        m.name="Scanned "+name;m.enableInstancing=true;m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",1);
        m.SetTexture("_BaseMap",Resources.Load<Texture2D>("Textures/"+name+"-color"));m.SetTexture("_BumpMap",Resources.Load<Texture2D>("Textures/"+name+"-normal"));m.SetFloat("_BumpScale",.65f);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",Resources.Load<Texture2D>("Textures/"+name+"-metal-smooth"));m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetTexture("_OcclusionMap",Resources.Load<Texture2D>("Textures/"+name+"-ao"));m.SetFloat("_OcclusionStrength",.65f);m.EnableKeyword("_OCCLUSIONMAP");m.SetTextureScale("_BaseMap",Vector2.one*tile);return m;
    }
    float Next()=>(float)random.NextDouble();
    float RawHeight(float x,float z){
        float distance=new Vector2(x,z).magnitude,edge=Mathf.SmoothStep(0,1,(distance-145)/185);
        float n=Mathf.PerlinNoise(x*.0027f+spec.Seed*.017f,z*.0027f+17)*.72f+Mathf.PerlinNoise(x*.006f+8,z*.006f+spec.Seed*.02f)*.28f;
        float ridge=1-Mathf.Abs(Mathf.PerlinNoise(x*.0035f+spec.Seed*.02f,z*.0035f+12)*2-1);
        float distant=1+Mathf.SmoothStep(0,1,(distance-350)/650)*1.2f;
        return edge*spec.Relief*Mathf.Pow(n*.70f+ridge*.30f,1.8f)*distant;
    }
    public float Height(float x,float z){
        float raw=RawHeight(x,z);if(spec.Relief==0||!roadGrid.TryGetValue(new Vector2Int(Mathf.FloorToInt(x/32),Mathf.FloorToInt(z/32)),out var segments))return raw;
        float closest=400,y=raw;
        foreach(var segment in segments){
            var a=segment.a;var b=segment.b;float dx=b.x-a.x,dz=b.z-a.z;
            float t=Mathf.Clamp01(((x-a.x)*dx+(z-a.z)*dz)/(dx*dx+dz*dz+.00001f));
            float d=(x-a.x-t*dx)*(x-a.x-t*dx)+(z-a.z-t*dz)*(z-a.z-t*dz);
            if(d<closest){closest=d;y=Mathf.Lerp(a.y,b.y,t);}
        }
        return Mathf.Lerp(y,raw,Mathf.SmoothStep(0,1,(Mathf.Sqrt(closest)-10)/10));
    }
    public void Build(){
        var tint=spec.Name=="Красный каньон"?new Color(.9f,.62f,.42f):spec.Name=="Вулканический берег"?new Color(.38f,.40f,.42f):Color.white;
        terrain=TerrainMaterial(tint);road=Surface("asphalt",Color.white);concrete=Surface("concrete",new Color(.85f,.86f,.83f));
        marking=Geometry.Lit("Ivory lane markings",new Color(.84f,.83f,.73f),0,.24f);steel=Geometry.Lit("Steel railing",new Color(.25f,.29f,.31f),.7f,.46f);glass=Geometry.Lit("Facade glazing",new Color(.19f,.28f,.32f),.65f,.9f);
        facades.Add(Surface("brick",new Color(.86f,.83f,.78f),1));facades.Add(Surface("concrete",new Color(.92f,.94f,.95f),1));facades.Add(Surface("brick",new Color(.60f,.63f,.68f),1));
        if(spec.Layout!=3&&spec.Layout!=6)CreateRoadPaths();
        for(int z=0;z<8;z++)for(int x=0;x<8;x++)TerrainChunk(-1200+x*300,-1200+z*300);
        // A clear, level starting apron is shared as a safe spawn, not an invisible floor.
        Rectangle("Arrival plaza",new Vector3(0,.012f,-40),110,150,road);
        if(spec.Layout==3)City();else if(spec.Layout==1)Harbor();else if(spec.Layout==6)SpeedPark();else ScenicRoad();
        Parkour();Rocks();if(spec.Layout==4||spec.Layout==2)Trees();BatchDetails();
    }
    Material TerrainMaterial(Color tint){
        var material=RuntimeResources.Own(new Material(Resources.Load<Material>("Render/RuntimeTerrain")));
        Color basis=spec.Surface=="snow"?new Color(.86f,.89f,.93f):spec.Surface=="sand"?new Color(.72f,.61f,.43f):spec.Surface=="rock"?new Color(.55f,.54f,.50f):spec.Surface=="concrete"?new Color(.58f,.59f,.57f):new Color(.42f,.49f,.31f);
        var macro=RuntimeResources.Own(new Texture2D(256,256,TextureFormat.RGBA32,true,false));macro.wrapMode=TextureWrapMode.Clamp;macro.name="World colour variation";
        var pixels=new Color[256*256];for(int z=0;z<256;z++)for(int x=0;x<256;x++){
            float n=Mathf.PerlinNoise(x*.037f+spec.Seed*.01f,z*.037f)*.7f+Mathf.PerlinNoise(x*.10f,z*.10f+17)*.3f;
            pixels[z*256+x]=basis*Mathf.Lerp(.78f,1.18f,n);pixels[z*256+x].a=1;
        }
        macro.SetPixels(pixels);macro.Apply(true,true);material.SetTexture("_BaseMap",macro);material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",.12f);material.SetFloat("_Metallic",0);
        material.SetTexture("_DetailAlbedoMap",Resources.Load<Texture2D>("Textures/"+spec.Surface+"-color"));material.SetFloat("_DetailAlbedoMapScale",.45f);
        material.SetTexture("_DetailNormalMap",Resources.Load<Texture2D>("Textures/"+spec.Surface+"-normal"));material.SetFloat("_DetailNormalMapScale",.75f);material.SetTextureScale("_DetailAlbedoMap",Vector2.one*600);material.EnableKeyword("_DETAIL_SCALED");return material;
    }
    void TerrainChunk(float ox,float oz){
        int n=Mathf.Abs(ox+150)<650&&Mathf.Abs(oz+150)<650?64:32;var v=new Vector3[(n+1)*(n+1)];var uv=new Vector2[v.Length];var indices=new int[n*n*6];
        for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){int i=z*(n+1)+x;float wx=ox+x*300/n,wz=oz+z*300/n;v[i]=new Vector3(wx,Height(wx,wz),wz);uv[i]=new Vector2((wx+1200)/2400,(wz+1200)/2400);}
        int k=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;indices[k++]=a;indices[k++]=a+n+1;indices[k++]=a+1;indices[k++]=a+1;indices[k++]=a+n+1;indices[k++]=a+n+2;}
        var mesh=new Mesh{name="Terrain tile"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateTangents();
        var go=Geometry.MeshObject("Terrain "+ox+", "+oz,root,mesh,terrain);go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    void Rectangle(string name,Vector3 p,float width,float depth,Material material){
        var mesh=new Mesh{name=name};mesh.vertices=new[]{new Vector3(-width/2,0,-depth/2),new Vector3(-width/2,0,depth/2),new Vector3(width/2,0,-depth/2),new Vector3(width/2,0,depth/2)};
        mesh.uv=new[]{Vector2.zero,new Vector2(0,depth/3),new Vector2(width/3,0),new Vector2(width/3,depth/3)};mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateNormals();mesh.RecalculateTangents();var go=Geometry.MeshObject(name,root,mesh,material);go.transform.localPosition=p;
    }
    void CreateRoadPaths(){
        var ring=new List<Vector3>();float wobble=spec.Layout==5?12:spec.Name=="Красный каньон"?85:45;
        for(int i=0;i<=560;i++){float a=i*2*Mathf.PI/560,r=390+Mathf.Sin(a*3+spec.Seed)*wobble;float x=Mathf.Sin(a)*r,z=Mathf.Cos(a)*r*.9f;ring.Add(new Vector3(x,RawHeight(x,z),z));}roadPaths.Add(ring);
        var connector=new List<Vector3>();for(int i=0;i<=140;i++){float z=Mathf.Lerp(-80,ring[0].z,i/140f);connector.Add(new Vector3(0,RawHeight(0,z),z));}roadPaths.Add(connector);
        if(spec.Layout==2){var alternative=new List<Vector3>();for(int i=0;i<=220;i++){float t=i/220f,z=Mathf.Lerp(ring[280].z,ring[0].z,t),x=145*Mathf.Sin(t*Mathf.PI*2+spec.Seed)*Mathf.Sin(t*Mathf.PI);alternative.Add(new Vector3(x,RawHeight(x,z),z));}roadPaths.Add(alternative);}
        foreach(var path in roadPaths)for(int i=1;i<path.Count;i++){
            var a=path[i-1];var b=path[i];
            for(int x=Mathf.FloorToInt((Mathf.Min(a.x,b.x)-20)/32);x<=Mathf.FloorToInt((Mathf.Max(a.x,b.x)+20)/32);x++)for(int z=Mathf.FloorToInt((Mathf.Min(a.z,b.z)-20)/32);z<=Mathf.FloorToInt((Mathf.Max(a.z,b.z)+20)/32);z++){
                var key=new Vector2Int(x,z);if(!roadGrid.TryGetValue(key,out var list))roadGrid[key]=list=new List<(Vector3,Vector3)>();list.Add((a,b));
            }
        }
    }
    void ScenicRoad(){foreach(var path in roadPaths)Ribbon(path,13,true);}
    void Ribbon(List<Vector3> points,float width,bool lines){
        var vertices=new Vector3[points.Count*2];var uv=new Vector2[vertices.Length];var triangles=new int[(points.Count-1)*6];float length=0;
        for(int i=0;i<points.Count;i++){
            var direction=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(Vector3.up,direction).normalized*width/2;
            vertices[i*2]=points[i]-side;vertices[i*2+1]=points[i]+side;
            // Match each road edge to the sampled terrain: no invisible steps.
            vertices[i*2].y=Height(vertices[i*2].x,vertices[i*2].z)+.03f;vertices[i*2+1].y=Height(vertices[i*2+1].x,vertices[i*2+1].z)+.03f;
            if(i>0)length+=Vector3.Distance(points[i-1],points[i]);uv[i*2]=new Vector2(0,length/3);uv[i*2+1]=new Vector2(width/3,length/3);
            if(i<points.Count-1){int k=i*6,a=i*2;triangles[k]=a;triangles[k+1]=a+2;triangles[k+2]=a+1;triangles[k+3]=a+1;triangles[k+4]=a+2;triangles[k+5]=a+3;}
            if(lines&&i%6==0&&i<points.Count-1){var p=points[i];var line=Geometry.Box("Centre dash",root,new Vector3(.12f,.008f,2.5f),p+Vector3.up*.03f,marking);line.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);}
        }
        var mesh=new Mesh{name="Continuous road surface"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();Geometry.MeshObject(mesh.name,root,mesh,road).AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    void City(){
        for(int i=-3;i<=3;i++){
            Rectangle("Avenue",new Vector3(i*135,.016f,0),22,950,road);Rectangle("Cross street",new Vector3(0,.018f,i*135),950,22,road);
            for(int j=-24;j<24;j++){Geometry.Box("Lane dash",root,new Vector3(.12f,.01f,4),new Vector3(i*135,.03f,j*19),marking);Geometry.Box("Cross lane dash",root,new Vector3(4,.01f,.12f),new Vector3(j*19,.031f,i*135),marking);}
        }
        for(int x=-3;x<3;x++)for(int z=-3;z<3;z++){
            var centre=new Vector3(x*135+67.5f,0,z*135+67.5f);if(centre.magnitude<110)continue;
            Geometry.Box("City pavement",root,new Vector3(106,.10f,106),centre+Vector3.up*.05f,concrete,true);
            foreach(float dx in new[]{-26f,26f})foreach(float dz in new[]{-25f,25f})Building(centre+new Vector3(dx,0,dz),24+Next()*12,14+Next()*49,28+Next()*12);
        }
    }
    void Building(Vector3 p,float w,float h,float d){
        var block=new GameObject("City building").transform;block.SetParent(root,false);block.localPosition=p;
        int style=random.Next(3);var wall=facades[style];
        Geometry.Box("Masonry facade",block,new Vector3(w,h,d),new Vector3(0,h/2,0),wall,true);
        Geometry.Box("Roof coping",block,new Vector3(w+.5f,.28f,d+.5f),new Vector3(0,h,0),concrete);
        Geometry.Box("Ground floor stone",block,new Vector3(w+.18f,1,d+.18f),new Vector3(0,.5f,0),facades[1]);
        int floors=Mathf.FloorToInt(h/3.6f);
        for(int f=0;f<floors;f++){
            float y=2.4f+f*3.6f;
            foreach(float side in new[]{-1f,1f}){
                if(style==0){
                    Geometry.Box("Office glazing",block,new Vector3(w-.8f,2.6f,.04f),new Vector3(0,y,side*(d/2+.03f)),glass);
                    Geometry.Box("Office glazing",block,new Vector3(.04f,2.6f,d-.8f),new Vector3(side*(w/2+.03f),y,0),glass);
                }else{
                    for(float x=-w/2+2.5f;x<w/2-1.5f;x+=4.2f){Geometry.Box("Window recess",block,new Vector3(2.15f,2.1f,.10f),new Vector3(x,y,side*(d/2+.06f)),steel);Geometry.Box("Window glazing",block,new Vector3(1.92f,1.87f,.03f),new Vector3(x,y,side*(d/2+.12f)),glass);Geometry.Box("Stone sill",block,new Vector3(2.3f,.13f,.28f),new Vector3(x,y-1.06f,side*(d/2+.12f)),concrete);}
                    for(float z=-d/2+2.5f;z<d/2-1.5f;z+=4.2f){Geometry.Box("Window recess",block,new Vector3(.10f,2.1f,2.15f),new Vector3(side*(w/2+.06f),y,z),steel);Geometry.Box("Window glazing",block,new Vector3(.03f,1.87f,1.92f),new Vector3(side*(w/2+.12f),y,z),glass);}
                }
            }
        }
        if(style==0)for(float x=-w/2+2;x<w/2;x+=2.6f)foreach(float side in new[]{-1f,1f})Geometry.Box("Glazing mullion",block,new Vector3(.10f,h,.10f),new Vector3(x,h/2,side*(d/2+.09f)),steel);
        Geometry.Box("Street entrance",block,new Vector3(3,2.7f,.08f),new Vector3(0,1.35f,d/2+.16f),glass);
        Geometry.Box("Entrance canopy",block,new Vector3(4.5f,.18f,2.2f),new Vector3(0,3,d/2+.6f),steel);
        for(int i=0;i<3;i++)Geometry.Box("Roof ventilation",block,new Vector3(2.7f,.9f,1.9f),new Vector3(-w*.25f+i*4.2f,h+.6f,-d*.22f),steel);
        Geometry.CombineVisuals(block);
    }
    void BatchDetails(){
        var batch=new GameObject("Batched road details").transform;batch.SetParent(root,false);
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
            var collider=renderer.GetComponent<Collider>();if(collider&&collider.enabled)continue;
            if(renderer.sharedMaterial==marking||renderer.gameObject.name=="Container rib")renderer.transform.SetParent(batch,true);
        }
        Geometry.Combine(batch);
    }
    void Harbor(){
        Rectangle("Harbor apron",Vector3.up*.012f,980,760,road);
        for(int i=0;i<16;i++){var p=new Vector3((i%4-1.5f)*140,0,170+(i/4)*65);Building(p,75,13+Next()*8,42);}
        var colors=new[]{new Color(.25f,.38f,.42f),new Color(.56f,.22f,.13f),new Color(.68f,.59f,.36f)};
        var paints=new Material[colors.Length];for(int i=0;i<paints.Length;i++)paints[i]=Geometry.Lit("Painted cargo container",colors[i],.35f,.35f);
        for(int i=0;i<48;i++){
            var p=new Vector3(-380+(i%8)*35,1.3f,-190-(i/8)*32);var m=paints[i%3];
            Geometry.Box("Freight container",root,new Vector3(12,2.6f,2.5f),p,m,true);
            for(int n=0;n<20;n++)Geometry.Box("Container rib",root,new Vector3(.06f,2.45f,.045f),p+new Vector3(-5.7f+n*.6f,0,-1.27f),steel);
        }
        ScenicRoad();
    }
    void SpeedPark(){
        Rectangle("Speed runway",Vector3.up*.014f,42,1100,road);
        Rectangle("Stunt apron",new Vector3(-170,.016f,180),260,300,road);
        for(int i=-28;i<=28;i++)Geometry.Box("Runway centreline",root,new Vector3(.22f,.012f,6),new Vector3(0,.035f,i*18),marking);
        for(int i=0;i<8;i++)Building(new Vector3(190,0,-420+i*115),65,9,45);
    }
    void Parkour(){
        var yard=new Vector3(42,0,-55);
        for(int i=0;i<6;i++)Geometry.Box("Climbing block",root,new Vector3(2.3f,.4f+i*.18f,3),yard+new Vector3(0,(.4f+i*.18f)/2,i*4.8f),concrete,true);
        for(int i=0;i<8;i++)Geometry.Box("Stair tread",root,new Vector3(4,.18f,1.0f),new Vector3(48,.09f+i*.18f,-70+i*.85f),concrete,true);
        Geometry.Box("Crawl training arch",root,new Vector3(3.5f,.25f,4),new Vector3(39,.83f,-76),steel,true);
        foreach(float x in new[]{37.4f,40.6f})Geometry.Box("Arch support",root,new Vector3(.18f,.8f,4),new Vector3(x,.4f,-76),steel,true);
    }
    void Rocks(){
        var prefab=Resources.Load<GameObject>("Scenery/coastal-cliff");if(!prefab)return;
        for(int i=0;i<14;i++){float a=i*2*Mathf.PI/14,r=525+Next()*40;var p=new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);p.y=Height(p.x,p.z)-3;var go=Object.Instantiate(prefab,root,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);go.transform.localScale=Vector3.one*(.4f+Next()*.5f);}
    }
    void Trees(){
        var large=Resources.Load<GameObject>("Scenery/pine-large_1-lod0");var largeLow=Resources.Load<GameObject>("Scenery/pine-large_1-lod2");
        var medium=Resources.Load<GameObject>("Scenery/pine-medium_1-lod0");var mediumLow=Resources.Load<GameObject>("Scenery/pine-medium_1-lod2");if(!large||!medium)return;
        int count=spec.Layout==4?900:200;
        for(int i=0;i<count;i++){float a=Next()*Mathf.PI*2,r=150+Next()*650;var p=new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);if(Mathf.Abs(p.x)<18||NearRoad(p,14))continue;p.y=Height(p.x,p.z);
            var tree=new GameObject("Pine LOD group");tree.transform.SetParent(root,false);tree.transform.localPosition=p;tree.transform.localRotation=Quaternion.Euler(0,Next()*360,0);tree.transform.localScale=Vector3.one*(.7f+Next()*.5f);
            bool tall=i%3==0;var high=Object.Instantiate(tall?large:medium,tree.transform,false);var low=Object.Instantiate(tall?largeLow:mediumLow,tree.transform,false);
            foreach(var renderer in tree.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)material.enableInstancing=true;
            var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=tall?.42f:.28f;trunk.height=tall?11:6;trunk.center=Vector3.up*trunk.height/2;
            var lod=tree.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.11f,high.GetComponentsInChildren<Renderer>()),new LOD(.018f,low.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
        }
    }
    bool NearRoad(Vector3 p,float radius){
        if(!roadGrid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.z/32)),out var segments))return false;
        foreach(var segment in segments){var a=segment.a;var b=segment.b;float dx=b.x-a.x,dz=b.z-a.z,t=Mathf.Clamp01(((p.x-a.x)*dx+(p.z-a.z)*dz)/(dx*dx+dz*dz+.00001f));if((p.x-a.x-t*dx)*(p.x-a.x-t*dx)+(p.z-a.z-t*dz)*(p.z-a.z-t*dz)<radius*radius)return true;}return false;
    }
}
}
