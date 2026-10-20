using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class M1ChoiceBuildsCommand
{
 public static void BuildA(){Build("v0.4.1","A_青金营地",new[]{"Assets/Scenes/v0.4/M1_HubWorld.unity","Assets/Scenes/v0.4/M1_ForestWorld.unity","Assets/Scenes/v0.4/M1_CanyonWorld.unity"});}
 public static void BuildB(){Build("v0.5.1","B_秋灯山海",new[]{"Assets/Scenes/v0.5/M1_HubWorld.unity","Assets/Scenes/v0.5/M1_ForestWorld.unity","Assets/Scenes/v0.5/M1_CanyonWorld.unity"});}
 public static void BuildC(){Build("v0.6.1","C_月影灵境",new[]{"Assets/Scenes/v0.6/M1_HubWorld.unity","Assets/Scenes/v0.6/M1_ForestWorld.unity","Assets/Scenes/v0.6/M1_CanyonWorld.unity"});}
 static void Build(string folder,string label,string[] scenes){string root=Directory.GetParent(Application.dataPath).FullName;string game=Directory.GetParent(root).FullName;string outDir=Path.Combine(game,"05_成品交付","Windows",folder);string exe=Path.Combine(outDir,"Shanhai_M1.exe");if(File.Exists(exe))throw new Exception("Refusing to overwrite existing build: "+exe);foreach(var s in scenes)if(!File.Exists(s))throw new Exception("Scene is missing: "+s);Directory.CreateDirectory(outDir);var options=new BuildPlayerOptions{scenes=scenes,locationPathName=exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};var report=BuildPipeline.BuildPlayer(options);var info="Version: "+folder+" "+label+"\nResult: "+report.summary.result+"\nScene count: "+scenes.Length+"\nScenes: "+string.Join(", ",scenes)+"\nOutput: "+exe+"\nSize bytes: "+report.summary.totalSize;File.WriteAllText(Path.Combine(outDir,"BUILD_INFO.txt"),info);Debug.Log(info);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);}
}
