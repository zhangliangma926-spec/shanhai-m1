using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M1WorldBuilderV044
{
    const string Hub = "Assets/Scenes/v0.4.4/M1_HubWorld.unity";
    const string Forest = "Assets/Scenes/v0.4.4/M1_ForestWorld.unity";
    const string Canyon = "Assets/Scenes/v0.4.4/M1_CanyonWorld.unity";
    const string BeastAsset = "Assets/Art/Models/External_Quaternius/BlueDemon.fbx";
    static Material ground, stone, green, darkGreen, gateMat, playerMat, enemyMat, accent, forestGround, sand, skin, cloth, boot, crystal;

    [MenuItem("Shanhai/Build v0.4.4 A_青金营地 Rendered Pass")]
    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Scenes/v0.4.4");
        foreach (var p in new[]{Hub, Forest, Canyon})
            if (File.Exists(p)) throw new System.Exception("Refusing to overwrite existing scene: " + p);
        EnsureGroundTexture();
        AssetDatabase.Refresh();
        ground = Mat(new Color(.18f,.28f,.29f)); forestGround = Mat(new Color(.16f,.31f,.22f)); sand=Mat(new Color(.56f,.39f,.24f)); stone = Mat(new Color(.29f,.35f,.37f));SetGroundTexture(ground);SetGroundTexture(forestGround);SetGroundTexture(sand);
        Debug.Log("Render debug: Standard="+(Shader.Find("Standard")?Shader.Find("Standard").name:"NULL")+" ground="+(ground?ground.name+" / "+(ground.shader?ground.shader.name:"no shader"):"NULL")+" floorTexture="+(groundTexture?groundTexture.name:"NULL"));
        green = Mat(new Color(.18f,.42f,.28f)); darkGreen = Mat(new Color(.12f,.28f,.2f));
        gateMat = Mat(new Color(.15f,.72f,.68f)); playerMat=Mat(new Color(.85f,.67f,.28f));
        enemyMat=Mat(new Color(.58f,.19f,.16f)); accent=Mat(new Color(.96f,.68f,.28f)); skin=Mat(new Color(.82f,.55f,.39f)); cloth=Mat(new Color(.08f,.24f,.28f)); boot=Mat(new Color(.15f,.12f,.12f)); crystal=Mat(new Color(.18f,.82f,.78f)); playerMat=Mat(new Color(.82f,.61f,.29f));
        MakeWorld(Hub,"初始营地",new Color(.44f,.54f,.58f), new[]{new PortalSpec(-7,14,Forest,"青岚灵林"),new PortalSpec(7,14,Canyon,"玄岩谷地")}, 0);
        MakeWorld(Forest,"青岚灵林",new Color(.31f,.53f,.38f), new[]{new PortalSpec(0,14,Hub,"初始营地"),new PortalSpec(8,12,Canyon,"玄岩谷地")}, 1);
        MakeWorld(Canyon,"玄岩谷地",new Color(.57f,.46f,.32f), new[]{new PortalSpec(0,14,Hub,"初始营地"),new PortalSpec(-8,12,Forest,"青岚灵林")}, 2);
        EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene(Hub,true),new EditorBuildSettingsScene(Forest,true),new EditorBuildSettingsScene(Canyon,true)};
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("M1 v0.3: created three connected small worlds; prior graybox scene retained.");
    }
    struct PortalSpec { public float x,z; public string scene,name; public PortalSpec(float x,float z,string scene,string name){this.x=x;this.z=z;this.scene=scene;this.name=name;} }
    static void MakeWorld(string path,string worldName,Color sky,PortalSpec[] portals,int kind)
    {
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SetupEnvironment(sky);
        var manager=new GameObject("M1 Game Manager").AddComponent<M1GameManager>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name=worldName+" · 36米区域"; floor.transform.localScale=new Vector3(3.6f,1,3.6f); var floorRenderer=floor.GetComponent<Renderer>(); var floorMaterial=kind==2?sand:(kind==1?forestGround:ground); if(!floorRenderer||!floorMaterial)throw new System.Exception("Ground renderer/material missing: "+worldName+" kind="+kind+" renderer="+(floorRenderer?"loaded":"NULL")+" material="+(floorMaterial?"loaded":"NULL")+" shader="+(Shader.Find("Standard")?Shader.Find("Standard").name:"NULL")+" texture="+(groundTexture?"loaded":"missing")); floorRenderer.sharedMaterial=floorMaterial; floorMaterial.mainTextureScale=new Vector2(10,10);
        Border(0,18,36,.7f); Border(0,-18,36,.7f); SideBorder(18,0,36,.7f); SideBorder(-18,0,36,.7f);
        if(kind==1) ForestDecor(); else if(kind==2) CanyonDecor(); else HubDecor();
        var player=MakePlayer(); player.transform.position=new Vector3(0,0,0);
        var enemy=MakeEnemy(new Vector3(-5,0,4));
        var beast=MakeBeast(new Vector3(5,0,5));
        manager.player=player;manager.enemy=enemy;manager.beast=beast;manager.gameObject.AddComponent<M1PetSystem>();
        MakeFox(new Vector3(8,0,7)); MakeTurtle(new Vector3(-10,0,8)); MakeSkyBird(new Vector3(12,0,5));
        foreach(var spec in portals) MakePortal(spec);
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; var camera=camObj.AddComponent<Camera>();camera.fieldOfView=56;camera.allowHDR=true;camera.allowMSAA=true;camera.clearFlags=CameraClearFlags.Skybox;camObj.AddComponent<AudioListener>();
        var follow=camObj.AddComponent<M1CameraFollow>();follow.target=player.transform;follow.distance=6.0f;follow.minDistance=3.6f;follow.maxDistance=9.5f;player.GetComponent<M1PlayerController>().cameraTransform=camObj.transform;
        var lightObj=new GameObject("Directional Light · 山海日光");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.color=new Color(1f,.91f,.76f);light.shadows=LightShadows.Soft;light.shadowStrength=.82f;lightObj.transform.rotation=Quaternion.Euler(38,-32,0);RenderSettings.sun=light;
        EditorSceneManager.SaveScene(scene,path);
    }
    static void SetupEnvironment(Color sky)
    {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=Color.Lerp(sky,new Color(.36f,.58f,.72f),.45f);
        RenderSettings.ambientEquatorColor=Color.Lerp(sky,new Color(.47f,.49f,.38f),.40f);
        RenderSettings.ambientGroundColor=new Color(.18f,.20f,.16f);
        RenderSettings.fog=true;RenderSettings.fogColor=Color.Lerp(sky,new Color(.60f,.72f,.72f),.25f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0042f;
        string key=ColorUtility.ToHtmlStringRGB(sky);string skyPath="Assets/Art/Materials/Generated/A_Sky_"+key+".mat";var skybox=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(!skybox){skybox=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(skybox,skyPath);}skybox.SetFloat("_AtmosphereThickness",1.05f);skybox.SetFloat("_Exposure",1.12f);skybox.SetColor("_SkyTint",Color.Lerp(sky,new Color(.38f,.65f,.78f),.6f));skybox.SetColor("_GroundColor",new Color(.23f,.27f,.24f));RenderSettings.skybox=skybox;
        QualitySettings.antiAliasing=4;QualitySettings.shadowDistance=65;QualitySettings.shadowResolution=ShadowResolution.High;
    }
    static void Border(float x,float z,float length,float width){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name="区域边界";o.transform.position=new Vector3(x,1,z);o.transform.localScale=new Vector3(length,2,width);o.GetComponent<Renderer>().sharedMaterial=stone;}
    static void SideBorder(float x,float z,float length,float width){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name="区域边界";o.transform.position=new Vector3(x,1,z);o.transform.localScale=new Vector3(width,2,length);o.GetComponent<Renderer>().sharedMaterial=stone;}
    static M1Actor MakePlayer()
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
    static M1Actor MakeBeast(Vector3 at)
    {
        GameObject root=null;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BeastAsset);
        if(prefab) root=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(!root){root=GameObject.CreatePrimitive(PrimitiveType.Sphere);root.transform.localScale=new Vector3(1.25f,1.2f,1.5f);}
        root.name="岚角灵兽 · 可驯服";root.transform.position=at;root.transform.localScale*=.72f;
        var coll=root.GetComponent<Collider>();if(!coll)coll=root.AddComponent<CapsuleCollider>();
        if(coll is CapsuleCollider cap){cap.height=2f;cap.radius=.75f;cap.center=new Vector3(0,1,0);}
        var actor=root.AddComponent<M1Actor>();actor.isTameable=true;actor.speciesId="blue_demon";actor.displayName="岚角灵兽";root.gameObject.AddComponent<M1SpiritBeastMotion>();return actor;
    }
    static void MakePortal(PortalSpec spec)
    {
        var root=new GameObject("传送门 · "+spec.name);root.transform.position=new Vector3(spec.x,0,spec.z);
        var portal=root.AddComponent<M1Portal>();portal.targetScene=Path.GetFileNameWithoutExtension(spec.scene);portal.destinationName=spec.name;
        Part(root.transform,PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(2.2f,.08f,2.2f),gateMat);
        Part(root.transform,PrimitiveType.Cube,new Vector3(-.85f,1.4f,0),new Vector3(.22f,2.8f,.25f),accent);
        Part(root.transform,PrimitiveType.Cube,new Vector3(.85f,1.4f,0),new Vector3(.22f,2.8f,.25f),accent);
        Part(root.transform,PrimitiveType.Cube,new Vector3(0,2.75f,0),new Vector3(1.9f,.22f,.25f),gateMat);
    }
    static void HubDecor()
    {
        var plaza=Visual(PrimitiveType.Cylinder,"青金营地 · 环形石坪",null,new Vector3(0,.09f,9),new Vector3(11,.18f,11),stone);
        Visual(PrimitiveType.Cylinder,"营火祭台",null,new Vector3(0,.22f,9),new Vector3(3.6f,.26f,3.6f),accent);
        Visual(PrimitiveType.Cylinder,"火盆",null,new Vector3(0,.52f,9),new Vector3(1.4f,.62f,1.4f),stone);
        var flame=Visual(PrimitiveType.Sphere,"灵火",null,new Vector3(0,1.10f,9),new Vector3(.72f,1.12f,.72f),Glow(new Color(1f,.47f,.12f),.9f));
        AddPointLight("营火暖光",new Vector3(0,1.2f,9),new Color(1f,.48f,.16f),3.2f,10f);
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4f;var slab=Visual(PrimitiveType.Cube,"石坪拼砖",null,new Vector3(Mathf.Cos(a)*6.5f,.20f,9+Mathf.Sin(a)*6.5f),new Vector3(2.1f,.14f,1.25f),i%2==0?ground:accent);slab.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
        for(int i=0;i<6;i++){float z=1.6f+i*1.2f;var step=Visual(PrimitiveType.Cube,"青石迎宾路",null,new Vector3(0,.055f,z),new Vector3(2.2f,.08f,.82f),i%2==0?stone:ground);}
        for(int side=-1;side<=1;side+=2){
            float x=side*5.2f;float z=10.6f;
            Visual(PrimitiveType.Cylinder,"营帐立柱",null,new Vector3(x,1.65f,z),new Vector3(.24f,1.65f,.24f),accent);
            var roof=Visual(PrimitiveType.Cube,"青金营帐檐",null,new Vector3(x,3.3f,z),new Vector3(3.2f,.30f,3.0f),green);roof.transform.rotation=Quaternion.Euler(0,45,0);
            Visual(PrimitiveType.Cube,"营帐金边",null,new Vector3(x,3.08f,z),new Vector3(3.45f,.12f,3.25f),accent);
            var banner=Visual(PrimitiveType.Cube,"山海旗帜",null,new Vector3(x,2.45f,z+1.05f),new Vector3(.72f,1.25f,.10f),crystal);banner.transform.rotation=Quaternion.Euler(0,side*4,0);
        }
        for(int side=-1;side<=1;side+=2){
            float x=side*9f;float z=4.2f;
            Visual(PrimitiveType.Cylinder,"灵灯石柱",null,new Vector3(x,1.45f,z),new Vector3(.42f,1.45f,.42f),stone);
            var orb=Visual(PrimitiveType.Sphere,"青金灵灯",null,new Vector3(x,3.05f,z),new Vector3(.75f,.92f,.75f),Glow(new Color(.20f,.88f,.83f),1.2f));
            AddPointLight("灵灯柔光",orb.transform.position,new Color(.23f,.78f,.78f),2.2f,7f);
        }
        for(int i=0;i<12;i++){float x=-15+i*2.7f;float z=15+(i%3)*1.1f;var rock=Visual(PrimitiveType.Sphere,"远山青石",null,new Vector3(x,.6f,z),new Vector3(2.6f,1.2f,2.0f),i%2==0?stone:darkGreen);}
        for(int i=0;i<12;i++){float x=-14+i*2.5f;float z=-13+(i%4)*1.0f;Visual(PrimitiveType.Sphere,"营地草丛",null,new Vector3(x,.26f,z),new Vector3(1.6f,.48f,1.1f),green);}
        AddLantern( -12,2.25f,2.8f);AddLantern(12,2.25f,2.8f);
    }
    static void AddLantern(float x,float y,float z){Visual(PrimitiveType.Cylinder,"路边灯柱",null,new Vector3(x,y*.55f,z),new Vector3(.18f,y*.55f,.18f),accent);var g=Visual(PrimitiveType.Sphere,"路边灵灯",null,new Vector3(x,y,z),new Vector3(.46f,.60f,.46f),Glow(new Color(.23f,.86f,.84f),1f));AddPointLight("路灯微光",g.transform.position,new Color(.22f,.8f,.8f),1.3f,4.8f);}
    static void AddPointLight(string name,Vector3 pos,Color color,float intensity,float range){var o=new GameObject(name);o.transform.position=pos;var l=o.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
    static void ForestDecor()
    {
        for(int i=0;i<16;i++){float x=-15+(i%8)*4.3f;float z=3+(i/8)*8f;Visual(PrimitiveType.Cylinder,"青岚林树干",null,new Vector3(x,1.8f,z),new Vector3(.48f,1.8f,.48f),stone);var crown=Visual(PrimitiveType.Sphere,"青岚林树冠",null,new Vector3(x,4.0f,z),new Vector3(2.8f,3.0f,2.8f),green);crown.transform.rotation=Quaternion.Euler(0,i*27,0);Visual(PrimitiveType.Sphere,"林间花簇",null,new Vector3(x+1.15f,.3f,z+.5f),new Vector3(.65f,.38f,.65f),accent);}
        Visual(PrimitiveType.Cube,"青石林间长径",null,new Vector3(0,.04f,8),new Vector3(3.4f,.08f,26f),stone);
        for(int i=0;i<9;i++){float x=-10+i*2.5f;var glow=Visual(PrimitiveType.Sphere,"浮游灵萤",null,new Vector3(x,1.4f,5+(i%3)*4),new Vector3(.22f,.22f,.22f),Glow(new Color(.25f,.88f,.78f),1.0f));}
        Visual(PrimitiveType.Cylinder,"林中祭树",null,new Vector3(0,2.8f,14),new Vector3(1.5f,2.8f,1.5f),accent);Visual(PrimitiveType.Sphere,"古树灵冠",null,new Vector3(0,6.2f,14),new Vector3(4.4f,3.8f,4.4f),green);
    }
    static void CanyonDecor()
    {
        for(int i=0;i<18;i++){float x=-15+(i%9)*3.8f;float z=3+(i/9)*10f;var rock=Visual(PrimitiveType.Cube,"玄岩层叠巨石",null,new Vector3(x,1.0f,z),new Vector3(2.6f,2.2f,2.7f),stone);rock.transform.rotation=Quaternion.Euler(i*3,i*21,7);var shard=Visual(PrimitiveType.Cube,"青玉晶簇",null,new Vector3(x+.45f,2.0f,z+.45f),new Vector3(.48f,1.8f,.48f),Glow(new Color(.20f,.82f,.91f),.8f));shard.transform.rotation=Quaternion.Euler(0,i*18,8);}
        Visual(PrimitiveType.Cube,"峡谷青金古道",null,new Vector3(0,.04f,8),new Vector3(3.6f,.08f,26f),accent);
        for(int i=0;i<8;i++)Visual(PrimitiveType.Cylinder,"风化石柱",null,new Vector3(-14+i*4f,1.2f,16),new Vector3(.4f,2.4f,.4f),stone);
        var altar=Visual(PrimitiveType.Cylinder,"峡谷灵晶祭坛",null,new Vector3(0,.25f,14),new Vector3(3.2f,.5f,3.2f),stone);Visual(PrimitiveType.Cube,"祭坛晶核",null,new Vector3(0,1.5f,14),new Vector3(.8f,2.4f,.8f),Glow(new Color(.18f,.84f,.95f),1.2f));AddPointLight("灵晶冷光",new Vector3(0,2f,14),new Color(.16f,.72f,.95f),2f,9f);
    }
    static M1Actor MakeFox(Vector3 at)
    {
        var root=NewPet("wind_fox","赤尾灵狐",at,new Color(.83f,.30f,.10f),new Color(1f,.70f,.18f));
        Visual(PrimitiveType.Capsule,"狐身",root.transform,new Vector3(0,.72f,0),new Vector3(.58f,.42f,1.10f),Mat(new Color(.78f,.27f,.10f)));
        Visual(PrimitiveType.Sphere,"狐首",root.transform,new Vector3(0,1.22f,.45f),new Vector3(.62f,.55f,.62f),Mat(new Color(.90f,.38f,.12f)));
        Visual(PrimitiveType.Sphere,"胸前浅毛",root.transform,new Vector3(0,.87f,.42f),new Vector3(.34f,.42f,.12f),accent);
        for(int side=-1;side<=1;side+=2){var ear=Visual(PrimitiveType.Cube,"狐耳",root.transform,new Vector3(side*.22f,1.66f,.42f),new Vector3(.18f,.48f,.18f),Mat(new Color(.75f,.24f,.10f)));ear.transform.rotation=Quaternion.Euler(0,0,-side*16);Visual(PrimitiveType.Sphere,"狐眼",root.transform,new Vector3(side*.22f,1.29f,.72f),new Vector3(.08f,.09f,.07f),boot);}
        for(int side=-1;side<=1;side+=2)for(int front=-1;front<=1;front+=2)Visual(PrimitiveType.Capsule,"狐腿",root.transform,new Vector3(side*.20f,.30f,front*.32f),new Vector3(.18f,.34f,.18f),Mat(new Color(.63f,.21f,.10f)));
        var tail=Visual(PrimitiveType.Capsule,"蓬松赤尾",root.transform,new Vector3(0,.95f,-.78f),new Vector3(.42f,.42f,.95f),accent);tail.transform.rotation=Quaternion.Euler(50,0,0);Visual(PrimitiveType.Sphere,"尾尖灵光",root.transform,new Vector3(0,1.35f,-1.12f),new Vector3(.30f,.30f,.30f),Glow(new Color(1f,.60f,.16f),.8f));root.gameObject.AddComponent<M1SpiritBeastMotion>();return root.GetComponent<M1Actor>();
    }
    static M1Actor MakeTurtle(Vector3 at)
    {
        var root=NewPet("stone_turtle","岩甲灵龟",at,new Color(.25f,.43f,.34f),new Color(.42f,.65f,.48f));
        Visual(PrimitiveType.Sphere,"灵龟身躯",root.transform,new Vector3(0,.70f,0),new Vector3(1.2f,.72f,1.45f),Mat(new Color(.22f,.45f,.35f)));
        Visual(PrimitiveType.Sphere,"岩甲龟壳",root.transform,new Vector3(0,1.12f,-.05f),new Vector3(1.35f,.85f,1.35f),stone);
        Visual(PrimitiveType.Sphere,"龟首",root.transform,new Vector3(0,.96f,.92f),new Vector3(.63f,.52f,.64f),Mat(new Color(.35f,.60f,.43f)));
        for(int side=-1;side<=1;side+=2){Visual(PrimitiveType.Sphere,"灵龟眼",root.transform,new Vector3(side*.22f,1.13f,1.18f),new Vector3(.09f,.09f,.08f),accent);for(int front=-1;front<=1;front+=2)Visual(PrimitiveType.Cylinder,"龟足",root.transform,new Vector3(side*.52f,.28f,front*.56f),new Vector3(.28f,.31f,.28f),Mat(new Color(.31f,.50f,.39f)));var spike=Visual(PrimitiveType.Cube,"龟甲棱刺",root.transform,new Vector3(side*.47f,1.76f,-.1f),new Vector3(.25f,.5f,.25f),accent);spike.transform.rotation=Quaternion.Euler(0,0,-side*18);}
        root.gameObject.AddComponent<M1SpiritBeastMotion>();return root.GetComponent<M1Actor>();
    }
    static M1Actor MakeSkyBird(Vector3 at)
    {
        var root=NewPet("sky_bird","青羽灵雀",at,new Color(.20f,.62f,.68f),new Color(.54f,.88f,.85f));
        Visual(PrimitiveType.Sphere,"灵雀身躯",root.transform,new Vector3(0,1.05f,0),new Vector3(.66f,.86f,.72f),Mat(new Color(.15f,.55f,.65f)));
        Visual(PrimitiveType.Sphere,"灵雀头冠",root.transform,new Vector3(0,1.72f,.24f),new Vector3(.56f,.56f,.55f),accent);
        Visual(PrimitiveType.Cube,"灵雀喙",root.transform,new Vector3(0,1.60f,.63f),new Vector3(.23f,.16f,.42f),accent);
        for(int side=-1;side<=1;side+=2){var wing=Visual(PrimitiveType.Capsule,"青羽翅膀",root.transform,new Vector3(side*.55f,1.15f,-.04f),new Vector3(.22f,.72f,.66f),Mat(new Color(.12f,.40f,.61f)));wing.transform.rotation=Quaternion.Euler(0,0,side*35);Visual(PrimitiveType.Sphere,"靈雀眼",root.transform,new Vector3(side*.19f,1.78f,.48f),new Vector3(.08f,.08f,.08f),boot);}
        var tail=Visual(PrimitiveType.Capsule,"靈雀長尾羽",root.transform,new Vector3(0,.78f,-.55f),new Vector3(.18f,.75f,.18f),accent);tail.transform.rotation=Quaternion.Euler(-20,0,0);root.gameObject.AddComponent<M1SpiritBeastMotion>();return root.GetComponent<M1Actor>();
    }
    static M1Actor NewPet(string id,string label,Vector3 at,Color main,Color accentColor)
    {
        var root=new GameObject(label+" · 可收服");root.transform.position=at;var cap=root.AddComponent<CapsuleCollider>();cap.height=1.9f;cap.radius=.58f;cap.center=new Vector3(0,.9f,0);var actor=root.AddComponent<M1Actor>();actor.isTameable=true;actor.speciesId=id;actor.displayName=label;return actor;
    }
    static Material Glow(Color c,float power){return PersistentMaterial(c,true,power);}
    static void EnsureGroundTexture()
    {
        string dir="Assets/Art/Materials/Generated";Directory.CreateDirectory(dir);string path=dir+"/A_青金营地地表.png";
        if(!File.Exists(path)){var tex=new Texture2D(128,128,TextureFormat.RGB24,false);tex.wrapMode=TextureWrapMode.Repeat;for(int y=0;y<128;y++)for(int x=0;x<128;x++){float n=Mathf.PerlinNoise(x*.055f,y*.055f)*.12f+Mathf.PerlinNoise(x*.19f,y*.19f)*.045f;float grit=Random.value*.025f;float v=.83f+n+grit;tex.SetPixel(x,y,new Color(v,v,v));}tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);}
        groundTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D groundTexture;
    static void SetGroundTexture(Material m){if(groundTexture){m.mainTexture=groundTexture;m.mainTextureScale=new Vector2(10,10);}}
    static Material Mat(Color c){return PersistentMaterial(c,false,0f);}
    static Material PersistentMaterial(Color c,bool emission,float power){string key=ColorUtility.ToHtmlStringRGBA(c)+(emission?"_Glow":"_Mat");string path="Assets/Art/Materials/Generated/A_"+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Metallic",.12f);m.SetFloat("_Glossiness",.34f);if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*power);}return m;}
    static GameObject Visual(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;if(parent)o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;var c=o.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);var r=o.GetComponent<Renderer>();if(r&&mat)r.sharedMaterial=mat;return o;
    }    static GameObject Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat){var o=GameObject.CreatePrimitive(type);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}

}










