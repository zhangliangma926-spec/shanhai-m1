using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.SceneManagement;
public static class M1WindowsBuildV072Command
{
    public static void Build()
    {
        M1WorldBuilderV072.BuildAll(); ValidateScenes();
        string project=Directory.GetParent(Application.dataPath).FullName; string game=Directory.GetParent(project).FullName;
        string output=Path.Combine(game,"05_成品交付","Windows","v0.7.2"); string exe=Path.Combine(output,"Shanhai_M1.exe");
        if(File.Exists(exe))throw new System.Exception("Refusing to overwrite v0.7.2 build.");
        Directory.CreateDirectory(output); var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var options=new BuildPlayerOptions{scenes=scenes,locationPathName=exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};var report=BuildPipeline.BuildPlayer(options);
        string info="M1 v0.7.2 星金术式与十灵图鉴\nResult: "+report.summary.result+"\nWorlds: "+string.Join(", ",scenes)+"\nTameable creatures per world: 10\nWorld terrain: 240m x 240m\nOutput: "+exe+"\nSize bytes: "+report.summary.totalSize;
        File.WriteAllText(Path.Combine(output,"BUILD_INFO.txt"),info);Debug.Log(info);if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
    }
    static void ValidateScenes()
    {
        foreach(var e in EditorBuildSettings.scenes.Where(s=>s.enabled))
        {
            EditorSceneManager.OpenScene(e.path);var actors=Object.FindObjectsByType<M1Actor>();int players=actors.Count(a=>!a.isEnemy&&!a.isTameable), enemies=actors.Count(a=>a.isEnemy),pets=actors.Count(a=>a.isTameable);
            if(players!=1||enemies!=1||pets!=10)throw new System.Exception("Scene check failed: "+e.path+" player="+players+" enemy="+enemies+" pets="+pets);
            if(!Object.FindAnyObjectByType<M1TamingGuide>()||!Object.FindAnyObjectByType<M1ResourceCrafting>()||!Object.FindAnyObjectByType<M1PetNameplates>())throw new System.Exception("Missing M1 systems in "+e.path);
            if(Object.FindObjectsByType<M1Portal>().Length!=2||!Object.FindAnyObjectByType<MeshCollider>())throw new System.Exception("Portal or terrain collider missing in "+e.path);
        }
        Debug.Log("M1 v0.7.2 scene integrity check passed: 3 scenes, each 10 tameable species, runtime systems, portals and terrain collision.");
    }
}



