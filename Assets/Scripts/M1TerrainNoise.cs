using UnityEngine;

/// <summary>
/// 地形高度与颜色函数（v0.8.0 从编辑器构建脚本移到运行时）。
/// 和 MC 一样，地形完全由“种子 + 噪声函数”决定：任意坐标都能算出高度，所以世界可以无限延伸；
/// 编辑器生成的 800 米核心区和运行时生成的外围区块调用同一个函数，接缝处高度完全一致。
/// kind：0 星金营地，1 青岚灵林，2 玄岩谷地。
/// </summary>
public static class M1TerrainNoise
{
    public const float CoreHalf = 400f;  // 手工布置的核心区半边长（800 米）

    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
    public static float PathCenter(int kind, float z) => Mathf.Sin(z * .0115f + kind * 1.1f) * (kind == 2 ? 5f : 3f);
    public static float CrossZ(int kind, float x) => 120f + Mathf.Sin(x * .014f + kind) * 8f;
    public static float StreamZ(float x) => 62f + Mathf.Sin(x * .013f) * 12f;

    /// 1 = 平地（主路、横路、营地、传送广场），0 = 自然起伏
    public static float Flat(int kind, float x, float z)
    {
        float dMain = (z > -95f && z < 250f) ? Mathf.Abs(x - PathCenter(kind, z)) - 6f : 999f;
        float dCross = Mathf.Abs(x) < 236f ? Mathf.Abs(z - CrossZ(kind, x)) - 5f : 999f;
        float dCamp = new Vector2(x, z - 30f).magnitude - 40f;
        float dPlaza = new Vector2(x, z - 236f).magnitude - 22f;
        float d = Mathf.Min(Mathf.Min(dMain, dCross), Mathf.Min(dCamp, dPlaza));
        return 1f - Smooth(0f, 16f, d);
    }

    public static float Height(int kind, float x, float z)
    {
        float flat = Flat(kind, x, z);
        float n = Mathf.PerlinNoise((x + kind * 31f + 500f) * .012f, (z - kind * 17f + 500f) * .012f);
        float rolling = Mathf.Max(0f, n - .38f) * 8f;
        float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        float m = Smooth(CoreHalf * .58f, CoreHalf * .95f, edge);
        float mn = Mathf.PerlinNoise((x + 157f + kind * 19f) * .03f, (z + 283f - kind * 11f) * .03f);
        // 核心区外：山地高度再按大尺度噪声起伏，形成连绵的山岭和谷地，而不是一圈等高的围墙
        float far = Mathf.PerlinNoise((x - 911f) * .004f, (z + 377f) * .004f);
        float mountain = m * (5f + mn * 18f + Mathf.Sin(x * .05f + kind) * 2f + Mathf.Cos(z * .043f) * 2f) * Mathf.Lerp(1f, .35f + far * 1.3f, Smooth(CoreHalf, CoreHalf * 1.6f, edge));
        if (kind == 2) rolling *= 1.5f;
        float h = (1f - flat) * (rolling + mountain);
        if (kind == 1 && Mathf.Abs(x) < CoreHalf - 24f) { float ds = Mathf.Abs(z - StreamZ(x)); h -= (1f - Smooth(2f, 7f, ds)) * .6f * (1f - flat); }
        return h;
    }

    public static Color GroundColor(int kind, float x, float z, float y)
    {
        float n = Mathf.PerlinNoise((x + 53f) * .05f, (z + 77f) * .05f);
        Color low = kind == 2 ? new Color(.66f, .52f, .40f) : kind == 1 ? new Color(.36f, .58f, .34f) : new Color(.48f, .66f, .36f);
        Color high = kind == 2 ? new Color(.74f, .58f, .46f) : new Color(.56f, .70f, .42f);
        Color top = kind == 2 ? new Color(.64f, .6f, .56f) : new Color(.58f, .6f, .54f);
        var c = Color.Lerp(low, high, n * .6f); c = Color.Lerp(c, top, Smooth(4f, 16f, y) * .85f);
        float patch = Mathf.PerlinNoise((x + 211f) * .011f, (z - 97f) * .011f), fine = Mathf.PerlinNoise(x * .21f, z * .21f);
        c = Color.Lerp(c, c * new Color(1.08f, 1.12f, .92f), Smooth(.42f, .72f, patch) * .6f); c *= .9f + .18f * fine;
        float f = Flat(kind, x, z); if (f > .5f && (Mathf.Abs(x - PathCenter(kind, z)) < 9f || Mathf.Abs(z - CrossZ(kind, x)) < 8f)) c = Color.Lerp(c, kind == 2 ? new Color(.7f, .58f, .48f) : new Color(.62f, .6f, .44f), .35f);
        return c;
    }
}
