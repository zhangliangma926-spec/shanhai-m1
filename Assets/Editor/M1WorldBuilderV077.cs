using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using A = M1AssetSetupV077;

/// <summary>
/// v0.7.7 世界生成：640×640 米（原 320 米的两倍边长），3D 骨骼动画主角与异兽，风格化植被，牧群与飞鸟。
/// 菜单：Shanhai → Build v0.7.7 · 3D 角色 + 大地图（B 版）会先调用这里生成三张场景。
/// 全部模型为 CC0：Quaternius（RPG Characters / Animal Pack / Monster Pack / Farm Animals / Stylized Nature MegaKit）与 Kenney Nature Kit。
/// </summary>
public static class M1WorldBuilderV077
{
    public const string SceneDir = "Assets/Scenes/v0.7.7/";
    public const string Hub = SceneDir + "M1_HubWorld.unity", Forest = SceneDir + "M1_ForestWorld.unity", Canyon = SceneDir + "M1_CanyonWorld.unity";
    // v0.7.9：地图边长 800 米（上一版 640 米）。依赖边长的数值全部从这里推导。
    const float Size = 800f, Half = 400f;
    const float Edge = Half - 24f;   // 可摆放区域边界（原先 296 = 320 - 24）
    const float Wall = Half - 20f;   // 隐形边界墙位置
    const float Dens = 1.5f;         // 景物数量倍率：800 米地图面积是 640 米的 1.56 倍，取 1.5 保持相近疏密
    public const float RenderDistance = 100f; // v0.8.0：区块渲染距离（参考 MC），以角色为中心 100 米内的区块才显示
    const string LanJiao = "Assets/Art/Models/v080/Beasts/LanJiao_SpiritBeast_Rigged.fbx"; // v0.8.0：Blender 绑骨版，带待机/走/跑/攻击/受击（原静态模型保留在 Blender_Ready）
    const string BlueDemon = "Assets/Art/Models/External_Quaternius/BlueDemon.fbx";

    static int kind; static System.Random rng;

    // ---------------- v0.7.9 防穿模、命名、材质 ----------------
    // 树、石、大型景物、道具都记录占地圆；新摆放的同类实体若与之重叠就跳过，模型底部取周围最低地面贴合。
    struct Foot { public float x, z, r; }
    static readonly List<Foot> placedFoot = new List<Foot>();
    static readonly Dictionary<long, List<int>> footGrid = new Dictionary<long, List<int>>();
    static bool overlapCheck = true;
    const float GridCell = 8f;
    static long CellKey(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

    static void Register(float x, float z, float r)
    {
        placedFoot.Add(new Foot { x = x, z = z, r = r });
        long key = CellKey(Mathf.FloorToInt(x / GridCell), Mathf.FloorToInt(z / GridCell));
        if (!footGrid.TryGetValue(key, out var list)) { list = new List<int>(); footGrid[key] = list; }
        list.Add(placedFoot.Count - 1);
    }
    static bool Occupied(float x, float z, float r)
    {
        int cx = Mathf.FloorToInt(x / GridCell), cz = Mathf.FloorToInt(z / GridCell), reach = Mathf.CeilToInt((r + 6f) / GridCell);
        for (int i = -reach; i <= reach; i++)
            for (int j = -reach; j <= reach; j++)
            {
                if (!footGrid.TryGetValue(CellKey(cx + i, cz + j), out var list)) continue;
                foreach (int idx in list)
                {
                    var f = placedFoot[idx];
                    float dx = f.x - x, dz = f.z - z, minDist = (f.r + r) * .85f;
                    if (dx * dx + dz * dz < minDist * minDist) return true;
                }
            }
        return false;
    }
    static float Footprint(Cat cat, float size)
    {
        switch (cat)
        {
            case Cat.Tree: return Mathf.Clamp(size * .16f, .9f, 4f);
            case Cat.Bush: return Mathf.Clamp(size * .45f, .5f, 2f);
            case Cat.Rock: return Mathf.Clamp(size * .42f, .5f, 4f);
            case Cat.Big: return Mathf.Clamp(size * .45f, 1.5f, 6f);
            case Cat.Prop: return Mathf.Clamp(size * .3f, .4f, 3f);
            default: return 0f;
        }
    }
    /// 模型底部取中心和四周 4 个采样点里最低的地面高度，坡上不悬空，略微下沉 2 厘米贴合
    static float MinGround(float x, float z, float r)
    {
        float k = Mathf.Max(.4f, r * .7f);
        float m = Mathf.Min(H(x, z), H(x + k, z));
        m = Mathf.Min(m, H(x - k, z)); m = Mathf.Min(m, H(x, z + k)); m = Mathf.Min(m, H(x, z - k));
        return m - .02f;
    }
    /// 层级面板里的中文类别名（配合模型文件名，方便在 Hierarchy 里找到具体模型）
    static string CatName(Cat c)
    {
        switch (c)
        {
            case Cat.Tree: return "树木";
            case Cat.Bush: return "灌木";
            case Cat.Plant: return "植物";
            case Cat.Grass: return "草丛";
            case Cat.Flower: return "花丛";
            case Cat.Rock: return "岩石";
            case Cat.Pebble: return "石子";
            case Cat.Big: return "大型景物";
            default: return "道具";
        }
    }

    static Texture2D mottleTex;
    /// 材质斑驳贴图：可平铺的灰度噪声，乘在纯色材质上，打破塑料般的均匀颜色
    static Texture2D Mottle()
    {
        if (mottleTex) return mottleTex;
        string dir = "Assets/Art/Textures/v0.7.9/"; Directory.CreateDirectory(dir); string path = dir + "Mottle.asset";
        mottleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (mottleTex) return mottleTex;
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = .55f * TileNoise(x, y, size, 4) + .3f * TileNoise(x, y, size, 12) + .15f * TileNoise(x, y, size, 40);
                float g = Mathf.Lerp(.82f, 1f, v);
                px[y * size + x] = new Color(g, g * .99f, g * .96f);
            }
        tex.SetPixels(px); tex.Apply(true, false); AssetDatabase.CreateAsset(tex, path); mottleTex = tex; return tex;
    }
    static float TileNoise(float x, float y, int period, int cells)
    {
        float fx = x / period * cells, fy = y / period * cells;
        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
        float tx = fx - x0, ty = fy - y0; tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
        float a = LatticeHash(x0, y0, cells), b = LatticeHash(x0 + 1, y0, cells), c = LatticeHash(x0, y0 + 1, cells), d = LatticeHash(x0 + 1, y0 + 1, cells);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }
    static float LatticeHash(int x, int y, int cells)
    {
        x = ((x % cells) + cells) % cells; y = ((y % cells) + cells) % cells;
        return Mathf.Repeat(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f, 1f);
    }
    static Material terrainMat, pathMat, waterMat, crystalMat, jadeMat, gateMat, accentMat, stoneMat, shellMat, shellDark, eyeMat;
    static readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();

    enum Cat { Tree, Bush, Plant, Grass, Flower, Rock, Pebble, Big, Prop }

    // ---------------- 物种表：同一个 Quaternius 模型按物种调色、调尺寸 ----------------
    enum Flight { None, Hover, Perch }
    struct Species
    {
        public string id, name, model; public float height; public Color tint; public Flight flight;
        public Species(string id, string name, string model, float height, Color tint, Flight flight = Flight.None) { this.id = id; this.name = name; this.model = model; this.height = height; this.tint = tint; this.flight = flight; }
    }
    // v0.8.0：狐、鹿、犬换成 Quaternius 动物包的专用模型（带走、奔跑、攻击、受击动作）；飞行类异兽离地飞行
    static readonly Species[] Beasts =
    {
        new Species("blue_demon",     "岚角灵兽", LanJiao,                    1.7f, Color.white),
        new Species("spirit_beast",   "幽蓝翼兽", BlueDemon,                  1.9f, new Color(.55f,.82f,1f)),
        new Species("wind_fox",       "赤尾灵狐", A.Animals+"Fox.fbx",        .9f,  new Color(1f,.48f,.2f)),
        new Species("stone_turtle",   "岩甲灵龟", A.V083+"RockTurtle_v083.fbx", 1.15f, Color.white),
        new Species("sky_bird",       "青羽灵雀", A.V083+"SpiritSparrow_v083.fbx", .75f, new Color(.35f,.85f,.95f), Flight.Perch),
        new Species("ember_cat",      "赤焰山猫", A.V083+"Lynx_v083.fbx",     .85f, new Color(1f,.42f,.18f)),
        new Species("mist_deer",      "雾角灵鹿", A.Animals+"Stag.fbx",       1.9f, new Color(.78f,.93f,.88f)),
        new Species("rock_boar",      "玄岩獠猪", A.V083+"Pig_v083.fbx",      1.05f,new Color(.45f,.38f,.34f)),
        new Species("glass_marten",   "琉璃水貂", A.V083+"Mink_v083.fbx",     .55f, new Color(.32f,.82f,.88f)),
        new Species("moon_moth",      "月羽灵蝶", A.V083+"Butterfly_v083.fbx", 1.1f, new Color(.82f,.68f,1f),   Flight.Hover),
        new Species("six_eye_fox",    "六目灵狐", A.Animals+"Fox.fbx",        .85f, new Color(.98f,.68f,.26f)),
        new Species("seven_face_fox", "七面幻狐", A.Animals+"Fox.fbx",        1.0f, new Color(.66f,.46f,.88f)),
        new Species("red_xiao",       "赤霄朱雀", A.V083+"Vermilion_v083.fbx", 1.15f,new Color(1f,.28f,.14f),   Flight.Perch),
        new Species("qing_jiao",      "青蛟",     A.Creatures+"Dragon.fbx",   1.9f, new Color(.2f,.78f,.72f),  Flight.Hover),
        new Species("azure_crane",    "苍羽仙鹤", A.V083+"Crane_v083.fbx",    1.4f, Color.white,   Flight.Perch),
        new Species("amber_leopard",  "琥珀雷豹", A.V083+"Leopard_v083.fbx",  1.15f,new Color(1f,.72f,.2f)),
        // v0.7.8 新增：同一批 CC0 模型换色、换体型得到的四种宠物
        new Species("frost_wolf",     "霜牙灵狼", A.Beasts+"Wolf_v080.fbx",     1.0f, new Color(.62f,.9f,1f)),
        new Species("jade_ox",        "玉角灵牛", A.V084+"Cow_v084.fbx",     1.45f,new Color(.72f,.92f,.78f)),
        new Species("flame_llama",    "焰驼",     A.Animals+"Alpaca.fbx",     1.45f,new Color(1f,.5f,.22f)),
        new Species("jade_hound",     "白玉灵犬", A.Animals+"Husky.fbx",      1.0f, new Color(.92f,1f,.96f)),
    };

    // ---------------- 入口 ----------------
    [MenuItem("Shanhai/Tools/仅生成 v0.7.7 场景（不出包）")]
    public static void BuildAll()
    {
        A.ConfigureImports();
        Directory.CreateDirectory(SceneDir);
        foreach (var p in new[] { Hub, Forest, Canyon }) if (File.Exists(p)) AssetDatabase.DeleteAsset(p); // v0.7.7 场景可重复生成
        stoneMat = Mat(new Color(.46f, .48f, .47f)); crystalMat = Glow(new Color(.32f, .9f, 1f), 2.2f); jadeMat = Glow(new Color(.2f, .66f, .52f), .45f); // v0.8.4：青玉矿的晶尖用玉绿色、弱发光，之前用灯芯同款强发光，整块曝成白色 gateMat = Glow(new Color(.18f, .6f, .75f), 1.2f);
        accentMat = Mat(new Color(.86f, .62f, .24f)); shellMat = Mat(new Color(.42f, .56f, .44f)); shellDark = Mat(new Color(.24f, .33f, .26f)); eyeMat = Mat(new Color(.05f, .06f, .07f));
        MakeWorld(Hub, "星金营地", 0, new[] { new PortalTo(Forest, "青岚灵林", -12f), new PortalTo(Canyon, "玄岩谷地", 12f) });
        MakeWorld(Forest, "青岚灵林", 1, new[] { new PortalTo(Hub, "星金营地", -12f), new PortalTo(Canyon, "玄岩谷地", 12f) });
        MakeWorld(Canyon, "玄岩谷地", 2, new[] { new PortalTo(Hub, "星金营地", -12f), new PortalTo(Forest, "青岚灵林", 12f) });
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Hub, true), new EditorBuildSettingsScene(Forest, true), new EditorBuildSettingsScene(Canyon, true) };
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("M1 worlds built: 800m, 3D characters, stylized nature (" + M1GameManager.Version + ").");
    }

    struct PortalTo { public string scene, name; public float x; public PortalTo(string s, string n, float x) { scene = s; name = n; this.x = x; } }
    struct Pick2 { public string asset; public float size; public Pick2(string a, float s) { asset = a; size = s; } }
    static void MakeWorld(string path, string worldName, int k, PortalTo[] portals)
    {
        kind = k; rng = new System.Random(770 + k * 31); groups.Clear(); placedFoot.Clear(); footGrid.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupAtmosphere(k);
        var manager = new GameObject("M1 Game Manager").AddComponent<M1GameManager>();
        manager.gameObject.AddComponent<M1PetSystem>(); manager.gameObject.AddComponent<M1TamingGuide>(); manager.gameObject.AddComponent<M1ResourceCrafting>();
        manager.gameObject.AddComponent<M1PetNameplates>(); manager.gameObject.AddComponent<M1AudioDirector>(); manager.gameObject.AddComponent<M1WildSpawner>(); manager.gameObject.AddComponent<M1Onboarding>(); // v0.8.2 新手引导

        terrainMat = TerrainMaterial(k); pathMat = Mat(k == 2 ? new Color(.56f, .45f, .38f) : new Color(.58f, .49f, .36f)); // v0.8.2：道路压暗 pathMat.SetFloat("_Glossiness", .05f);
        waterMat = Mat(new Color(.18f, .58f, .66f)); waterMat.SetFloat("_Glossiness", .9f); waterMat.EnableKeyword("_EMISSION"); waterMat.SetColor("_EmissionColor", new Color(.05f, .22f, .26f));

        BuildTerrain(worldName); BuildTrails(); if (k == 1) BuildStream();
        ScatterNature(); DecorateTrails(); WorldDecor(); ExtraProps(); MakeSpring(); MakeInfiniteWorld(); // v0.8.0：去掉隐形边界墙，改为无限区块地图

        var player = MakePlayer(); player.transform.position = new Vector3(0, 0, 0);
        var enemy = MakeEnemy(new Vector3(-9, H(-9, 16), 16));
        M1Actor first = null;
        for (int i = 0; i < Beasts.Length; i++) { var b = MakeBeast(Beasts[i], BeastSpot(i)); if (!first) first = b; }
        MakeAmbientLife();
        MakeResources(player.transform);
        manager.player = player; manager.enemy = enemy; manager.beast = first; manager.respawnPoint = new Vector3(0, 0, 30f);
        foreach (var p in portals) MakePortal(p.scene, p.name, new Vector3(p.x, 0, 236f));

        var camObj = new GameObject("Main Camera"); camObj.tag = "MainCamera"; var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 58f; cam.nearClipPlane = .2f; cam.farClipPlane = RenderDistance + 30f; camObj.AddComponent<AudioListener>(); camObj.transform.position = new Vector3(0, 4, -7); // v0.8.0：远裁剪面 = 区块渲染距离 + 30 米，远处由雾遮住
        var follow = camObj.AddComponent<M1CameraFollow>(); follow.target = player.transform; follow.distance = 7.4f; follow.minDistance = 3.2f; follow.maxDistance = 12f;
        player.GetComponent<M1PlayerController>().cameraTransform = camObj.transform;
        AddFireflies(new Vector3(0, 1.2f, 30), k == 2 ? 20 : 34);
        AddPollen(k == 1 ? 260 : 180);
        EditorSceneManager.SaveScene(scene, path);
    }

    // ---------------- 天空、光照、雾 ----------------
    /// v0.8.2 程序化天空：曝光略低、天色略深，地平线不再白成一片；峡谷偏暖
    static Material Skybox(int k)
    {
        var sh = Shader.Find("Skybox/Procedural"); if (!sh) return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        string dir = "Assets/Art/Materials/v0.8.2/"; Directory.CreateDirectory(dir); string path = dir + "Sky_" + k + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.SetFloat("_SunSize", .045f); m.SetFloat("_AtmosphereThickness", k == 2 ? 1.15f : .95f); m.SetFloat("_Exposure", .95f);
        m.SetColor("_SkyTint", k == 2 ? new Color(.6f, .52f, .48f) : k == 1 ? new Color(.42f, .52f, .54f) : new Color(.45f, .52f, .62f));
        m.SetColor("_GroundColor", new Color(.32f, .33f, .3f)); EditorUtility.SetDirty(m); return m;
    }
    static void SetupAtmosphere(int k)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // v0.8.2 画面亮度：评价指出整体偏亮、远景发白。环境光整体压暗约 15%，阴影更深，层次更清楚
        RenderSettings.ambientSkyColor = k == 2 ? new Color(.54f, .52f, .58f) : new Color(.46f, .58f, .70f);
        RenderSettings.ambientEquatorColor = k == 2 ? new Color(.48f, .40f, .35f) : new Color(.38f, .46f, .40f);
        RenderSettings.ambientGroundColor = new Color(.20f, .19f, .17f);
        // v0.7.8 虚实：指数平方雾，近处清晰、远处逐渐发虚淡入天色，形成空气透视的纵深感
        // v0.8.0：改为线性雾，正好在区块渲染距离（100 米）处完全融入雾色，看不到区块边缘，类似 MC 的雾
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = RenderDistance * .3f; // v0.8.2：30 米起逐渐发虚，近、中、远三层更分明 RenderSettings.fogEndDistance = RenderDistance * 1.02f;
        RenderSettings.fogColor = k == 2 ? new Color(.74f, .64f, .58f) : k == 1 ? new Color(.52f, .66f, .66f) : new Color(.62f, .72f, .80f); // v0.8.2：雾色压暗，远景不再发白
        RenderSettings.fogDensity = k == 1 ? .0042f : k == 2 ? .0036f : .0029f;
        RenderSettings.skybox = Skybox(k); // v0.8.2：自定义程序化天空，比默认天空略深
        var sun = new GameObject("太阳 · 主光").AddComponent<Light>(); sun.type = LightType.Directional;
        sun.color = k == 2 ? new Color(1f, .86f, .7f) : new Color(1f, .95f, .84f); sun.intensity = 1.0f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .88f; // v0.8.2：太阳 1.25 → 1.0，阴影加深
        sun.transform.rotation = Quaternion.Euler(40, -38, 0); RenderSettings.sun = sun; // 太阳略低，影子更长，立体感更强
    }

    // ---------------- 地形 ----------------
    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
    // v0.8.0：高度、道路、颜色函数移到运行时的 M1TerrainNoise，编辑器生成的核心地形和运行时的无限区块共用同一套函数，接缝无缝
    public static float PathCenter(float z) => M1TerrainNoise.PathCenter(kind, z);
    static float CrossZ(float x) => M1TerrainNoise.CrossZ(kind, x);
    static float StreamZ(float x) => M1TerrainNoise.StreamZ(x);
    /// 1 = 平地（主路、横路、营地、传送广场），0 = 自然起伏
    static float Flat(float x, float z) => M1TerrainNoise.Flat(kind, x, z);
    public static float H(float x, float z) => M1TerrainNoise.Height(kind, x, z);
    static Color TerrainColor(float x, float z, float y) => M1TerrainNoise.GroundColor(kind, x, z, y);
    static void BuildTerrain(string worldName)
    {
        const int cells = 300; int side = cells + 1; var mesh = new Mesh { name = worldName + "_800m地形", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; // 361×361 顶点超过 16 位索引上限，必须用 32 位
        var v = new Vector3[side * side]; var colors = new Color[v.Length]; var uv = new Vector2[v.Length]; var tri = new int[cells * cells * 6];
        for (int ix = 0; ix <= cells; ix++) for (int iz = 0; iz <= cells; iz++)
        { int i = ix * side + iz; float x = -Half + Size * ix / cells, z = -Half + Size * iz / cells; float y = H(x, z); v[i] = new Vector3(x, y, z); colors[i] = TerrainColor(x, z, y); uv[i] = new Vector2(x / 10f, z / 10f); } // v0.8.0：细节贴图每 10 米平铺一次，草叶纹理更细
        int t = 0; for (int ix = 0; ix < cells; ix++) for (int iz = 0; iz < cells; iz++) { int a = ix * side + iz, b = (ix + 1) * side + iz, c = a + 1, d = b + 1; tri[t++] = a; tri[t++] = c; tri[t++] = b; tri[t++] = b; tri[t++] = c; tri[t++] = d; }
        mesh.vertices = v; mesh.colors = colors; mesh.uv = uv; mesh.triangles = tri; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string dir = "Assets/Art/Meshes/v0.7.7/"; Directory.CreateDirectory(dir); string mp = dir + "Terrain_" + kind + ".asset"; AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
        var root = new GameObject(worldName + " · 800米地形"); root.AddComponent<MeshFilter>().sharedMesh = mesh; root.AddComponent<MeshRenderer>().sharedMaterial = terrainMat; root.AddComponent<MeshCollider>().sharedMesh = mesh; root.AddComponent<M1GroundSurface>(); root.isStatic = true;
    }
    static Material TerrainMaterial(int k)
    {
        var sh = Shader.Find("Shanhai/VertexColorLitV080"); if (!sh) sh = Shader.Find("Shanhai/VertexColorLitV066"); if (!sh) sh = Shader.Find("Standard");
        string dir = "Assets/Art/Materials/v0.7.7/"; Directory.CreateDirectory(dir); string path = dir + "Terrain_" + k + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", GroundTexture(k)); if (m.HasProperty("_Color")) m.color = Color.white; return m;
    }
    /// v0.8.0 地面细节贴图：512 像素、四边无缝平铺（旧版用 Perlin 噪声，每 14 米能看到接缝）。
    /// 草地世界画上细小的草叶笔触和土点，峡谷画砂砾斑点；颜色由地形顶点色决定，这里只是明暗细节。
    static Texture2D GroundTexture(int k)
    {
        string dir = "Assets/Art/Textures/v0.8.0/"; Directory.CreateDirectory(dir); string path = dir + "Ground_" + k + ".asset";
        AssetDatabase.DeleteAsset(path); // 每次构建重新生成，参数改了能立即生效
        const int size = 512; var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var v = new float[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            v[y * size + x] = .86f + (TileNoise(x, y, size, 6) - .5f) * .16f + (TileNoise(x, y, size, 24) - .5f) * .12f + (TileNoise(x, y, size, 96) - .5f) * .1f;
        var rnd = new System.Random(4100 + k);
        if (k == 2)
        {
            for (int i = 0; i < 9000; i++) // 砂砾：小圆点，亮暗随机
            {
                int cx = rnd.Next(size), cy = rnd.Next(size); float r = 1f + (float)rnd.NextDouble() * 2.2f, d = rnd.NextDouble() < .55 ? -.12f : .09f;
                for (int oy = -3; oy <= 3; oy++) for (int ox = -3; ox <= 3; ox++) { float q = Mathf.Sqrt(ox * ox + oy * oy); if (q > r) continue; int px = (cx + ox + size) % size, py = (cy + oy + size) % size; v[py * size + px] += d * (1f - q / (r + .01f)); }
            }
        }
        else
        {
            for (int i = 0; i < 26000; i++) // 草叶：2–7 像素的细笔触，大多接近竖直，一半偏暗一半偏亮
            {
                int cx = rnd.Next(size), cy = rnd.Next(size); int len = 2 + rnd.Next(6); float ang = (float)(rnd.NextDouble() - .5) * 1.4f + Mathf.PI * .5f;
                float d = rnd.NextDouble() < .5 ? -.09f : .07f; float dx = Mathf.Cos(ang), dy = Mathf.Sin(ang);
                for (int s = 0; s < len; s++) { int px = ((int)(cx + dx * s) % size + size) % size, py = ((int)(cy + dy * s) % size + size) % size; v[py * size + px] += d * (1f - s / (float)len * .5f); }
            }
            for (int i = 0; i < 1400; i++) // 土点：零星的小块深色
            {
                int cx = rnd.Next(size), cy = rnd.Next(size); float r = 1.5f + (float)rnd.NextDouble() * 2.5f;
                for (int oy = -4; oy <= 4; oy++) for (int ox = -4; ox <= 4; ox++) { float q = Mathf.Sqrt(ox * ox + oy * oy); if (q > r) continue; int px = (cx + ox + size) % size, py = (cy + oy + size) % size; v[py * size + px] -= .08f * (1f - q / r); }
            }
        }
        var px2 = new Color[size * size];
        for (int i = 0; i < v.Length; i++) { float c = Mathf.Clamp(v[i], .55f, 1.08f); px2[i] = new Color(c, c, c); }
        tex.SetPixels(px2); tex.Apply(true, false); AssetDatabase.CreateAsset(tex, path); return tex;
    }

    // ---------------- 道路与溪流 ----------------
    static void BuildTrails()
    {
        Strip("主路 · 自然石土小径", 360, i => { float z = -95f + 345f * i / 360f; return new Vector3(PathCenter(z), 0, z); }, 2.6f, pathMat, .06f);
        Strip("横路 · 林间小道", 300, i => { float x = -236f + 472f * i / 300f; return new Vector3(x, 0, CrossZ(x)); }, 2.1f, pathMat, .06f);
    }
    static void BuildStream()
    {
        Strip("青岚浅溪", 260, i => { float x = -Edge + 2f * Edge * i / 260f; return new Vector3(x, 0, StreamZ(x)); }, 3.4f, waterMat, .02f);
        float bz = StreamZ(PathCenter(StreamZ(0))); Spawn(A.Kenney + "bridge_wood.fbx", new Vector3(PathCenter(bz), H(PathCenter(bz), bz), bz), 7f, 90f, "溪上木桥", Cat.Prop, false);
    }
    static void Strip(string name, int steps, System.Func<int, Vector3> at, float half, Material mat, float lift)
    {
        var verts = new Vector3[(steps + 1) * 2]; var uv = new Vector2[verts.Length]; var tri = new int[steps * 6];
        for (int i = 0; i <= steps; i++)
        {
            Vector3 p = at(i), q = at(Mathf.Min(i + 1, steps)), back = at(Mathf.Max(i - 1, 0)); Vector3 dir = (q - back); dir.y = 0; dir.Normalize();
            Vector3 side = new Vector3(dir.z, 0, -dir.x) * half; Vector3 l = p - side, r = p + side;
            l.y = H(l.x, l.z) + lift; r.y = H(r.x, r.z) + lift; verts[i * 2] = l; verts[i * 2 + 1] = r; uv[i * 2] = new Vector2(0, i * .5f); uv[i * 2 + 1] = new Vector2(1, i * .5f);
            if (i < steps) { int k = i * 6, a = i * 2; tri[k] = a; tri[k + 1] = a + 2; tri[k + 2] = a + 1; tri[k + 3] = a + 1; tri[k + 4] = a + 2; tri[k + 5] = a + 3; }
        }
        var m = new Mesh { name = name }; m.vertices = verts; m.uv = uv; m.triangles = tri; m.RecalculateNormals();
        var go = new GameObject(name); go.AddComponent<MeshFilter>().sharedMesh = m; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; go.isStatic = true;
    }

    // ---------------- 通用摆放：自动统一尺寸、贴地、碰撞、远距离剔除 ----------------
    static GameObject Spawn(string asset, Vector3 pos, float size, float yaw, string label, Cat cat, bool byHeight = true)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset); if (!prefab) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); if (!go) return null;
        var b = A.LocalBounds(prefab); float dim = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z); float s = size / Mathf.Max(dim, .01f);
        float fr = Footprint(cat, size);
        // v0.8.0：草、花、小植物不再长进树干、石头、灌木里
        if (fr <= 0f && overlapCheck && (cat == Cat.Grass || cat == Cat.Flower || cat == Cat.Plant) && Occupied(pos.x, pos.z, .15f)) { Object.DestroyImmediate(go); return null; }
        if (fr > 0f)
        {
            pos.y = MinGround(pos.x, pos.z, fr);
            if (overlapCheck && Occupied(pos.x, pos.z, fr)) { Object.DestroyImmediate(go); return null; } // 与已有实体重叠，放弃这一处
        }
        go.name = label + " · " + Path.GetFileNameWithoutExtension(asset);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * s;
        go.transform.position = pos - Vector3.up * (b.min.y * s + (cat == Cat.Rock || cat == Cat.Big ? size * .12f : 0f)); // 石头略微埋进地里更自然
        go.transform.SetParent(Group(cat), true);
        if (fr > 0f) Register(pos.x, pos.z, fr);
        A.ApplyTextures(go);
        FinishScenery(go, cat, b, size, s);
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }
    static Transform Group(Cat cat)
    {
        string n = "景物 · " + cat; if (groups.TryGetValue(n, out var t) && t) return t;
        t = new GameObject(n).transform; t.gameObject.isStatic = true; groups[n] = t; return t;
    }
    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    static T Pick<T>(T[] arr) => arr[rng.Next(arr.Length)];
    static bool Open(float x, float z, float margin = .25f)
    {
        if (Flat(x, z) > margin) return false;
        if (kind == 1 && Mathf.Abs(z - StreamZ(x)) < 4f) return false;
        if (Mathf.Abs(x) > Edge || Mathf.Abs(z) > Edge) return false;
        return true;
    }

    // ---------------- 植被与岩石 ----------------
    static string N(string n) => A.Nature + n + ".fbx";
    static string K(string n) => A.Kenney + n + ".fbx";
    static readonly string[] CommonTrees = { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5" };
    static readonly string[] Pines = { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" };
    static readonly string[] Twisted = { "TwistedTree_1", "TwistedTree_2", "TwistedTree_3", "TwistedTree_4", "TwistedTree_5" };
    static readonly string[] Dead = { "DeadTree_1", "DeadTree_2", "DeadTree_3", "DeadTree_4", "DeadTree_5" };
    static readonly string[] Bushes = { "Bush_Common", "Bush_Common_Flowers" };
    static readonly string[] Plants = { "Plant_1", "Plant_1_Big", "Plant_7", "Plant_7_Big", "Fern_1" };
    static readonly string[] Grasses = { "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Grass_Wispy_Tall" };
    static readonly string[] Flowers = { "Flower_3_Group", "Flower_3_Single", "Flower_4_Group", "Flower_4_Single", "Clover_1", "Clover_2" };
    static readonly string[] Rocks = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" };
    static readonly string[] Pebbles = { "Pebble_Round_1", "Pebble_Round_2", "Pebble_Round_3", "Pebble_Square_1", "Pebble_Square_2", "Pebble_Square_3" };
    static readonly string[] Mushrooms = { "Mushroom_Common", "Mushroom_Laetiporus" };
    static readonly string[] FallTrees = { "tree_oak_fall", "tree_detailed_fall", "tree_fat_fall" };

    static void ScatterNature()
    {
        int trees = kind == 1 ? 1150 : kind == 2 ? 420 : 760, rocks = kind == 2 ? 760 : kind == 1 ? 300 : 340;
        int bushes = kind == 2 ? 180 : 520, plants = kind == 2 ? 160 : 700, grass = kind == 2 ? 600 : 1500, flowers = kind == 2 ? 120 : 650;
        Scatter(trees, Cat.Tree, () =>
        {
            if (kind == 2) return rng.NextDouble() < .7 ? new Pick2(N(Pick(Dead)), R(5f, 8.5f)) : new Pick2(N(Pick(Twisted)), R(6f, 9f));
            if (kind == 1) return rng.NextDouble() < .55 ? new Pick2(N(Pick(Pines)), R(10f, 16f)) : new Pick2(N(Pick(CommonTrees)), R(8f, 12f));
            double p = rng.NextDouble(); return p < .5 ? new Pick2(N(Pick(CommonTrees)), R(7f, 11f)) : p < .8 ? new Pick2(N(Pick(Twisted)), R(6f, 9.5f)) : new Pick2(K(Pick(FallTrees)), R(6f, 9f));
        }, .2f);
        Scatter(rocks, Cat.Rock, () => kind == 2 && rng.NextDouble() < .25 ? new Pick2(K(rng.NextDouble() < .5 ? "rock_tall" : "stone_tall"), R(3f, 7f)) : new Pick2(N(Pick(Rocks)), R(1.2f, kind == 2 ? 4.5f : 3f)), .15f);
        Scatter(bushes, Cat.Bush, () => new Pick2(N(Pick(Bushes)), R(1.1f, 2f)), .1f);
        Scatter(plants, Cat.Plant, () => kind == 1 && rng.NextDouble() < .3 ? new Pick2(N(Pick(Mushrooms)), R(.35f, .8f)) : new Pick2(N(Pick(Plants)), R(.6f, 1.5f)), .1f);
        ScatterMeadow(Mathf.RoundToInt(grass * Dens / 5f * (kind == 2 ? 1.2f : 1.7f))); // v0.8.0：草坪更密（峡谷偏干，只略加）
        Scatter(flowers, Cat.Flower, () => new Pick2(N(Pick(Flowers)), R(.3f, .65f)), .05f);
        Scatter(kind == 2 ? 300 : 200, Cat.Pebble, () => new Pick2(N(Pick(Pebbles)), R(.25f, .7f)), .3f, false);
    }
    /// v0.7.9 草坪：按草坪斑块成簇种草，替代原来零散的单丛草。
    /// v0.8.0：草坪斑块更大（阈值 .45 → .36），每簇 4–9 丛、铺得更开
    static void ScatterMeadow(int clusters)
    {
        int placed = 0, tries = 0; float threshold = kind == 2 ? .45f : .36f;
        while (placed < clusters && tries++ < clusters * 8)
        {
            float x = R(-Edge, Edge), z = R(-Edge, Edge);
            if (Mathf.PerlinNoise((x + 300f) * .01f, (z - 200f) * .01f) < threshold) continue; // 只在草坪斑块上长草
            if (!Open(x, z, .3f)) continue;
            placed++;
            int blades = rng.Next(4, 10);
            for (int j = 0; j < blades; j++)
            {
                float px = x + R(-2.2f, 2.2f), pz = z + R(-2.2f, 2.2f);
                if (!Open(px, pz, .3f)) continue;
                bool flower = rng.NextDouble() < .2;
                string asset = flower ? N(Pick(Flowers)) : N(Pick(Grasses));
                Spawn(asset, new Vector3(px, H(px, pz), pz), flower ? R(.3f, .6f) : R(.55f, 1.15f), R(0, 360), "草丛", Cat.Grass);
            }
        }
    }
    static void Scatter(int count, Cat cat, System.Func<Pick2> pick, float margin, bool byHeight = true)
    {
        count = Mathf.RoundToInt(count * Dens);
        int placed = 0, tries = 0;
        while (placed < count && tries++ < count * 6)
        {
            float x = R(-Edge, Edge), z = R(-Edge, Edge);
            // 树木成片：用噪声控制疏密，空地与林地交替，更像真实野外
            if (cat == Cat.Tree && Mathf.PerlinNoise((x + 900f) * .018f, (z + 400f) * .018f) < (kind == 1 ? .3f : .42f)) continue;
            if (!Open(x, z, margin)) continue;
            var pk = pick(); string asset = pk.asset; float size = pk.size;
            if (Spawn(asset, new Vector3(x, H(x, z), z), size, R(0, 360), CatName(cat), cat, byHeight)) placed++;
        }
    }
    /// 路边点缀：石板、路灯、花丛，引导玩家沿路探索
    static void DecorateTrails()
    {
        for (float z = -80f; z < 232f; z += 3.2f)
        {
            float x = PathCenter(z) + R(-1.4f, 1.4f);
            if (rng.NextDouble() < .55) Spawn(N(Pick(new[] { "RockPath_Round_Small_1", "RockPath_Round_Small_2", "RockPath_Square_Small_1", "RockPath_Square_Small_2" })), new Vector3(x, H(x, z) + .02f, z), R(.9f, 1.5f), R(0, 360), "路面石板", Cat.Pebble, false);
        }
        for (float z = -60f; z < 232f; z += 26f)
            for (int side = -1; side <= 1; side += 2)
            {
                float x = PathCenter(z) + side * 4.2f; Lantern(new Vector3(x, H(x, z), z));
                for (int j = 0; j < 4; j++) { float px = x + side * R(.5f, 3f), pz = z + R(-3f, 3f); Spawn(N(Pick(Flowers)), new Vector3(px, H(px, pz), pz), R(.35f, .6f), R(0, 360), "路边花", Cat.Flower); }
            }
        for (float x = -220f; x < 222f; x += 30f) { float z = CrossZ(x) + 3.6f; Lantern(new Vector3(x, H(x, z), z)); }
        // v0.8.0：路边草带，让土路和草地之间有自然的过渡，而不是一条硬边
        if (kind != 2)
            for (float z = -90f; z < 240f; z += 1.7f)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = PathCenter(z) + side * R(2.9f, 4.4f);
                    Spawn(N(Pick(Grasses)), new Vector3(x, H(x, z), z), R(.45f, .9f), R(0, 360), "路边草", Cat.Grass);
                }
    }
    static void Lantern(Vector3 at)
    {
        var post = Prim(PrimitiveType.Cylinder, "青玉路灯", at + Vector3.up * .55f, new Vector3(.14f, .55f, .14f), stoneMat, Group(Cat.Prop), true);
        Prim(PrimitiveType.Sphere, "灯芯", new Vector3(0, 1.15f, 0), new Vector3(2.4f, .42f, 2.4f), crystalMat, post.transform);
    }

    // ---------------- 各世界特色景物 ----------------
    static void WorldDecor()
    {
        if (kind == 0)
        {
            Spawn(K("campfire_logs"), new Vector3(0, 0, 33f), 1.4f, 0, "营地篝火", Cat.Prop, false);
            var fire = new GameObject("营地火光").AddComponent<Light>(); fire.type = LightType.Point; fire.color = new Color(1f, .6f, .26f); fire.intensity = 1.4f; fire.range = 14f; fire.transform.position = new Vector3(0, 1.4f, 33f);
            float[] ang = { 30, 75, 120, 200, 250, 320 };
            for (int i = 0; i < ang.Length; i++) { float a = ang[i] * Mathf.Deg2Rad; var p = new Vector3(Mathf.Sin(a) * 15f, 0, 30f + Mathf.Cos(a) * 15f); Spawn(K(i % 2 == 0 ? "tent_detailedOpen" : "tent_smallClosed"), p, i % 2 == 0 ? 3.2f : 2.4f, ang[i] + 180f, "营地帐篷", Cat.Prop); }
            for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; var p = new Vector3(Mathf.Sin(a) * 7f, 0, 33f + Mathf.Cos(a) * 7f); Spawn(K("log"), p, .6f, i * 45f + 90f, "营地木座", Cat.Prop, false); }
            for (int i = 0; i < 24; i++) { float a = i * 15f * Mathf.Deg2Rad; var p = new Vector3(Mathf.Sin(a) * 36f, 0, 30f + Mathf.Cos(a) * 36f); if (Mathf.Abs(p.x - PathCenter(p.z)) < 6f) continue; Spawn(K("fence_simple"), new Vector3(p.x, H(p.x, p.z), p.z), 1.1f, i * 15f, "营地篱笆", Cat.Prop, false); }
            for (int i = 0; i < 40; i++) { float a = R(0, 6.28f), r = R(44f, 62f); float x = Mathf.Sin(a) * r, z = 30f + Mathf.Cos(a) * r; if (!Open(x, z, .5f)) continue; Spawn(K("crops_bambooStage"), new Vector3(x, H(x, z), z), R(3.5f, 5.5f), R(0, 360), "灵竹", Cat.Bush); }
            Spawn(K("statue_obelisk"), new Vector3(14, 0, 54), 5.5f, 20, "星金方尖碑", Cat.Big); Spawn(K("statue_obelisk"), new Vector3(-14, 0, 54), 5.5f, -20, "星金方尖碑", Cat.Big);
            Spawn(K("sign"), new Vector3(4.5f, 0, 6f), 1.6f, 200, "路牌", Cat.Prop);
        }
        else if (kind == 1)
        {
            for (int i = 0; i < 18; i++) { float x = R(-Edge, Edge), z = StreamZ(x) + R(-1.2f, 1.2f); Spawn(K("lily_large"), new Vector3(x, H(x, z) + .03f, z), R(.8f, 1.4f), R(0, 360), "溪中莲叶", Cat.Flower, false); }
            for (int i = 0; i < 40; i++) { float x = R(-Edge, Edge), z = R(-Edge, Edge); if (!Open(x, z, .3f)) continue; var c = new Vector3(x, H(x, z), z); for (int j = 0; j < 7; j++) { float a = j / 7f * 6.28f; var p = c + new Vector3(Mathf.Sin(a) * 1.8f, 0, Mathf.Cos(a) * 1.8f); p.y = H(p.x, p.z); Spawn(K(j % 2 == 0 ? "mushroom_redGroup" : "mushroom_tanGroup"), p, R(.4f, .7f), R(0, 360), "蘑菇仙环", Cat.Plant, false); } }
            for (int i = 0; i < 10; i++) { float x = R(-Edge * .9f, Edge * .9f), z = R(-Edge * .9f, Edge * .9f); if (!Open(x, z, .3f)) continue; Spawn(K("statue_column"), new Vector3(x, H(x, z), z), R(3f, 5f), R(0, 360), "林间古柱", Cat.Big); }
            for (int i = 0; i < 30; i++) { float x = R(-Edge, Edge), z = R(-Edge, Edge); if (!Open(x, z, .3f)) continue; Spawn(K(rng.NextDouble() < .5 ? "log_large" : "log_stackLarge"), new Vector3(x, H(x, z), z), R(1f, 1.8f), R(0, 360), "倒木", Cat.Prop); }
        }
        else
        {
            for (int i = 0; i < 70; i++) { float x = R(-Edge, Edge), z = R(-Edge, Edge); if (!Open(x, z, .2f) || Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < 120f) continue; Spawn(K(rng.NextDouble() < .6 ? "cliff_large_rock" : "cliff_block_rock"), new Vector3(x, H(x, z), z), R(6f, 14f), R(0, 360), "玄岩崖壁", Cat.Big); }
            for (int i = 0; i < 12; i++) { float x = R(-Edge * .85f, Edge * .85f), z = R(-Edge * .85f, Edge * .85f); if (!Open(x, z, .3f)) continue; Spawn(K(rng.NextDouble() < .5 ? "statue_ring" : "statue_obelisk"), new Vector3(x, H(x, z), z), R(4f, 7f), R(0, 360), "上古石刻", Cat.Big); }
            for (int i = 0; i < 26; i++)
            { float x = R(-30, 30), z = R(20, 60); if (Flat(x, z) > .9f && new Vector2(x, z - 30f).magnitude < 12f) continue; var shard = Prim(PrimitiveType.Cube, "玄岩谷青玉晶脉", new Vector3(x, H(x, z) + 1.2f, z), new Vector3(.45f, R(1.5f, 3.2f), .45f), jadeMat, Group(Cat.Prop), true); shard.transform.rotation = Quaternion.Euler(R(-12, 12), R(0, 360), R(-12, 12)); }
        }
    }
    /// v0.7.8 道具：每个世界的探索地标（灵石祭坛 / 灵石丛 / 古碑阵），让地图上有更多可看、可绕的目标
    static void ExtraProps()
    {
        if (kind == 0)
        {
            var c = new Vector3(-24f, 0, 58f);
            Spawn(K("statue_ring"), new Vector3(c.x, H(c.x, c.z), c.z), 4.2f, 0, "灵石祭坛", Cat.Big);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad, x = c.x + Mathf.Sin(a) * 4.6f, z = c.z + Mathf.Cos(a) * 4.6f;
                var shard = Prim(PrimitiveType.Cube, "祭坛灵晶", new Vector3(x, H(x, z) + 1.1f, z), new Vector3(.36f, 1.4f, .36f), crystalMat, Group(Cat.Prop), true);
                shard.transform.rotation = Quaternion.Euler(0, i * 90f, 8f);
            }
        }
        else if (kind == 1)
        {
            int placed = 0;
            for (int i = 0; i < 40 && placed < 8; i++)
            {
                float x = R(-Edge * .85f, Edge * .85f), z = R(-Edge * .85f, Edge * .85f); if (!Open(x, z, .3f)) continue; placed++;
                Spawn(K("rock_tall"), new Vector3(x, H(x, z), z), R(2.6f, 4f), R(0, 360), "灵石丛", Cat.Big);
                for (int j = 0; j < 2; j++)
                {
                    float px = x + R(-1.6f, 1.6f), pz = z + R(-1.6f, 1.6f);
                    var shard = Prim(PrimitiveType.Cube, "林间灵晶", new Vector3(px, H(px, pz) + .8f, pz), new Vector3(.25f, R(.9f, 1.5f), .25f), crystalMat, Group(Cat.Prop), true);
                    shard.transform.rotation = Quaternion.Euler(R(-10, 10), R(0, 360), R(-10, 10));
                }
                if (placed <= 2)
                {
                    var l = new GameObject("林间灵光").AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(.5f, 1f, .85f); l.intensity = .9f; l.range = 12f; l.shadows = LightShadows.None;
                    l.transform.position = new Vector3(x, H(x, z) + 2.4f, z);
                }
            }
        }
        else
        {
            var spots = new[] { new Vector2(-70f, -60f), new Vector2(70f, -60f), new Vector2(-70f, 60f), new Vector2(70f, 60f), new Vector2(0f, -120f), new Vector2(0f, 150f) };
            foreach (var s in spots)
            {
                float x = s.x + R(-8, 8), z = s.y + R(-8, 8);
                Spawn(K("statue_obelisk"), new Vector3(x, H(x, z), z), R(6f, 8f), R(0, 360), "古碑", Cat.Big);
                for (int j = 0; j < 3; j++)
                {
                    float px = x + R(-3f, 3f), pz = z + R(-3f, 3f);
                    var shard = Prim(PrimitiveType.Cube, "古碑晶尖", new Vector3(px, H(px, pz) + .6f, pz), new Vector3(.3f, R(.8f, 1.4f), .3f), crystalMat, Group(Cat.Prop), true);
                    shard.transform.rotation = Quaternion.Euler(R(-12, 12), R(0, 360), R(-12, 12));
                }
            }
        }
    }
    static void MakeSpring()
    {
        var dais = Prim(PrimitiveType.Cylinder, "中央灵泉 · 复活台", new Vector3(0, .12f, 30f), new Vector3(8f, .24f, 8f), stoneMat, null);
        dais.AddComponent<MeshCollider>().sharedMesh = dais.GetComponent<MeshFilter>().sharedMesh; // 可以踩上去的实体台面（扁圆柱不能用默认胶囊碰撞体，会变成大球）
        Prim(PrimitiveType.Cylinder, "中央灵泉 · 水面", new Vector3(0, .28f, 30f), new Vector3(5.6f, .1f, 5.6f), waterMat, null);
        Prim(PrimitiveType.Cylinder, "中央灵泉 · 青玉环", new Vector3(0, .36f, 30f), new Vector3(2.4f, .12f, 2.4f), crystalMat, null);
        var l = new GameObject("灵泉光").AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(.3f, .85f, 1f); l.intensity = 1.1f; l.range = 11f; l.transform.position = new Vector3(0, 1.2f, 30f);
    }
    /// 树干位置与半径（模型本地坐标）：取底部 12% 高度内的顶点，算水平重心和平均半径
    struct TrunkInfo { public float x, z, w; }
    static readonly Dictionary<Mesh, TrunkInfo> trunkCache = new Dictionary<Mesh, TrunkInfo>(); // 同一模型只算一次（地图上有上千棵树）
    static TrunkInfo Trunk(GameObject go, Bounds b)
    {
        var first = go.GetComponentInChildren<MeshFilter>(); var key = first ? first.sharedMesh : null;
        if (key && trunkCache.TryGetValue(key, out var cached)) return cached;
        var info = ComputeTrunk(go, b); if (key) trunkCache[key] = info; return info;
    }
    static TrunkInfo ComputeTrunk(GameObject go, Bounds b)
    {
        float limit = b.min.y + b.size.y * .12f; Vector3 sum = Vector3.zero; int n = 0; var pts = new List<Vector3>();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = mf.sharedMesh; if (!mesh) continue;
            var toRoot = go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach (var v in mesh.vertices) { var p = toRoot.MultiplyPoint3x4(v); if (p.y <= limit) { sum += p; n++; pts.Add(p); } }
        }
        if (n < 3) return new TrunkInfo { x = b.center.x, z = b.center.z, w = Mathf.Min(b.size.x, b.size.z) * .08f };
        float cx = sum.x / n, cz = sum.z / n, r = 0f; foreach (var p in pts) r += new Vector2(p.x - cx, p.z - cz).magnitude;
        return new TrunkInfo { x = cx, z = cz, w = r / n * 1.1f };
    }

    /// 阴影、碰撞体、远距离剔除（核心区摆放和无限区块模板共用）
    static Renderer[] FinishScenery(GameObject go, Cat cat, Bounds b, float size, float s)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        bool small = cat == Cat.Grass || cat == Cat.Flower || cat == Cat.Pebble || cat == Cat.Plant;
        foreach (var r in rs) { r.shadowCastingMode = small ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On; }
        // v0.8.1 碰撞统一：木、石、建筑是实体，草、花、蘑菇、小石子可穿。
        if (cat == Cat.Tree)
        {
            // 碰撞柱放在树干的实际位置（取模型底部 12% 高度内的顶点），之前放在整棵树包围盒中心，扭曲树、秋色树树冠偏一侧时会穿过树干、旁边却撞空气墙
            var trunk = Trunk(go, b);
            var c = go.AddComponent<CapsuleCollider>(); c.radius = Mathf.Clamp(trunk.w, .18f / s, .7f / s); c.height = size * .6f / s; c.center = new Vector3(trunk.x, b.min.y + size * .3f / s, trunk.z);
        }
        else if (cat == Cat.Rock || cat == Cat.Big)
        {
            // 石头、崖壁、石碑：贴合外形的凸包碰撞，之前的方盒在斜面石头上会出现空气墙或陷进去
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) { if (!mf.sharedMesh) continue; var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = true; }
        }
        else if (cat == Cat.Prop) { var c = go.AddComponent<BoxCollider>(); c.center = b.center; c.size = b.size * .9f; }
        else if (cat == Cat.Bush && go.name.StartsWith("灵竹")) // v0.8.4 碰撞规则：灵竹算木头，是实体；灌木、蕨类、高植物都能穿过
        { var c = go.AddComponent<CapsuleCollider>(); c.radius = Mathf.Min(b.size.x, b.size.z) * .32f; c.height = b.size.y * .85f; c.center = new Vector3(b.center.x, b.min.y + b.size.y * .45f, b.center.z); }
        float cull = cat == Cat.Grass ? .03f : cat == Cat.Flower || cat == Cat.Pebble ? .025f : cat == Cat.Plant ? .018f : cat == Cat.Bush ? .012f : cat == Cat.Rock || cat == Cat.Prop ? .008f : .003f;
        var lod = go.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(cull, rs) }); lod.RecalculateBounds();
        return rs;
    }

    // ---------------- v0.8.0 无限区块地图（参考 MC） ----------------
    // 核心区外的区块在运行时按种子生成；这里准备好各类景物的模板（已贴图、带碰撞、高度归一到典型尺寸），挂到 M1InfiniteWorld 上。
    static void MakeInfiniteWorld()
    {
        var w = new GameObject("无限地图 · 区块管理").AddComponent<M1InfiniteWorld>();
        w.kind = kind; w.seed = 770 + kind * 31; w.terrainMaterial = terrainMat; w.renderDistance = RenderDistance;
        var root = new GameObject("区块景物模板（隐藏）"); root.SetActive(false);
        GameObject[] T(IEnumerable<string> assets, Cat cat, float typical) => assets.Select(a => Template(a, cat, typical, root.transform)).Where(g => g).ToArray();
        var trees = kind == 2 ? Dead.Concat(Twisted).Select(N) : kind == 1 ? Pines.Concat(CommonTrees).Select(N) : CommonTrees.Concat(Twisted).Select(N).Concat(FallTrees.Select(K));
        w.treeTemplates = T(trees, Cat.Tree, kind == 1 ? 12f : kind == 2 ? 7f : 9f);
        w.rockTemplates = T(kind == 2 ? Rocks.Select(N).Concat(new[] { K("rock_tall"), K("stone_tall") }) : Rocks.Select(N), Cat.Rock, 2f);
        w.bushTemplates = T(Bushes.Select(N), Cat.Bush, 1.5f);
        w.plantTemplates = T(kind == 1 ? Plants.Concat(Mushrooms).Select(N) : Plants.Select(N), Cat.Plant, 1f);
        w.grassTemplates = T(Grasses.Select(N), Cat.Grass, .8f);
        w.flowerTemplates = T(Flowers.Select(N), Cat.Flower, .45f);
        // 尺寸倍率（相对模板的典型尺寸）
        w.treeSize = new Vector2(.8f, 1.25f); w.rockSize = new Vector2(.6f, 1.5f); w.bushSize = new Vector2(.75f, 1.3f);
        w.plantSize = new Vector2(.6f, 1.4f); w.grassSize = new Vector2(.7f, 1.4f); w.flowerSize = new Vector2(.7f, 1.4f);
        // 每个 40 米区块的数量（树、石、灌木、植物、草簇、花），与核心区疏密相近
        w.perChunk = kind == 1 ? new[] { new Vector2Int(5, 8), new Vector2Int(0, 2), new Vector2Int(1, 3), new Vector2Int(2, 5), new Vector2Int(2, 4), new Vector2Int(1, 3) }
                   : kind == 2 ? new[] { new Vector2Int(1, 3), new Vector2Int(2, 4), new Vector2Int(0, 1), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) }
                   : new[] { new Vector2Int(3, 6), new Vector2Int(1, 2), new Vector2Int(1, 3), new Vector2Int(2, 4), new Vector2Int(2, 4), new Vector2Int(2, 3) };
    }
    static GameObject Template(string asset, Cat cat, float size, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset); if (!prefab) return null;
        var holder = new GameObject("模板 · " + CatName(cat) + " · " + Path.GetFileNameWithoutExtension(asset)); holder.transform.SetParent(parent, false);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(holder.transform, false);
        var b = A.LocalBounds(prefab); float s = size / Mathf.Max(b.size.y, .01f);
        go.transform.localScale = Vector3.one * s; go.transform.localPosition = -Vector3.up * (b.min.y * s + (cat == Cat.Rock || cat == Cat.Big ? size * .12f : 0f));
        A.ApplyTextures(go); FinishScenery(go, cat, b, size, s);
        return holder;
    }
    static void Boundary()
    {
        var root = new GameObject("世界边界（隐形）");
        for (int i = 0; i < 4; i++)
        {
            var w = new GameObject("边界墙").AddComponent<BoxCollider>(); w.transform.SetParent(root.transform);
            bool xAxis = i < 2; float s = i % 2 == 0 ? 1 : -1;
            w.transform.position = xAxis ? new Vector3(s * Wall, 20, 0) : new Vector3(0, 20, s * Wall);
            w.size = xAxis ? new Vector3(2, 80, Size) : new Vector3(Size, 80, 2);
        }
    }

    // ---------------- 主角：Quaternius 巫师（骨骼动画） ----------------
    static M1Actor MakePlayer()
    {
        var root = new GameObject("玩家 · 山海旅者");
        var cc = root.AddComponent<CharacterController>(); cc.height = 1.75f; cc.radius = .32f; cc.center = new Vector3(0, .9f, 0); cc.slopeLimit = 50f; cc.stepOffset = .35f;
        var actor = root.AddComponent<M1Actor>(); actor.displayName = "山海旅者";
        var rig = new GameObject("角色造型").transform; rig.SetParent(root.transform, false);
        string wizard = A.V083 + "Wizard_v083.fbx"; // v0.8.3：Blender 重做的走跑（脚步速度与移动速度一致），其它动作同原版
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(wizard);
        if (prefab)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab); model.name = "巫师模型"; model.transform.SetParent(rig, false);
            var b = A.LocalBounds(prefab); float s = 1.78f / Mathf.Max(b.size.y, .01f); model.transform.localScale = Vector3.one * s; model.transform.localPosition = new Vector3(0, -b.min.y * s, 0);
            A.ApplyTextures(model);
            var anim = model.GetComponent<Animator>(); if (!anim) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = A.Controller(wizard, true); anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var hand = FindDeep(model.transform, "Weapon.R_end") ?? FindDeep(model.transform, "Weapon.R") ?? FindDeep(model.transform, "Fist.R");
            var orb = Prim(PrimitiveType.Sphere, "法杖灵晶", Vector3.zero, Vector3.one, crystalMat, hand ? hand : rig);
            orb.transform.localPosition = Vector3.zero; SetWorldScale(orb.transform, .15f);
            var glow = orb.AddComponent<Light>(); glow.type = LightType.Point; glow.color = new Color(.35f, .9f, 1f); glow.intensity = .8f; glow.range = 3f; glow.shadows = LightShadows.None;
        }
        else
        {
            Prim(PrimitiveType.Capsule, "旅者身形（缺少 Wizard.fbx）", new Vector3(0, .9f, 0), new Vector3(.6f, .88f, .6f), accentMat, rig);
            Prim(PrimitiveType.Sphere, "法杖灵晶", new Vector3(.45f, 1.3f, .2f), Vector3.one * .16f, crystalMat, rig);
        }
        root.AddComponent<M1PlayerController>(); root.AddComponent<M1ChargeWeaponSystem>(); root.AddComponent<M1CharacterAnimator>();
        return actor;
    }

    // ---------------- 守卫：Quaternius 骷髅（染成炎甲色） ----------------
    static M1Actor MakeEnemy(Vector3 at)
    {
        var root = new GameObject("星金营地守卫 · 炎甲骸卫"); root.transform.position = at;
        var actor = root.AddComponent<M1Actor>(); actor.isEnemy = true; actor.displayName = "星金营地守卫";
        string path = A.Creatures + "Skeleton.fbx";
        if (!AttachModel(root, path, 1.95f, new Color(1f, .55f, .32f), "enemy_skeleton", "炎甲骸卫"))
        { Prim(PrimitiveType.Capsule, "守卫躯干", new Vector3(0, .95f, 0), new Vector3(.9f, .9f, .8f), accentMat, root.transform); }
        var col = root.AddComponent<CapsuleCollider>(); col.height = 1.9f; col.center = new Vector3(0, .95f, 0); col.radius = .48f;
        Animate(root, path, 1.95f);
        return actor;
    }

    // ---------------- 异兽 ----------------
    static Vector3 BeastSpot(int i)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float r = 30f + i * 15.5f + attempt * 3f, a = (i * 137.5f + kind * 40f + attempt * 23f) * Mathf.Deg2Rad;
            float x = Mathf.Sin(a) * r, z = 30f + Mathf.Cos(a) * r; z = Mathf.Clamp(z, -(Edge - 40f), Edge - 40f); x = Mathf.Clamp(x, -(Edge - 40f), Edge - 40f);
            if (Mathf.Abs(x - PathCenter(z)) < 7f || (kind == 1 && Mathf.Abs(z - StreamZ(x)) < 5f)) continue;
            if (Occupied(x, z, 1.4f)) continue; // v0.8.0：不出生在树、石头、灌木里
            return new Vector3(x, H(x, z), z);
        }
        float fx = 20f + i * 4f; return new Vector3(fx, H(fx, 60f), 60f);
    }
    static M1Actor MakeBeast(Species sp, Vector3 at)
    {
        var root = new GameObject(sp.name + " · 野生异兽"); root.transform.position = at; root.transform.rotation = Quaternion.Euler(0, R(0, 360), 0);
        bool ok = sp.model != null && AttachModel(root, sp.model, sp.height, sp.tint, sp.id, sp.name);
        if (!ok) Turtle(root.transform, sp.tint);
        float h = Mathf.Max(sp.height, .9f);
        var col = root.AddComponent<CapsuleCollider>(); col.height = Mathf.Clamp(h, 1f, 2.6f); col.radius = Mathf.Clamp(h * .32f, .35f, .9f); col.center = new Vector3(0, col.height * .5f, 0);
        if (sp.flight != Flight.None) { col.height = Mathf.Min(col.height, 1.4f); col.center = new Vector3(0, col.height * .5f, 0); } // 飞行类身体小，碰撞体随模型升空
        var actor = root.AddComponent<M1Actor>(); actor.isTameable = true; actor.displayName = sp.name; actor.speciesId = sp.id;
        root.AddComponent<M1SpiritBeastMotion>(); Animate(root, sp.model, sp.height);
        if (sp.flight == Flight.None && ok && sp.id != "spirit_beast") root.AddComponent<M1GroundAlign>(); // 四足动物随坡面倾斜；两足的幽蓝翼兽保持直立
        if (sp.flight != Flight.None)
        {
            var fly = root.AddComponent<M1FlyerMotion>();
            // v0.8.0：离地高度明显高于体型，看起来是在空中飞，而不是贴着地面“飞”
            if (sp.flight == Flight.Perch) { fly.style = M1FlyerMotion.Style.Perch; fly.hoverHeight = 2.6f; fly.bobAmplitude = .18f; fly.bobFrequency = 1.4f; fly.swayAmplitude = .12f; }
            else if (sp.id == "moon_moth") { fly.style = M1FlyerMotion.Style.Hover; fly.hoverHeight = 2.6f; fly.bobAmplitude = .24f; fly.bobFrequency = 2.2f; fly.swayAmplitude = .3f; fly.maxBank = 30f; } // v0.8.3：新蝴蝶自带扑翼起伏，飞行组件的起伏调小 // 蝶：忽上忽下地扑扇
            else { fly.style = M1FlyerMotion.Style.Hover; fly.hoverHeight = 3.4f; fly.bobAmplitude = .35f; fly.bobFrequency = .4f; fly.swayAmplitude = .45f; fly.maxBank = 18f; } // 蛟：在空中慢速游动
        }
        return actor;
    }
    /// 动画驱动：按体型换算速度；没有奔跑动作的模型走得快时加快步频
    static M1CreatureAnimator Animate(GameObject root, string path, float height)
    {
        var ad = root.AddComponent<M1CreatureAnimator>(); ad.bodySize = Mathf.Max(height, .5f); ad.hasRun = path != null && A.HasRunClip(path); ad.attackHitDelay = HitDelay(path); return ad;
    }
    /// v0.8.3 攻击命中帧（秒），来自阶段 C 清单：伤害在这个时间点结算，和攻击动作对齐
    static float HitDelay(string path)
    {
        string n = path == null ? "" : Path.GetFileNameWithoutExtension(path);
        if (n.StartsWith("Pig_v083")) return .6f;             // 冲撞
        if (n.StartsWith("RockTurtle")) return .4f;           // 伸颈咬
        if (n.StartsWith("Mink") || n.StartsWith("Leopard") || n.StartsWith("Lynx")) return .45f; // 扑击挥爪
        if (n.StartsWith("SpiritSparrow") || n.StartsWith("Vermilion") || n.StartsWith("Crane")) return .5f; // 俯冲啄击
        if (n.StartsWith("Eagle_v083")) return .58f;
        if (n.StartsWith("Butterfly")) return .33f;
        if (n.EndsWith("_v084")) return .46f;                 // v0.8.4 蹄类：后踢 / 冲顶，第 11 帧命中
        return .25f;
    }
    /// 把模型挂到 root 下：统一高度、贴地、按物种调色、自动接上动画控制器
    static bool AttachModel(GameObject root, string path, float height, Color tint, string key, string displayName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!prefab) return false;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab); model.name = displayName + " · 模型（" + Path.GetFileNameWithoutExtension(path) + "）"; model.transform.SetParent(root.transform, false);
        var b = A.LocalBounds(prefab); float s = height / Mathf.Max(b.size.y, .01f);
        var yaw = Quaternion.Euler(0f, ModelYaw(path), 0f); Vector3 c = yaw * b.center;
        model.transform.localScale = Vector3.one * s; model.transform.localRotation = yaw; model.transform.localPosition = new Vector3(-c.x * s, -b.min.y * s, -c.z * s);
        A.ApplyTextures(model); A.Tint(model, key, tint);
        var ctrl = A.Controller(path, false);
        if (ctrl) { var anim = model.GetComponent<Animator>(); if (!anim) anim = model.AddComponent<Animator>(); anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false; }
        return true;
    }
    /// v0.8.0 朝向修正：游戏里“前方”是 +Z。逐个模型实测（头部、前腿骨骼相对包围盒的位置）：
    /// 猫、狗、狼（Quaternius Animal Pack Vol.2）身体沿 X 轴、头朝 -X，之前一直侧着身子横着走；
    /// 岚角灵兽、岩甲灵龟（Blender 正面朝 +Y 导出）头朝 -Z，之前是倒着走。
    static float ModelYaw(string path)
    {
        string n = Path.GetFileNameWithoutExtension(path);
        if (n.StartsWith("Cat") || n.StartsWith("Dog") || n.StartsWith("Wolf")) return 90f;
        if (n.StartsWith("LanJiao") || n.StartsWith("StoneTurtle")) return 180f;
        return 0f;
    }
    static void Turtle(Transform r, Color tint)
    {
        var shell = Mat(tint); var belly = Mat(Color.Lerp(tint, new Color(.85f, .8f, .6f), .6f));
        Prim(PrimitiveType.Sphere, "龟甲", new Vector3(0, .55f, 0), new Vector3(1.5f, .8f, 1.7f), shell, r);
        for (int i = 0; i < 6; i++) { float a = i / 6f * 6.28f; Prim(PrimitiveType.Sphere, "甲纹", new Vector3(Mathf.Sin(a) * .42f, .86f, Mathf.Cos(a) * .48f), new Vector3(.42f, .16f, .42f), shellDark, r); }
        Prim(PrimitiveType.Sphere, "甲顶岩晶", new Vector3(0, .98f, 0), new Vector3(.4f, .22f, .4f), crystalMat, r);
        Prim(PrimitiveType.Cylinder, "腹甲", new Vector3(0, .3f, 0), new Vector3(1.45f, .08f, 1.6f), belly, r);
        Prim(PrimitiveType.Sphere, "龟首", new Vector3(0, .55f, 1.0f), new Vector3(.48f, .42f, .55f), belly, r);
        Prim(PrimitiveType.Sphere, "左眼", new Vector3(-.14f, .66f, 1.22f), new Vector3(.09f, .09f, .06f), eyeMat, r); Prim(PrimitiveType.Sphere, "右眼", new Vector3(.14f, .66f, 1.22f), new Vector3(.09f, .09f, .06f), eyeMat, r);
        for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) Prim(PrimitiveType.Capsule, "龟足", new Vector3(sx * .62f, .2f, sz * .55f), new Vector3(.3f, .22f, .3f), belly, r);
    }

    // ---------------- 氛围生物：牧群与飞鸟 ----------------
    static void MakeAmbientLife()
    {
        var root = new GameObject("氛围生物").transform;
        // v0.8.0：牧群用中文名；营地的巴哥犬换成柴犬，灵林的黄牛换成鹿群，牧群有实体碰撞不再能穿过去
        string[] herd = kind == 0 ? new[] { "Sheep", "Llama", "Horse", "ShibaInu" } : kind == 1 ? new[] { "Deer", "Horse", "Slime", "Sheep" } : new[] { "Zebra", "Llama", "Skeleton", "Slime" };
        int made = 0;
        for (int tries = 0; tries < 300 && made < Mathf.RoundToInt(8 * Dens); tries++)
        {
            float x = R(-Edge * .8f, Edge * .8f), z = R(-Edge * .6f, Edge * .8f); if (!Open(x, z, .5f) || new Vector2(x, z - 30f).magnitude < 50f) continue;
            made++;
            int type = rng.Next(herd.Length); string kindName = herd[type]; int count = kindName == "ShibaInu" ? 3 : rng.Next(3, 7);
            var info = Ambient(kindName);
            for (int i = 0; i < count; i++)
            {
                float px = x + R(-6, 6), pz = z + R(-6, 6);
                if (Occupied(px, pz, .9f)) continue; // v0.8.0：不出生在树、石头、灌木里
                var go = new GameObject(info.name + " · 牧群"); go.transform.SetParent(root); go.transform.position = new Vector3(px, H(px, pz), pz); go.transform.rotation = Quaternion.Euler(0, R(0, 360), 0);
                Color tint = kindName == "Slime" ? (kind == 1 ? new Color(.4f, .9f, .55f) : new Color(.85f, .55f, .9f)) : kindName == "Skeleton" ? new Color(.9f, .85f, .7f) : Color.white;
                float h = info.height * R(.9f, 1.1f);
                if (!AttachModel(go, info.path, h, tint, "ambient_" + kindName + kind, info.name)) { Object.DestroyImmediate(go); continue; }
                Animate(go, info.path, h); var c = go.AddComponent<M1AmbientCritter>(); c.radius = 9f; c.speed = kindName == "Slime" ? .7f : 1.1f;
                SolidBody(go, h); if (kindName != "Slime" && kindName != "Skeleton") go.AddComponent<M1GroundAlign>();
                LifeActor(go, info, h);
            }
        }
        string flyer = kind == 2 ? "Bat" : "Eagle"; string flyerName = kind == 2 ? "夜蝠" : "苍鹰"; string flyerPath = kind == 2 ? A.Creatures + "Bat.fbx" : A.V083 + "Eagle_v083.fbx"; // v0.8.3：苍鹰换成修正朝向的新版（原版是倒着飞的）
        for (int i = 0; i < 5; i++)
        {
            float x = R(-Edge * .7f, Edge * .7f), z = R(-Edge * .5f, Edge * .7f);
            var go = new GameObject(flyerName + " · 盘旋飞鸟"); go.transform.SetParent(root); go.transform.position = new Vector3(x, H(x, z), z);
            float h = flyer == "Bat" ? 1.2f : 1.5f;
            if (!AttachModel(go, flyerPath, h, Color.white, "ambient_" + flyer, flyerName)) { Object.DestroyImmediate(go); continue; }
            Animate(go, flyerPath, h); var c = go.AddComponent<M1AmbientCritter>(); c.mode = M1AmbientCritter.Mode.Circle; c.radius = R(12f, 24f); c.height = R(9f, 15f); c.speed = R(1.6f, 2.6f); // v0.8.4：飞低一些，能被射中
            go.transform.position += Vector3.up * c.height; // 一开始就在空中（远处的飞鸟不更新，之前会停在地上）
            SolidBody(go, h);
            LifeActor(go, new AmbientInfo { name = flyerName, id = flyer, hp = flyer == "Bat" ? 30f : 45f, hostile = true }, h).selfMoving = true;
        }
    }
    /// v0.8.4 氛围动物也有名字、血量，能被打死：家畜和野兽是可猎杀动物（挨打逃跑），斑马 / 骸骨 / 灵胶 / 飞鸟是怪物（红名）
    static M1Actor LifeActor(GameObject go, AmbientInfo info, float h)
    {
        var a = go.AddComponent<M1Actor>(); a.displayName = info.name; a.speciesId = "ambient_" + info.id; a.maxHealth = a.health = info.hp;
        if (info.hostile) { a.isEnemy = true; a.aggressive = info.aggressive; a.attackDamage = info.damage; a.aggroRange = info.aggro; }
        else { a.isWildlife = true; a.fleeSpeed = Mathf.Clamp(h * 2.2f, 2.6f, 4.6f); }
        return a;
    }
    struct AmbientInfo { public string name, path, id; public float height, hp, damage, aggro; public bool hostile, aggressive; }
    static AmbientInfo Ambient(string model)
    {
        switch (model)
        {
            case "Sheep": return new AmbientInfo { name = "绵羊", id = model, path = A.V084 + "Sheep_v084.fbx", height = 1.1f, hp = 45f };
            case "Llama": return new AmbientInfo { name = "羊驼", id = model, path = A.Animals + "Alpaca.fbx", height = 1.6f, hp = 60f };
            case "Horse": return new AmbientInfo { name = "骏马", id = model, path = A.V084 + "Horse_v084.fbx", height = 1.7f, hp = 80f };
            case "ShibaInu": return new AmbientInfo { name = "柴犬", id = model, path = A.Animals + "ShibaInu.fbx", height = .65f, hp = 35f };
            case "Deer": return new AmbientInfo { name = "林鹿", id = model, path = A.Animals + "Deer.fbx", height = 1.5f, hp = 55f };
            case "Slime": return new AmbientInfo { name = "灵胶", id = model, path = A.Creatures + "Slime.fbx", height = .6f, hp = 40f, hostile = true, aggressive = true, damage = 4f, aggro = 8f };
            case "Zebra": return new AmbientInfo { name = "斑马", id = model, path = A.V084 + "Zebra_v084.fbx", height = 1.7f, hp = 90f, hostile = true, aggressive = false, damage = 7f, aggro = 13f }; // 挨打才反击
            case "Skeleton": return new AmbientInfo { name = "游荡骸骨", id = model, path = A.Creatures + "Skeleton.fbx", height = 1.7f, hp = 85f, hostile = true, aggressive = true, damage = 6f, aggro = 10f };
            default: return new AmbientInfo { name = model, id = model, path = A.Creatures + model + ".fbx", height = 1.2f, hp = 50f };
        }
    }
    /// 会移动的氛围动物：胶囊碰撞体 + 运动学刚体，玩家撞上会被挡住，不会穿模
    static void SolidBody(GameObject go, float height)
    {
        var col = go.AddComponent<CapsuleCollider>(); col.height = Mathf.Max(height, .5f); col.radius = Mathf.Clamp(height * .3f, .22f, .6f); col.center = new Vector3(0, col.height * .5f, 0);
        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
    }

    // ---------------- 采集点与工坊 ----------------
    static void MakeResources(Transform player)
    {
        var root = new GameObject("采集点").transform;
        overlapCheck = false; // 采集点的树/石是固定摆放的，不参与防重叠判定
        for (int i = 0; i < 44; i++)
        {
            bool ore = i % 2 == 1; float x, z; int guard = 0;
            do { float along = R(-40f, 225f); double pick = rng.NextDouble(); bool cross = pick < .35; if (pick > .85) { x = R(-Edge * .8f, Edge * .8f); z = R(-Edge * .8f, Edge * .8f); } else if (cross) { x = R(-200, 200); z = CrossZ(x) + (rng.NextDouble() < .5 ? -1 : 1) * R(8, 22); } else { z = along; x = PathCenter(z) + (rng.NextDouble() < .5 ? -1 : 1) * R(9, 30); } }
            while (++guard < 20 && (!Open(x, z, .6f) || new Vector2(x, z - 30f).magnitude < 14f || Occupied(x, z, 2.2f)));
            var node = new GameObject(ore ? "青玉矿点" : "灵木采集点"); node.transform.SetParent(root); node.transform.position = new Vector3(x, H(x, z), z); Register(x, z, 6f); // 节点之间至少隔开约 7 米
            if (ore)
            {
                var rock = Spawn(N(Pick(Rocks)), node.transform.position, R(1.4f, 2f), R(0, 360), "矿脉", Cat.Rock); if (rock) rock.transform.SetParent(node.transform, true); // v0.8.1：按石头处理（贴合外形的碰撞），不再是整块方盒
                for (int s = 0; s < 3; s++) { var sh = Prim(PrimitiveType.Cube, "青玉晶尖", new Vector3(R(-.4f, .4f), R(1f, 1.6f), R(-.4f, .4f)), new Vector3(.22f, R(.6f, 1f), .22f), jadeMat, node.transform); sh.transform.localRotation = Quaternion.Euler(R(-20, 20), R(0, 360), R(-20, 20)); }
            }
            else { var tree = Spawn(N(Pick(CommonTrees)), node.transform.position, R(4.5f, 6f), R(0, 360), "灵木", Cat.Tree); if (tree) tree.transform.SetParent(node.transform, true); Prim(PrimitiveType.Sphere, "灵木灵光", new Vector3(0, 1.2f, 0), Vector3.one * .3f, crystalMat, node.transform); } // v0.8.1：按树处理（碰撞柱在树干上），不再是罩住整个树冠的空气墙
            var col = node.AddComponent<BoxCollider>(); col.center = new Vector3(0, 1f, 0); col.size = new Vector3(1.6f, 2f, 1.6f);
            var h = node.AddComponent<M1HarvestNode>(); h.resource = ore ? "矿石" : "木材"; h.amount = 2; h.remaining = 4; h.player = player;
            Label(node.transform, ore ? "F · 青玉矿（镐 / 灵契符）" : "F · 灵木（斧 / 灵契符）", 3.2f);
        }
        var bench = Prim(PrimitiveType.Cube, "星金工坊工作台", new Vector3(5, .55f, 8), new Vector3(1.6f, 1.1f, .9f), Mat(new Color(.36f, .23f, .13f)), null);
        Prim(PrimitiveType.Cube, "台面", new Vector3(0, .5f, 0), new Vector3(1.08f, .1f, 1.15f), Mat(new Color(.6f, .4f, .2f)), bench.transform);
        Prim(PrimitiveType.Cube, "铁砧", new Vector3(.2f, .72f, 0), new Vector3(.24f, .22f, .32f), Mat(new Color(.45f, .5f, .52f)), bench.transform);
        bench.AddComponent<BoxCollider>(); Label(bench.transform, "B · 星金工坊", 1.4f);
        overlapCheck = true;
    }
    static void Label(Transform parent, string text, float y)
    {
        var go = new GameObject("提示牌"); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0, y, 0);
        var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.fontSize = 28; tm.characterSize = .05f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(.98f, .88f, .6f);
        go.AddComponent<M1BillboardLabel>();
    }

    // ---------------- 传送门 ----------------
    static void MakePortal(string scene, string name, Vector3 at)
    {
        at.y = H(at.x, at.z);
        var root = new GameObject("传送门 · " + name); root.transform.position = at;
        var portal = root.AddComponent<M1Portal>(); portal.targetScene = Path.GetFileNameWithoutExtension(scene); portal.destinationName = name; portal.interactDistance = 3.4f;
        Prim(PrimitiveType.Cylinder, "门台", new Vector3(0, .1f, 0), new Vector3(3.2f, .1f, 3.2f), stoneMat, root.transform);
        Prim(PrimitiveType.Cube, "左门柱", new Vector3(-1.2f, 1.9f, 0), new Vector3(.32f, 3.8f, .36f), accentMat, root.transform);
        Prim(PrimitiveType.Cube, "右门柱", new Vector3(1.2f, 1.9f, 0), new Vector3(.32f, 3.8f, .36f), accentMat, root.transform);
        Prim(PrimitiveType.Cube, "门楣", new Vector3(0, 3.85f, 0), new Vector3(3.1f, .3f, .4f), accentMat, root.transform);
        Prim(PrimitiveType.Cube, "灵光门面", new Vector3(0, 1.85f, 0), new Vector3(2.1f, 3.4f, .06f), gateMat, root.transform);
        var l = new GameObject("门光").AddComponent<Light>(); l.transform.SetParent(root.transform, false); l.transform.localPosition = new Vector3(0, 2f, .6f); l.type = LightType.Point; l.color = new Color(.3f, .8f, 1f); l.intensity = 1.6f; l.range = 9f;
        Label(root.transform, "前往 · " + name, 4.6f);
    }

    /// v0.7.9 飘散灵尘：缓慢漂浮的淡黄光点，覆盖整张大地图，让空气更有层次
    static void AddPollen(int count)
    {
        var go = new GameObject("飘散灵尘"); go.transform.position = new Vector3(0, 2.5f, 0);
        var ps = go.AddComponent<ParticleSystem>(); var main = ps.main;
        main.loop = true; main.startLifetime = 9f; main.startSpeed = .12f; main.startSize = .05f; main.startColor = new Color(1f, .98f, .8f, .5f); main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(Edge * 1.4f, 7f, Edge * 1.4f);
        var em = ps.emission; em.rateOverTime = count * .3f;
        var r = go.GetComponent<ParticleSystemRenderer>(); r.material = new Material(Shader.Find("Sprites/Default")); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static void AddFireflies(Vector3 center, int count)
    {
        var go = new GameObject("灵萤粒子"); go.transform.position = center; var ps = go.AddComponent<ParticleSystem>(); var main = ps.main;
        main.loop = true; main.startLifetime = 7f; main.startSpeed = .1f; main.startSize = .1f; main.startColor = new Color(.5f, .95f, 1f, .8f); main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 22f; var em = ps.emission; em.rateOverTime = count * .5f;
        var r = go.GetComponent<ParticleSystemRenderer>(); r.material = new Material(Shader.Find("Sprites/Default")); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ---------------- 小工具 ----------------
    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, bool solid = false)
    {
        var o = GameObject.CreatePrimitive(type); o.name = name; if (parent) o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
        var c = o.GetComponent<Collider>(); if (c && !solid) Object.DestroyImmediate(c); var r = o.GetComponent<Renderer>(); if (r && mat) r.sharedMaterial = mat; return o;
    }
    static Transform FindDeep(Transform t, string name) { if (t.name == name) return t; foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; } return null; }
    static void SetWorldScale(Transform t, float s) { var ls = t.lossyScale; t.localScale = new Vector3(s / Mathf.Max(ls.x, 1e-5f), s / Mathf.Max(ls.y, 1e-5f), s / Mathf.Max(ls.z, 1e-5f)); }
    static Material Mat(Color c)
    {
        string dir = "Assets/Art/Materials/v0.7.7/"; Directory.CreateDirectory(dir); string path = dir + "C_" + ColorUtility.ToHtmlStringRGB(c) + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (!m) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.color = c; m.SetFloat("_Glossiness", .07f); m.SetFloat("_Metallic", 0f); m.mainTexture = Mottle(); m.mainTextureScale = new Vector2(.35f, .35f); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static Material Glow(Color c, float power)
    {
        var m = Mat(c); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * power); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; EditorUtility.SetDirty(m); return m;
    }
}
