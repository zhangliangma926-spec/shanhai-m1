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
    Animator anim; CharacterController cc; float speed;

    void Awake() { anim = GetComponentInChildren<Animator>(); cc = GetComponent<CharacterController>(); if (anim) anim.applyRootMotion = false; }
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
    }

    public void Attack() { if (anim && anim.runtimeAnimatorController) anim.SetTrigger(AttackId); }
    public void Hit() { if (anim && anim.runtimeAnimatorController) anim.SetTrigger(HitId); }
    public void Roll() { if (anim && anim.runtimeAnimatorController) anim.SetTrigger(RollId); }
}
