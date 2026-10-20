using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M1WorldBuilderV03
{
    const string Hub = "Assets/Scenes/M1_HubWorld.unity";
    const string Forest = "Assets/Scenes/M1_ForestWorld.unity";
    const string Canyon = "Assets/Scenes/M1_CanyonWorld.unity";
    const string BeastAsset = "Assets/Art/Models/Blender_Ready/LanJiao_SpiritBeast.fbx";
    static Material ground, stone, green, darkGreen, gateMat, playerMat, enemyMat, accent;

    [MenuItem("Shanhai/Build v0.3 Three Small Worlds")]
    public static void BuildAll()
    {
        foreach (var p in new[]{Hub, Forest, Canyon})
            if (File.Exists(p)) throw new System.Exception("Refusing to overwrite existing scene: " + p);
        AssetDatabase.Refresh();
        ground = Mat(new Color(.28f,.34f,.33f)); stone = Mat(new Color(.34f,.37f,.4f));
        green = Mat(new Color(.18f,.42f,.28f)); darkGreen = Mat(new Color(.12f,.28f,.2f));
        gateMat = Mat(new Color(.15f,.72f,.68f)); playerMat=Mat(new Color(.85f,.67f,.28f));
        enemyMat=Mat(new Color(.65f,.18f,.12f)); accent=Mat(new Color(.82f,.65f,.3f));
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
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name=worldName+" · 36米区域"; floor.transform.localScale=new Vector3(3.6f,1,3.6f); floor.GetComponent<Renderer>().sharedMaterial=kind==2?stone:ground;
        Border(0,18,36,.7f); Border(0,-18,36,.7f); SideBorder(18,0,36,.7f); SideBorder(-18,0,36,.7f);
        if(kind==1) ForestDecor(); else if(kind==2) CanyonDecor(); else HubDecor();
        var player=MakePlayer(); player.transform.position=new Vector3(0,0,0);
        var enemy=MakeEnemy(new Vector3(-5,0,4));
        var beast=MakeBeast(new Vector3(5,0,5));
        manager.player=player;manager.enemy=enemy;manager.beast=beast;
        foreach(var spec in portals) MakePortal(spec);
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; camObj.AddComponent<Camera>().fieldOfView=62; camObj.AddComponent<AudioListener>();
        var follow=camObj.AddComponent<M1CameraFollow>();follow.target=player.transform;player.GetComponent<M1PlayerController>().cameraTransform=camObj.transform;
        var lightObj=new GameObject("Directional Light");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;lightObj.transform.rotation=Quaternion.Euler(48,-28,0);
        EditorSceneManager.SaveScene(scene,path);
    }
    static void Border(float x,float z,float length,float width){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name="区域边界";o.transform.position=new Vector3(x,1,z);o.transform.localScale=new Vector3(length,2,width);o.GetComponent<Renderer>().sharedMaterial=stone;}
    static void SideBorder(float x,float z,float length,float width){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name="区域边界";o.transform.position=new Vector3(x,1,z);o.transform.localScale=new Vector3(width,2,length);o.GetComponent<Renderer>().sharedMaterial=stone;}
    static M1Actor MakePlayer()
    {
        var root=new GameObject("玩家 · 小比例灰盒");var cc=root.AddComponent<CharacterController>();cc.height=1.7f;cc.radius=.3f;cc.center=new Vector3(0,.86f,0);
        var actor=root.AddComponent<M1Actor>();
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="玩家身体";body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.78f,0);body.transform.localScale=new Vector3(.62f,.78f,.5f);Object.DestroyImmediate(body.GetComponent<Collider>());body.GetComponent<Renderer>().sharedMaterial=playerMat;
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="玩家头部";head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(0,1.48f,.04f);head.transform.localScale=Vector3.one*.52f;Object.DestroyImmediate(head.GetComponent<Collider>());head.GetComponent<Renderer>().sharedMaterial=accent;
        root.AddComponent<M1PlayerController>();return actor;
    }
    static M1Actor MakeEnemy(Vector3 at)
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
        var actor=root.AddComponent<M1Actor>();actor.isTameable=true;return actor;
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
    static void HubDecor(){for(int i=0;i<6;i++){var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name="营地石墩";b.transform.position=new Vector3(-11+i*4,.35f,-7);b.transform.localScale=new Vector3(1.2f,.7f,1.2f);b.GetComponent<Renderer>().sharedMaterial=stone;}}
    static void ForestDecor(){for(int i=0;i<10;i++){float x=-13+(i%5)*6.2f;float z=-10+(i/5)*8f;var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="青岚林树干";trunk.transform.position=new Vector3(x,1.4f,z);trunk.transform.localScale=new Vector3(.45f,1.4f,.45f);trunk.GetComponent<Renderer>().sharedMaterial=stone;var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.name="青岚林树冠";crown.transform.position=new Vector3(x,3.3f,z);crown.transform.localScale=new Vector3(2.2f,2.5f,2.2f);crown.GetComponent<Renderer>().sharedMaterial=green;}}
    static void CanyonDecor(){for(int i=0;i<12;i++){float x=-14+(i%6)*5.4f;float z=-11+(i/6)*16f;var rock=GameObject.CreatePrimitive(PrimitiveType.Cube);rock.name="玄岩巨石";rock.transform.position=new Vector3(x,.8f,z);rock.transform.localScale=new Vector3(2.4f,1.6f,2.1f);rock.transform.rotation=Quaternion.Euler(0,i*21,7);rock.GetComponent<Renderer>().sharedMaterial=stone;}}
    static GameObject Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat){var o=GameObject.CreatePrimitive(type);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
    static Material Mat(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;return m;}
}
