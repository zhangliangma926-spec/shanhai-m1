using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class M1WindowsBuildV066Command
{
    public static void Build()
    {
        string project=Directory.GetParent(Application.dataPath).FullName;
        string game=Directory.GetParent(project).FullName;
        string output=Path.Combine(game,"05_成品交付","Windows","v0.6.6");
        if(File.Exists(Path.Combine(output,"Shanhai_M1.exe")))throw new System.Exception("Refusing to overwrite v0.6.6 build.");
        M1WorldBuilderV066.BuildAll();
        ValidateScenes();
        Directory.CreateDirectory(output);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        if(scenes.Length!=3)throw new System.Exception("Expected three worlds; got "+scenes.Length);
        var options=new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(output,"Shanhai_M1.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};
        var report=BuildPipeline.BuildPlayer(options);
        string info="Version: M1 v0.6.6 星金营地\nResult: "+report.summary.result+"\nScene count: "+scenes.Length+"\nScenes: "+string.Join(", ",scenes)+"\nOutput: "+options.locationPathName+"\nSize bytes: "+report.summary.totalSize+"\nValidation: 3 scenes, player/enemy/pet manager, 3 tameable species per world, terrain collider, and portals.";
        File.WriteAllText(Path.Combine(output,"BUILD_INFO.txt"),info);
        Debug.Log(info);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        M1PreviewCaptureV066.CaptureHub();
    }
    static void ValidateScenes()
    {
        string[] paths=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        foreach(string path in paths)
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            var actors=Object.FindObjectsByType<M1Actor>();
            int players=actors.Count(a=>!a.isEnemy&&!a.isTameable), enemies=actors.Count(a=>a.isEnemy),pets=actors.Count(a=>a.isTameable);
            if(players!=1||enemies!=1||pets!=3)throw new System.Exception("Scene validation failed: "+path+" has player="+players+", enemy="+enemies+", tameables="+pets);
            if(!Object.FindAnyObjectByType<M1PetSystem>())throw new System.Exception("Missing pet collection system in "+path);
            if(Object.FindObjectsByType<M1Portal>().Length!=2)throw new System.Exception("Expected two portals in "+path);
            if(!Object.FindFirstObjectByType<MeshCollider>())throw new System.Exception("Missing terrain collision in "+path);
        }
        Debug.Log("v0.6.6 validation passed: 3 scenes, 3 tameable creatures per scene, 2 portals and terrain collider per scene.");
    }
}



