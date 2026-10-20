using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M1WorldBuilderV064
{
    const string Hub = "Assets/Scenes/v0.6.4/M1_HubWorld.unity";
    const string Forest = "Assets/Scenes/v0.6.4/M1_ForestWorld.unity";
    const string Canyon = "Assets/Scenes/v0.6.4/M1_CanyonWorld.unity";
    const string BeastAsset = "Assets/Art/Models/Blender_Ready/LanJiao_SpiritBeast.fbx";
    const string FoxAsset = "Assets/Art/Models/Blender_Custom/RedTailFox_v064/RedTail_SpiritFox_v064.fbx";
    const string DemonAsset = "Assets/Art/Models/External_Quaternius/BlueDemon.fbx";
    const string Nature = "Assets/Art/Models/External_KenneyNature/";
    static Material pathMat, waterMat, terrainMat;
    static int currentKind;
    static Material ground, stone, green, darkGreen, gateMat, playerMat, enemyMat, accent, forestGround, sand, skin, cloth, boot, crystal;

    [MenuItem("Shanhai/Build v0.6.4 星金营地 · 扩展自然世界")]
    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Scenes/v0.6.4");
        foreach (var p in new[]{Hub, Forest, Canyon})
            if (File.Exists(p)) throw new System.Exception("Refusing to overwrite existing scene: " + p);
        AssetDatabase.Refresh();
        ground = Mat(new Color(.12f,.22f,.19f)); forestGround = Mat(new Color(.08f,.19f,.18f)); sand=Mat(new Color(.31f,.23f,.18f)); stone = Mat(new Color(.21f,.25f,.26f));
        green = Mat(new Color(.10f,.34f,.38f)); darkGreen = Mat(new Color(.05f,.15f,.25f));
        gateMat = Mat(new Color(.15f,.52f,.63f)); playerMat=Mat(new Color(.85f,.67f,.28f));
        enemyMat=Mat(new Color(.58f,.19f,.16f)); accent=Mat(new Color(.86f,.57f,.20f)); skin=Mat(new Color(.82f,.55f,.39f)); cloth=Mat(new Color(.08f,.24f,.28f)); boot=Mat(new Color(.15f,.12f,.12f)); crystal=Mat(new Color(.20f,.68f,.72f)); playerMat=Mat(new Color(.82f,.61f,.29f));
        MakeWorld(Hub,"星金营地",new Color(.13f,.19f,.30f), new[]{new PortalSpec(-7,45,Forest,"青岚灵林"),new PortalSpec(7,45,Canyon,"玄岩谷地")}, 0);
        MakeWorld(Forest,"青岚灵林",new Color(.08f,.22f,.25f), new[]{new PortalSpec(0,45,Hub,"星金营地"),new PortalSpec(7,45,Canyon,"玄岩谷地")}, 1);
        MakeWorld(Canyon,"玄岩谷地",new Color(.18f,.17f,.27f), new[]{new PortalSpec(0,45,Hub,"星金营地"),new PortalSpec(-7,45,Forest,"青岚灵林")}, 2);
        EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene(Hub,true),new EditorBuildSettingsScene(Forest,true),new EditorBuildSettingsScene(Canyon,true)};
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("M1 v0.6.4 expanded nature worlds built.");
    }
    struct PortalSpec { public float x,z; public string scene,name; public PortalSpec(float x,float z,string scene,string name){this.x=x;this.z=z;this.scene=scene;this.name=name;} }
    static void MakeWorld(string path,string worldName,Color sky,PortalSpec[] portals,int kind)
    {
        currentKind=kind;
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=sky*1.35f; RenderSettings.ambientEquatorColor=kind==2?new Color(.37f,.28f,.25f):new Color(.20f,.34f,.32f); RenderSettings.ambientGroundColor=new Color(.09f,.11f,.12f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogColor=sky; RenderSettings.fogStartDistance=70f; RenderSettings.fogEndDistance=165f;
        RenderSettings.skybox=AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        var manager=new GameObject("M1 Game Manager").AddComponent<M1GameManager>(); manager.gameObject.AddComponent<M1PetSystem>();
        terrainMat=TerrainMaterial(kind); pathMat=Mat(kind==2?new Color(.34f,.26f,.24f):new Color(.37f,.29f,.20f)); pathMat.SetFloat("_Glossiness",.08f);
        waterMat=Mat(new Color(.10f,.49f,.53f)); waterMat.SetFloat("_Glossiness",.82f); waterMat.SetFloat("_Metallic",.18f); waterMat.EnableKeyword("_EMISSION"); waterMat.SetColor("_EmissionColor",new Color(.04f,.23f,.25f));
        BuildTerrain(worldName,kind); BuildTrail(kind); if(kind==1) BuildStream(); ScatterNature(kind);
        if(kind==1) ForestDecor(); else if(kind==2) CanyonDecor(); else HubDecor();
        var player=MakePlayer(); player.transform.position=new Vector3(0,0,0);
        var enemy=MakeEnemy(new Vector3(-5,0,7));
        var beast=MakeBeast(new Vector3(5,0,6),BeastAsset,"岚角灵兽","blue_demon",.48f);
        var fox=MakeBeast(new Vector3(-8,0,12),FoxAsset,"赤尾灵狐","wind_fox",.60f);
        var demon=MakeBeast(new Vector3(10,0,18),DemonAsset,"幽蓝翼兽","spirit_beast",.52f);
        manager.player=player; manager.enemy=enemy; manager.beast=beast;
        foreach(var spec in portals) MakePortal(spec);
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; var cam=camObj.AddComponent<Camera>(); cam.fieldOfView=60f; cam.farClipPlane=175f; camObj.AddComponent<AudioListener>();
        var follow=camObj.AddComponent<M1CameraFollow>(); follow.target=player.transform; follow.distance=7.2f; follow.minDistance=3.2f; follow.maxDistance=11.5f; player.GetComponent<M1PlayerController>().cameraTransform=camObj.transform;
        var lightObj=new GameObject("Warm Moon Directional Light");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.color=kind==2?new Color(1f,.78f,.62f):new Color(1f,.88f,.72f);light.intensity=.92f;light.shadows=LightShadows.Soft;lightObj.transform.rotation=Quaternion.Euler(42,-32,0);
        AddFireflies(new Vector3(0,1.2f,26),kind==2?18:30);
        EditorSceneManager.SaveScene(scene,path);
    }
    static Material TerrainMaterial(int kind)
    {
        var sh=Shader.Find("Shanhai/VertexColorLit"); if(!sh) sh=Shader.Find("Standard"); string dir="Assets/Art/Materials/v0.6.4";Directory.CreateDirectory(dir);string path=dir+"/M1_Terrain_"+kind+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        if(m.HasProperty("_Color"))m.color=kind==2?new Color(.93f,.86f,.80f):new Color(.88f,.96f,.88f); return m;
    }    static float PathCenter(float z,int kind){return Mathf.Sin(z*.045f+kind*1.1f)*(kind==2?2.2f:1.25f);}
    static float TerrainHeight(float x,float z,int kind)
    {
        if(z>-18f && Mathf.Abs(x-PathCenter(z,kind))<7.4f)return 0f;
        if(x*x+(z-16f)*(z-16f)<18f*18f)return 0f;
        float edge=Mathf.Max(Mathf.Abs(x),Mathf.Abs(z)); float t=Mathf.InverseLerp(31f,58f,edge); t=t*t*(3f-2f*t);
        float n=Mathf.PerlinNoise((x+157f+kind*19f)*.045f,(z+283f-kind*11f)*.045f);
        float waves=(Mathf.Sin(x*.115f+kind)*.65f+Mathf.Cos(z*.098f-kind)*.45f);
        return t*Mathf.Clamp(1.2f+n*3.6f+waves,0f,5f);
    }
    static Color TerrainColor(float x,float z,float y,int kind)
    {
        float n=Mathf.PerlinNoise((x+53f)*.07f,(z+77f)*.07f);
        Color low=kind==2?new Color(.40f,.31f,.27f):kind==1?new Color(.12f,.31f,.24f):new Color(.22f,.36f,.25f);
        Color high=kind==2?new Color(.55f,.43f,.36f):new Color(.31f,.41f,.31f);
        Color top=kind==2?new Color(.57f,.53f,.48f):new Color(.37f,.47f,.39f);
        var c=Color.Lerp(low,high,n*.54f); return Color.Lerp(c,top,Mathf.InverseLerp(1.5f,4.8f,y)*.84f);
    }
    static void BuildTerrain(string worldName,int kind)
    {
        const int cells=80; const float size=120f; int side=cells+1; var mesh=new Mesh(); mesh.name=worldName+"_120m地形网格";
        var v=new Vector3[side*side]; var colors=new Color[v.Length]; var uv=new Vector2[v.Length]; var tri=new int[cells*cells*6];
        for(int ix=0;ix<=cells;ix++)for(int iz=0;iz<=cells;iz++){int i=ix*side+iz;float x=-size*.5f+size*ix/cells,z=-size*.5f+size*iz/cells;float y=TerrainHeight(x,z,kind);v[i]=new Vector3(x,y,z);colors[i]=TerrainColor(x,z,y,kind);uv[i]=new Vector2(ix/(float)cells,iz/(float)cells);}
        int t=0;for(int ix=0;ix<cells;ix++)for(int iz=0;iz<cells;iz++){int a=ix*side+iz,b=(ix+1)*side+iz,c=a+1,d=b+1;tri[t++]=a;tri[t++]=c;tri[t++]=b;tri[t++]=b;tri[t++]=c;tri[t++]=d;}
        mesh.vertices=v;mesh.colors=colors;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();
        var root=new GameObject(worldName+" · 120米起伏地形");var mf=root.AddComponent<MeshFilter>();mf.sharedMesh=mesh;var mr=root.AddComponent<MeshRenderer>();mr.sharedMaterial=terrainMat;root.AddComponent<MeshCollider>().sharedMesh=mesh;root.isStatic=true;
    }
    static void BuildTrail(int kind)
    {
        const int steps=92; var verts=new Vector3[(steps+1)*2];var uv=new Vector2[verts.Length];var tri=new int[steps*6];
        for(int i=0;i<=steps;i++){float z=-17f+66f*i/steps;float x=PathCenter(z,kind);float y=TerrainHeight(x,z,kind)+.055f;float half=kind==2?2.25f:2.05f;verts[i*2]=new Vector3(x-half,y,z);verts[i*2+1]=new Vector3(x+half,y,z);uv[i*2]=new Vector2(0,i*.55f);uv[i*2+1]=new Vector2(1,i*.55f);if(i<steps){int k=i*6,a=i*2,b=a+1,c=a+2,d=a+3;tri[k]=a;tri[k+1]=c;tri[k+2]=b;tri[k+3]=b;tri[k+4]=c;tri[k+5]=d;}}
        var m=new Mesh();m.name="自然弯曲石土小径";m.vertices=verts;m.uv=uv;m.triangles=tri;m.RecalculateNormals();var go=new GameObject("自然弯曲石土小径");go.AddComponent<MeshFilter>().sharedMesh=m;go.AddComponent<MeshRenderer>().sharedMaterial=pathMat;go.isStatic=true;
        for(int i=0;i<8;i++){float z=-10f+i*7f;float x=PathCenter(z,kind);var mark=Visual(PrimitiveType.Cylinder,"路边青玉引灯",null,new Vector3(x+(i%2==0?-3.2f:3.2f),.20f,z),new Vector3(.18f,.38f,.18f),crystal);}
    }
    static void BuildStream()
    {
        const int steps=64;var verts=new Vector3[(steps+1)*2];var uv=new Vector2[verts.Length];var tri=new int[steps*6];
        for(int i=0;i<=steps;i++){float x=-56f+112f*i/steps;float z=25f+Mathf.Sin(x*.052f)*3.8f;float y=TerrainHeight(x,z,1)+.09f;verts[i*2]=new Vector3(x,y,z-1.5f);verts[i*2+1]=new Vector3(x,y,z+1.5f);uv[i*2]=new Vector2(0,i*.8f);uv[i*2+1]=new Vector2(1,i*.8f);if(i<steps){int k=i*6,a=i*2,b=a+1,c=a+2,d=a+3;tri[k]=a;tri[k+1]=b;tri[k+2]=c;tri[k+3]=b;tri[k+4]=d;tri[k+5]=c;}}
        var mesh=new Mesh();mesh.name="青岚浅溪";mesh.vertices=verts;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();var go=new GameObject("青岚浅溪 · 细流");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=waterMat;go.isStatic=true;
        SpawnNature("bridge_stoneRound.fbx",new Vector3(0,0,25f),new Vector3(2.3f,1.2f,2.3f),90f,"青石小桥");
    }
    static string AssetPath(string name){return Nature+name;}
    static GameObject SpawnNature(string file,Vector3 pos,Vector3 scale,float yaw,string label)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(file));if(!prefab)return null;
        var go=PrefabUtility.InstantiatePrefab(prefab) as GameObject;if(!go)return null;go.name=label;go.transform.position=pos;go.transform.localScale=scale;go.transform.rotation=Quaternion.Euler(0,yaw,0);GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccludeeStatic);return go;
    }
    static void ScatterNature(int kind)
    {
        var rng=new System.Random(640+kind*17);int treeCount=kind==1?96:kind==2?45:68;int placed=0,tries=0;
        string[] trees=kind==2?new[]{"tree_pineTallA_detailed.fbx","tree_default_dark.fbx","tree_oak.fbx"}:new[]{"tree_oak.fbx","tree_pineTallA_detailed.fbx","tree_default_dark.fbx"};
        while(placed<treeCount&&tries++<treeCount*10){float x=(float)(rng.NextDouble()*112-56),z=(float)(rng.NextDouble()*112-56);if(!ClearForNature(x,z,kind))continue;float s=kind==2?2.7f+(float)rng.NextDouble()*.9f:2.8f+(float)rng.NextDouble()*1.15f;if(kind==1)s*=1.10f;var p=SpawnNature(trees[rng.Next(trees.Length)],new Vector3(x,TerrainHeight(x,z,kind),z),Vector3.one*s,(float)rng.NextDouble()*360f,"Kenney树木");if(p)placed++;}
        int rocks=kind==2?102:kind==1?52:48;string[] rockModels={"rock_largeA.fbx","rock_largeC.fbx","rock_smallB.fbx","stone_largeB.fbx"};
        for(int i=0;i<rocks;i++){float x=(float)(rng.NextDouble()*108-54),z=(float)(rng.NextDouble()*108-54);if(!ClearForNature(x,z,kind))continue;float s=(float)(rng.NextDouble()*(kind==2?1.45:1.05)+(kind==2?2.2:1.8));if(kind==2)s*=1.12f;SpawnNature(rockModels[rng.Next(rockModels.Length)],new Vector3(x,TerrainHeight(x,z,kind),z),Vector3.one*s,(float)rng.NextDouble()*360f,"Kenney岩石");}
        int foliage=kind==1?220:kind==2?94:150;string[] plants={"plant_bushDetailed.fbx","plant_bushLarge.fbx","grass_large.fbx","grass_leafsLarge.fbx","flower_yellowB.fbx","flower_purpleA.fbx","stump_oldTall.fbx"};
        for(int i=0;i<foliage;i++){float x=(float)(rng.NextDouble()*104-52),z=(float)(rng.NextDouble()*104-52);if(!ClearForNature(x,z,kind))continue;string plant=plants[rng.Next(plants.Length)];float baseScale=plant.Contains("grass")?2.15f:plant.Contains("bush")?1.55f:plant.Contains("flower")?1.0f:1.25f;float s=baseScale*((float)rng.NextDouble()*.55f+.8f);if(kind==1)s*=1.06f;SpawnNature(plant,new Vector3(x,TerrainHeight(x,z,kind),z),Vector3.one*s,(float)rng.NextDouble()*360f,"Kenney野草与花丛");}
        for(int i=0;i<14;i++){float z=4f+i*3.8f;float x=PathCenter(z,kind)+(i%2==0?-3.9f:3.9f);string f=i%2==0?"flower_yellowB.fbx":"flower_purpleA.fbx";SpawnNature(f,new Vector3(x,0,z),Vector3.one*.75f,i*41,"路径灵花");}
    }
    static bool ClearForNature(float x,float z,int kind)
    {
        if(Mathf.Abs(x)<9f&&z<8f&&z>-8f)return false;
        if(z>-18f&&Mathf.Abs(x-PathCenter(z,kind))<5f)return false;
        if((x*x+(z-16f)*(z-16f))<14f*14f)return false;
        if(z>38f&&Mathf.Abs(x)<14f)return false;
        return true;
    }
    static void AddFireflies(Vector3 center,int count)
    {
        var go=new GameObject("夜色灵萤 · 轻量粒子");go.transform.position=center;var ps=go.AddComponent<ParticleSystem>();var main=ps.main;main.loop=true;main.startLifetime=7f;main.startSpeed=.08f;main.startSize=.08f;main.startColor=new Color(.42f,.9f,1f,.72f);main.maxParticles=count;main.simulationSpace=ParticleSystemSimulationSpace.World;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=18f;var emission=ps.emission;emission.rateOverTime=count*.55f;var r=go.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Billboard;r.material=new Material(Shader.Find("Sprites/Default"));r.material.color=new Color(.42f,.92f,1f,.8f);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
    }    static M1Actor MakePlayer()
    {
        var root=new GameObject("玩家 · 山海旅者");var cc=root.AddComponent<CharacterController>();cc.height=1.72f;cc.radius=.31f;cc.center=new Vector3(0,.86f,0);
        var actor=root.AddComponent<M1Actor>();var rig=new GameObject("角色造型").transform;rig.SetParent(root.transform,false);
        var torso=Visual(PrimitiveType.Capsule,"旅行衣身",rig,new Vector3(0,.92f,0),new Vector3(.58f,.48f,.37f),cloth);
        Visual(PrimitiveType.Cube,"金色腰带",rig,new Vector3(0,.65f,0),new Vector3(.48f,.09f,.38f),accent);
        Visual(PrimitiveType.Sphere,"头部",rig,new Vector3(0,1.48f,.035f),new Vector3(.36f,.39f,.34f),skin);
        Visual(PrimitiveType.Sphere,"深色发冠",rig,new Vector3(0,1.66f,-.015f),new Vector3(.37f,.22f,.35f),boot);
        Visual(PrimitiveType.Cube,"青色肩巾",rig,new Vector3(0,1.23f,.015f),new Vector3(.62f,.15f,.4f),crystal);
        Visual(PrimitiveType.Cube,"背负小包",rig,new Vector3(0,.94f,-.25f),new Vector3(.35f,.4f,.18f),accent);
        var la=Visual(PrimitiveType.Capsule,"左臂",rig,new Vector3(-.39f,1.05f,0),new Vector3(.17f,.42f,.17f),playerMat).transform;la.localPosition=new Vector3(-.39f,1.05f,0);
        var ra=Visual(PrimitiveType.Capsule,"右臂",rig,new Vector3(.39f,1.05f,0),new Vector3(.17f,.42f,.17f),playerMat).transform;ra.localPosition=new Vector3(.39f,1.05f,0);
        var ll=Visual(PrimitiveType.Capsule,"左腿",rig,new Vector3(-.16f,.38f,0),new Vector3(.19f,.4f,.2f),cloth).transform;
        var rl=Visual(PrimitiveType.Capsule,"右腿",rig,new Vector3(.16f,.38f,0),new Vector3(.19f,.4f,.2f),cloth).transform;
        Visual(PrimitiveType.Cube,"左靴",rig,new Vector3(-.16f,.12f,.06f),new Vector3(.23f,.16f,.34f),boot);Visual(PrimitiveType.Cube,"右靴",rig,new Vector3(.16f,.12f,.06f),new Vector3(.23f,.16f,.34f),boot);
        var motion=root.AddComponent<M1PlayerVisualMotion>();motion.rig=rig;motion.leftArm=la;motion.rightArm=ra;motion.leftLeg=ll;motion.rightLeg=rl;
        root.AddComponent<M1PlayerController>();return actor;
    }    static M1Actor MakeEnemy(Vector3 at)
    {
        var root=new GameObject("测试敌人");root.transform.position=at;var col=root.AddComponent<CapsuleCollider>();col.height=1.8f;col.center=new Vector3(0,.9f,0);col.radius=.48f;
        var actor=root.AddComponent<M1Actor>();actor.isEnemy=true;
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.9f,0);body.transform.localScale=new Vector3(.8f,.9f,.8f);Object.DestroyImmediate(body.GetComponent<Collider>());body.GetComponent<Renderer>().sharedMaterial=enemyMat;return actor;
    }
    static M1Actor MakeBeast(Vector3 at,string asset,string displayName,string speciesId,float scale)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);var root=new GameObject(displayName+" · 野生异兽");root.transform.position=at;
        GameObject model=null;if(prefab)model=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(!model){model=GameObject.CreatePrimitive(PrimitiveType.Sphere);model.transform.localScale=new Vector3(1.15f,1.25f,1.15f);var c=model.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);}
        model.name=displayName+" · 模型";model.transform.SetParent(root.transform,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one*scale;
        if(speciesId=="spirit_beast"){foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i])mats[i]=Tinted(mats[i],"BlueDemon_"+i,new Color(.54f,.84f,.96f));renderer.sharedMaterials=mats;}}
        var coll=root.AddComponent<CapsuleCollider>();coll.height=1.65f;coll.radius=.55f;coll.center=new Vector3(0,.78f,0);
        var actor=root.AddComponent<M1Actor>();actor.isTameable=true;actor.displayName=displayName;actor.speciesId=speciesId;root.AddComponent<M1SpiritBeastMotion>();return actor;
    }    static void MakePortal(PortalSpec spec)
    {
        var root=new GameObject("传送门 · "+spec.name);root.transform.position=new Vector3(spec.x,0,spec.z);
        var portal=root.AddComponent<M1Portal>();portal.targetScene=Path.GetFileNameWithoutExtension(spec.scene);portal.destinationName=spec.name;
        Part(root.transform,PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(2.2f,.08f,2.2f),gateMat);
        Part(root.transform,PrimitiveType.Cube,new Vector3(-.85f,1.4f,0),new Vector3(.22f,2.8f,.25f),accent);
        Part(root.transform,PrimitiveType.Cube,new Vector3(.85f,1.4f,0),new Vector3(.22f,2.8f,.25f),accent);
        Part(root.transform,PrimitiveType.Cube,new Vector3(0,2.75f,0),new Vector3(1.9f,.22f,.25f),gateMat);
        var portalLight=new GameObject("传送门青金光");portalLight.transform.SetParent(root.transform,false);portalLight.transform.localPosition=new Vector3(0,1.45f,.3f);var pl=portalLight.AddComponent<Light>();pl.type=LightType.Point;pl.color=new Color(.24f,.74f,1f);pl.intensity=1.5f;pl.range=7f;pl.shadows=LightShadows.None;
    }
    static void HubDecor()
    {
        SpawnNature("campfire_stones.fbx",new Vector3(0,0,27f),new Vector3(1.7f,1.2f,1.7f),0,"星金营地篝火石圈");
        Visual(PrimitiveType.Cylinder,"星金营地灵纹祭台",null,new Vector3(0,.12f,25f),new Vector3(7.4f,.24f,7.4f),stone);
        Visual(PrimitiveType.Cylinder,"星金营地内环",null,new Vector3(0,.27f,25f),new Vector3(4.5f,.15f,4.5f),crystal);
        for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;Visual(PrimitiveType.Cylinder,"星金营地青玉灯",null,new Vector3(Mathf.Sin(a)*9f,.3f,25f+Mathf.Cos(a)*9f),new Vector3(.18f,.6f,.18f),crystal);}
        var fire=new GameObject("营地暖光");fire.transform.position=new Vector3(0,1.4f,27f);var flame=fire.AddComponent<Light>();flame.type=LightType.Point;flame.color=new Color(1f,.59f,.25f);flame.intensity=1.0f;flame.range=14f;flame.shadows=LightShadows.None;
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3f;SpawnNature("stump_oldTall.fbx",new Vector3(Mathf.Sin(a)*6.4f,0,27f+Mathf.Cos(a)*6.4f),Vector3.one*.95f,i*60,"营地木座");}
    }    static void ForestDecor()
    {
        SpawnNature("stump_oldTall.fbx",new Vector3(-9,0,10),Vector3.one*1.5f,13,"古树灵纹树桩");
        Visual(PrimitiveType.Cylinder,"溪畔青玉灵石",null,new Vector3(8,.8f,19),new Vector3(.45f,1.6f,.45f),crystal);
        for(int i=0;i<5;i++){float x=-10+i*5;var gl=Visual(PrimitiveType.Sphere,"浅溪灵光",null,new Vector3(x,.45f,25+Mathf.Sin(i)*2f),new Vector3(.16f,.16f,.16f),crystal);}
    }
    static void CanyonDecor()
    {
        for(int i=0;i<8;i++){float x=-22+i*6.2f;float z=30+(i%3)*5;var shard=Visual(PrimitiveType.Cube,"玄岩谷青玉晶脉",null,new Vector3(x,1.1f,z),new Vector3(.4f,2.2f,.4f),crystal);shard.transform.rotation=Quaternion.Euler(0,i*31,8);}
        SpawnNature("rock_largeC.fbx",new Vector3(-11,0,22),new Vector3(3.2f,2.7f,3.0f),24,"玄岩谷巨石地标");
        SpawnNature("rock_largeA.fbx",new Vector3(13,0,31),new Vector3(3.8f,3.3f,3.7f),317,"玄岩谷岩壁地标");
    }    static GameObject Visual(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;if(parent)o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;var c=o.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);var r=o.GetComponent<Renderer>();if(r&&mat)r.sharedMaterial=mat;return o;
    }    static GameObject Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat){var o=GameObject.CreatePrimitive(type);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
    static Material Tinted(Material source,string suffix,Color tint)
    {
        string dir="Assets/Art/Materials/v0.6.4";Directory.CreateDirectory(dir);string path=dir+"/M1_"+suffix+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(source);AssetDatabase.CreateAsset(m,path);}m.color=tint;EditorUtility.SetDirty(m);return m;
    }    static Material Mat(Color c){string dir="Assets/Art/Materials/v0.6.4";Directory.CreateDirectory(dir);string path=dir+"/M1_"+ColorUtility.ToHtmlStringRGB(c)+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));m.color=c;AssetDatabase.CreateAsset(m,path);}else{m.color=c;EditorUtility.SetDirty(m);}return m;}
}










