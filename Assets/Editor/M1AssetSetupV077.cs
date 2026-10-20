using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// v0.7.7 素材准备：
/// 1. 配置 Quaternius 角色/动物 FBX 的导入方式（Generic 骨骼、哪些动作循环）。
/// 2. 按材质名自动套贴图（主角、风格化自然包），叶片/花草开启镂空。
/// 3. 为每个带动画的模型自动生成 Animator Controller（待机/走/跑混合 + 攻击/受击/翻滚/持杖施法）。
/// 4. 按物种给同一模型调出不同毛色（材质另存，不改原始模型）。
/// 全部素材均为 CC0，来源见 01_资产来源/README_素材说明.md。
/// </summary>
public static class M1AssetSetupV077
{
    public const string Root = "Assets/Art/Models/v077/";
    public const string Characters = Root + "Characters/", Creatures = Root + "Creatures/", Nature = Root + "Nature/", Kenney = Root + "Kenney/", Textures = Root + "Textures/";
    // v0.8.0：Quaternius「Ultimate Animated Animals」（CC0），带 Walk / Gallop / Attack / HitReact 等完整动作
    public const string Animals = "Assets/Art/Models/v080/Animals/";
    // v0.8.0：用 Blender 脚本绑骨、做动作的灵兽模型（岚角灵兽等）
    public const string Beasts = "Assets/Art/Models/v080/Beasts/";
    // v0.8.3：阶段 C 用 Blender 做的模型与动作（主角走跑、灵蝶、三种灵鸟、苍鹰、灵龟、猫系三种、猪冲撞），来源见目录里的 ATTRIBUTION_清单.md
    public const string V083 = "Assets/Art/Models/v083/";
    const string MatDir = "Assets/Art/Materials/v0.7.7/", CtrlDir = "Assets/Art/Animators/v0.7.7/";

    static readonly string[] LoopKeys = { "idle", "walk", "run", "gallop", "fly", "glide", "swim", "jump", "eating" };
    static readonly string[] OneShotKeys = { "death", "attack", "hit", "roll", "pickup", "spell", "punch", "bite", "spawn" };

    // ---------- 1. 导入设置 ----------
    public static void ConfigureImports()
    {
        foreach (var dir in new[] { Characters, Creatures, Animals, Beasts, V083 })
            foreach (var path in FbxIn(dir)) ConfigureAnimatedModel(path);
        ConfigureAnimatedModel("Assets/Art/Models/External_Quaternius/BlueDemon.fbx"); // 旧版的幽蓝翼兽，同为 Quaternius 模型
        foreach (var dir in new[] { Nature, Kenney })
            foreach (var path in FbxIn(dir)) ConfigureStaticModel(path);
        AssetDatabase.Refresh();
    }

    public static IEnumerable<string> FbxIn(string dir) =>
        Directory.Exists(dir) ? Directory.GetFiles(dir, "*.fbx").Select(p => p.Replace('\\', '/')) : Enumerable.Empty<string>();

    static void ConfigureAnimatedModel(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter; if (!imp) return;
        bool dirty = false;
        if (imp.animationType != ModelImporterAnimationType.Generic) { imp.animationType = ModelImporterAnimationType.Generic; dirty = true; }
        if (!imp.importAnimation) { imp.importAnimation = true; dirty = true; }
        if (dirty) imp.SaveAndReimport();
        var clips = imp.defaultClipAnimations; if (clips == null || clips.Length == 0) return;
        foreach (var c in clips)
        {
            string n = Short(c.name).ToLowerInvariant();
            bool oneShot = OneShotKeys.Any(n.Contains) && (!n.Contains("idle") || n.Contains("hitreact")); // Idle_HitReact 是受击动作，只播一次
            c.loopTime = !oneShot && LoopKeys.Any(n.Contains);
            c.loopPose = false;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    static void ConfigureStaticModel(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter; if (!imp) return;
        if (imp.importAnimation || imp.animationType != ModelImporterAnimationType.None)
        { imp.importAnimation = false; imp.animationType = ModelImporterAnimationType.None; imp.SaveAndReimport(); }
    }

    public static string Short(string clipName) { int i = clipName.LastIndexOf('|'); return i >= 0 ? clipName.Substring(i + 1) : clipName; }

    /// 模型是否带奔跑类动作（Run / Gallop / Running）。没有的话，动画驱动会把走路动作加快播放来跟上速度。
    public static bool HasRunClip(string fbxPath) =>
        AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().Any(c => { var n = Short(c.name).ToLowerInvariant(); return !c.name.StartsWith("__preview__") && (n == "run" || n.StartsWith("gallop") && !n.Contains("jump") || n.Contains("running")); });

    // ---------- 2. 贴图材质 ----------
    static readonly Dictionary<string, string> TexAlias = new Dictionary<string, string>
    {
        { "PathRocks", "PathRocks_Diffuse" }, { "Rocks", "Rocks_Diffuse" }, { "Leaves_Pine", "Leaf_Pine" },
    };
    // v0.7.8：材质名叫 "Material" 的很多模型都有（猪、骷髅等），原先的别名会误套树叶贴图。
    // 现在只在 Nature 自然包里把 "Material" 映射为扭曲树叶贴图。
    const string NatureOnlyAlias = "Material", NatureOnlyTexture = "Leaves_TwistedTree";
    // v0.8.0：风格化自然包的树叶贴图只是白色叶形遮罩，颜色原本在作者的 Blender 着色器里，导出 FBX 后丢失，
    // 之前材质底色设成白色，导致树叶、灌木整片发白。这里按叶子种类补上底色（贴图仍负责叶形和镂空）。
    static readonly Dictionary<string, Color> LeafTint = new Dictionary<string, Color>
    {
        { "Leaves_NormalTree", new Color(.40f, .64f, .25f) },   // 普通阔叶树：鲜绿
        { "Leaves_TwistedTree", new Color(.52f, .66f, .27f) },  // 扭曲树、灌木：偏黄的绿
        { "Leaves_Pine", new Color(.22f, .44f, .27f) },         // 松针：深绿
        { "Leaves", new Color(.38f, .62f, .27f) },              // 三叶草等小植物
    };
    static readonly Dictionary<string, Material> texturedCache = new Dictionary<string, Material>();

    /// 把实例上的材质换成“同名贴图”的材质（主角、风格化自然包）。找不到贴图的材质保持原样。
    public static void ApplyTextures(GameObject go)
    {
        var srcObj = PrefabUtility.GetCorrespondingObjectFromSource(go);
        bool nature = srcObj && AssetDatabase.GetAssetPath(srcObj).Contains("/Nature/");
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                var m = TexturedMaterial(mats[i].name, nature); if (m) { mats[i] = m; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    static Material TexturedMaterial(string matName, bool natureContext)
    {
        matName = matName.Replace(" (Instance)", "");
        if (matName == NatureOnlyAlias && !natureContext) return null;
        string cacheKey = matName + (natureContext ? "#nature" : "");
        if (texturedCache.TryGetValue(cacheKey, out var cached) && cached) return cached;
        string texName = TexAlias.TryGetValue(matName, out var alias) ? alias : matName == NatureOnlyAlias ? NatureOnlyTexture : matName;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + texName + ".png");
        if (!tex) { texturedCache[cacheKey] = null; return null; }
        Directory.CreateDirectory(MatDir);
        string path = MatDir + "Tex_" + matName + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        string tintKey = matName == NatureOnlyAlias ? NatureOnlyTexture : matName;
        m.mainTexture = tex; m.color = LeafTint.TryGetValue(tintKey, out var leaf) ? leaf : Color.white; m.SetFloat("_Glossiness", .12f); m.SetFloat("_Metallic", 0f);
        bool cutout = matName.StartsWith("Leaves") || matName.StartsWith("Leaf") || matName == "Grass" || matName == "Flowers" || (matName == NatureOnlyAlias && natureContext);
        if (cutout)
        {
            m.SetFloat("_Mode", 1f); m.SetFloat("_Cutoff", .45f); m.SetOverrideTag("RenderType", "TransparentCutout");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero); m.SetInt("_ZWrite", 1);
            m.EnableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        m.enableInstancing = true; EditorUtility.SetDirty(m);
        texturedCache[cacheKey] = m; return m;
    }

    // ---------- 3. 按物种调色 ----------
    static readonly string[] KeepColorKeys = { "eye", "black", "claw", "beak", "nose", "pink" };

    public static void Tint(GameObject go, string key, Color tint, float strength = .82f)
    {
        if (tint == Color.white) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i]; if (!src || !src.HasProperty("_Color")) continue;
                string lower = src.name.ToLowerInvariant(); if (KeepColorKeys.Any(lower.Contains)) continue;
                Directory.CreateDirectory(MatDir);
                string path = MatDir + "Tint_" + key + "_" + Safe(src.name) + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
                Color c = src.color; float g = c.grayscale;
                m.color = Color.Lerp(c, tint * (.45f + .8f * g), strength); m.SetFloat("_Glossiness", .18f); m.enableInstancing = true;
                EditorUtility.SetDirty(m); mats[i] = m;
            }
            r.sharedMaterials = mats;
        }
    }
    static string Safe(string s) { foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_'); return s.Replace(' ', '_').Replace('.', '_'); }

    // ---------- 4. 动画控制器 ----------
    static readonly Dictionary<string, RuntimeAnimatorController> ctrlCache = new Dictionary<string, RuntimeAnimatorController>();

    public static RuntimeAnimatorController Controller(string fbxPath, bool player)
    {
        string key = fbxPath + (player ? "#player" : "");
        if (ctrlCache.TryGetValue(key, out var cached) && cached) return cached;
        var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        if (clips.Length == 0) return null;

        var idle = FindClip(clips, "Idle", "Skeleton_Idle", "Slime_Idle", "Dragon_Flying", "Bat_Flying", "Flying", "Swimming") ?? clips[0];
        var walk = FindClip(clips, "Walk", "Walking", "Slime_Walk", "WalkSlow", "Skeleton_Running", "Dragon_Flying", "Bat_Flying", "Flying", "Jump");
        var run = FindClip(clips, "Run", "Gallop", "Skeleton_Running", "Running");
        var attack = player ? FindClip(clips, "Spell1", "Staff_Attack", "Punch") : FindClip(clips, "Attack", "Dragon_Attack", "Bat_Attack", "Slime_Attack", "Skeleton_Attack", "Punch", "Bite", "Jump");
        var hit = FindClip(clips, "RecieveHit", "Dragon_Hit", "Bat_Hit", "Idle_HitReact_Left", "Hit");
        var roll = player ? FindClip(clips, "Roll") : null;
        var castIdle = player ? FindClip(clips, "Idle_Weapon", "Idle_Attacking") : null;
        var castMove = player ? FindClip(clips, "Run_Weapon") : null;

        Directory.CreateDirectory(CtrlDir);
        string path = CtrlDir + Path.GetFileNameWithoutExtension(fbxPath) + (player ? "_Player" : "") + ".controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Cast", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("TakeOff", AnimatorControllerParameterType.Trigger); // v0.8.3 飞行类起飞 / 降落
        ctrl.AddParameter("Land", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;

        // 移动混合：玩家 0 / 1.6 / 5 m/s，异兽 0 / .8 / 3 m/s
        var move = ctrl.CreateBlendTreeInController("Move", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        if (walk && walk != idle) tree.AddChild(walk, player ? 1.6f : .8f);
        if (run) tree.AddChild(run, player ? 5f : 3f);
        sm.defaultState = move;

        if (castIdle)
        {
            var cast = ctrl.CreateBlendTreeInController("CastMove", out BlendTree ct, 0);
            ct.blendType = BlendTreeType.Simple1D; ct.blendParameter = "Speed"; ct.useAutomaticThresholds = false;
            ct.AddChild(castIdle, 0f); if (castMove) ct.AddChild(castMove, 2.5f);
            var toCast = move.AddTransition(cast); toCast.hasExitTime = false; toCast.duration = .12f; toCast.AddCondition(AnimatorConditionMode.If, 0, "Cast");
            var back = cast.AddTransition(move); back.hasExitTime = false; back.duration = .15f; back.AddCondition(AnimatorConditionMode.IfNot, 0, "Cast");
        }
        AddOneShot(sm, move, attack, "Attack", .08f);
        AddOneShot(sm, move, hit, "Hit", .06f);
        AddOneShot(sm, move, roll, "Roll", .05f);
        if (!player) { AddOneShot(sm, move, FindExact(clips, "TakeOff"), "TakeOff", .06f); AddOneShot(sm, move, FindExact(clips, "Land"), "Land", .08f); }
        EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
        ctrlCache[key] = ctrl; return ctrl;
    }

    /// 只按完整片段名匹配（TakeOff / Land 不能用模糊匹配，否则会套到别的动作上）
    static AnimationClip FindExact(AnimationClip[] clips, string key) => clips.FirstOrDefault(c => Short(c.name).Equals(key, System.StringComparison.OrdinalIgnoreCase));
    static AnimationClip FindClip(AnimationClip[] clips, params string[] keys)
    {
        foreach (var k in keys) { var hit = clips.FirstOrDefault(c => Short(c.name).Equals(k, System.StringComparison.OrdinalIgnoreCase)); if (hit) return hit; }
        foreach (var k in keys) { var hit = clips.FirstOrDefault(c => Short(c.name).IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0 && Short(c.name).IndexOf("death", System.StringComparison.OrdinalIgnoreCase) < 0); if (hit) return hit; }
        return null;
    }

    static void AddOneShot(AnimatorStateMachine sm, AnimatorState back, AnimationClip clip, string trigger, float blend)
    {
        if (!clip) return;
        var st = sm.AddState(trigger); st.motion = clip;
        var t = sm.AddAnyStateTransition(st); t.hasExitTime = false; t.duration = blend; t.canTransitionToSelf = false; t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        var r = st.AddTransition(back); r.hasExitTime = true; r.exitTime = .85f; r.duration = .15f;
    }

    // ---------- 尺寸归一 ----------
    static readonly Dictionary<string, Bounds> boundsCache = new Dictionary<string, Bounds>();

    /// 模型在缩放为 1 时的包围盒（相对自身原点），用于把不同来源的模型统一到想要的尺寸
    public static Bounds LocalBounds(GameObject prefab)
    {
        string key = AssetDatabase.GetAssetPath(prefab);
        if (boundsCache.TryGetValue(key, out var b)) return b;
        var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab); tmp.transform.position = Vector3.zero; tmp.transform.rotation = Quaternion.identity; tmp.transform.localScale = Vector3.one;
        var rs = tmp.GetComponentsInChildren<Renderer>(true);
        b = rs.Length > 0 ? rs[0].bounds : new Bounds(Vector3.zero, Vector3.one);
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Object.DestroyImmediate(tmp);
        boundsCache[key] = b; return b;
    }
}
