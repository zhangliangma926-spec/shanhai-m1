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
    static void FindAim(Transform aimCamera,Transform caster,out Vector3 point,out M1Actor actor)
    {
        Transform cam=aimCamera?aimCamera:(Camera.main?Camera.main.transform:null);Vector3 origin=cam?cam.position:(caster?caster.position+Vector3.up*1.3f:Vector3.zero);Vector3 direction=cam?cam.forward:(caster?caster.forward:Vector3.forward);
        if(cam&&Camera.main){Ray ray=Camera.main.ScreenPointToRay(M1ChargeWeaponSystem.AimScreenPoint());origin=ray.origin;direction=ray.direction;}
        int count=Physics.RaycastNonAlloc(origin,direction,aimHits,160f,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(aimHits,0,count,Comparer);actor=null;point=origin+direction*160f;var hits=new System.ArraySegment<RaycastHit>(aimHits,0,count);
        foreach(var hit in hits){if(!hit.collider)continue;var root=hit.collider.transform.root;if(caster&&root==caster.root)continue;point=hit.point;actor=hit.collider.GetComponentInParent<M1Actor>();if(actor&&(actor.tamed||(!actor.isEnemy&&!actor.isTameable)))actor=null;return;}
    }
    public static bool Cast(M1Actor caster,float amount,Color color,string weaponName,Transform aimCamera)
    {
        var gm=M1GameManager.Instance;if(!gm||!caster)return false;Vector3 point;M1Actor target;FindAim(aimCamera,caster.transform,out point,out target);
        Vector3 origin=caster.transform.position+Vector3.up*1.25f+caster.transform.forward*.45f;
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=weaponName+" · 准星灵弹";go.transform.position=origin;go.transform.localScale=Vector3.one*.24f;var col=go.GetComponent<Collider>();if(col)Object.Destroy(col);
        var r=go.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard"));m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*3.3f);r.material=m;var light=go.AddComponent<Light>();light.color=color;light.intensity=2f;light.range=4f;
        var tr=go.AddComponent<TrailRenderer>();tr.time=.22f;tr.startWidth=.18f;tr.endWidth=.012f;tr.material=new Material(Shader.Find("Sprites/Default"));tr.startColor=new Color(color.r,color.g,color.b,.9f);tr.endColor=new Color(color.r,color.g,color.b,0);tr.minVertexDistance=.035f;
        var spell=go.AddComponent<M1SpellProjectile>();spell.target=target;spell.destination=point;spell.damage=Mathf.Clamp(amount,1f,40f);spell.tint=color;
        gm.ShowAction(target?weaponName+"命中准星目标："+target.displayName+"，伤害 "+Mathf.RoundToInt(spell.damage)+"。":weaponName+"射向准星位置；没有命中异兽或守卫，不造成伤害。");return true;
    }
    void Update(){life-=Time.deltaTime;if(life<=0f){Destroy(gameObject);return;}transform.position=Vector3.MoveTowards(transform.position,destination,speed*Time.deltaTime);transform.Rotate(0,660f*Time.deltaTime,0);if(Vector3.Distance(transform.position,destination)<.18f){if(target&&target.gameObject.activeInHierarchy){float applied=damage;if(target.isTameable)applied=Mathf.Min(applied,Mathf.Max(0f,target.health-1f));if(applied>0f)target.TakeDamage(applied);M1AudioDirector.PlayUI("hit");}Impact(destination,tint);Destroy(gameObject);}}
    static void Impact(Vector3 point,Color color){var ring=GameObject.CreatePrimitive(PrimitiveType.Sphere);ring.name="准星命中灵光";ring.transform.position=point;ring.transform.localScale=Vector3.one*.55f;var c=ring.GetComponent<Collider>();if(c)Object.Destroy(c);var r=ring.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard"));m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2.4f);r.material=m;Object.Destroy(ring,.15f);}
}