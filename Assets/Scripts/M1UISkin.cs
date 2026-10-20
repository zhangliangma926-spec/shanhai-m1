using UnityEngine;

/// <summary>
/// 统一界面皮肤：墨青底 + 金色描边 + 青玉点缀的仙侠风格。
/// 所有纹理在运行时用代码生成，不依赖任何外部图片资源。
/// 用法：在任意 OnGUI() 第一行调用 M1UISkin.Apply()，之后的 GUI.Box / GUI.Button / GUI.Label 自动套用新外观。
/// </summary>
public static class M1UISkin
{
    // ---- 配色 ----
    public static readonly Color Ink      = new Color(.045f, .085f, .10f, .93f);
    public static readonly Color InkLight = new Color(.075f, .15f, .165f, .95f);
    public static readonly Color InkHover = new Color(.10f, .22f, .23f, .97f);
    public static readonly Color Gold     = new Color(.88f, .73f, .40f, 1f);
    public static readonly Color GoldSoft = new Color(.88f, .73f, .40f, .42f);
    public static readonly Color Jade     = new Color(.36f, .86f, .80f, 1f);
    public static readonly Color Text     = new Color(.93f, .94f, .90f, 1f);
    public static readonly Color TextDim  = new Color(.66f, .74f, .72f, 1f);
    public static readonly Color Danger   = new Color(.90f, .32f, .26f, 1f);

    static GUISkin skin;
    static Texture2D white, backdrop, divider, glow;
    public static GUIStyle Title, Subtitle, Heading, Body, Small, Toast, Prompt;

    public static GUISkin Skin { get { if (!skin) Build(); return skin; } }
    public static void Apply() { GUI.skin = Skin; }

    static void Build()
    {
        // 必须在 OnGUI 内首次调用，此时 GUI.skin 是 Unity 默认皮肤，以它为底复制，保留字体等设置。
        skin = Object.Instantiate(GUI.skin);
        skin.hideFlags = HideFlags.HideAndDontSave; // 防止切换场景时被 UnloadUnusedAssets 回收

        white    = Solid(Color.white);
        backdrop = VerticalGradient(new Color(.03f, .07f, .10f, 1f), new Color(.08f, .16f, .18f, 1f));
        divider  = HorizontalFade(Gold);
        glow     = RadialGlow();

        var panel     = Frame(Ink, GoldSoft, 2);
        var btnNormal = Frame(InkLight, GoldSoft, 1);
        var btnHover  = Frame(InkHover, Gold, 1);
        var btnActive = Frame(new Color(.04f, .09f, .10f, .98f), Gold, 1);

        // 面板（GUI.Box）：标题文字在顶部居中，金色
        skin.box.normal.background = panel;
        skin.box.border = new RectOffset(6, 6, 6, 6);
        skin.box.padding = new RectOffset(14, 14, 10, 10);
        skin.box.normal.textColor = Gold;
        skin.box.fontSize = 16;
        skin.box.fontStyle = FontStyle.Bold;
        skin.box.alignment = TextAnchor.UpperCenter;
        skin.box.wordWrap = true;

        // 按钮：普通 / 悬停 / 按下 三态
        skin.button.normal.background = btnNormal;   skin.button.normal.textColor = Text;
        skin.button.hover.background  = btnHover;    skin.button.hover.textColor  = Gold;
        skin.button.active.background = btnActive;   skin.button.active.textColor = Jade;
        skin.button.focused.background = btnNormal;  skin.button.focused.textColor = Text;
        skin.button.onNormal.background = btnHover;  skin.button.onNormal.textColor = Gold;
        skin.button.onHover.background = btnHover;   skin.button.onHover.textColor = Gold;
        skin.button.border = new RectOffset(6, 6, 6, 6);
        skin.button.fontSize = 15;
        skin.button.alignment = TextAnchor.MiddleCenter;
        skin.button.wordWrap = true;

        // 普通文字
        skin.label.normal.textColor = Text;
        skin.label.fontSize = 14;
        skin.label.wordWrap = true;

        // 滚动条：细金条
        skin.verticalScrollbar.normal.background = Solid(new Color(0, 0, 0, .25f));
        skin.verticalScrollbar.fixedWidth = 8;
        skin.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
        skin.verticalScrollbarThumb.normal.background = Solid(GoldSoft);
        skin.verticalScrollbarThumb.fixedWidth = 8;
        skin.verticalScrollbarThumb.border = new RectOffset(0, 0, 0, 0);
        skin.scrollView.normal.background = null;

        // 自定义样式
        Title = new GUIStyle(skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        Title.normal.textColor = Gold;
        Subtitle = new GUIStyle(skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
        Subtitle.normal.textColor = Jade;
        Heading = new GUIStyle(skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Heading.normal.textColor = Gold;
        Body = new GUIStyle(skin.label) { fontSize = 16, richText = true };
        Body.normal.textColor = Text;
        Small = new GUIStyle(skin.label) { fontSize = 12 };
        Small.normal.textColor = TextDim;
        Toast = new GUIStyle(skin.box) { fontSize = 15, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(18, 18, 8, 8) };
        Toast.normal.textColor = Text;
        Prompt = new GUIStyle(skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(14, 14, 6, 6) };
        Prompt.normal.textColor = Gold;
    }

    // ---- 绘制小工具 ----

    /// 全屏渐变背景（标题页用）
    public static void Backdrop() { GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backdrop ? backdrop : Skin.box.normal.background); }

    /// 全屏压暗（暂停菜单用，让游戏画面退到背后）
    public static void Dim(float alpha) { Tint(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, alpha)); }

    /// 中间亮、两端淡出的金色分隔线
    public static void Divider(float centerX, float y, float width)
    { var _ = Skin; GUI.DrawTexture(new Rect(centerX - width * .5f, y, width, 1.5f), divider); }

    /// 带投影的文字
    public static void ShadowLabel(Rect r, string text, GUIStyle style)
    {
        var old = style.normal.textColor;
        style.normal.textColor = new Color(0, 0, 0, .6f);
        GUI.Label(new Rect(r.x + 2, r.y + 3, r.width, r.height), text, style);
        style.normal.textColor = old;
        GUI.Label(r, text, style);
    }

    /// 进度条 / 血条
    public static void Bar(Rect r, float ratio, Color fill)
    {
        Tint(r, new Color(0, 0, 0, .45f));
        Tint(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(ratio), r.height - 2), fill);
    }

    /// 柔光点（标题页漂浮灵光用）
    public static void GlowDot(Vector2 center, float size, Color c)
    {
        var _ = Skin; var old = GUI.color; GUI.color = c;
        GUI.DrawTexture(new Rect(center.x - size * .5f, center.y - size * .5f, size, size), glow);
        GUI.color = old;
    }

    public static void Tint(Rect r, Color c)
    { var _ = Skin; var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old; }

    // ---- 纹理生成 ----

    static Texture2D NewTex(int w, int h)
    { var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave }; return t; }

    static Texture2D Solid(Color c) { var t = NewTex(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

    /// 圆角 + 描边的九宫格底图
    static Texture2D Frame(Color fill, Color border, int borderPx)
    {
        const int S = 16; const float R = 4.5f; var t = NewTex(S, S);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float cx = Mathf.Clamp(x + .5f, R, S - R), cy = Mathf.Clamp(y + .5f, R, S - R);
            float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy)); // 到圆角内核的距离
            float edge = R - d;                                   // >0 在形状内
            if (edge <= 0f) { t.SetPixel(x, y, Color.clear); continue; }
            Color c = edge <= borderPx ? border : fill;
            c.a *= Mathf.Clamp01(edge);                           // 边缘抗锯齿
            t.SetPixel(x, y, c);
        }
        t.Apply(); return t;
    }

    static Texture2D VerticalGradient(Color top, Color bottom)
    {
        var t = NewTex(1, 128);
        for (int y = 0; y < 128; y++) t.SetPixel(0, y, Color.Lerp(bottom, top, y / 127f)); // 纹理 y=0 显示在底部
        t.Apply(); return t;
    }

    static Texture2D HorizontalFade(Color c)
    {
        var t = NewTex(128, 1);
        for (int x = 0; x < 128; x++) { float k = 1f - Mathf.Abs(x / 127f * 2f - 1f); t.SetPixel(x, 0, new Color(c.r, c.g, c.b, c.a * k * k)); }
        t.Apply(); return t;
    }

    static Texture2D RadialGlow()
    {
        const int S = 32; var t = NewTex(S, S);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(S * .5f, S * .5f)) / (S * .5f);
            float a = Mathf.Clamp01(1f - d); t.SetPixel(x, y, new Color(1, 1, 1, a * a));
        }
        t.Apply(); return t;
    }
}
