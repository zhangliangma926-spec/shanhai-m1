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
    // v0.8.4：可猎杀的动物和氛围怪物
    [Tooltip("可猎杀的动物：有名牌血条，受击后逃跑，不能结契。")] public bool isWildlife;
    [Tooltip("敌人是否主动攻击；关掉时只在挨打后反击。")] public bool aggressive = true;
    [Tooltip("位置由别的组件控制（盘旋飞鸟），本组件不移动它。")] public bool selfMoving;
    public float attackDamage = 8f, aggroRange = 13f, fleeSpeed = 3.4f;
    [Tooltip("被打死后多少秒在别处重新出现；0 = 不再出现。")] public float respawnSeconds = 1800f;
    bool dead; float fleeTimer;
    public bool IsDead => dead;
    /// 正在追击或逃跑，牧群漫游组件这时让出控制
    public bool Engaged { get; private set; }
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
        else if (!tamed) health = maxHealth;
    }
    void Update()
    {
        if(dead)return;
        float dt=Time.deltaTime; var game=M1GameManager.Instance; var player=game?game.player:null;
        bool wild=isTameable&&!tamed;Engaged=false;
        if(wild&&player)
        {
            float pd=Vector3.Distance(transform.position,player.transform.position);
            if(pd<3.6f&&!provoked) peacefulNearPlayer+=dt; else if(pd>=3.6f) peacefulNearPlayer=0f;
        }
        markTimer=Mathf.Max(0f,markTimer-dt);
        if(((isEnemy&&(aggressive||provoked))||(wild&&provoked))&&player&&!selfMoving)
        {
            var p=player.transform;float d=Vector3.Distance(transform.position,p.position);
            if(d<aggroRange)Engaged=true;
            if(d>1.8f&&d<aggroRange){MoveSafely(p.position,(isEnemy?1.7f:1.45f));Face(p.position);}
            if(d<=2f&&attackTimer<=0f){attackTimer=isEnemy?1.2f:1.55f;if(animDriver)animDriver.Attack();if(wild)M1AudioDirector.PlayCreatureAction(speciesId,transform.position,.30f);StartCoroutine(HitPlayerLater(animDriver?animDriver.attackHitDelay:.25f,isEnemy?attackDamage:5f));} // v0.8.3：伤害在攻击动作命中那一帧结算
            attackTimer-=dt;
        }
        else if(isWildlife&&fleeTimer>0f&&player&&!selfMoving) // v0.8.4：动物挨打后往远离玩家的方向跑
        {
            fleeTimer-=dt;Engaged=true;Vector3 away=transform.position-player.transform.position;away.y=0f;
            Vector3 to=transform.position+(away.sqrMagnitude>.01f?away.normalized:transform.forward)*4f;MoveSafely(to,fleeSpeed);Face(to);
        }
        else if(wild&&(!player||(transform.position-player.transform.position).sqrMagnitude<SimulateRadius*SimulateRadius)) Roam(dt);
        if(tamed&&followTarget)
        {
            Vector3 target=followTarget.position-followTarget.forward*1.35f+followTarget.right*.85f;
            if(Vector3.Distance(transform.position,target)>1.6f)MoveSafely(target,2.5f);
            if(game&&game.enemy&&game.enemy.gameObject.activeSelf&&!game.enemy.dead&&companionAttackTimer<=0f&&Vector3.Distance(transform.position,game.enemy.transform.position)<2.7f&&Vector3.Distance(followTarget.position,game.enemy.transform.position)<5.5f)
            {companionAttackTimer=1.8f;if(animDriver)animDriver.Attack();M1AudioDirector.PlayCreatureAction(speciesId,transform.position,.36f);StartCoroutine(HitEnemyLater(animDriver?animDriver.attackHitDelay:.25f,game.enemy));game.ShowAction(displayName+" 发出攻击叫声并协同攻击测试敌人！");}
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
            if(!hit.collider||hit.collider.transform.IsChildOf(transform))continue; // v0.8.4：牧群挂在“氛围生物”分组下，原来按 root 比较会把自己的碰撞体当成障碍，走不动
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
        if(tamed||dead)return;if(isTameable)M1AudioDirector.PlayCreatureAction(speciesId,transform.position,.32f);
        float before=health/Mathf.Max(1f,maxHealth);health-=amount;if(isTameable||isEnemy)provoked=true;if(isWildlife)fleeTimer=6f;
        if(animDriver)animDriver.Hit();
        // v0.8.1 战斗反馈：伤害数字、模型弹一下；生命降到 10% 以下时明确提示“可结契”
        var col=GetComponent<Collider>();Vector3 top=col?new Vector3(col.bounds.center.x,col.bounds.max.y+.3f,col.bounds.center.z):transform.position+Vector3.up*2f;
        M1CombatFeedback.Popup(top,"-"+Mathf.Max(1,Mathf.RoundToInt(amount)),M1CombatFeedback.Damage);
        if(transform.childCount>0)M1CombatFeedback.Punch(transform.GetChild(0));
        float after=health/Mathf.Max(1f,maxHealth);
        if(isTameable&&before>.10f&&after<=.10f&&health>0f){M1CombatFeedback.Popup(top+Vector3.up*.5f,"可结契！",M1CombatFeedback.Calm,28f);M1GameManager.Instance?.ShowAction(displayName+" 已虚弱，靠近后按 E 结契（需要灵契符）。");}
        if(body&&hasBodyColor){body.material.color=Color.Lerp(bodyColor,Color.white,.75f);CancelInvoke(nameof(RestoreColor));Invoke(nameof(RestoreColor),.12f);}
        if(health<=0f)Die();
    }
    // v0.8.4：打死后播放死亡动作（模型没有死亡动作就侧倒），飞行的掉到地上，几秒后沉入地面消失；respawnSeconds 秒后在别处重新出现
    readonly List<Behaviour> pausedOnDeath=new List<Behaviour>();
    void Die()
    {
        dead=true;Engaged=false;health=0f;M1GameManager.Instance?.ActorDefeated(this);
        foreach(var b in GetComponents<Behaviour>())if(b!=this&&b.enabled&&!(b is M1CreatureAnimator)&&!(b is Animator)){b.enabled=false;pausedOnDeath.Add(b);}
        var col=GetComponent<Collider>();if(col)col.enabled=false;
        bool clip=animDriver&&animDriver.Die();
        StartCoroutine(DeathRoutine(clip));
    }
    System.Collections.IEnumerator DeathRoutine(bool hasClip)
    {
        Vector3 start=transform.position;Quaternion rot=transform.rotation;float ground=GroundBelow(start);
        for(float t=0f;t<.7f;t+=Time.deltaTime)
        {
            float k=t/.7f;k*=k;var p=start;p.y=Mathf.Lerp(start.y,ground,k);transform.position=p;
            if(!hasClip)transform.rotation=rot*Quaternion.AngleAxis(80f*k,Vector3.forward); // 没有死亡动作：向一侧倒下
            yield return null;
        }
        yield return new WaitForSeconds(hasClip?2.6f:1.8f);
        Vector3 down=transform.position;
        for(float t=0f;t<1.2f;t+=Time.deltaTime){transform.position=down+Vector3.down*t;yield return null;}
        transform.rotation=rot;gameObject.SetActive(false);
        var gm=M1GameManager.Instance;if(respawnSeconds>0f&&gm)gm.StartCoroutine(RespawnLater());
    }
    System.Collections.IEnumerator RespawnLater()
    {
        yield return new WaitForSeconds(respawnSeconds);
        if(!this||tamed||gameObject.activeSelf)yield break; // 已被刷新器清除、或已经结契
        Vector3 p=RespawnPoint();
        transform.position=p;transform.rotation=Quaternion.Euler(0f,Random.Range(0f,360f),0f);home=p;roamTarget=p;
        health=maxHealth;provoked=false;fleeTimer=0f;dead=false;
        var col=GetComponent<Collider>();if(col)col.enabled=true;
        foreach(var b in pausedOnDeath)if(b)b.enabled=true;pausedOnDeath.Clear();if(animDriver)animDriver.enabled=true;
        gameObject.SetActive(true);SendMessage("OnRespawned",p,SendMessageOptions.DontRequireReceiver);
    }
    /// 在原出生点 25–60 米外找一块地面，尽量离玩家 40 米以上
    Vector3 RespawnPoint()
    {
        var game=M1GameManager.Instance;Vector3 player=game&&game.player?game.player.transform.position:new Vector3(9999f,0f,9999f);Vector3 best=home;
        for(int i=0;i<10;i++)
        {
            Vector2 o=Random.insideUnitCircle.normalized*Random.Range(25f,60f);Vector3 p=home+new Vector3(o.x,0f,o.y);p.y=GroundBelow(p);best=p;
            if(new Vector2(p.x-player.x,p.z-player.z).sqrMagnitude>40f*40f)break;
        }
        return best;
    }
    float GroundBelow(Vector3 at)
    {
        var world=M1InfiniteWorld.Instance;if(world)return M1TerrainNoise.Height(world.kind,at.x,at.z);
        int n=Physics.RaycastNonAlloc(at+Vector3.up*60f,Vector3.down,groundHits,200f,~0,QueryTriggerInteraction.Ignore);float best=float.MaxValue,y=at.y;
        for(int i=0;i<n;i++){var h=groundHits[i];if(!h.collider||h.transform.root==transform.root||!(h.collider is MeshCollider))continue;if(h.distance<best){best=h.distance;y=h.point.y;}}
        return y;
    }
    // v0.8.3：等攻击动作打到“命中帧”再结算伤害；这段时间里目标跑出攻击范围（2.6 米）就算躲开了
    System.Collections.IEnumerator HitPlayerLater(float delay,float dmg)
    {
        yield return new WaitForSeconds(delay);var game=M1GameManager.Instance;if(!game||!game.player||!gameObject.activeInHierarchy||tamed||dead)yield break;
        if(Vector3.Distance(transform.position,game.player.transform.position)<=2.6f)game.DamagePlayer(dmg);else M1CombatFeedback.Popup(game.player.transform.position+Vector3.up*2.1f,"闪避",M1CombatFeedback.Calm,20f);
    }
    System.Collections.IEnumerator HitEnemyLater(float delay,M1Actor target)
    {
        yield return new WaitForSeconds(delay);if(target&&target.gameObject.activeInHierarchy&&!target.dead&&Vector3.Distance(transform.position,target.transform.position)<=3.2f)target.TakeDamage(8f);
    }
    void RestoreColor(){if(body&&hasBodyColor)body.material.color=bodyColor;} // 恢复模型原本的颜色（原来固定成蓝/红，会把贴图模型染错色）
}

