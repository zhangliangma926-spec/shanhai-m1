using System.IO;
using UnityEngine;

/// <summary>Version variants choose center-screen or mouse-pointer aiming through aim-mode.txt beside the executable.</summary>
public class M1ChargeWeaponSystem : MonoBehaviour
{
    struct Weapon{public string name;public Color color;public float seconds,min,max;public Weapon(string n,Color c,float s,float lo,float hi){name=n;color=c;seconds=s;min=lo;max=hi;}}
    readonly Weapon[] weapons={
        new Weapon("星金灵杖",new Color(.28f,.88f,1f),2f,1f,12f),
        new Weapon("青玉灵杖",new Color(.34f,1f,.72f),1.35f,1f,8f),
        new Weapon("赤焰灵杖",new Color(1f,.48f,.24f),2.55f,2f,10f),
        new Weapon("赤霄灵杖",new Color(1f,.22f,.12f),3.25f,4f,32f)
    };
    public static bool UsePointerAim{get;private set;}
    /// 本帧准星指向的目标与落点（每帧在 Update 里算一次，OnGUI 和角色转向直接读，避免重复射线检测）
    public static M1Actor Aimed{get;private set;}
    public static Vector3 AimPoint;
    public static bool IsCharging{get;private set;}
    int selected;bool charging,keyMode;float charge,cooldown;Transform crystal;Vector3 crystalBaseScale;Renderer crystalRenderer;
    void Awake(){LoadAimMode();}
    void Start(){var t=FindDeep(transform,"法杖灵晶");if(t){crystal=t;crystalBaseScale=t.localScale;crystalRenderer=t.GetComponent<Renderer>();}var gm=M1GameManager.Instance;if(gm&&gm.IsGameplayActive)gm.SetCursorForUI(false);}
    static void LoadAimMode()
    {
        // v0.7.7 起默认使用 B 版（鼠标准星）；只有 aim-mode.txt 明确写 center 时才用中心准星
        UsePointerAim=true;
        try{string exeDir=Directory.GetParent(Application.dataPath)?.FullName;string cfg=Path.Combine(exeDir??Application.dataPath,"aim-mode.txt");if(File.Exists(cfg))UsePointerAim=!File.ReadAllText(cfg).Trim().Equals("center",System.StringComparison.OrdinalIgnoreCase);}
        catch{UsePointerAim=true;}
    }
    public static Vector3 AimScreenPoint()
    {
        if(!UsePointerAim)return new Vector3(Screen.width*.5f,Screen.height*.5f,0f);
        Vector3 p=Input.mousePosition;p.x=Mathf.Clamp(p.x,1f,Screen.width-1f);p.y=Mathf.Clamp(p.y,1f,Screen.height-1f);return p;
    }
    void Update()
    {
        var gm=M1GameManager.Instance;if(!gm||!gm.IsGameplayActive){charging=false;IsCharging=false;charge=0;Aimed=null;UpdateCrystal(0);return;}
        cooldown=Mathf.Max(0,cooldown-Time.deltaTime);
        Aimed=M1SpellProjectile.GetAimedActor(Camera.main?Camera.main.transform:null,transform,out AimPoint);
        if(!charging&&Input.GetKeyDown(KeyCode.R)){selected=(selected+1)%weapons.Length;gm.ShowAction("切换武器："+weapons[selected].name+"，伤害上限 "+weapons[selected].max+"。");}
        if(!charging&&cooldown<=0&&(Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.J))){charging=true;keyMode=Input.GetKeyDown(KeyCode.J);charge=0;M1AudioDirector.PlayUI("charge");}
        if(charging){charge=Mathf.Min(weapons[selected].seconds,charge+Time.deltaTime);UpdateCrystal(Mathf.Clamp01(charge/weapons[selected].seconds));bool released=keyMode?Input.GetKeyUp(KeyCode.J):Input.GetMouseButtonUp(0);if(released)Fire();}
        IsCharging=charging;
    }
    void Fire()
    {
        var w=weapons[selected];float ratio=Mathf.Clamp01(charge/w.seconds);float damage=Mathf.Min(w.max,Mathf.Lerp(w.min,w.max,Mathf.Pow(ratio,1.25f)));
        charging=false;charge=0;cooldown=.38f;UpdateCrystal(0);var gm=M1GameManager.Instance;var caster=gm?gm.player:GetComponent<M1Actor>();var cam=Camera.main;
        if(M1SpellProjectile.Cast(caster,damage,w.color,w.name,cam?cam.transform:null)){M1AudioDirector.PlayUI("release");var ca=GetComponent<M1CharacterAnimator>();if(ca)ca.Attack();}
    }
    void UpdateCrystal(float ratio)
    {
        if(!crystal)return;crystal.localScale=crystalBaseScale*(1f+ratio*.68f);
        if(crystalRenderer){var m=crystalRenderer.material;if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",weapons[selected].color*(.8f+ratio*3.5f));}}
    }
    GUIStyle sAim,sName,sR;
    static Transform FindDeep(Transform root,string name){if(root.name==name)return root;foreach(Transform c in root){var r=FindDeep(c,name);if(r)return r;}return null;}
    static void DrawCross(float cx,float cy,float gap,Color c)
    {
        const float len=8f,th=2f;
        M1UISkin.Tint(new Rect(cx-th*.5f,cy-gap-len,th,len),c);M1UISkin.Tint(new Rect(cx-th*.5f,cy+gap,th,len),c);
        M1UISkin.Tint(new Rect(cx-gap-len,cy-th*.5f,len,th),c);M1UISkin.Tint(new Rect(cx+gap,cy-th*.5f,len,th),c);
        M1UISkin.Tint(new Rect(cx-1.5f,cy-1.5f,3,3),c);
    }
    void OnGUI()
    {
        if(Event.current.type!=EventType.Repaint)return; // 这里没有按钮，只在绘制事件里画，省掉每帧多余的布局/输入事件
        var gm=M1GameManager.Instance;if(!gm||!gm.IsGameplayActive)return;
        M1UISkin.Apply();
        if(sAim==null){sAim=new GUIStyle(M1UISkin.Body){fontSize=13};sName=new GUIStyle(M1UISkin.Body){fontSize=15,fontStyle=FontStyle.Bold};sR=new GUIStyle(M1UISkin.Small){alignment=TextAnchor.MiddleCenter};}
        var cf=Camera.main?Camera.main.GetComponent<M1CameraFollow>():null;bool dragCam=cf&&cf.IsDragging;
        var w=weapons[selected];Vector3 sp=AimScreenPoint();float cx=sp.x,cy=Screen.height-sp.y;
        float ratio=charging?Mathf.Clamp01(charge/w.seconds):0;
        // 准星：先画一层深色描边保证在亮背景上也看得清，蓄力时四臂向外张开
        float gap=5f+ratio*6f;
        if(!dragCam){DrawCross(cx+1,cy+1,gap,new Color(0,0,0,.55f));DrawCross(cx,cy,gap,new Color(w.color.r,w.color.g,w.color.b,.97f));}
        // 瞄中目标：名称 + 迷你血条
        M1Actor aimed=Aimed;
        float lx=Mathf.Min(cx+18,Screen.width-200),ly=Mathf.Clamp(cy+12,4,Screen.height-44);
        if(aimed)
        {
            GUI.Box(new Rect(lx,ly,186,40),GUIContent.none);
            GUI.Label(new Rect(lx+10,ly+3,170,18),aimed.displayName+"  "+Mathf.CeilToInt(aimed.health)+"/"+Mathf.CeilToInt(aimed.maxHealth),sAim);
            float hr=Mathf.Clamp01(aimed.health/Mathf.Max(1f,aimed.maxHealth));
            M1UISkin.Bar(new Rect(lx+10,ly+25,166,7),hr,aimed.isTameable&&hr<=.10f?M1UISkin.Jade:Color.Lerp(M1UISkin.Danger,M1UISkin.Gold,hr));
        }
        // 底部灵杖面板
        float bw=380,bx=Screen.width*.5f-bw*.5f,by=Screen.height-88;
        GUI.Box(new Rect(bx,by,bw,74),GUIContent.none);
        var nameStyle=sName;nameStyle.normal.textColor=w.color;
        GUI.Label(new Rect(bx+16,by+8,220,22),"灵杖 · "+w.name,nameStyle);
        for(int i=0;i<weapons.Length;i++)M1UISkin.Tint(new Rect(bx+bw-98+i*14,by+15,9,9),i==selected?weapons[i].color:new Color(1,1,1,.18f));
        GUI.Label(new Rect(bx+bw-40,by+9,36,18),"R",sR);
        M1UISkin.Bar(new Rect(bx+16,by+34,bw-32,10),ratio,w.color);
        GUI.Label(new Rect(bx+16,by+50,bw-32,18),charging?"蓄力 "+Mathf.RoundToInt(ratio*100)+"%  ·  松开发射  ·  "+Mathf.RoundToInt(Mathf.Lerp(w.min,w.max,ratio))+" 伤害":"按住左键 / J 蓄力  ·  伤害 "+w.min+" – "+w.max,M1UISkin.Small);
    }
}