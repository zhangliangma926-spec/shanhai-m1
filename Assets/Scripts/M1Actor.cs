using System.Collections.Generic;
using UnityEngine;

public class M1Actor : MonoBehaviour
{
    // 注册表：所有当前启用的 M1Actor 实例。替代各处每帧/每次按键调用的 FindObjectsByType<M1Actor>()。
    static readonly List<M1Actor> registry = new List<M1Actor>();
    public static IReadOnlyList<M1Actor> All => registry;
    void OnEnable() { registry.Add(this); }
    void OnDisable() { registry.Remove(this); }

    public float maxHealth = 100f;
    public float health = 100f;
    public bool isEnemy;
    public bool isTameable;
    public string speciesId = "spirit_beast";
    public string displayName = "灵兽";
    public bool tamed;
    public Transform followTarget;
    bool provoked;
    float attackTimer;
    float companionAttackTimer;
    float markTimer;
    float peacefulNearPlayer;
    Vector3 home, roamTarget;
    float roamTimer;
    Renderer body;
    static readonly RaycastHit[] moveHits=new RaycastHit[12];
    const float SimulateRadius=90f; // 离玩家太远的野生异兽暂停漫游，地图扩大后保持帧率
    M1CreatureAnimator animDriver; Color bodyColor=Color.white; bool hasBodyColor;
    static readonly RaycastHit[] groundHits=new RaycastHit[8];
    void Awake()
    {
        body = GetComponentInChildren<Renderer>(); home = transform.position; roamTarget = home;
        animDriver = GetComponent<M1CreatureAnimator>();
        if (body && body.sharedMaterial && body.sharedMaterial.HasProperty("_Color")) { bodyColor = body.sharedMaterial.color; hasBodyColor = true; }
    }
    void Start()
    {
        if (isTameable)
        {
            maxHealth = M1SpeciesDatabase.MaxHealth(speciesId);
            if (displayName == "灵兽") displayName = M1SpeciesDatabase.DisplayName(speciesId);
            health = maxHealth;
            if (!GetComponent<M1CreatureAudio>()) gameObject.AddComponent<M1CreatureAudio>();
        }
    }
    void Update()
    {
        float dt=Time.deltaTime; var game=M1GameManager.Instance; var player=game?game.player:null;
        bool wild=isTameable&&!tamed;
        if(wild&&player)
        {
            float pd=Vector3.Distance(transform.position,player.transform.position);
            if(pd<3.6f&&!provoked) peacefulNearPlayer+=dt; else if(pd>=3.6f) peacefulNearPlayer=0f;
        }
        markTimer=Mathf.Max(0f,markTimer-dt);
        if((isEnemy||(wild&&provoked))&&player)
        {
            var p=player.transform;float d=Vector3.Distance(transform.position,p.position);
            if(d>1.8f&&d<13f){MoveSafely(p.position,(isEnemy?1.7f:1.45f));Face(p.position);}
            if(d<=2f&&attackTimer<=0f){attackTimer=isEnemy?1.2f:1.55f;if(animDriver)animDriver.Attack();game.DamagePlayer(isEnemy?8f:5f);if(wild)M1AudioDirector.PlayCreatureAction(speciesId,transform.position,.30f);}
            attackTimer-=dt;
        }
        else if(wild&&(!player||(transform.position-player.transform.position).sqrMagnitude<SimulateRadius*SimulateRadius)) Roam(dt);
        if(tamed&&followTarget)
        {
            Vector3 target=followTarget.position-followTarget.forward*1.35f+followTarget.right*.85f;
            if(Vector3.Distance(transform.position,target)>1.6f)MoveSafely(target,2.5f);
            if(game&&game.enemy&&game.enemy.gameObject.activeSelf&&companionAttackTimer<=0f&&Vector3.Distance(transform.position,game.enemy.transform.position)<2.7f&&Vector3.Distance(followTarget.position,game.enemy.transform.position)<5.5f)
            {companionAttackTimer=1.8f;if(animDriver)animDriver.Attack();M1AudioDirector.PlayCreatureAction(speciesId,transform.position,.36f);game.enemy.TakeDamage(8f);game.ShowAction(displayName+" 发出攻击叫声并协同攻击测试敌人！");}
            companionAttackTimer-=dt;
        }
    }
    void Roam(float dt)
    {
        roamTimer-=dt;
        if(roamTimer<=0f){roamTimer=Random.Range(3f,7f);Vector2 o=Random.insideUnitCircle*6f;roamTarget=home+new Vector3(o.x,0,o.y);}
        if(roamTimer<4.5f&&(roamTarget-transform.position).sqrMagnitude>.3f){MoveSafely(roamTarget,.85f);Face(roamTarget);} // 到点后停下来张望
    }
    void MoveSafely(Vector3 destination,float speed)
    {
        Vector3 delta=destination-transform.position;delta.y=0f;float distance=delta.magnitude;if(distance<.001f)return;
        Vector3 direction=delta/distance;float step=Mathf.Min(speed*Time.deltaTime,distance);Vector3 bottom=transform.position+Vector3.up*.34f;Vector3 top=transform.position+Vector3.up*1.36f;float allowed=step;
        int n=Physics.CapsuleCastNonAlloc(bottom,top,.30f,direction,moveHits,step+.08f,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)
        {
            var hit=moveHits[i];
            if(!hit.collider||hit.collider.transform.root==transform)continue;
            allowed=Mathf.Min(allowed,Mathf.Max(0f,hit.distance-.08f));
        }
        if(allowed>0f){transform.position+=direction*allowed;SnapToGround();}
    }    void Face(Vector3 p){Vector3 d=p-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),1f-Mathf.Exp(-8f*Time.deltaTime));}
    /// 贴合地面：地图有起伏后，异兽走上坡/下坡时不再悬空或陷进地里
    void SnapToGround()
    {
        int n=Physics.RaycastNonAlloc(transform.position+Vector3.up*4f,Vector3.down,groundHits,10f,~0,QueryTriggerInteraction.Ignore);
        float best=float.MaxValue,y=transform.position.y;
        for(int i=0;i<n;i++){var h=groundHits[i];if(!h.collider||h.transform.root==transform.root||h.collider.GetComponentInParent<M1Actor>())continue;if(h.distance<best){best=h.distance;y=h.point.y;}}
        if(best<float.MaxValue){var pos=transform.position;pos.y=Mathf.Lerp(pos.y,y,.5f);transform.position=pos;}
    }
    public void MarkForTaming(float seconds){if(isTameable&&!tamed){markTimer=seconds;provoked=false;M1GameManager.Instance?.ShowAction(displayName+" 感知到灵纹，短暂安定了。");}}
    public float CalmMarkSeconds=>markTimer;
    public float PeacefulSecondsNearPlayer=>peacefulNearPlayer;
    public bool IsProvoked=>provoked;
    public void TakeDamage(float amount)
    {
        if(tamed)return;if(isTameable)M1AudioDirector.PlayCreatureAction(speciesId,transform.position,.32f);health-=amount;if(isTameable)provoked=true;
        if(animDriver)animDriver.Hit();
        if(body&&hasBodyColor){body.material.color=Color.Lerp(bodyColor,Color.white,.75f);CancelInvoke(nameof(RestoreColor));Invoke(nameof(RestoreColor),.12f);}
        if(health<=0f){M1GameManager.Instance?.ActorDefeated(this);gameObject.SetActive(false);}
    }
    void RestoreColor(){if(body&&hasBodyColor)body.material.color=bodyColor;} // 恢复模型原本的颜色（原来固定成蓝/红，会把贴图模型染错色）
}

