using UnityEngine;

/// <summary>
/// 飞行类异兽的姿态（v0.8.0）：模型离地悬停，带上下起伏、转弯侧倾和前冲俯仰。
/// Hover：一直在空中（月羽灵蝶、青蛟）；Perch：停下时落地待机，移动时起飞（灵雀、朱雀、仙鹤）。
/// 只移动模型子物体和碰撞体中心，根物体仍贴地，结契、名牌、刷新逻辑不受影响。
/// </summary>
public class M1FlyerMotion : MonoBehaviour
{
    public enum Style { Hover, Perch }
    public Style style = Style.Hover;
    public float hoverHeight = 1.4f;
    [Tooltip("上下起伏幅度（米）和频率（次/秒）。灵蝶用大幅快频的“扑扇”，青蛟用小幅慢频的“游动”。")]
    public float bobAmplitude = .18f, bobFrequency = 1.2f;
    [Tooltip("左右飘移幅度（米），让轨迹不是一条直线。")]
    public float swayAmplitude = .1f;
    public float maxBank = 22f;

    Transform visual; Vector3 visualBase; Quaternion visualRot;
    CapsuleCollider body; Vector3 bodyBase;
    M1CreatureAnimator animDriver;
    Vector3 last; float lastYaw, speed, lift, bank, pitch, phase, idleTime; bool wasAirborne;

    void Awake()
    {
        visual = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (visual) { visualBase = visual.localPosition; visualRot = visual.localRotation; }
        body = GetComponent<CapsuleCollider>(); if (body) bodyBase = body.center;
        animDriver = GetComponent<M1CreatureAnimator>();
        last = transform.position; lastYaw = transform.eulerAngles.y; phase = Random.value * 10f;
        lift = style == Style.Hover ? hoverHeight : 0f;
        idleTime = style == Style.Perch ? 2f : 0f; wasAirborne = style == Style.Hover; // 停落型一开始在地上，不会刚出生就播起飞
    }

    void Update()
    {
        if (!visual) return;
        float dt = Mathf.Max(Time.deltaTime, .0001f);
        Vector3 d = transform.position - last; d.y = 0; last = transform.position;
        speed = Mathf.Lerp(speed, d.magnitude / dt, 1f - Mathf.Exp(-6f * dt));
        float yaw = transform.eulerAngles.y, turn = Mathf.DeltaAngle(lastYaw, yaw) / dt; lastYaw = yaw;

        bool moving = speed > .2f;
        idleTime = moving ? 0f : idleTime + dt;
        bool airborne = style == Style.Hover || moving || idleTime < 1.2f; // 停下后再盘旋一会儿才落地
        // v0.8.3：停落型飞鸟离地时播放起飞、快落地时播放降落动作
        if (style == Style.Perch && animDriver) { if (airborne && !wasAirborne && lift < .05f) animDriver.Trigger("TakeOff"); else if (!airborne && wasAirborne) animDriver.Trigger("Land"); }
        wasAirborne = airborne;
        lift = Mathf.MoveTowards(lift, airborne ? hoverHeight : 0f, dt * (airborne ? 2.2f : 1.4f));
        float air = hoverHeight > 0f ? lift / hoverHeight : 0f;

        float t = Time.time + phase;
        float bob = Mathf.Sin(t * bobFrequency * 6.283f) * bobAmplitude * air;
        float sway = Mathf.Sin(t * bobFrequency * 2.1f) * swayAmplitude * air;
        bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * .12f, -maxBank, maxBank) * air, 1f - Mathf.Exp(-4f * dt));
        pitch = Mathf.Lerp(pitch, Mathf.Clamp(speed * 4f, 0f, 14f) * air, 1f - Mathf.Exp(-3f * dt));

        visual.localPosition = visualBase + new Vector3(sway, lift + bob, 0f);
        visual.localRotation = Quaternion.Euler(pitch, 0f, bank) * visualRot;
        if (body) body.center = bodyBase + Vector3.up * (lift + bob); // 碰撞体跟着模型，法术能打中、名牌在模型上方
        if (animDriver) animDriver.minBlend = air > .3f ? 1f : 0f;     // 在空中保持飞行动作
    }
}
