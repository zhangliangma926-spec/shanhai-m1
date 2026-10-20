using UnityEngine;

public class M1SpellProjectile : MonoBehaviour
{
    M1Actor target;Vector3 destination;float speed=26f,damage=1f,life=4f;Color tint;
    public static void Cast(M1Actor caster){Cast(caster,12f,new Color(.28f,.88f,1f),"星金灵杖",Camera.main?Camera.main.transform:null);}
    public static M1Actor GetAimedActor(Transform aimCamera,Transform caster){Vector3 point;M1Actor actor;FindAim(aimCamera,caster,out point,out actor);return actor;}
    public static M1Actor GetAimedActor(Transform aimCamera,Transform caster,out Vector3 point){M1Actor actor;FindAim(aimCamera,caster,out point,out actor);return actor;}
    static readonly RaycastHit[] aimHits=new RaycastHit[32];
    sealed class HitComparer:System.Collections.Generic.IComparer<RaycastHit>{public int Compare(RaycastHit a,RaycastHit b)=>a.distance.CompareTo(b.distance);}
    static readonly HitComparer Comparer=new HitComparer();
    static bool Hostile(M1Actor a)=>a&&!a.tamed&&(a.isEnemy||a.isTameable)&&a.gameObject.activeInHierarchy;

    /// v0.8.1 瞄准：射线仍从镜头穿过准星，但
    /// 1) 跳过镜头和角色之间的东西（之前镜头拉远时，身后的宠物、灌木、树会先被射中，所以远近效果不一样）；
    /// 2) 结契伙伴不挡枪；
    /// 3) 精确射线没打中异兽时，在射线附近放宽命中范围；容差按目标离镜头的距离算（屏幕上约 40 像素），镜头拉近拉远一样好打。
    static void FindAim(Transform aimCamera,Transform caster,out Vector3 point,out M1Actor actor)
    {
        Transform cam=aimCamera?aimCamera:(Camera.main?Camera.main.transform:null);Vector3 origin=cam?cam.position:(caster?caster.position+Vector3.up*1.3f:Vector3.zero);Vector3 direction=cam?cam.forward:(caster?caster.forward:Vector3.forward);
        if(cam&&Camera.main){Ray ray=Camera.main.ScreenPointToRay(M1ChargeWeaponSystem.AimScreenPoint());origin=ray.origin;direction=ray.direction;}
        AimAlongRay(origin,direction,caster,out point,out actor);
    }
    /// 指定镜头和屏幕坐标求瞄准结果（编辑器测试用，和游戏内瞄准走同一套逻辑）
    public static M1Actor AimFromScreen(Camera camera,Vector3 screen,Transform caster,out Vector3 point){var ray=camera.ScreenPointToRay(screen);AimAlongRay(ray.origin,ray.direction,caster,out point,out var actor);return actor;}
    static void AimAlongRay(Vector3 origin,Vector3 direction,Transform caster,out Vector3 point,out M1Actor actor)
    {
        float skip=caster?Mathf.Max(0f,Vector3.Dot(caster.position+Vector3.up*1.2f-origin,direction)-.4f):0f; // 角色在射线上的位置，之前的命中一律忽略
        int count=Physics.RaycastNonAlloc(origin,direction,aimHits,160f,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(aimHits,0,count,Comparer);actor=null;point=origin+direction*160f;float hitDist=160f;
        for(int i=0;i<count;i++)
        {
            var hit=aimHits[i];if(!hit.collider||hit.distance<skip)continue;var root=hit.collider.transform.root;if(caster&&root==caster.root)continue;
            var a=hit.collider.GetComponentInParent<M1Actor>();if(a&&a.tamed)continue; // 伙伴不挡枪
            point=hit.point;hitDist=hit.distance;actor=Hostile(a)?a:null;break;
        }
        if(actor)return;
        // 命中容差：沿射线找离射线最近的异兽 / 守卫（只到第一个障碍物为止，不隔墙锁定）
        float best=float.MaxValue;
        foreach(var a in M1Actor.All)
        {
            if(!Hostile(a)||(caster&&a.transform.root==caster.root))continue;
            Vector3 c=a.transform.position+Vector3.up*.8f;var col=a.GetComponent<Collider>();if(col)c=col.bounds.center;
            float t=Vector3.Dot(c-origin,direction);if(t<skip||t>hitDist+1.5f||t>90f)continue;
            float off=Vector3.Distance(origin+direction*t,c),allow=Mathf.Clamp(t*.045f,.3f,3f)+(col?Mathf.Min(col.bounds.extents.x,col.bounds.extents.z)*.5f:0f); // 按离镜头的距离放宽：屏幕上约 40 像素的容差，镜头远近一致
            if(off<allow&&off/allow<best){best=off/allow;actor=a;point=c;}
        }
    }
    public static bool Cast(M1Actor caster,float amount,Color color,string weaponName,Transform aimCamera)
    {
        var gm=M1GameManager.Instance;if(!gm||!caster)return false;Vector3 point;M1Actor target;FindAim(aimCamera,caster.transform,out point,out target);
        Vector3 origin=caster.transform.position+Vector3.up*1.25f+caster.transform.forward*.45f;
        // 角色和目标之间有树、石头挡着：法弹打在障碍物上，不能隔着障碍伤害目标
        if(Physics.Linecast(origin,point,out var block,~0,QueryTriggerInteraction.Ignore))
        {
            var ba=block.collider.GetComponentInParent<M1Actor>();
            bool self=block.collider.transform.root==caster.transform.root,isTarget=target&&ba==target,pet=ba&&ba.tamed;
            if(!self&&!isTarget&&!pet&&Vector3.Distance(block.point,point)>.6f){point=block.point;target=null;}
        }
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=weaponName+" · 准星灵弹";go.transform.position=origin;go.transform.localScale=Vector3.one*.24f;var col=go.GetComponent<Collider>();if(col)Object.Destroy(col);
        var r=go.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard"));m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*3.3f);r.material=m;var light=go.AddComponent<Light>();light.color=color;light.intensity=2f;light.range=4f;
        var tr=go.AddComponent<TrailRenderer>();tr.time=.22f;tr.startWidth=.18f;tr.endWidth=.012f;tr.material=new Material(Shader.Find("Sprites/Default"));tr.startColor=new Color(color.r,color.g,color.b,.9f);tr.endColor=new Color(color.r,color.g,color.b,0);tr.minVertexDistance=.035f;
        var spell=go.AddComponent<M1SpellProjectile>();spell.target=target;spell.destination=point;spell.damage=Mathf.Clamp(amount,1f,40f);spell.tint=color;
        gm.ShowAction(target?weaponName+"射向："+target.displayName:weaponName+"射向准星位置；没有瞄准异兽或守卫。");return true;
    }
    void Update()
    {
        life-=Time.deltaTime;if(life<=0f){Destroy(gameObject);return;}
        if(target&&target.gameObject.activeInHierarchy){var c=target.GetComponent<Collider>();destination=c?c.bounds.center:target.transform.position+Vector3.up*.8f;} // 追踪目标，移动中的灵兽也能打中
        transform.position=Vector3.MoveTowards(transform.position,destination,speed*Time.deltaTime);transform.Rotate(0,660f*Time.deltaTime,0);
        if(Vector3.Distance(transform.position,destination)<.18f)
        {
            if(target&&target.gameObject.activeInHierarchy){float applied=damage;if(target.isTameable)applied=Mathf.Min(applied,Mathf.Max(0f,target.health-1f));if(applied>0f)target.TakeDamage(applied);else M1CombatFeedback.Popup(destination,"已虚弱",M1CombatFeedback.Calm);M1AudioDirector.PlayUI("hit");}
            Impact(destination,tint);Destroy(gameObject);
        }
    }
    static void Impact(Vector3 point,Color color){var ring=GameObject.CreatePrimitive(PrimitiveType.Sphere);ring.name="准星命中灵光";ring.transform.position=point;ring.transform.localScale=Vector3.one*.55f;var c=ring.GetComponent<Collider>();if(c)Object.Destroy(c);var r=ring.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard"));m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2.4f);r.material=m;Object.Destroy(ring,.15f);}
}
