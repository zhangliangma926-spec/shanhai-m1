using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M1WindowsBuildV075Command
{
    public static void Build()
    {
        M1WorldBuilderV075.BuildAll();ValidateScenes();
        string project=Directory.GetParent(Application.dataPath).FullName;string game=Directory.GetParent(project).FullName;
        string root=Path.Combine(game,"05_成品交付","Windows","v0.7.5");string a=Path.Combine(root,"A_固定中心准星");string b=Path.Combine(root,"B_鼠标移动准星");
        if(Directory.Exists(a)||Directory.Exists(b))throw new Exception("Refusing to overwrite existing A/B v0.7.5 output folders.");
        Directory.CreateDirectory(a);Directory.CreateDirectory(b);
        string exe=Path.Combine(a,"Shanhai_M1.exe");var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("A build failed: "+report.summary.result);
        CopyDirectory(a,b);File.WriteAllText(Path.Combine(a,"aim-mode.txt"),"center\n");File.WriteAllText(Path.Combine(b,"aim-mode.txt"),"mouse\n");
        string common="M1 v0.7.5 · Shared movement, combat and model upgrades\nScenes: "+string.Join(", ",scenes)+"\nWorld size: 320m x 320m\nTameable species per world: 16\nBuild size bytes: "+report.summary.totalSize+"\n";
        File.WriteAllText(Path.Combine(a,"BUILD_INFO.txt"),common+"Variant: A · fixed center reticle\nAim mode: center\n");
        File.WriteAllText(Path.Combine(b,"BUILD_INFO.txt"),common+"Variant: B · movable mouse reticle\nAim mode: mouse\n");
        Debug.Log("M1 v0.7.5 A/B builds succeeded. A=screen center; B=mouse pointer; shared 16-creature, 320m worlds.");
    }
    static void CopyDirectory(string source,string destination)
    {
        foreach(string dir in Directory.GetDirectories(source,"*",SearchOption.AllDirectories))Directory.CreateDirectory(dir.Replace(source,destination));
        foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories))File.Copy(file,file.Replace(source,destination),true);
    }
    static void ValidateScenes()
    {
        foreach(var e in EditorBuildSettings.scenes.Where(s=>s.enabled))
        {
            EditorSceneManager.OpenScene(e.path);var actors=UnityEngine.Object.FindObjectsByType<M1Actor>();int players=actors.Count(x=>!x.isEnemy&&!x.isTameable),enemies=actors.Count(x=>x.isEnemy),pets=actors.Count(x=>x.isTameable);
            if(players!=1||enemies!=1||pets!=16)throw new Exception("Scene check failed "+e.path+" player="+players+" enemy="+enemies+" pets="+pets);
            if(!UnityEngine.Object.FindAnyObjectByType<M1ChargeWeaponSystem>()||!UnityEngine.Object.FindAnyObjectByType<M1GameManager>()||!UnityEngine.Object.FindAnyObjectByType<M1PetNameplates>())throw new Exception("Missing runtime systems in "+e.path);
            if(UnityEngine.Object.FindObjectsByType<M1Portal>().Length!=2||!UnityEngine.Object.FindAnyObjectByType<MeshCollider>())throw new Exception("Portal or terrain collision missing in "+e.path);
        }
        Debug.Log("v0.7.5 scenes passed: 3 worlds, 16 pets each, player, enemy, portals and collision.");
    }
}