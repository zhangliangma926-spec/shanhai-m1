using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M1WorldBuilderV04
{
    const string Hub = "Assets/Scenes/v0.4/M1_HubWorld.unity";
    const string Forest = "Assets/Scenes/v0.4/M1_ForestWorld.unity";
    const string Canyon = "Assets/Scenes/v0.4/M1_CanyonWorld.unity";
    const string BeastAsset = "Assets/Art/Models/Blender_Ready/LanJiao_SpiritBeast.fbx";
    static Material ground, stone, green, darkGreen, gateMat, playerMat, enemyMat, accent, forestGround, sand, skin, cloth, boot, crystal;

    [MenuItem("Shanhai/Build v0.4 Visual Pass")]
    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Scenes/v0.4");
        foreach (var p in new[]{Hub, Forest, Canyon})
            if (File.Exists(p)) throw new System.Exception("Refusing to overwrite existing scene: " + p);
        AssetDatabase.Refresh();
        ground = Mat(new Color(.18f,.28f,.29f)); forestGround = Mat(new Color(.16f,.31f,.22f)); sand=Mat(new Color(.56f,.39f,.24f)); stone = Mat(new Color(.29f,.35f,.37f));
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
        RenderSettings.ambientLight=sky; RenderSettings.fog=true; RenderSettings.fogColor=sky; RenderSettings.fogDensity=.006f;
        var manager=new GameObject("M1 Game Manager").AddComponent<M1GameManager>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name=worldName+" · 36米区域"; floor.transform.localScale=new Vector3(3.6f,1,3.6f); floor.GetComponent<Renderer>().sharedMaterial=kind==2?sand:(kind==1?forestGround:ground);
        Border(0,18,36,.7f); Border(0,-18,36,.7f); SideBorder(18,0,36,.7f); SideBorder(-18,0,36,.7f);
        if(kind==1) ForestDecor(); else if(kind==2) CanyonDecor(); else HubDecor();
        var player=MakePlayer(); player.transform.position=new Vector3(0,0,0);
        var enemy=MakeEnemy(new Vector3(-5,0,4));
        var beast=MakeBeast(new Vector3(5,0,5));
        manager.player=player;manager.enemy=enemy;manager.beast=beast;
        foreach(var spec in portals) MakePortal(spec);
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; camObj.AddComponent<Camera>().fieldOfView=62; camObj.AddComponent<AudioListener>();
        var follow=camObj.AddComponent<M1CameraFollow>();follow.target=player.transform;follow.distance=6.0f;follow.minDistance=3.6f;follow.maxDistance=9.5f;player.GetComponent<M1PlayerController>().cameraTransform=camObj.transform;
        var lightObj=new GameObject("Directional Light");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.45f;light.shadows=LightShadows.Soft;lightObj.transform.rotation=Quaternion.Euler(48,-28,0);
        EditorSceneManager.SaveScene(scene,path);
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
        var actor=root.AddComponent<M1Actor>();actor.isTameable=true;root.AddComponent<M1SpiritBeastMotion>();return actor;
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
        Visual(PrimitiveType.Cylinder,"营地圆台",null,new Vector3(0,.08f,-9),new Vector3(5.2f,.16f,5.2f),stone);
        Visual(PrimitiveType.Cylinder,"古老营火台",null,new Vector3(0,.32f,-9),new Vector3(2.8f,.42f,2.8f),accent);
        Visual(PrimitiveType.Cylinder,"灵石灯柱",null,new Vector3(-8,1.5f,-7),new Vector3(.5f,3f,.5f),stone);Visual(PrimitiveType.Cylinder,"灵石灯柱",null,new Vector3(8,1.5f,-7),new Vector3(.5f,3f,.5f),stone);
        Visual(PrimitiveType.Sphere,"青色灵灯",null,new Vector3(-8,3.15f,-7),new Vector3(.62f,.68f,.62f),crystal);Visual(PrimitiveType.Sphere,"青色灵灯",null,new Vector3(8,3.15f,-7),new Vector3(.62f,.68f,.62f),crystal);
        for(int i=0;i<7;i++){var b=Visual(PrimitiveType.Cube,"营地石墩",null,new Vector3(-12+i*4,.35f,-14),new Vector3(1.2f,.7f,1.2f),stone);b.transform.rotation=Quaternion.Euler(0,i*11,0);}
    }
    static void ForestDecor()
    {
        for(int i=0;i<12;i++){float x=-14+(i%6)*5.5f;float z=-11+(i/6)*9f;Visual(PrimitiveType.Cylinder,"青岚林树干",null,new Vector3(x,1.4f,z),new Vector3(.42f,1.4f,.42f),stone);var crown=Visual(PrimitiveType.Sphere,"青岚林树冠",null,new Vector3(x,3.25f,z),new Vector3(2.25f,2.5f,2.25f),green);crown.transform.rotation=Quaternion.Euler(0,i*27,0);Visual(PrimitiveType.Sphere,"花丛",null,new Vector3(x+1.1f,.28f,z+.5f),new Vector3(.42f,.28f,.42f),accent);}
        Visual(PrimitiveType.Cube,"林间石径",null,new Vector3(0,.035f,-5),new Vector3(3.4f,.07f,18f),stone);
        for(int i=0;i<5;i++)Visual(PrimitiveType.Sphere,"灵蝶光点",null,new Vector3(-5+i*2.5f,1.2f,-1+i%2*2),new Vector3(.18f,.18f,.18f),crystal);
    }
    static void CanyonDecor()
    {
        for(int i=0;i<14;i++){float x=-14+(i%7)*4.7f;float z=-12+(i/7)*17f;var rock=Visual(PrimitiveType.Cube,"玄岩巨石",null,new Vector3(x,.8f,z),new Vector3(2.3f,1.7f,2.1f),stone);rock.transform.rotation=Quaternion.Euler(i*4,i*21,7);var shard=Visual(PrimitiveType.Cube,"青玉晶簇",null,new Vector3(x+.5f,1.5f,z+.3f),new Vector3(.45f,1.4f,.45f),crystal);shard.transform.rotation=Quaternion.Euler(0,i*18,8);}
        Visual(PrimitiveType.Cube,"峡谷古道",null,new Vector3(0,.04f,-4),new Vector3(3f,.08f,19f),accent);
        for(int i=0;i<6;i++)Visual(PrimitiveType.Cylinder,"风化石柱",null,new Vector3(-12+i*4.8f,.8f,13),new Vector3(.35f,1.6f,.35f),stone);
    }
    static GameObject Visual(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;if(parent)o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;var c=o.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);var r=o.GetComponent<Renderer>();if(r&&mat)r.sharedMaterial=mat;return o;
    }    static GameObject Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat){var o=GameObject.CreatePrimitive(type);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
    static Material Mat(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;return m;}
}

