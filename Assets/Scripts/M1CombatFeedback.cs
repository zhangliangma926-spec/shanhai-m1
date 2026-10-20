using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗反馈（v0.8.1）：伤害数字飘字、命中时模型弹一下，让玩家一眼看出“打中了没有、打了多少、目标进入什么状态”。
/// 首次调用时自动创建，不需要在场景里摆放。
/// </summary>
public class M1CombatFeedback : MonoBehaviour
{
    public static readonly Color Damage = new Color(1f, .86f, .32f), Calm = new Color(.42f, 1f, .78f), Hurt = new Color(1f, .36f, .3f);
    struct Pop { public Vector3 pos; public string text; public Color color; public float age, life, size; }
    static readonly List<Pop> pops = new List<Pop>();
    static readonly Dictionary<Transform, Vector3> baseScale = new Dictionary<Transform, Vector3>();
    static readonly Dictionary<Transform, float> punchTime = new Dictionary<Transform, float>();
    static M1CombatFeedback instance;
    GUIStyle style;

    static void Ensure() { if (!instance) instance = new GameObject("战斗反馈（飘字）").AddComponent<M1CombatFeedback>(); }

    public static void Popup(Vector3 world, string text, Color color, float size = 24f)
    {
        Ensure(); pops.Add(new Pop { pos = world + new Vector3(Random.Range(-.25f, .25f), 0f, Random.Range(-.25f, .25f)), text = text, color = color, life = 1.1f, size = size });
    }

    /// 命中时模型放大一下再弹回（0.15 秒），只改缩放，不影响贴地、飞行组件对位置和旋转的控制
    public static void Punch(Transform visual)
    {
        if (!visual) return; Ensure();
        if (!baseScale.ContainsKey(visual)) baseScale[visual] = visual.localScale;
        punchTime[visual] = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = pops.Count - 1; i >= 0; i--) { var p = pops[i]; p.age += dt; if (p.age >= p.life) pops.RemoveAt(i); else pops[i] = p; }
        var done = new List<Transform>();
        foreach (var kv in new List<KeyValuePair<Transform, float>>(punchTime))
        {
            var t = kv.Key; if (!t) { done.Add(t); continue; }
            float age = kv.Value + dt; punchTime[t] = age;
            float k = age < .15f ? Mathf.Sin(age / .15f * Mathf.PI) : 0f;
            t.localScale = baseScale[t] * (1f + .14f * k);
            if (age >= .15f) done.Add(t);
        }
        foreach (var t in done) { punchTime.Remove(t); if (t) t.localScale = baseScale[t]; }
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || pops.Count == 0) return;
        var cam = Camera.main; if (!cam) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        foreach (var p in pops)
        {
            Vector3 sp = cam.WorldToScreenPoint(p.pos + Vector3.up * (p.age * 1.3f)); if (sp.z <= 0f) continue;
            float a = 1f - Mathf.Clamp01((p.age - p.life * .6f) / (p.life * .4f)), grow = p.age < .12f ? 1f + (1f - p.age / .12f) * .45f : 1f;
            style.fontSize = Mathf.RoundToInt(p.size * grow);
            var r = new Rect(sp.x - 120f, Screen.height - sp.y - 22f, 240f, 44f);
            style.normal.textColor = new Color(0f, 0f, 0f, .75f * a); GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), p.text, style); // 阴影
            style.normal.textColor = new Color(p.color.r, p.color.g, p.color.b, a); GUI.Label(r, p.text, style);
        }
    }
}
