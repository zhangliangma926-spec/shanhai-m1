using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class M1WindowsBuildV031Command
{
    public static void Build()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string gameRoot = Directory.GetParent(projectRoot).FullName;
        string outputDir = Path.Combine(gameRoot, "05_成品交付", "Windows", "v0.3.1");
        if (File.Exists(Path.Combine(outputDir, "Shanhai_M1.exe"))) throw new Exception("Refusing to overwrite v0.3.1 build.");
        Directory.CreateDirectory(outputDir);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 3) throw new Exception("Expected exactly three enabled mini-world scenes, found " + scenes.Length);
        var options = new BuildPlayerOptions { scenes = scenes, locationPathName = Path.Combine(outputDir, "Shanhai_M1.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        string summary = "Version: M1 v0.3.1" + Environment.NewLine + "Result: " + report.summary.result + Environment.NewLine + "Scene count: " + scenes.Length + Environment.NewLine + "Scenes: " + string.Join(", ", scenes) + Environment.NewLine + "Output: " + options.locationPathName + Environment.NewLine + "Size bytes: " + report.summary.totalSize + Environment.NewLine;
        File.WriteAllText(Path.Combine(outputDir, "BUILD_INFO.txt"), summary);
        Debug.Log(summary);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
    }
}
