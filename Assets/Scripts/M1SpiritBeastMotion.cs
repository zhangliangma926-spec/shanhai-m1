using UnityEngine;

public class M1SpiritBeastMotion : MonoBehaviour
{
    M1Actor actor; Vector3 home,lastPosition; float phase,attackCooldown; Transform visual; Vector3 visualBase; Vector3 attackOffset;
    void Awake(){actor=GetComponent<M1Actor>();home=transform.position;lastPosition=transform.position;phase=Random.value*6.28f;visual=transform.childCount>0?transform.GetChild(0):null;if(visual)visualBase=visual.localPosition;if(GetComponentInChildren<Animator>())visual=null;/* 有骨骼动画的模型不再叠加上下浮动 */}
    void Update()
    {
        float dt=Mathf.Max(Time.deltaTime,.0001f);Vector3 delta=transform.position-lastPosition;delta.y=0;float speed=delta.magnitude/dt;lastPosition=transform.position;
        if(delta.sqrMagnitude>.0004f){var face=delta.normalized;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(face),Time.deltaTime*8f);}
        if(visual)visual.localPosition=visualBase+Vector3.up*(Mathf.Sin((Time.time+phase)*(speed>.12f?7f:2.4f))*(speed>.12f?.055f:.035f))+attackOffset;
        attackOffset=Vector3.Lerp(attackOffset,Vector3.zero,Time.deltaTime*7f);
        attackCooldown-=Time.deltaTime;
        if(actor && actor.tamed && actor.followTarget)home=actor.followTarget.position-actor.followTarget.forward*1.45f;
        if(Vector3.Distance(transform.position,home)>3.4f)home=transform.position;
        if(actor && actor.tamed && Input.GetKeyDown(KeyCode.H) && attackCooldown<=0f)
        {
            attackCooldown=1.4f;var game=M1GameManager.Instance;
            if(game && game.enemy && game.enemy.gameObject.activeSelf && Vector3.Distance(transform.position,game.enemy.transform.position)<7f)
            {attackOffset=Vector3.forward*.32f;game.enemy.TakeDamage(35f);game.ShowAction("岚角灵兽冲锋攻击！");}
            else if(game)game.ShowAction("靠近测试敌人后按 H 让异兽攻击。");
        }
    }
}
