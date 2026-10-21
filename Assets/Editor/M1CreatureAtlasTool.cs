// 生物图册（v0.8.4）：主角、灵兽、敌人、可猎杀动物、怪物逐个拍 正面 / 侧面走路 / 动作 三张图，汇总成一页 HTML。
// 菜单 Shanhai → Tools → 生成生物图册；命令行：-executeMethod M1CreatureAtlasTool.Run。输出到 外层\04_验证记录\<版本号>\图册\。
// 先构建世界（场景里是当前版本的生物）再运行。
using System.Collections.Generic; using System.IO; using System.Linq; using System.Text;
using UnityEditor; using UnityEditor.Animations; using UnityEditor.SceneManagement; using UnityEngine;
using L = M1ModelLibraryTool;
public static class M1CreatureAtlasTool
{
    class Entry { public string cat, name, model, hp, clips, behaviour; public readonly List<string> worlds = new List<string>(); }
    static readonly string[] Order = { "主角", "灵兽", "敌人", "怪物", "可猎杀动物" };

    [MenuItem("Shanhai/Tools/生成生物图册")]
    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string outDir = Path.Combine(root, "04_验证记录", M1GameManager.Version, "图册"), img = Path.Combine(outDir, "图片"); Directory.CreateDirectory(img);
        var entries = new Dictionary<string, Entry>();
        var scenes = new[] { (M1WorldBuilderV077.Hub, "星金营地"), (M1WorldBuilderV077.Forest, "青岚灵林"), (M1WorldBuilderV077.Canyon, "玄岩谷地") };
        foreach (var (scene, world) in scenes)
        {
            EditorSceneManager.OpenScene(scene);
            foreach (var a in Object.FindObjectsByType<M1Actor>())
            {
                var critter = a.GetComponent<M1AmbientCritter>();
                string cat = a.GetComponent<M1PlayerController>() ? "主角" : a.isTameable ? "灵兽" : a.isWildlife ? "可猎杀动物" : a.isEnemy ? (critter ? "怪物" : "敌人") : null;
                if (cat == null) continue;
                string key = cat + "/" + a.displayName;
                if (entries.TryGetValue(key, out var old)) { if (!old.worlds.Contains(world)) old.worlds.Add(world); continue; }
                var e = new Entry { cat = cat, name = a.displayName, model = L.SourceOf(a.transform) };
                e.worlds.Add(world);
                e.hp = cat == "主角" ? "—" : Mathf.RoundToInt(a.isTameable ? M1SpeciesDatabase.MaxHealth(a.speciesId) : a.maxHealth).ToString();
                e.behaviour = cat == "主角" ? "玩家操控" : cat == "灵兽" ? "挨打后反击；生命 ≤10% 时按 E 结契；打死 30 分钟后在别处出现"
                    : cat == "可猎杀动物" ? "挨打后逃跑；打死 30 分钟后在别处出现"
                    : a.selfMoving ? "高空盘旋，可被射落；打死 30 分钟后在别处出现"
                    : a.aggressive ? "靠近 " + Mathf.RoundToInt(a.aggroRange) + " 米内主动攻击，每次 " + Mathf.RoundToInt(a.attackDamage) + " 点" : "挨打后反击，每次 " + Mathf.RoundToInt(a.attackDamage) + " 点";
                var anim = a.GetComponentInChildren<Animator>();
                var ac = anim ? anim.runtimeAnimatorController as AnimatorController : null;
                e.clips = ac ? string.Join("、", ac.animationClips.Select(c => L.Short(c.name)).Distinct()) : "（无动画）";
                bool fly = a.GetComponent<M1FlyerMotion>() || (critter && critter.mode == M1AmbientCritter.Mode.Circle);
                string file = Safe(cat + "_" + a.displayName);
                Shoot(a.transform, ac, Path.Combine(img, file + "_正面.png"), fly ? "Flying" : "Idle", .3f, f => f + Vector3.up * .25f);
                Shoot(a.transform, ac, Path.Combine(img, file + "_侧面.png"), fly ? "Flying" : "Walk", .25f, f => Vector3.Cross(Vector3.up, f) + Vector3.up * .15f);
                Shoot(a.transform, ac, Path.Combine(img, file + "_动作.png"), cat == "主角" ? "Spell1" : "Attack", .5f, f => f * .75f + Vector3.Cross(Vector3.up, f) * .55f + Vector3.up * .35f);
                entries[key] = e;
            }
        }
        WriteHtml(Path.Combine(outDir, "生物图册.html"), entries.Values.OrderBy(x => System.Array.IndexOf(Order, x.cat)).ToList());
        Debug.Log("生物图册已生成：" + outDir + "（" + entries.Count + " 个）");
    }

    static void Shoot(Transform actor, AnimatorController ac, string file, string clip, float at, System.Func<Vector3, Vector3> view)
    {
        var copy = Object.Instantiate(actor.gameObject, new Vector3(20000, 0, 20000), actor.rotation);
        foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        var anim = copy.GetComponentInChildren<Animator>();
        if (anim && ac)
        {
            var c = ac.animationClips.FirstOrDefault(x => L.Short(x.name) == clip) ?? ac.animationClips.FirstOrDefault(x => L.Short(x.name).Contains(clip))
                 ?? ac.animationClips.FirstOrDefault(x => L.Short(x.name) == "Run") ?? ac.animationClips.FirstOrDefault(x => L.Short(x.name) == "Idle");
            if (c) c.SampleAnimation(anim.gameObject, c.length * at);
        }
        L.RenderDir(copy, file, view(copy.transform.forward).normalized);
        Object.DestroyImmediate(copy);
    }

    static string Safe(string s) { foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_'); return s.Replace(' ', '_'); }
    static string Esc(string s) => System.Security.SecurityElement.Escape(s ?? "");

    static void WriteHtml(string path, List<Entry> list)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>山海驭灵录 生物图册 ").Append(M1GameManager.Version).Append("</title><style>")
          .Append(":root{--bg:#f4f2ec;--card:#fff;--ink:#1f2328;--muted:#646a73;--line:#ddd8cc;--accent:#2f7d6d}")
          .Append("@media (prefers-color-scheme:dark){:root{--bg:#16181b;--card:#202327;--ink:#e8e6e1;--muted:#9aa0a6;--line:#33373c;--accent:#6fc3b0}}")
          .Append("body{margin:0;background:var(--bg);color:var(--ink);font:15px/1.6 \"Microsoft YaHei\",\"PingFang SC\",sans-serif}main{max-width:1200px;margin:0 auto;padding:24px 16px 64px}")
          .Append("h1{font-size:26px;margin:0 0 4px}h2{font-size:20px;margin:36px 0 12px;padding-bottom:6px;border-bottom:2px solid var(--accent)}.sub{color:var(--muted);margin:0 0 8px}")
          .Append(".grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(340px,1fr));gap:16px}.card{background:var(--card);border:1px solid var(--line);border-radius:10px;overflow:hidden}")
          .Append(".shots{display:grid;grid-template-columns:repeat(3,1fr);gap:2px;background:var(--line)}.shots figure{margin:0;background:var(--card)}.shots img{width:100%;display:block}.shots figcaption{font-size:12px;color:var(--muted);text-align:center;padding:2px 0}")
          .Append(".info{padding:10px 14px 14px}.info h3{margin:0 0 6px;font-size:18px}.info dl{display:grid;grid-template-columns:5em 1fr;gap:2px 8px;margin:0;font-size:13px}.info dt{color:var(--muted)}.info dd{margin:0;word-break:break-all}")
          .Append("</style></head><body><main><h1>山海驭灵录 · 生物图册</h1><p class=\"sub\">版本 ").Append(M1GameManager.Version).Append("，共 ").Append(list.Count).Append(" 种。图片由游戏内模型直接渲染：正面待机、侧面走路（飞行类为飞行）、攻击动作。</p>");
        foreach (var cat in Order)
        {
            var items = list.Where(e => e.cat == cat).ToList(); if (items.Count == 0) continue;
            sb.Append("<h2>").Append(cat).Append("（").Append(items.Count).Append("）</h2><div class=\"grid\">");
            foreach (var e in items)
            {
                string f = "图片/" + Safe(e.cat + "_" + e.name);
                sb.Append("<div class=\"card\"><div class=\"shots\">");
                foreach (var v in new[] { "正面", "侧面", "动作" }) sb.Append("<figure><img loading=\"lazy\" src=\"").Append(Esc(f + "_" + v + ".png")).Append("\" alt=\"").Append(Esc(e.name + v)).Append("\"><figcaption>").Append(v).Append("</figcaption></figure>");
                sb.Append("</div><div class=\"info\"><h3>").Append(Esc(e.name)).Append("</h3><dl>")
                  .Append("<dt>生命</dt><dd>").Append(e.hp).Append("</dd><dt>行为</dt><dd>").Append(Esc(e.behaviour)).Append("</dd>")
                  .Append("<dt>出现在</dt><dd>").Append(string.Join("、", e.worlds)).Append("</dd><dt>模型</dt><dd>").Append(Esc(e.model)).Append("</dd>")
                  .Append("<dt>动作</dt><dd>").Append(Esc(e.clips)).Append("</dd></dl></div></div>");
            }
            sb.Append("</div>");
        }
        sb.Append("</main></body></html>");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }
}
