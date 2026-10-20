using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 采集节点（v0.7.9）：可采 4 次，采空后约 2 分钟重新生长。
/// 采集改为"按住 F 读条"，读条与取消由 M1ResourceCrafting 负责；这里负责采集结算、枯竭、重生的外观。
/// </summary>
public class M1HarvestNode : MonoBehaviour
{
    // 注册表：所有当前启用的采集节点。
    static readonly List<M1HarvestNode> registry = new List<M1HarvestNode>();
    public static IReadOnlyList<M1HarvestNode> All => registry;
    void OnEnable() { registry.Add(this); }
    void OnDisable() { registry.Remove(this); }

    public string resource = "木材";
    public int amount = 2;
    public int remaining = 4;
    public Transform player;
    public float reach = 3.8f, regrowSeconds = 120f;

    int maxRemaining = 4;
    bool depleted;
    float regrowTimer, popTimer, growTimer = 1f;
    Renderer[] renderers;
    Collider col;
    Vector3 baseScale = Vector3.one;

    void Start()
    {
        maxRemaining = Mathf.Max(1, remaining);
        renderers = GetComponentsInChildren<Renderer>(true);
        col = GetComponent<Collider>();
        baseScale = transform.localScale;
    }

    public bool CanHarvest => !depleted && remaining > 0 && player && Vector3.Distance(player.position, transform.position) <= reach;

    public bool Harvest()
    {
        if (!CanHarvest) return false;
        int got = amount + M1ResourceCrafting.ToolBonus(resource);
        remaining--;
        M1ResourceCrafting.Collect(resource, got);
        M1GameManager.Instance?.ShowAction("采集到 " + resource + " × " + got + "。剩余 " + remaining + " 次。");
        popTimer = .22f;
        if (remaining <= 0) Deplete();
        return true;
    }

    void Deplete()
    {
        depleted = true;
        regrowTimer = regrowSeconds;
        SetVisible(false);
        M1GameManager.Instance?.ShowAction(resource + "资源点已采空，约 2 分钟后重新生长。");
    }

    void Regrow()
    {
        depleted = false;
        remaining = maxRemaining;
        growTimer = 0f;
        SetVisible(true);
        transform.localScale = baseScale * .2f;
    }

    void SetVisible(bool on)
    {
        foreach (var r in renderers) if (r) r.enabled = on;
        if (col) col.enabled = on;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (depleted)
        {
            regrowTimer -= dt;
            if (regrowTimer <= 0f) Regrow();
            return;
        }
        if (growTimer < 1f)
        {
            growTimer = Mathf.Min(1f, growTimer + dt / .8f);
            transform.localScale = baseScale * Mathf.SmoothStep(.2f, 1f, growTimer);
        }
        else if (popTimer > 0f)
        {
            popTimer -= dt;
            float k = 1f + Mathf.Sin(Mathf.Clamp01(popTimer / .22f) * Mathf.PI) * .08f;
            transform.localScale = baseScale * k;
        }
    }
}
