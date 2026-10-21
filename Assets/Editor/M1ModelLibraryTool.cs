// 游戏模型库（项目规则，2026-10-20 起）：所有模型统一存放在 工程外层\游戏素材\游戏模型\，
// 文件名 = 游戏里的名字（灵兽名、动物名、场景物件中文名），旁边放同名 .png 渲染图；用 Blender 做的模型同时放同名 .blend。
// 菜单 Shanhai → Tools → 更新游戏模型库；命令行：-executeMethod M1ModelLibraryTool.Run。新增或替换模型后运行一次即可。
using System.Collections.Generic; using System.IO; using System.Linq; using System.Text; using System.Text.RegularExpressions;
using UnityEditor; using UnityEditor.Animations; using UnityEditor.SceneManagement; using UnityEngine;
using A = M1AssetSetupV077;
public static class M1ModelLibraryTool
{
    const int Layer = 31;
    static string lib;
    static readonly StringBuilder index = new StringBuilder();
    static readonly HashSet<string> done = new HashSet<string>();

    [MenuItem("Shanhai/Tools/更新游戏模型库")]
    public static void Run()
    {
        lib = Path.Combine(Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName, "游戏素材", "游戏模型");
        index.Clear(); done.Clear(); // 编辑器里重复运行时不累加
        index.AppendLine("类别\t名字\t工程内源文件");
        foreach (var scene in new[] { M1WorldBuilderV077.Hub, M1WorldBuilderV077.Forest, M1WorldBuilderV077.Canyon })
        {
            EditorSceneManager.OpenScene(scene);
            foreach (var a in Object.FindObjectsByType<M1Actor>(FindObjectsSortMode.None))
            {
                string cat = a.GetComponent<M1PlayerController>() ? "主角" : a.isEnemy ? "敌人" : a.isTameable ? "灵兽" : null; if (cat == null) continue;
                string name = cat == "敌人" ? "星金营地守卫" : a.displayName;
                Shoot(cat, name, a.transform, a.GetComponent<M1FlyerMotion>() ? "Flying" : "Idle");
            }
            foreach (var c in Object.FindObjectsByType<M1AmbientCritter>(FindObjectsSortMode.None))
                Shoot("氛围动物", c.name.Split('·')[0].Trim(), c.transform, c.mode == M1AmbientCritter.Mode.Circle ? "Flying" : "Idle");
            // 场景物件：每个模型文件单独摆一份渲染
            if (scene == M1WorldBuilderV077.Hub)
            {
                foreach (var p in A.FbxIn(A.Nature)) Prop("场景/自然", p);
                foreach (var p in A.FbxIn(A.Kenney)) Prop("场景/道具", p);
            }
        }
        File.WriteAllText(Path.Combine(lib, "索引.tsv"), index.ToString(), new UTF8Encoding(true));
        CopyModels();
        Debug.Log("游戏模型库已更新：" + lib);
    }

    /// 按索引把模型文件复制成“中文名.fbx”；在 01_资产来源 里找到同名的 Blender 源文件就一起复制成“中文名.blend”；贴图复制到 贴图\
    static void CopyModels()
    {
        string project = Directory.GetParent(Application.dataPath).FullName, root = Directory.GetParent(project).FullName;
        var blends = Directory.Exists(Path.Combine(root, "01_资产来源")) ? Directory.GetFiles(Path.Combine(root, "01_资产来源"), "*.blend", SearchOption.AllDirectories) : new string[0];
        foreach (var line in index.ToString().Split('\n').Skip(1))
        {
            var cols = line.TrimEnd('\r').Split('\t'); if (cols.Length < 3 || !cols[2].EndsWith(".fbx")) continue;
            string dir = Path.Combine(lib, cols[0].Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)); Directory.CreateDirectory(dir);
            File.Copy(Path.Combine(project, cols[2]), Path.Combine(dir, cols[1] + ".fbx"), true);
            var blend = blends.FirstOrDefault(b => Path.GetFileNameWithoutExtension(b) == Path.GetFileNameWithoutExtension(cols[2]));
            if (blend != null) File.Copy(blend, Path.Combine(dir, cols[1] + ".blend"), true);
        }
        string tex = Path.Combine(lib, "贴图"); Directory.CreateDirectory(tex);
        foreach (var t in Directory.GetFiles(Path.Combine(project, A.Textures.Replace('/', Path.DirectorySeparatorChar)), "*.png")) File.Copy(t, Path.Combine(tex, Path.GetFileName(t)), true);
    }

    static string SourceOf(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (src) { var path = AssetDatabase.GetAssetPath(src); if (path.EndsWith(".fbx") || path.EndsWith(".FBX")) return path; }
        }
        return "（几何体拼装，无模型文件）";
    }

    static void Shoot(string cat, string name, Transform actor, string clip)
    {
        string key = cat + "/" + name; if (done.Contains(key)) return; done.Add(key);
        index.AppendLine(cat + "\t" + name + "\t" + SourceOf(actor));
        var copy = Object.Instantiate(actor.gameObject, new Vector3(20000, 0, 20000), actor.rotation); // 复制一份到远处单独拍，保留游戏里的染色
        foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        var anim = copy.GetComponentInChildren<Animator>();
        if (anim && anim.runtimeAnimatorController is AnimatorController ac)
        {
            var c = ac.animationClips.FirstOrDefault(x => Short(x.name) == clip) ?? ac.animationClips.FirstOrDefault(x => Short(x.name) == "Idle");
            if (c) c.SampleAnimation(anim.gameObject, c.length * .3f);
        }
        Render(copy, Path.Combine(lib, cat, name + ".png"), copy.transform.forward, copy.transform.right);
        Object.DestroyImmediate(copy);
    }

    static void Prop(string cat, string fbx)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx); if (!prefab) return;
        string name = Cn(Path.GetFileNameWithoutExtension(fbx));
        index.AppendLine(cat.Replace('/', '\\') + "\t" + name + "\t" + fbx);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.position = new Vector3(20000, 0, 20000);
        A.ApplyTextures(go);
        Render(go, Path.Combine(lib, cat, name + ".png"), Vector3.forward, Vector3.right);
        Object.DestroyImmediate(go);
    }

    static void Render(GameObject go, string file, Vector3 fwd, Vector3 right)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var camGo = new GameObject("TmpCam"); var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.80f, .84f, .86f); cam.cullingMask = 1 << Layer; cam.fieldOfView = 30f; cam.nearClipPlane = .01f; cam.farClipPlane = 1000f;
        float size = Mathf.Max(b.size.magnitude, .1f);
        var dir = (fwd.normalized * .75f + right.normalized * .55f + Vector3.up * .35f).normalized; // 侧前方 3/4 视角，能看到脸
        camGo.transform.position = b.center + dir * size * 1.9f; camGo.transform.LookAt(b.center);
        var lightGo = new GameObject("TmpLight"); var l = lightGo.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = .9f; l.cullingMask = 1 << Layer; lightGo.transform.rotation = Quaternion.LookRotation(-dir + Vector3.down * .6f);
        var rt = new RenderTexture(768, 768, 24) { antiAliasing = 4 }; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(768, 768, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 768, 768), 0, 0); tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(file)); File.WriteAllBytes(file, tex.EncodeToPNG());
        RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(camGo); Object.DestroyImmediate(lightGo);
    }

    static string Short(string n) { int i = n.LastIndexOf('|'); return i >= 0 ? n.Substring(i + 1) : n; }

    // 场景物件的中文名（带编号的保留编号）
    static readonly Dictionary<string, string> CnMap = new Dictionary<string, string>
    {
        {"Bush_Common","灌木"},{"Bush_Common_Flowers","开花灌木"},{"Clover","三叶草"},{"CommonTree","阔叶树"},{"DeadTree","枯树"},{"Fern","蕨类"},
        {"Flower_3_Group","花丛甲"},{"Flower_3_Single","单花甲"},{"Flower_4_Group","花丛乙"},{"Flower_4_Single","单花乙"},
        {"Grass_Common_Short","矮草"},{"Grass_Common_Tall","高草"},{"Grass_Wispy_Short","细矮草"},{"Grass_Wispy_Tall","细高草"},
        {"Mushroom_Common","蘑菇"},{"Mushroom_Laetiporus","硫磺菌"},{"Pebble_Round","圆石子"},{"Pebble_Square","方石子"},{"Petal","花瓣"},{"Pine","松树"},
        {"Plant_1","植物甲"},{"Plant_1_Big","大植物甲"},{"Plant_7","植物乙"},{"Plant_7_Big","大植物乙"},
        {"RockPath_Round_Small","小圆石板"},{"RockPath_Round_Thin","窄圆石板"},{"RockPath_Round_Wide","宽圆石板"},{"RockPath_Square_Small","小方石板"},{"RockPath_Square_Thin","窄方石板"},{"RockPath_Square_Wide","宽方石板"},
        {"Rock_Medium","岩石"},{"TwistedTree","扭曲树"},
        {"bridge_wood","木桥"},{"campfire_logs","篝火"},{"cliff_block_rock","方块崖壁"},{"cliff_large_rock","大崖壁"},{"crops_bambooStage","灵竹"},{"fence_simple","篱笆"},
        {"lily_large","莲叶"},{"log","木座"},{"log_large","倒木"},{"log_stackLarge","木堆"},{"mushroom_redGroup","红蘑菇丛"},{"mushroom_tanGroup","棕蘑菇丛"},
        {"rock_tall","高石柱"},{"sign","路牌"},{"statue_column","古柱"},{"statue_obelisk","方尖碑"},{"statue_ring","石环祭坛"},{"stone_tall","高岩石"},
        {"tent_detailedOpen","开口帐篷"},{"tent_smallClosed","小帐篷"},{"tree_detailed_fall","秋色细叶树"},{"tree_fat_fall","秋色胖树"},{"tree_oak_fall","秋色橡树"},
    };
    static string Cn(string n)
    {
        if (CnMap.TryGetValue(n, out var full)) return full;
        var m = Regex.Match(n, @"^(.*)_(\d+)$");
        if (m.Success && CnMap.TryGetValue(m.Groups[1].Value, out var baseName)) return baseName + m.Groups[2].Value;
        return n;
    }
}
