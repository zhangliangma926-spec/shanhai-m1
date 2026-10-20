using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class M1WindowsBuildV04Command
{
 public static void Build(){string root=Directory.GetParent(Application.dataPath).FullName;string game=Directory.GetParent(root).FullName;string outDir=Path.Combine(game,"05_成品交付","Windows","v0.4");if(File.Exists(Path.Combine(outDir,"Shanhai_M1.exe")))throw new Exception("Refusing to overwrite v0.4 build.");Directory.CreateDirectory(outDir);var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();if(scenes.Length!=3)throw new Exception("Expected three v0.4 worlds; got "+scenes.Length);var options=new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(outDir,"Shanhai_M1.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};var report=BuildPipeline.BuildPlayer(options);var info="Version: M1 v0.4\nResult: "+report.summary.result+"\nScene count: "+scenes.Length+"\nScenes: "+string.Join(", ",scenes)+"\nOutput: "+options.locationPathName+"\nSize bytes: "+report.summary.totalSize;File.WriteAllText(Path.Combine(outDir,"BUILD_INFO.txt"),info);Debug.Log(info);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);}
}
