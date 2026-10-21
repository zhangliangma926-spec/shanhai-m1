using System.Collections.Generic;
using UnityEngine;

public class M1PetSystem : MonoBehaviour
{
    public static M1PetSystem Instance;
    static readonly List<M1Actor> Collection=new List<M1Actor>();
    static M1Actor activePet;
    bool panelOpen;
    Vector2 petScroll;
    readonly float[] cooldowns=new float[3];
    float shieldTimer;
    public bool IsShieldActive=>shieldTimer>0f;
    public M1Actor ActivePet=>activePet;
    public int Count=>Collection.Count;
    void Awake(){Instance=this;}
    void Start(){if(activePet)SetActivePet(activePet);}
    void OnDestroy(){if(Instance==this)Instance=null;}
    // "重新开始" 时由 M1GameManager 调用：Collection/activePet 是 static 的，场景重载不会自动清空，
    // 不清掉的话已驯服的异兽和出战状态会原样带进新的一局。注意：世界之间的传送门不调用这个方法，
    // 过图时应该保留队伍，只有真正重开一局才清空。
    public static void ResetProgress()
    {
        foreach(var pet in Collection) if(pet) Object.Destroy(pet.gameObject);
        Collection.Clear();
        if(activePet) Object.Destroy(activePet.gameObject);
        activePet=null;
    }
    void Update()
    {
        if(!M1GameManager.Instance||M1GameManager.Instance.Paused)return;
        if(!M1GameManager.Instance.IsGameplayActive&&!panelOpen)return;
        if(Input.GetKeyDown(KeyCode.P)){panelOpen=!panelOpen;M1GameManager.Instance.SetCursorForUI(panelOpen);}
        if(!M1GameManager.Instance.IsGameplayActive)return;
        shieldTimer=Mathf.Max(0f,shieldTimer-Time.deltaTime);
        for(int i=0;i<cooldowns.Length;i++)cooldowns[i]=Mathf.Max(0f,cooldowns[i]-Time.deltaTime);
        if(!activePet||!activePet.gameObject.activeSelf)return;
        if(Input.GetKeyDown(KeyCode.Alpha1))Cast(0);
        if(Input.GetKeyDown(KeyCode.Alpha2))Cast(1);
        if(Input.GetKeyDown(KeyCode.Alpha3))Cast(2);
    }
    public void TryCalmNearest(Vector3 at)
    {
        M1Actor nearest=null;float distance=4.8f;
        foreach(var a in M1Actor.All)
        {if(!a||!a.isTameable||a.tamed||a.IsDead)continue;float d=Vector3.Distance(at,a.transform.position);if(d<distance){distance=d;nearest=a;}}
        if(!nearest){M1GameManager.Instance?.ShowAction("附近没有感应到灵纹的异兽。");return;}nearest.MarkForTaming(7f);
    }
    public void TryCapture(Vector3 playerPosition)
    {
        M1Actor nearest=null;float distance=3.25f;
        foreach(var a in M1Actor.All)
        {if(!a||!a.isTameable||a.tamed||a.IsDead)continue;float d=Vector3.Distance(playerPosition,a.transform.position);if(d<distance){distance=d;nearest=a;}}
        if(!nearest){M1GameManager.Instance?.ShowAction("靠近一只野生异兽后按 E。查看屏幕里的物种提示。");return;}
        string reason=TamingRequirement(nearest);if(!string.IsNullOrEmpty(reason)){M1GameManager.Instance?.ShowAction(reason);return;}if(!M1ResourceCrafting.ConsumeSeal()){M1GameManager.Instance?.ShowAction("结契需要灵契符：按 F 采集木材和矿石，再按 B 制作。");return;}
        if(!Collection.Contains(nearest))Collection.Add(nearest);
        nearest.tamed=true;nearest.isTameable=false;nearest.followTarget=null;nearest.transform.SetParent(null);DontDestroyOnLoad(nearest.gameObject);nearest.gameObject.SetActive(false);
        panelOpen=true;M1GameManager.Instance?.SetCursorForUI(true);M1GameManager.Instance?.ShowAction(nearest.displayName+" 已结契并收入灵契图鉴。按 P 管理队伍。");
    }
    string TamingRequirement(M1Actor pet)
    {
        if(pet.health>pet.maxHealth*.10f)return pet.displayName+" 仍有战力：先将生命削弱至 10% 或以下，再靠近按 E 结契。";
        return null;
    }    void SetActivePet(M1Actor pet)
    {
        if(!pet||!M1GameManager.Instance||!M1GameManager.Instance.player)return;
        if(activePet&&activePet!=pet)activePet.gameObject.SetActive(false);
        activePet=pet;activePet.gameObject.SetActive(true);activePet.tamed=true;activePet.isTameable=false;activePet.followTarget=M1GameManager.Instance.player.transform;
        activePet.transform.position=M1GameManager.Instance.player.transform.position+M1GameManager.Instance.player.transform.forward*1.6f+M1GameManager.Instance.player.transform.right*.9f;
        M1GameManager.Instance.beast=activePet;M1GameManager.Instance.ShowAction(activePet.displayName+" 已出战；靠近敌人时会协同攻击，按 1/2/3 施放技能。");
    }
    void Deploy(int i){if(i<0||i>=Collection.Count)return;SetActivePet(Collection[i]);panelOpen=false;M1GameManager.Instance?.SetCursorForUI(false);}
    void Recall(){if(!activePet)return;activePet.gameObject.SetActive(false);activePet=null;if(M1GameManager.Instance)M1GameManager.Instance.beast=null;M1GameManager.Instance?.ShowAction("异兽已收回灵契面板。");}
    void Cast(int skill)
    {
        float[] cd={2.2f,5.5f,12f};if(cooldowns[skill]>0f)return;var game=M1GameManager.Instance;
        if(skill<2&&(!game||!game.enemy||!game.enemy.gameObject.activeSelf)){game?.ShowAction("附近没有可攻击的测试敌人。");return;}
        if(skill<2&&Vector3.Distance(activePet.transform.position,game.enemy.transform.position)>(skill==0?6.5f:10f)){game.ShowAction("敌人距离太远，让异兽靠近后再使用技能。");return;}
        cooldowns[skill]=cd[skill];var petAnim=activePet.GetComponent<M1CreatureAnimator>();if(petAnim)petAnim.Attack();M1AudioDirector.PlayCreatureAction(activePet.speciesId,activePet.transform.position,.42f);
        if(skill==0){game.enemy.TakeDamage(28f);Effect(game.enemy.transform.position+Vector3.up,new Color(.24f,.88f,.92f),1.2f);game.ShowAction(activePet.displayName+" 使用【风刃】！");}
        else if(skill==1){game.enemy.TakeDamage(46f);Effect(game.enemy.transform.position+Vector3.up,new Color(.42f,.62f,1f),2f);game.ShowAction(activePet.displayName+" 使用【灵爆】！");}
        else{shieldTimer=5f;Effect(game.player.transform.position+Vector3.up*.8f,new Color(.2f,.85f,.75f),1.7f);game.ShowAction("【灵护】已开启：5 秒内受到的伤害减半。");}
    }
    static void Effect(Vector3 at,Color color,float size)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name="灵兽技能效果";o.transform.position=at;o.transform.localScale=Vector3.one*size;
        var col=o.GetComponent<Collider>();if(col)Destroy(col);
        var r=o.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard"));m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2.5f);r.material=m;
        var light=o.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=1.5f;light.range=size*2.5f;Destroy(o,.35f);
    }
    void OnGUI()
    {
        if(!panelOpen||!M1GameManager.Instance||M1GameManager.Instance.Paused)return;
        M1UISkin.Apply();GUI.depth=-50;
        const float rowH=66f;
        float w=Mathf.Min(500,Screen.width-30),h=Mathf.Min(Screen.height-60,Mathf.Max(220,140+Collection.Count*rowH));float x=(Screen.width-w)*.5f,y=(Screen.height-h)*.5f;
        M1UISkin.Dim(.35f);GUI.Box(new Rect(x,y,w,h),GUIContent.none);
        GUI.Label(new Rect(x,y+14,w,28),"灵契队伍",M1UISkin.Heading);M1UISkin.Divider(x+w*.5f,y+46,w-60);
        GUI.Label(new Rect(x+22,y+54,w-44,20),"已结契 "+Collection.Count+" 只  ·  P 关闭  ·  出战后 1/2/3 施放技能",M1UISkin.Small);
        if(Collection.Count==0){GUI.Label(new Rect(x+22,y+92,w-44,60),"尚无异兽。\n将野生异兽削弱到 10% 生命以下，带着灵契符靠近按 E 结契。",M1UISkin.Body);return;}
        Rect view=new Rect(x+16,y+82,w-32,h-(activePet&&shieldTimer>0?124:98));
        petScroll=GUI.BeginScrollView(view,petScroll,new Rect(0,0,view.width-14,Collection.Count*rowH));
        for(int i=0;i<Collection.Count;i++)
        {
            var pet=Collection[i];if(!pet)continue;float row=i*rowH;bool out_=activePet==pet&&pet.gameObject.activeSelf;
            GUI.Box(new Rect(0,row,view.width-16,rowH-8),GUIContent.none);
            if(out_)M1UISkin.Tint(new Rect(0,row+6,3,rowH-20),M1UISkin.Jade);
            GUI.Label(new Rect(14,row+6,view.width-140,22),"<b>"+pet.displayName+"</b>  <color="+(out_?"#5CDBCC":"#8FA9A4")+">"+(out_?"出战中":"灵契中")+"</color>",M1UISkin.Body);
            GUI.Label(new Rect(14,row+30,view.width-140,20),"1 风刃 · 2 灵爆 · 3 灵护",M1UISkin.Small);
            if(out_){if(GUI.Button(new Rect(view.width-120,row+12,96,34),"收回"))Recall();}
            else if(GUI.Button(new Rect(view.width-120,row+12,96,34),"放出"))Deploy(i);
        }
        GUI.EndScrollView();
        if(activePet&&shieldTimer>0)GUI.Label(new Rect(x+22,y+h-36,w-44,22),"<color=#5CDBCC>灵护剩余 "+shieldTimer.ToString("0.0")+" 秒</color>",M1UISkin.Body);
    }
}



