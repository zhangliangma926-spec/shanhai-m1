using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class M1SceneBuilder
{
    const string ScenePath = "Assets/Scenes/M1_Graybox.unity";
    [MenuItem("Shanhai/Build M1 Graybox Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath)) { EditorUtility.DisplayDialog("场景已存在", "为避免覆盖，未改动现有场景：" + ScenePath, "确定"); return; }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var manager = new GameObject("M1 Game Manager").AddComponent<M1GameManager>();
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "测试场地"; ground.transform.localScale = new Vector3(3,1,3); ground.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.23f,.29f,.3f));
        for (int i=0;i<8;i++) { var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="灰盒障碍"; wall.transform.position=new Vector3(-10+i*2.8f,.5f,7); wall.transform.localScale=new Vector3(1.5f,1,1.5f); wall.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.4f,.43f,.45f)); }
        var player=GameObject.CreatePrimitive(PrimitiveType.Capsule); player.name="玩家（灰盒）"; player.transform.position=new Vector3(0,1,0); player.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.88f,.72f,.28f)); Object.DestroyImmediate(player.GetComponent<CapsuleCollider>()); var cc=player.AddComponent<CharacterController>(); cc.height=2; cc.radius=.42f; cc.center=new Vector3(0,1,0); var pa=player.AddComponent<M1Actor>(); pa.maxHealth=100; pa.health=100; player.AddComponent<M1PlayerController>();
        var enemy=GameObject.CreatePrimitive(PrimitiveType.Capsule); enemy.name="测试敌人"; enemy.transform.position=new Vector3(4,1,3); enemy.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.65f,.18f,.12f)); var ea=enemy.AddComponent<M1Actor>(); ea.isEnemy=true;
        var beast=GameObject.CreatePrimitive(PrimitiveType.Sphere); beast.name="可驯服异兽（灰盒）"; beast.transform.position=new Vector3(-4,.7f,3); beast.transform.localScale=new Vector3(1.25f,1.35f,1.6f); beast.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.25f,.62f,.8f)); var ba=beast.AddComponent<M1Actor>(); ba.isTameable=true;
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; var cam=camObj.AddComponent<Camera>(); cam.fieldOfView=62; camObj.AddComponent<AudioListener>(); var follow=camObj.AddComponent<M1CameraFollow>(); follow.target=player.transform; player.GetComponent<M1PlayerController>().cameraTransform=camObj.transform;
        var lightObj=new GameObject("Directional Light"); var light=lightObj.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.15f; lightObj.transform.rotation=Quaternion.Euler(50,-30,0);
        manager.player=pa; manager.enemy=ea; manager.beast=ba;
        var spawn=GameObject.CreatePrimitive(PrimitiveType.Cylinder); spawn.name="灵纹测试台"; spawn.transform.position=new Vector3(0,.15f,5); spawn.transform.localScale=new Vector3(1.5f,.15f,1.5f); spawn.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.1f,.48f,.44f));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets(); EditorUtility.DisplayDialog("M1 场景已创建", "已生成独立灰盒场景，可打开 Assets/Scenes/M1_Graybox.unity 后按 Play。", "确定");
    }
    static Material Mat(Color c) { var m=new Material(Shader.Find("Standard")); m.color=c; return m; }
}
