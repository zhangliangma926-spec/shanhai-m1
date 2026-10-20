using UnityEngine;

/// <summary>
/// 第三人称跟随镜头（v0.7.7 流畅度重写）。
/// B 版（鼠标准星）：普通移动鼠标只移动准星；按住右键拖动旋转镜头；准星推到屏幕边缘时镜头缓慢跟转。
/// A 版（中心准星）：鼠标直接旋转镜头。
/// 平滑：跟随点做指数平滑；遮挡时快速拉近、离开遮挡后缓慢恢复，避免画面抖动。
/// </summary>
public class M1CameraFollow : MonoBehaviour
{
    public Transform target;
    public float sensitivity = 2.6f, minPitch = -8f, maxPitch = 60f, minDistance = 3.2f, maxDistance = 10f, distance = 6.8f;
    [Tooltip("B 版：准星距离屏幕边缘多少比例开始推镜头")] public float edgeBand = .06f;
    [Tooltip("B 版：边缘推镜头的最大角速度（度/秒）")] public float edgeSpeed = 85f;

    float yaw, pitch = 20f, smoothYaw, smoothPitch, safeDistance, zoomTarget;
    Vector3 smoothFocus;
    bool dragging, initialized;
    static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    void Start()
    {
        if (!target) return;
        yaw = smoothYaw = target.eulerAngles.y; smoothPitch = pitch;
        smoothFocus = target.position + Vector3.up * .95f; safeDistance = zoomTarget = distance; initialized = true;
    }

    void LateUpdate()
    {
        if (!target) return;
        if (!initialized) Start();
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
        var gm = M1GameManager.Instance;
        bool active = !gm || gm.IsGameplayActive;

        if (active) ReadInput(dt); else if (dragging) EndDrag();

        // 角度平滑：跟手但不生硬
        float k = 1f - Mathf.Exp(-18f * dt);
        smoothYaw = Mathf.LerpAngle(smoothYaw, yaw, k);
        smoothPitch = Mathf.Lerp(smoothPitch, pitch, k);
        distance = Mathf.Lerp(distance, zoomTarget, 1f - Mathf.Exp(-10f * dt));

        // 跟随点平滑：消除角色加减速时的镜头顿挫
        Vector3 focusWanted = target.position + Vector3.up * .95f;
        smoothFocus = Vector3.Lerp(smoothFocus, focusWanted, 1f - Mathf.Exp(-14f * dt));
        if ((smoothFocus - focusWanted).sqrMagnitude > 25f) smoothFocus = focusWanted; // 传送/复活时直接跳过去

        Quaternion rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
        Vector3 dir = rotation * Vector3.back;

        // 遮挡检测：拉近要快、恢复要慢
        float blocked = distance;
        int n = Physics.SphereCastNonAlloc(smoothFocus, .25f, dir, hitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = hitBuffer[i]; if (!h.collider || h.transform.root == target.root) continue;
            if (h.collider.GetComponentInParent<M1Actor>()) continue; // 异兽挡镜头时不拉近，避免频繁抽动
            blocked = Mathf.Min(blocked, Mathf.Max(.8f, h.distance - .18f));
        }
        safeDistance = blocked < safeDistance ? Mathf.Lerp(safeDistance, blocked, 1f - Mathf.Exp(-30f * dt))
                                              : Mathf.Lerp(safeDistance, blocked, 1f - Mathf.Exp(-4f * dt));

        transform.position = smoothFocus + dir * safeDistance;
        transform.rotation = Quaternion.LookRotation(smoothFocus - transform.position, Vector3.up);
    }

    void ReadInput(float dt)
    {
        zoomTarget = Mathf.Clamp(zoomTarget - Input.GetAxis("Mouse ScrollWheel") * 4f, minDistance, maxDistance);
        float mx = Input.GetAxisRaw("Mouse X"), my = Input.GetAxisRaw("Mouse Y");

        if (!M1ChargeWeaponSystem.UsePointerAim) { yaw += mx * sensitivity; pitch -= my * sensitivity; }
        else
        {
            // 右键拖动旋转镜头：拖动时锁定并隐藏光标，松开后光标回到原处
            if (Input.GetMouseButtonDown(1)) { dragging = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (dragging && !Input.GetMouseButton(1)) EndDrag();
            if (dragging) { yaw += mx * sensitivity; pitch -= my * sensitivity; }
            else
            {
                // 准星推到屏幕边缘：按深入程度渐进转动
                Vector3 p = Input.mousePosition; float bw = Screen.width * edgeBand, bh = Screen.height * edgeBand;
                float ex = p.x < bw ? -(1f - p.x / bw) : p.x > Screen.width - bw ? (1f - (Screen.width - p.x) / bw) : 0f;
                float ey = p.y < bh ? -(1f - p.y / bh) : p.y > Screen.height - bh ? (1f - (Screen.height - p.y) / bh) : 0f;
                if (p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height) // 光标在窗口外时不推
                { yaw += ex * ex * Mathf.Sign(ex) * edgeSpeed * dt; pitch -= ey * ey * Mathf.Sign(ey) * edgeSpeed * .5f * dt; }
            }
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void EndDrag() { dragging = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = false; }

    /// 镜头正在被右键拖动（准星暂时隐藏）
    public bool IsDragging => dragging;
}
