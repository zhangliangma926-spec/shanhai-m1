using UnityEngine;

/// <summary>
/// 异兽 / 守卫 / 氛围动物的动画驱动（v0.7.7）：按实际位移速度在待机、行走、奔跑（或飞行）之间混合，
/// 攻击与受击时播放一次性动作。没有 Animator 的旧几何体模型会自动跳过。
/// v0.8.0：按体型换算速度，并让步频跟随实际移动速度，避免大体型慢步打滑、小体型快跑时腿跟不上（“滑步”）。
/// </summary>
public class M1CreatureAnimator : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed"), AttackId = Animator.StringToHash("Attack"), HitId = Animator.StringToHash("Hit");
    // 与 M1AssetSetupV077.Controller 里异兽混合树的阈值一致：走 .8、跑 3（按 1 米体型）
    const float WalkRef = .8f, RunRef = 3f;

    [Tooltip("模型高度（米）。速度按体型换算后再驱动动画，1.7 米的鹿和 .9 米的狐狸步幅不同。")]
    public float bodySize = 1f;
    [Tooltip("没有奔跑动作的模型（只有走路），速度超过走路时把走路动作加快播放。")]
    public bool hasRun = true;
    [Tooltip("攻击动作的命中帧（秒）：伤害在这个时间点结算，和动作对齐（v0.8.3）。")]
    public float attackHitDelay = .25f;
    /// 由飞行组件每帧设置：悬停时也要保持“飞行”动作，而不是落地待机。
    [System.NonSerialized] public float minBlend;

    Animator anim; Vector3 last; float speed;
    public bool HasAnimator => anim && anim.runtimeAnimatorController;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>(); last = transform.position;
        if (anim) { anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms; anim.Update(Random.value * 2f); } // 随机起始帧，避免整群动作同步
    }

    void Update()
    {
        if (!HasAnimator) return;
        float dt = Mathf.Max(Time.deltaTime, .0001f);
        Vector3 d = transform.position - last; d.y = 0; last = transform.position;
        speed = Mathf.Lerp(speed, d.magnitude / dt, 1f - Mathf.Exp(-10f * dt));
        float scaled = speed / Mathf.Max(bodySize, .3f);
        float blend = Mathf.Max(scaled, minBlend);
        anim.SetFloat(SpeedId, blend);
        // 步频同步：在混合树最高一档以上继续加快播放，让脚步与地面位移对得上
        float top = hasRun ? RunRef : WalkRef;
        anim.speed = blend > top ? Mathf.Clamp(blend / top, 1f, hasRun ? 1.6f : 2.4f) : 1f;
    }

    public void Attack() { if (HasAnimator) anim.SetTrigger(AttackId); }
    public void Hit() { if (HasAnimator) anim.SetTrigger(HitId); }
    /// 触发一次性动作（TakeOff / Land 等）；控制器里没有这个参数时忽略
    public void Trigger(string name) { if (!HasAnimator) return; foreach (var p in anim.parameters) if (p.name == name && p.type == AnimatorControllerParameterType.Trigger) { anim.SetTrigger(name); return; } }
}
