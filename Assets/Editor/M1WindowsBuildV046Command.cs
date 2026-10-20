using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class M1WindowsBuildV046Command
{
    public static void Build()
    {
        M1WorldBuilderV046Rendered.BuildAll();
        string outputDir = @"D:\游戏开发\项目\山海驭灵录_M1\05_成品交付\Windows\v0.4.6";
        if (Directory.Exists(outputDir)) throw new Exception("Refusing to overwrite existing version folder: " + outputDir);
        Directory.CreateDirectory(outputDir);
        string exe = Path.Combine(outputDir, "Shanhai_M1.exe");
        string[] scenes =
        {
            "Assets/Scenes/v0.4.6_Rendered/M1_HubWorld.unity",
            "Assets/Scenes/v0.4.6_Rendered/M1_ForestWorld.unity",
            "Assets/Scenes/v0.4.6_Rendered/M1_CanyonWorld.unity"
        };
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=scenes,
            locationPathName=exe,
            target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None
        });
        string info="Version: v0.4.6 Rendered A · 青金营地\nResult: "+report.summary.result+
            "\nScene count: "+scenes.Length+"\nOutput: "+exe+
            "\nSize bytes: "+report.summary.totalSize;
        File.WriteAllText(Path.Combine(outputDir,"BUILD_INFO.txt"),info);
        Debug.Log(info);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("v0.4.6 build failed: "+report.summary.result);
    }
}

