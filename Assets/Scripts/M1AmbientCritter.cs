using UnityEngine;

/// <summary>
/// 地图氛围生物（v0.7.7）：牧群在出生点附近慢慢游荡，飞鸟在高空盘旋。
/// 不可攻击、不可结契，只为让世界更有生气；离玩家太远时暂停更新。
/// </summary>
public class M1AmbientCritter : MonoBehaviour
{
    public enum Mode { Graze, Circle }
    public Mode mode = Mode.Graze;
    public float radius = 10f, speed = 1.1f, height = 18f;
    Vector3 home, target; float timer, angle;
    static readonly RaycastHit[] groundHits = new RaycastHit[6];

    void Start() { home = transform.position; target = home; angle = Random.value * 360f; timer = Random.Range(0f, 4f); }

    void Update()
    {
        var gm = M1GameManager.Instance;
        if (gm && gm.player && (transform.position - gm.player.transform.position).sqrMagnitude > 140f * 140f) return;
        float dt = Time.deltaTime;
        if (mode == Mode.Circle)
        {
            angle += speed * 10f * dt;
            float r = angle * Mathf.Deg2Rad;
            Vector3 p = home + new Vector3(Mathf.Cos(r) * radius, height + Mathf.Sin(r * 2f) * 1.5f, Mathf.Sin(r) * radius);
            Vector3 dir = p - transform.position;
            if (dir.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(dir.normalized);
            transform.position = p;
            return;
        }
        timer -= dt;
        if (timer <= 0f) { timer = Random.Range(3f, 8f); Vector2 o = Random.insideUnitCircle * radius; target = home + new Vector3(o.x, 0, o.y); }
        Vector3 delta = target - transform.position; delta.y = 0;
        if (delta.magnitude > .3f && timer < 5.5f) // 每次到点后先停一会儿“吃草”
        {
            Vector3 step = delta.normalized * Mathf.Min(speed * dt, delta.magnitude);
            if (Blocked(delta.normalized, step.magnitude)) { timer = 0f; return; } // v0.8.0：前面有树、石头、其它动物就换个方向，不再穿过去
            transform.position += step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), 1f - Mathf.Exp(-5f * dt));
            SnapToGround();
        }
    }

    static readonly RaycastHit[] blockHits = new RaycastHit[8];
    CapsuleCollider body;
    bool Blocked(Vector3 dir, float step)
    {
        if (!body) body = GetComponent<CapsuleCollider>(); if (!body) return false;
        float r = body.radius * .9f; Vector3 c = transform.position + body.center;
        Vector3 a = c + Vector3.up * Mathf.Max(0f, body.height * .5f - r), b = c - Vector3.up * Mathf.Max(0f, body.height * .5f - r - .25f); // 底部抬高一点，不被地面挡住
        int n = Physics.CapsuleCastNonAlloc(a, b, r, dir, blockHits, step + .15f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = blockHits[i];
            if (!h.collider || h.distance <= 0f || h.transform.root == transform.root || h.collider is MeshCollider) continue; // 地形网格不算障碍；起点已重叠的不算，让它能走出来
            return true;
        }
        return false;
    }

    void SnapToGround()
    {
        int n = Physics.RaycastNonAlloc(transform.position + Vector3.up * 6f, Vector3.down, groundHits, 14f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue; float y = transform.position.y;
        for (int i = 0; i < n; i++) { var h = groundHits[i]; if (h.transform.root == transform.root || !(h.collider is MeshCollider)) continue; if (h.distance < best) { best = h.distance; y = h.point.y; } } // 只贴地形，不会踩到灌木、石头的碰撞盒上
        if (best < float.MaxValue) { var p = transform.position; p.y = y; transform.position = p; }
    }
}
