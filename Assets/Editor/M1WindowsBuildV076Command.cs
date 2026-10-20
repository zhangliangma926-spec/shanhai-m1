using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// v0.7.6 一键构建：复用现有的 v0.7.5 三张场景（不重建世界），编译出 A/B 两个 Windows 版本。
/// 菜单：Shanhai → Build v0.7.6 · UI 美化版（A/B 两个 exe）
/// 输出位置：工程上一级目录\05_成品交付\Windows\v0.7.6\ ；已存在时自动加时间戳，不覆盖旧文件。
/// </summary>
public static class M1WindowsBuildV076Command
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/v0.7.5/M1_HubWorld.unity",
        "Assets/Scenes/v0.7.5/M1_ForestWorld.unity",
        "Assets/Scenes/v0.7.5/M1_CanyonWorld.unity",
    };

    [MenuItem("Shanhai/Build v0.7.6 · UI 美化版（A/B 两个 exe）")]
    public static void BuildFromMenu()
    {
        try { string root = Build(); EditorUtility.RevealInFinder(root); EditorUtility.DisplayDialog("构建完成", "A/B 两个版本已生成：\n" + root + "\n\n进入任一文件夹双击 Shanhai_M1.exe 即可试玩。", "好的"); }
        catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("构建失败", e.Message + "\n\n请把 Console 窗口里的红色报错截图发给 Claude。", "知道了"); }
    }

    /// 也可用命令行批处理调用：-executeMethod M1WindowsBuildV076Command.Build
    public static string Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new Exception("已取消：有未保存的场景。");
        foreach (var s in Scenes) if (!File.Exists(s)) throw new Exception("找不到场景 " + s + "。请先运行菜单 Shanhai/Build v0.7.5 生成场景。");
        EditorBuildSettings.scenes = Scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
        ValidateScenes();

        string project = Directory.GetParent(Application.dataPath).FullName;
        string game = Directory.GetParent(project).FullName;
        string root = Path.Combine(game, "05_成品交付", "Windows", "v0.7.6");
        if (Directory.Exists(root)) root += "_" + DateTime.Now.ToString("MMdd_HHmm"); // 不覆盖已有输出
        string a = Path.Combine(root, "A_固定中心准星"), b = Path.Combine(root, "B_鼠标移动准星");
        Directory.CreateDirectory(a);

        EditorUtility.DisplayProgressBar("山海驭灵录 v0.7.6", "正在编译 Windows 版本，可能需要几分钟…", .3f);
        try
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            { scenes = Scenes, locationPathName = Path.Combine(a, "Shanhai_M1.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("编译失败：" + report.summary.result + "（详见 Console）");

            EditorUtility.DisplayProgressBar("山海驭灵录 v0.7.6", "复制 B 版本…", .85f);
            CopyDirectory(a, b);
            File.WriteAllText(Path.Combine(a, "aim-mode.txt"), "center\n");
            File.WriteAllText(Path.Combine(b, "aim-mode.txt"), "mouse\n");
            string info = "M1 v0.7.6 · UI 美化 + 性能优化 + 重开局清零修复\nBuilt: " + DateTime.Now + "\nBuild size bytes: " + report.summary.totalSize + "\n";
            File.WriteAllText(Path.Combine(a, "BUILD_INFO.txt"), info + "Variant: A · fixed center reticle\n");
            File.WriteAllText(Path.Combine(b, "BUILD_INFO.txt"), info + "Variant: B · movable mouse reticle\n");
        }
        finally { EditorUtility.ClearProgressBar(); }
        Debug.Log("M1 v0.7.6 A/B builds succeeded: " + root);
        return root;
    }

    static void CopyDirectory(string source, string destination)
    {
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(dir.Replace(source, destination));
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories)) File.Copy(file, file.Replace(source, destination), true);
    }

    static void ValidateScenes()
    {
        foreach (var path in Scenes)
        {
            EditorSceneManager.OpenScene(path);
            var actors = UnityEngine.Object.FindObjectsByType<M1Actor>();
            int players = actors.Count(x => !x.isEnemy && !x.isTameable), enemies = actors.Count(x => x.isEnemy), pets = actors.Count(x => x.isTameable);
            if (players != 1 || enemies != 1 || pets != 16) throw new Exception("场景检查未通过 " + path + " player=" + players + " enemy=" + enemies + " pets=" + pets);
            if (!UnityEngine.Object.FindAnyObjectByType<M1GameManager>() || !UnityEngine.Object.FindAnyObjectByType<M1ChargeWeaponSystem>()) throw new Exception("场景缺少核心系统：" + path);
        }
        EditorSceneManager.OpenScene(Scenes[0]);
    }
}
