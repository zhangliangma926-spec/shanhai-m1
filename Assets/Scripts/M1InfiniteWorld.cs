using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无限地图 + 区块渲染距离（v0.8.0，参考 MC）：
/// - 世界按 40 米区块划分。以角色为中心、renderDistance（默认 100 米）以内的区块才存在和显示，范围外是“虚无”，由雾遮住边缘。
/// - 800 米核心区（营地、道路、传送门、灵兽）是编辑器里布置好的；核心区里的树、石、草等景物也按区块显示 / 隐藏。
/// - 核心区之外的区块由“种子 + 区块坐标”在运行时生成：地形网格（与核心地形同一个高度函数 M1TerrainNoise，接缝无缝）、
///   树木、岩石、灌木、草、花。同一个区块每次生成的结果完全一样，所以离开再回来看到的是同一片山野。
/// - 每帧最多生成 1 个区块，离开范围的区块销毁（回来再按种子重建），避免一次性卡顿。
/// </summary>
public class M1InfiniteWorld : MonoBehaviour
{
    public static M1InfiniteWorld Instance { get; private set; }
    public int kind;
    public int seed = 770;
    public float chunkSize = 40f;
    public float renderDistance = 100f;
    public Material terrainMaterial;
    [Tooltip("外围区块用的景物模板（编辑器生成，未激活）。每个模板高度已归一到 1 米，生成时乘以尺寸。")]
    public GameObject[] treeTemplates, rockTemplates, bushTemplates, plantTemplates, grassTemplates, flowerTemplates;
    public Vector2 treeSize = new Vector2(7f, 11f), rockSize = new Vector2(1.2f, 3f), bushSize = new Vector2(1.1f, 2f), plantSize = new Vector2(.6f, 1.4f), grassSize = new Vector2(.55f, 1.1f), flowerSize = new Vector2(.3f, .6f);
    [Tooltip("每个区块的景物数量（树、石、灌木、植物、草簇、花）")]
    public Vector2Int[] perChunk = new Vector2Int[0];

    const int Cells = 15;   // 40 米 / 15 格 = 2.667 米，与 800 米核心地形（300 格）的网格完全对齐
    readonly Dictionary<Vector2Int, GameObject> loaded = new Dictionary<Vector2Int, GameObject>();
    readonly Dictionary<Vector2Int, List<GameObject>> coreObjects = new Dictionary<Vector2Int, List<GameObject>>();
    readonly HashSet<Vector2Int> coreVisible = new HashSet<Vector2Int>();
    readonly List<Vector2Int> wanted = new List<Vector2Int>();
    Vector2Int lastCenter = new Vector2Int(int.MinValue, int.MinValue);
    Transform chunkRoot;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        chunkRoot = new GameObject("无限区块（运行时生成）").transform;
        // 核心区景物登记到区块：只收集“景物 · *”分组下的物体（树、石、草、花、道具等）
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.parent || !t.name.StartsWith("景物 · ")) continue; // 采集点有自己的逻辑，不参与隐藏
            foreach (Transform child in t)
            {
                var key = ChunkOf(child.position);
                if (!coreObjects.TryGetValue(key, out var list)) coreObjects[key] = list = new List<GameObject>();
                list.Add(child.gameObject);
            }
        }
        foreach (var kv in coreObjects) foreach (var go in kv.Value) go.SetActive(false); // 先全部隐藏，下面按距离显示
        Refresh(true);
    }

    Vector2Int ChunkOf(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / chunkSize), Mathf.FloorToInt(p.z / chunkSize));
    bool InCore(Vector2Int c) // 区块完全在 800 米核心区内：地形已由编辑器生成
    {
        float h = M1TerrainNoise.CoreHalf;
        return c.x * chunkSize >= -h && (c.x + 1) * chunkSize <= h && c.y * chunkSize >= -h && (c.y + 1) * chunkSize <= h;
    }
    float ChunkDistance(Vector2Int c, Vector3 p)
    {
        float cx = Mathf.Clamp(p.x, c.x * chunkSize, (c.x + 1) * chunkSize), cz = Mathf.Clamp(p.z, c.y * chunkSize, (c.y + 1) * chunkSize);
        return new Vector2(p.x - cx, p.z - cz).magnitude; // 到区块最近点的距离
    }

    void Update() { Refresh(false); }

    void Refresh(bool force)
    {
        var gm = M1GameManager.Instance; var cam = Camera.main;
        Transform focus = gm && gm.player ? gm.player.transform : cam ? cam.transform : null; if (!focus) return;
        Vector3 p = focus.position; var center = ChunkOf(p);
        if (force || center != lastCenter)
        {
            lastCenter = center; wanted.Clear();
            int r = Mathf.CeilToInt(renderDistance / chunkSize) + 1;
            for (int dx = -r; dx <= r; dx++) for (int dz = -r; dz <= r; dz++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dz);
                if (ChunkDistance(c, p) <= renderDistance) wanted.Add(c);
            }
            wanted.Sort((a, b) => ChunkDistance(a, p).CompareTo(ChunkDistance(b, p)));
            // 核心区景物：范围内显示，范围外隐藏（MC 式渲染距离）
            var keep = new HashSet<Vector2Int>(wanted);
            foreach (var c in new List<Vector2Int>(coreVisible)) if (!keep.Contains(c)) { SetCore(c, false); coreVisible.Remove(c); }
            foreach (var c in wanted) if (!coreVisible.Contains(c)) { SetCore(c, true); coreVisible.Add(c); }
            // 外围区块：离开范围（多留一个区块的缓冲，来回走不会反复生成）就销毁
            var drop = new List<Vector2Int>();
            foreach (var kv in loaded) if (ChunkDistance(kv.Key, p) > renderDistance + chunkSize) drop.Add(kv.Key);
            foreach (var c in drop) { Destroy(loaded[c]); loaded.Remove(c); }
        }
        // 每帧最多生成一个外围区块，由近到远
        foreach (var c in wanted)
        {
            if (loaded.ContainsKey(c) || InCore(c)) continue;
            loaded[c] = BuildChunk(c); break;
        }
    }

    void SetCore(Vector2Int c, bool on) { if (coreObjects.TryGetValue(c, out var list)) foreach (var go in list) if (go) go.SetActive(on); }

    GameObject BuildChunk(Vector2Int c)
    {
        var root = new GameObject("区块 " + c.x + "," + c.y); root.transform.SetParent(chunkRoot, false);
        float x0 = c.x * chunkSize, z0 = c.y * chunkSize, step = chunkSize / Cells;
        // 1. 地形网格（只生成核心区以外的部分）
        int side = Cells + 1; var v = new Vector3[side * side]; var col = new Color[v.Length]; var uv = new Vector2[v.Length]; var tri = new List<int>(Cells * Cells * 6);
        for (int ix = 0; ix <= Cells; ix++) for (int iz = 0; iz <= Cells; iz++)
        {
            int i = ix * side + iz; float x = x0 + ix * step, z = z0 + iz * step, y = M1TerrainNoise.Height(kind, x, z);
            v[i] = new Vector3(x, y, z); col[i] = M1TerrainNoise.GroundColor(kind, x, z, y); uv[i] = new Vector2(x / 10f, z / 10f);
        }
        float h = M1TerrainNoise.CoreHalf;
        for (int ix = 0; ix < Cells; ix++) for (int iz = 0; iz < Cells; iz++)
        {
            float cx = x0 + (ix + .5f) * step, cz = z0 + (iz + .5f) * step;
            if (Mathf.Abs(cx) < h && Mathf.Abs(cz) < h) continue; // 核心区里已有地形，跳过这一格
            int a = ix * side + iz, b = (ix + 1) * side + iz, cc = a + 1, d = b + 1;
            tri.Add(a); tri.Add(cc); tri.Add(b); tri.Add(b); tri.Add(cc); tri.Add(d);
        }
        if (tri.Count > 0)
        {
            var mesh = new Mesh { name = root.name }; mesh.vertices = v; mesh.colors = col; mesh.uv = uv; mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var ground = new GameObject("地形"); ground.transform.SetParent(root.transform, false);
            ground.AddComponent<MeshFilter>().sharedMesh = mesh; ground.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            ground.AddComponent<MeshCollider>().sharedMesh = mesh; ground.AddComponent<M1GroundSurface>();
        }
        // 2. 景物：种子 + 区块坐标决定，离开再回来完全一样
        var rng = new System.Random(unchecked(seed * 73856093 ^ kind * 19349663 ^ c.x * 83492791 ^ c.y * 50331653));
        var placed = new List<Vector3>();
        int Count(int idx) { if (perChunk == null || idx >= perChunk.Length) return 0; var r = perChunk[idx]; return rng.Next(r.x, r.y + 1); }
        Scatter(root.transform, rng, treeTemplates, treeSize, Count(0), x0, z0, placed, 2.2f, true);
        Scatter(root.transform, rng, rockTemplates, rockSize, Count(1), x0, z0, placed, 1.4f, false);
        Scatter(root.transform, rng, bushTemplates, bushSize, Count(2), x0, z0, placed, 1f, false);
        Scatter(root.transform, rng, plantTemplates, plantSize, Count(3), x0, z0, placed, 0f, false);
        int clusters = Count(4);
        for (int k = 0; k < clusters; k++)
        {
            float cx = x0 + (float)rng.NextDouble() * chunkSize, cz = z0 + (float)rng.NextDouble() * chunkSize; int blades = rng.Next(4, 10);
            for (int j = 0; j < blades; j++) Place(root.transform, rng, grassTemplates, grassSize, cx + R(rng, -2.2f, 2.2f), cz + R(rng, -2.2f, 2.2f), placed, 0f, false);
        }
        Scatter(root.transform, rng, flowerTemplates, flowerSize, Count(5), x0, z0, placed, 0f, false);
        return root;
    }

    static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    void Scatter(Transform parent, System.Random rng, GameObject[] pool, Vector2 size, int count, float x0, float z0, List<Vector3> placed, float footprint, bool clump)
    {
        for (int i = 0; i < count; i++)
        {
            float x = x0 + (float)rng.NextDouble() * chunkSize, z = z0 + (float)rng.NextDouble() * chunkSize;
            if (clump && Mathf.PerlinNoise((x + 900f) * .018f, (z + 400f) * .018f) < .4f) continue; // 树木成片，空地与林地交替
            Place(parent, rng, pool, size, x, z, placed, footprint, footprint > 0f);
        }
    }
    void Place(Transform parent, System.Random rng, GameObject[] pool, Vector2 size, float x, float z, List<Vector3> placed, float footprint, bool register)
    {
        if (pool == null || pool.Length == 0) return;
        var tpl = pool[rng.Next(pool.Length)]; float s = R(rng, size.x, size.y), yaw = R(rng, 0f, 360f);
        if (Mathf.Abs(x) < M1TerrainNoise.CoreHalf && Mathf.Abs(z) < M1TerrainNoise.CoreHalf) return; // 核心区由编辑器布置
        foreach (var q in placed) if ((q.x - x) * (q.x - x) + (q.z - z) * (q.z - z) < (q.y + Mathf.Max(footprint, .4f)) * (q.y + Mathf.Max(footprint, .4f)) * .7f) return; // 防穿模：不和已摆放的实体重叠
        if (!tpl) return;
        float y = M1TerrainNoise.Height(kind, x, z);
        // 取周围 4 点里最低的地面，坡上不悬空
        float k = Mathf.Max(.4f, footprint * .7f);
        y = Mathf.Min(y, Mathf.Min(Mathf.Min(M1TerrainNoise.Height(kind, x + k, z), M1TerrainNoise.Height(kind, x - k, z)), Mathf.Min(M1TerrainNoise.Height(kind, x, z + k), M1TerrainNoise.Height(kind, x, z - k)))) - .02f;
        var go = Instantiate(tpl, new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f), parent);
        go.transform.localScale = tpl.transform.localScale * s; go.SetActive(true);
        if (register) placed.Add(new Vector3(x, footprint, z));
    }
}
