using UnityEngine;

/// <summary>
/// 异兽 / 守卫 / 氛围动物的动画驱动（v0.7.7）：按实际位移速度在待机、行走、奔跑（或飞行）之间混合，
/// 攻击与受击时播放一次性动作。没有 Animator 的旧几何体模型会自动跳过。
/// </summary>
public class M1CreatureAnimator : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed"), AttackId = Animator.StringToHash("Attack"), HitId = Animator.StringToHash("Hit");
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
        anim.SetFloat(SpeedId, speed);
    }

    public void Attack() { if (HasAnimator) anim.SetTrigger(AttackId); }
    public void Hit() { if (HasAnimator) anim.SetTrigger(HitId); }
}
