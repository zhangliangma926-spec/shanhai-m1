using UnityEngine;

/// <summary>
/// 新手引导（v0.8.2）：把开场长说明拆成 7 个随操作推进的短步骤，右上角“当前目标”面板逐步提示。
/// 每一步达成条件后自动进入下一步；已经做到的后续步骤会直接跳过。完成一次后记住，不再重复（PlayerPrefs）。
/// 引导结束后，目标面板改为显示长期目标（图鉴收集进度）。F2 跳过 / 重新开始引导。
/// </summary>
public class M1Onboarding : MonoBehaviour
{
    const string DoneKey = "M1_OnboardingDone_v082";
    struct Step { public string title, hint; }
    static readonly Step[] Steps =
    {
        new Step { title = "移动与视角", hint = "WASD 移动，按住 Shift 加速；按住鼠标右键拖动转镜头，滚轮缩放。" },
        new Step { title = "攻击野生灵兽", hint = "把准星移到灵兽身上，按住左键蓄力，松开发射。头顶数字是造成的伤害。" },
        new Step { title = "削弱到可结契", hint = "继续攻击，把它的生命打到 10% 以下——名牌变绿并出现“可结契”。" },
        new Step { title = "采集材料", hint = "靠近发光的灵木或青玉矿，按住 F 约 1 秒采集。" },
        new Step { title = "制作灵契符", hint = "按 B 打开工坊，用灵木和青玉制作一张灵契符。" },
        new Step { title = "结契", hint = "带着灵契符靠近虚弱的灵兽（3 米内），按 E 结契。" },
        new Step { title = "召唤伙伴", hint = "按 P 打开队伍放出伙伴，1 / 2 / 3 使用伙伴技能。" },
    };

    int step; bool done, hidden; Vector3 startPos; bool hasStart; float flash, doneToastTimer;
    GUIStyle titleStyle, hintStyle, headStyle;

    void Start()
    {
        done = PlayerPrefs.GetInt(DoneKey, 0) == 1;
    }

    void Update()
    {
        var gm = M1GameManager.Instance; if (!gm || !gm.IsGameplayActive || !gm.player) return;
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (done) { done = false; step = 0; hasStart = false; PlayerPrefs.SetInt(DoneKey, 0); gm.ShowAction("新手引导已重新开始。"); }
            else { Finish(); gm.ShowAction("已跳过新手引导，按 F2 可重新开始。"); }
        }
        if (Input.GetKeyDown(KeyCode.F3)) hidden = !hidden;
        if (done) return;
        if (!hasStart) { startPos = gm.player.transform.position; hasStart = true; }
        // 从当前步开始检查，已经做到的步骤连续跳过
        int guard = 0;
        while (!done && guard++ < Steps.Length && Reached(step, gm))
        {
            step++; flash = 1.2f;
            if (step >= Steps.Length) { Finish(); gm.ShowAction("新手引导完成！接下来收集图鉴里的 20 种灵兽吧。"); }
            else gm.ShowAction("目标完成！下一步：" + Steps[step].title);
        }
        flash = Mathf.Max(0f, flash - Time.deltaTime);
    }

    void Finish() { done = true; step = Steps.Length; PlayerPrefs.SetInt(DoneKey, 1); PlayerPrefs.Save(); doneToastTimer = 4f; }

    static bool AnyTameable(System.Func<M1Actor, bool> test) { foreach (var a in M1Actor.All) if (a && a.isTameable && !a.tamed && a.gameObject.activeInHierarchy && test(a)) return true; return false; }
    bool Reached(int i, M1GameManager gm)
    {
        var pets = M1PetSystem.Instance;
        switch (i)
        {
            case 0: { var d = gm.player.transform.position - startPos; d.y = 0; return d.magnitude > 6f; }
            case 1: return AnyTameable(a => a.health < a.maxHealth) || (pets && pets.Count > 0);
            case 2: return AnyTameable(a => a.health / Mathf.Max(1f, a.maxHealth) <= .10f) || (pets && pets.Count > 0);
            case 3: return M1ResourceCrafting.wood + M1ResourceCrafting.ore > 0 || M1ResourceCrafting.seals > 0 || (pets && pets.Count > 0);
            case 4: return M1ResourceCrafting.seals > 0 || (pets && pets.Count > 0);
            case 5: return pets && pets.Count > 0;
            case 6: return pets && pets.ActivePet && pets.ActivePet.gameObject.activeInHierarchy;
        }
        return true;
    }

    /// 长期目标（引导结束后）：图鉴收集进度
    static string LongGoal() { int n = M1PetSystem.Instance ? M1PetSystem.Instance.Count : 0; return "收集灵兽图鉴：" + n + " / 20\n削弱野生灵兽到 10% 以下，用灵契符结契。"; }

    void OnGUI()
    {
        var gm = M1GameManager.Instance; if (!gm || !gm.IsGameplayActive || Event.current.type != EventType.Repaint) return;
        M1UISkin.Apply();
        if (titleStyle == null)
        {
            headStyle = new GUIStyle(M1UISkin.Small) { fontSize = 13 }; headStyle.normal.textColor = M1UISkin.Gold;
            titleStyle = new GUIStyle(M1UISkin.Body) { fontSize = 18, fontStyle = FontStyle.Bold }; titleStyle.normal.textColor = M1UISkin.Jade;
            hintStyle = new GUIStyle(M1UISkin.Body) { fontSize = 15, wordWrap = true };
        }
        float w = Mathf.Min(380f, Screen.width * .32f), x = Screen.width - w - 16f, y = 16f;
        if (hidden) { GUI.Label(new Rect(x, y, w, 20), "F3 显示目标", headStyle); return; }
        string head, title, hint;
        if (done) { head = doneToastTimer > 0f ? "新手引导完成" : "当前目标"; title = "山海探索"; hint = LongGoal(); doneToastTimer -= Time.deltaTime; }
        else { head = "新手引导 " + (step + 1) + " / " + Steps.Length; title = Steps[step].title; hint = Steps[step].hint; }
        float hintH = hintStyle.CalcHeight(new GUIContent(hint), w - 32f), h = 84f + hintH; // 底部留一行按键提示
        var old = GUI.color; if (flash > 0f) GUI.color = Color.Lerp(Color.white, new Color(.6f, 1f, .9f), Mathf.PingPong(flash * 4f, 1f));
        GUI.Box(new Rect(x, y, w, h), GUIContent.none); GUI.color = old;
        GUI.Label(new Rect(x + 16, y + 10, w - 32 - (done ? 0f : Steps.Length * 12f + 8f), 18), head, headStyle); // 右侧留给进度点，不再重叠
        GUI.Label(new Rect(x + 16, y + h - 24, w - 32, 18), done ? "F2 重新开始引导 · F3 隐藏" : "F2 跳过引导 · F3 隐藏面板", headStyle);
        GUI.Label(new Rect(x + 16, y + 28, w - 32, 26), title, titleStyle);
        GUI.Label(new Rect(x + 16, y + 56, w - 32, hintH), hint, hintStyle);
        if (!done) // 步骤进度点
            for (int i = 0; i < Steps.Length; i++) M1UISkin.Tint(new Rect(x + w - 16 - (Steps.Length - i) * 12f, y + 14, 8, 8), i < step ? M1UISkin.Jade : i == step ? M1UISkin.Gold : new Color(1, 1, 1, .25f));
    }
}
