using UnityEngine;

/// <summary>
/// 贴地倾斜（v0.8.0）：四足动物在坡上让模型随地面法线倾斜，避免前脚悬空、后脚陷进地里。
/// 只转动模型子物体，根物体的朝向和移动逻辑不变；离镜头远时不更新。
/// </summary>
public class M1GroundAlign : MonoBehaviour
{
    public float maxTilt = 24f;
    const float UpdateDistance = 70f;
    static readonly RaycastHit[] hits = new RaycastHit[6];

    Transform visual; Quaternion baseRot; Vector3 normal = Vector3.up;

    void Awake()
    {
        visual = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (visual) baseRot = visual.localRotation;
    }

    void LateUpdate()
    {
        if (!visual) return;
        var cam = Camera.main;
        if (cam && (cam.transform.position - transform.position).sqrMagnitude > UpdateDistance * UpdateDistance) return;
        int n = Physics.RaycastNonAlloc(transform.position + Vector3.up * 1.5f, Vector3.down, hits, 4f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue; Vector3 target = Vector3.up;
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (!h.collider || h.transform.root == transform.root || !(h.collider is MeshCollider)) continue; // 只认地形网格，不被石头、灌木的碰撞盒带歪
            if (h.distance < best) { best = h.distance; target = h.normal; }
        }
        if (Vector3.Angle(Vector3.up, target) > maxTilt) target = Vector3.Slerp(Vector3.up, target, maxTilt / Vector3.Angle(Vector3.up, target));
        normal = Vector3.Slerp(normal, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
        var tilt = Quaternion.FromToRotation(Vector3.up, transform.InverseTransformDirection(normal));
        visual.localRotation = tilt * baseRot;
    }
}
