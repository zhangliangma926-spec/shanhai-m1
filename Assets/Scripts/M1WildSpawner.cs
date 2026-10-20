using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v0.7.9 概率刷怪 + 100 米刷新圈（按 H 显示 / 隐藏圆圈）。
/// - 以角色为中心画一个 100 米半径的淡青色圆圈，贴合地面。
/// - 角色在圈内活动时，每隔 spawnInterval 秒按 spawnChance 概率，在 30–100 米处刷出一只随机灵兽。
/// - 圈内同时存在的刷出灵兽有上限；离开 despawnDistance 之外的刷出灵兽会消失。
/// - 结契成功的灵兽不会被清除；被击败的刷出灵兽会被移除。
/// - 灵兽模板取自场景中已摆放的同种异兽（20 种全部有），运行时克隆，不额外加载资源。
/// </summary>
public class M1WildSpawner : MonoBehaviour
{
    public float radius = 100f, minSpawnDistance = 30f, despawnDistance = 150f;
    public float spawnInterval = 6f, spawnChance = .4f;
    public int maxSpawnedNearby = 6;
    const int RingSegments = 96;

    readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
    readonly List<string> speciesKeys = new List<string>();
    readonly List<M1Actor> spawned = new List<M1Actor>();
    MeshCollider[] terrain = new MeshCollider[0];
    LineRenderer ring;
    Vector3 ringCenter;
    bool ringDirty = true, showRing = true;
    float timer = 5f;

    void Start()
    {
        // 模板放在一个未激活的隐藏父物体下：克隆时 Awake 要等到设置好位置再执行，home 才是正确的
        var holder = new GameObject("灵兽刷新模板（隐藏）");
        holder.SetActive(false);
        foreach (var a in M1Actor.All)
        {
            if (!a || !a.isTameable || a.tamed || templates.ContainsKey(a.speciesId)) continue;
            templates[a.speciesId] = Instantiate(a.gameObject, holder.transform);
            speciesKeys.Add(a.speciesId);
        }

        var cols = new List<MeshCollider>();
        foreach (var mc in FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude))
            if (mc.name.Contains("地形")) cols.Add(mc);
        terrain = cols.ToArray();

        var ringObj = new GameObject("灵圈 · 100 米刷新范围");
        ring = ringObj.AddComponent<LineRenderer>();
        ring.useWorldSpace = true; ring.loop = true; ring.positionCount = RingSegments; ring.widthMultiplier = .16f;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.startColor = ring.endColor = new Color(.36f, .86f, .8f, .45f);
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ring.receiveShadows = false;
        ring.enabled = false;
    }

    void Update()
    {
        var gm = M1GameManager.Instance;
        if (!gm || !gm.player || !gm.IsGameplayActive) { if (ring) ring.enabled = false; return; }
        if (Input.GetKeyDown(KeyCode.H)) showRing = !showRing;

        Vector3 p = gm.player.transform.position;
        ring.enabled = showRing;
        if (showRing && (ringDirty || (p - ringCenter).sqrMagnitude > 4f))
        {
            ringCenter = p; ringDirty = false; RebuildRing();
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;
        CullSpawned(p);
        if (templates.Count == 0 || CountNearby(p) >= maxSpawnedNearby) return;
        if (Random.value <= spawnChance) TrySpawn(p);
    }

    void RebuildRing()
    {
        for (int i = 0; i < RingSegments; i++)
        {
            float a = i * Mathf.PI * 2f / RingSegments;
            float x = ringCenter.x + Mathf.Sin(a) * radius, z = ringCenter.z + Mathf.Cos(a) * radius;
            ring.SetPosition(i, new Vector3(x, GroundAt(x, z) + .15f, z));
        }
    }

    float GroundAt(float x, float z)
    {
        var down = new Ray(new Vector3(x, 400f, z), Vector3.down);
        foreach (var t in terrain)
            if (t && t.Raycast(down, out var hit, 800f)) return hit.point.y;
        return ringCenter.y;
    }

    void CullSpawned(Vector3 p)
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var a = spawned[i];
            if (!a) { spawned.RemoveAt(i); continue; }
            if (a.tamed) { spawned.RemoveAt(i); continue; }                 // 结契成功，留在世界里
            if (!a.gameObject.activeSelf) { Destroy(a.gameObject); spawned.RemoveAt(i); continue; } // 已被击败
            if (Vector3.Distance(a.transform.position, p) > despawnDistance)
            {
                Destroy(a.gameObject); spawned.RemoveAt(i);
            }
        }
    }

    int CountNearby(Vector3 p)
    {
        int n = 0;
        foreach (var a in spawned)
            if (a && !a.tamed && a.gameObject.activeSelf && Vector3.Distance(a.transform.position, p) <= radius) n++;
        return n;
    }

    void TrySpawn(Vector3 p)
    {
        Bounds b = terrain.Length > 0 && terrain[0] ? terrain[0].bounds : new Bounds(Vector3.zero, Vector3.one * 800f);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(minSpawnDistance, radius);
            float x = p.x + Mathf.Sin(a) * r, z = p.z + Mathf.Cos(a) * r;
            if (x < b.min.x + 10f || x > b.max.x - 10f || z < b.min.z + 10f || z > b.max.z - 10f) continue;
            if (IsCrowded(x, z) || Blocked(new Vector3(x, GroundAt(x, z), z))) continue;

            string species = speciesKeys[Random.Range(0, speciesKeys.Count)];
            var go = Instantiate(templates[species]);
            go.transform.position = new Vector3(x, GroundAt(x, z), z);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.SetActive(true);
            var actor = go.GetComponent<M1Actor>();
            if (actor) { spawned.Add(actor); go.name = actor.displayName + " · 刷新灵兽"; } // 层级里显示中文名，而不是 “(Clone)”
            M1GameManager.Instance?.ShowAction("附近出现了野生「" + (actor ? actor.displayName : species) + "」。");
            return;
        }
    }

    static readonly Collider[] overlapBuf = new Collider[8];
    /// v0.8.0：刷新点和树、石头、灌木、牧群等实体重叠时换个位置，避免灵兽一出现就卡在树干或石头里
    bool Blocked(Vector3 at)
    {
        int n = Physics.OverlapCapsuleNonAlloc(at + Vector3.up * .8f, at + Vector3.up * 1.7f, .7f, overlapBuf, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = overlapBuf[i];
            if (!c || (c is MeshCollider mc && System.Array.IndexOf(terrain, mc) >= 0)) continue;
            return true;
        }
        return false;
    }

    bool IsCrowded(float x, float z)
    {
        foreach (var a in M1Actor.All)
        {
            if (!a) continue;
            var q = a.transform.position;
            if ((q.x - x) * (q.x - x) + (q.z - z) * (q.z - z) < 9f) return true;
        }
        return false;
    }
}
