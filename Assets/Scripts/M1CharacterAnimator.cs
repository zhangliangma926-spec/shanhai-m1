using UnityEngine;

/// <summary>
/// 主角 3D 模型动画驱动（v0.7.7）：根据移动速度、蓄力状态切换待机/走/跑/持杖施法，
/// 发射、翻滚、受击时播放对应的一次性动作。Animator Controller 由编辑器构建脚本自动生成。
/// </summary>
public class M1CharacterAnimator : MonoBehaviour
{
    public static M1CharacterAnimator Player { get; private set; }
    static readonly int SpeedId = Animator.StringToHash("Speed"), CastId = Animator.StringToHash("Cast"),
        AttackId = Animator.StringToHash("Attack"), HitId = Animator.StringToHash("Hit"), RollId = Animator.StringToHash("Roll");
    static readonly int MoveState = Animator.StringToHash("Move"), CastMoveState = Animator.StringToHash("CastMove");
    // 与 M1AssetSetupV077.Controller 里玩家混合树一致：奔跑动作在 5 m/s 时步幅刚好贴地
    const float RunRef = 5f;
    Animator anim; CharacterController cc; float speed, lean, bank, lastYaw; Transform visual; Quaternion visualRot;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>(); cc = GetComponent<CharacterController>(); if (anim) anim.applyRootMotion = false;
        visual = anim ? anim.transform : null; if (visual) visualRot = visual.localRotation; lastYaw = transform.eulerAngles.y;
    }
    void OnEnable() { if (GetComponent<M1PlayerController>()) Player = this; }
    void OnDisable() { if (Player == this) Player = null; }

    void Update()
    {
        if (!anim || !anim.runtimeAnimatorController) return;
        float raw = 0f;
        if (cc) { var v = cc.velocity; v.y = 0; raw = v.magnitude; }
        speed = Mathf.Lerp(speed, raw, 1f - Mathf.Exp(-12f * Time.deltaTime));
        anim.SetFloat(SpeedId, speed);
        anim.SetBool(CastId, M1ChargeWeaponSystem.IsCharging);

        // v0.8.0 步频同步：冲刺（8 m/s）超过奔跑动作的设计速度时加快播放，脚步不再打滑；翻滚、攻击等一次性动作不受影响
        var st = anim.GetCurrentAnimatorStateInfo(0);
        bool moving = st.shortNameHash == MoveState || st.shortNameHash == CastMoveState;
        anim.speed = moving && speed > RunRef ? Mathf.Clamp(speed / RunRef, 1f, 1.6f) : 1f;

        // v0.8.0 身体姿态：冲刺时略微前倾，转弯时向内侧倾，动作更有重量感
        if (visual)
        {
            float dt = Mathf.Max(Time.deltaTime, .0001f), yaw = transform.eulerAngles.y, turn = Mathf.DeltaAngle(lastYaw, yaw) / dt; lastYaw = yaw;
            lean = Mathf.Lerp(lean, Mathf.Clamp01((speed - RunRef * .9f) / 3f) * 7f, 1f - Mathf.Exp(-6f * dt));
            bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * .02f, -8f, 8f) * Mathf.Clamp01(speed / RunRef), 1f - Mathf.Exp(-6f * dt));
            visual.localRotation = Quaternion.Euler(lean, 0f, bank) * visualRot;
        }
    }

    public void Attack() { if (anim && anim.runtimeAnimatorController) anim.SetTrigger(AttackId); }
    public void Hit() { if (anim && anim.runtimeAnimatorController) anim.SetTrigger(HitId); }
    public void Roll() { if (anim && anim.runtimeAnimatorController) anim.SetTrigger(RollId); }
}
