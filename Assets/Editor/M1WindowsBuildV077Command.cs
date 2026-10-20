using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// v0.7.7 一键出包：生成三张 640 米新场景（3D 主角/异兽/风格化植被），编译 B 版（鼠标准星），完成后自动启动游戏。
/// 菜单：Shanhai → Build v0.7.7 · 3D 角色 + 大地图（B 版）
/// 输出：工程上一级目录\05_成品交付\Windows\v0.7.7\ （已存在时自动加时间戳，不覆盖旧文件）
/// </summary>
public static class M1WindowsBuildV077Command
{
    [MenuItem("Shanhai/Build v0.7.7 · 3D 角色 + 大地图（B 版）", priority = 0)]
    public static void BuildFromMenu()
    {
        try
        {
            string exe = Build();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = true });
            Debug.Log("v0.7.7 已启动试玩：" + exe);
        }
        catch (Exception e) { EditorUtility.ClearProgressBar(); Debug.LogException(e); EditorUtility.DisplayDialog("构建失败", e.Message + "\n\n请把 Console 窗口里的红色报错截图发给 Claude。", "知道了"); }
    }

    /// 命令行：-executeMethod M1WindowsBuildV077Command.Build
    public static string Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new Exception("已取消：有未保存的场景。");
        EditorUtility.DisplayProgressBar("山海驭灵录 v0.7.7", "生成 640 米世界、摆放 3D 模型…（约 1–3 分钟）", .15f);
        M1WorldBuilderV077.BuildAll();
        var scenes = new[] { M1WorldBuilderV077.Hub, M1WorldBuilderV077.Forest, M1WorldBuilderV077.Canyon };
        Validate(scenes);

        string project = Directory.GetParent(Application.dataPath).FullName, game = Directory.GetParent(project).FullName;
        string root = Path.Combine(game, "05_成品交付", "Windows", "v0.7.7");
        if (Directory.Exists(root)) root += "_" + DateTime.Now.ToString("MMdd_HHmm");
        string dir = Path.Combine(root, "山海驭灵录_v0.7.7"); Directory.CreateDirectory(dir);
        string exe = Path.Combine(dir, "Shanhai_M1.exe");

        EditorUtility.DisplayProgressBar("山海驭灵录 v0.7.7", "编译 Windows 版本…（几分钟）", .45f);
        try
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = exe, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("编译失败：" + report.summary.result + "（详见 Console）");
            File.WriteAllText(Path.Combine(dir, "aim-mode.txt"), "mouse\n");
            File.WriteAllText(Path.Combine(dir, "BUILD_INFO.txt"),
                "M1 v0.7.7 · 3D 骨骼动画主角与异兽 · 640m 世界 · 风格化植被 · 流畅度优化\nBuilt: " + DateTime.Now + "\nAim: mouse (B)\nBuild size bytes: " + report.summary.totalSize + "\n" +
                "Assets: Quaternius (CC0), Kenney (CC0)\n");
        }
        finally { EditorUtility.ClearProgressBar(); }
        return exe;
    }

    // 每个场景的野生异兽数量，必须与 M1WorldBuilderV077 里的 Beasts 物种表一致（v0.7.8 起为 20 种）
    const int ExpectedPets = 20;

    static void Validate(string[] scenes)
    {
        foreach (var path in scenes)
        {
            EditorSceneManager.OpenScene(path);
            var actors = UnityEngine.Object.FindObjectsByType<M1Actor>();
            int players = actors.Count(x => !x.isEnemy && !x.isTameable), enemies = actors.Count(x => x.isEnemy), pets = actors.Count(x => x.isTameable);
            if (players != 1 || enemies != 1 || pets != ExpectedPets) throw new Exception("场景检查未通过 " + path + " player=" + players + " enemy=" + enemies + " pets=" + pets + "（期望 pets=" + ExpectedPets + "）");
            if (!UnityEngine.Object.FindAnyObjectByType<M1GameManager>() || UnityEngine.Object.FindObjectsByType<M1Portal>().Length != 2) throw new Exception("场景缺少核心系统或传送门：" + path);
        }
        EditorSceneManager.OpenScene(scenes[0]);
    }
}
