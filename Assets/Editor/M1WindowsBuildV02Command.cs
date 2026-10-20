using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class M1WindowsBuildV02Command
{
    public static void Build()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string gameRoot = Directory.GetParent(projectRoot).FullName;
        string outputDir = Path.Combine(gameRoot, "05_成品交付", "Windows", "v0.2");
        Directory.CreateDirectory(outputDir);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new Exception("No enabled scenes are listed in Build Settings.");
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, "Shanhai_M1.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        string summary = "Version: M1 v0.2" + Environment.NewLine +
                         "Result: " + report.summary.result + Environment.NewLine +
                         "Scene count: " + scenes.Length + Environment.NewLine +
                         "Output: " + options.locationPathName + Environment.NewLine +
                         "Size bytes: " + report.summary.totalSize + Environment.NewLine;
        File.WriteAllText(Path.Combine(outputDir, "BUILD_INFO.txt"), summary);
        Debug.Log(summary);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
    }
}
