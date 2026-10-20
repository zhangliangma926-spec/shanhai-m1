using UnityEngine;
using UnityEngine.SceneManagement;

public class M1GameManager : MonoBehaviour
{
    public static M1GameManager Instance;
    public static bool StartImmediatelyOnNextScene;
    public M1Actor player, enemy, beast;
    public float playerHealth = 100f;
    public Vector3 respawnPoint = new Vector3(0,0,25f);
    string message="准备进入山海灵境。";
    float messageTimer;
    bool started, paused, uiFocus, titleOpen, instructionsOpen;
    Vector2 manualScroll;
    public bool Paused=>paused;
    public bool IsGameplayActive=>started&&!paused&&!uiFocus&&!titleOpen&&!instructionsOpen;

    void Awake()
    {
        Instance=this;
        QualitySettings.shadowDistance=70f; // 大地图下限制阴影距离，减轻显卡压力
        QualitySettings.vSyncCount=1;       // v0.7.8 开启垂直同步，减少画面撕裂，帧间距更均匀
        bool direct=StartImmediatelyOnNextScene;StartImmediatelyOnNextScene=false;
        started=direct;titleOpen=!direct;paused=false;Time.timeScale=direct?1f:0f;SetCursorForUI(!direct);
    }
    void Update()
    {
        if(messageTimer>0f)messageTimer-=Time.unscaledDeltaTime;
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(titleOpen)return;
            if(instructionsOpen){instructionsOpen=false;SetCursorForUI(true);return;}
            if(paused)ResumeGame();else PauseGame();
        }
    }
    public void SetCursorForUI(bool open){uiFocus=open;bool release=open||paused||!started||titleOpen||instructionsOpen;if(release){Cursor.visible=true;Cursor.lockState=CursorLockMode.None;}else{Cursor.visible=false;Cursor.lockState=M1ChargeWeaponSystem.UsePointerAim?CursorLockMode.None:CursorLockMode.Locked;}}
    public void StartGame(){titleOpen=false;instructionsOpen=false;started=true;paused=false;Time.timeScale=1f;SetCursorForUI(false);ShowAction(M1ChargeWeaponSystem.UsePointerAim?"开始探索！鼠标移动准星，按住右键拖动转镜头，Shift 加速。":"开始探索！WASD 移动，鼠标转镜头，Shift 加速。");}
    public void PauseGame(){paused=true;Time.timeScale=0f;SetCursorForUI(true);}
    public void ResumeGame(){instructionsOpen=false;paused=false;Time.timeScale=1f;SetCursorForUI(false);}
    public void OpenInstructions(){instructionsOpen=true;paused=true;Time.timeScale=0f;SetCursorForUI(true);manualScroll=Vector2.zero;}
    public void CloseInstructions(){instructionsOpen=false;SetCursorForUI(true);}
    public void ReturnToTitle(){instructionsOpen=false;paused=true;titleOpen=true;started=false;Time.timeScale=0f;SetCursorForUI(true);manualScroll=Vector2.zero;}
    public void RestartGame(){M1PetSystem.ResetProgress();M1ResourceCrafting.ResetProgress();Time.timeScale=1f;Cursor.lockState=CursorLockMode.None;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
    public void QuitGame(){Time.timeScale=1f;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#endif
    }
    void OnApplicationQuit(){Time.timeScale=1f;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;}
    public void DamagePlayer(float n)
    {
        if(M1PetSystem.Instance&&M1PetSystem.Instance.IsShieldActive)n*=.5f;
        playerHealth=Mathf.Max(0f,playerHealth-n);
        if(M1CharacterAnimator.Player)M1CharacterAnimator.Player.Hit();
        if(player)M1CombatFeedback.Popup(player.transform.position+Vector3.up*2.1f,"-"+Mathf.Max(1,Mathf.RoundToInt(n)),M1CombatFeedback.Hurt,22f); // v0.8.1：玩家受伤也飘字
        if(playerHealth<=0f)
        {
            playerHealth=100f;
            if(player){var cc=player.GetComponent<CharacterController>();if(cc)cc.enabled=false;player.transform.position=respawnPoint;if(cc)cc.enabled=true;}
            ShowAction("你已回到中央灵泉，生命恢复。靠近灵泉继续探索。");
        }
    }
    public void TryTame(Vector3 at){if(M1PetSystem.Instance){M1PetSystem.Instance.TryCapture(at);return;}ShowAction("驯服系统尚未加载。");}
    public void CastSpiritMark(Vector3 at)
    {
        if(M1PetSystem.Instance)M1PetSystem.Instance.TryCalmNearest(at);
        var ring=GameObject.CreatePrimitive(PrimitiveType.Sphere);ring.name="灵纹效果";ring.transform.position=at;ring.transform.localScale=Vector3.one*.65f;
        var c=ring.GetComponent<Collider>();if(c)Destroy(c);var r=ring.GetComponent<Renderer>();r.material=new Material(Shader.Find("Standard"));r.material.color=new Color(.15f,.9f,.85f,.7f);r.material.SetFloat("_Mode",3);r.material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);r.material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);r.material.SetInt("_ZWrite",0);r.material.DisableKeyword("_ALPHATEST_ON");r.material.EnableKeyword("_ALPHABLEND_ON");r.material.renderQueue=3000;Destroy(ring,.55f);
    }
    public void ActorDefeated(M1Actor actor){if(actor==enemy)ShowAction("测试敌人已击败！");else if(actor&&actor.isTameable)ShowAction(actor.displayName+" 失去意识；本轮无法再结契。");}
    public void ShowAction(string text){message=text;messageTimer=3.4f;}
    static string ManualText =
        "山海驭灵录 · 星金营地试玩说明\n\n"+
        "【世界介绍】你是山海旅者，持灵杖探索营地、青岚灵林和玄岩谷。野生异兽各有生命、习性和灵息；削弱后用灵契符收服，再放出并肩战斗。\n\n"+
        "【移动与视角】WASD 移动，空格跳跃，按住 Shift 加速（任意方向）；Left Alt+方向键闪避。移动鼠标控制准星；按住鼠标右键拖动旋转镜头，准星推到屏幕边缘时镜头也会缓慢跟转；滚轮缩放。蓄力时角色自动面向准星。\n\n"+
        "【灵杖攻击】将准星对准守卫或异兽，按住鼠标左键或 J 蓄力，松开攻击；A 版射线固定从屏幕中心穿过，B 版射线从鼠标准星位置穿过；两版都不会自动吸附最近目标。没有瞄中角色时不造成伤害。R 切换武器。星金杖上限 12，青玉杖上限 8，赤焰杖上限 10，赤霄杖上限 32。野生异兽受击会至少保留 1 点生命，避免误杀。\n\n"+
        "【结契流程】攻击野生异兽至生命 ≤10%；靠近灵木/青玉矿按住 F 读条约 1 秒采集；按 B 制作灵契符；靠近目标约 3.25 米内按 E 结契（消耗 1 张）。按 P 查看队伍并放出/收回异兽，1/2/3 使用宠物技能。\n\n"+
        "【资源说明】灵木用于灵木斧和灵契符；青玉矿用于青玉镐和灵契符。斧增加采木量，镐增加采矿量。资源点的标牌也会显示用途。采空后约 2 分钟重新生长。\n\n"+
        "【刷新圈】角色脚下的淡青色圆圈是 100 米刷新圈：在圈内活动时，会概率刷出野生灵兽，刷出的灵兽离开圈外一定距离后会消失。按 H 可隐藏圆圈。\n\n"+
        "【灵兽图鉴】按 F1 打开可滚动图鉴，可查看名称、生命上限、简介与结契方法。野外名牌显示异兽名称和生命；结契伙伴放出后也显示名称。\n\n"+
        "【灵兽名录】岚角灵兽、赤尾灵狐、幽蓝翼兽、岩甲灵龟、青羽灵雀、赤焰山猫、雾角灵鹿、玄岩獠猪、琉璃水貂、月羽灵蝶、六目灵狐、七面幻狐、赤霄朱雀、青蛟、苍羽仙鹤、琥珀雷豹、霜牙灵狼、玉角灵牛、焰驼、白玉灵犬。\n\n"+
        "【安全与退出】Esc 暂停、继续、重新开始、查看本说明、回到标题或退出。生命归零后会在当前世界的中央灵泉复活。\n\n"+
        "【声音】本试用版含原创程序合成的森林氛围、法杖动作音与各异兽音色。可在正式试听后再调整混音与叫声。";

    public const string Version="v0.8.1";
    // 说明文字里的【小标题】自动染成金色加粗
    static readonly string ManualRich=ManualText.Replace("【","<color=#E0BA66><b>【").Replace("】","】</b></color>");

    void DrawManual(Rect area)
    {
        GUI.Box(area,GUIContent.none);Rect view=new Rect(area.x+20,area.y+16,area.width-34,area.height-32);
        float contentH=M1UISkin.Body.CalcHeight(new GUIContent(ManualRich),view.width-22)+20;
        Rect content=new Rect(0,0,view.width-22,Mathf.Max(contentH,view.height));
        manualScroll=GUI.BeginScrollView(view,manualScroll,content);GUI.Label(content,ManualRich,M1UISkin.Body);GUI.EndScrollView();
    }
    void DrawFloatingLights()
    {
        float t=Time.unscaledTime;
        for(int i=0;i<34;i++)
        {
            float seed=i*12.9898f;float fx=Mathf.Repeat(Mathf.Sin(seed)*43758.5f,1f);float speed=.012f+.02f*Mathf.Repeat(seed*.37f,1f);
            float fy=1f-Mathf.Repeat(t*speed+fx*3.1f,1f);float sway=Mathf.Sin(t*.6f+i)*18f;
            float pulse=.35f+.35f*Mathf.Sin(t*1.7f+i*1.3f);Color c=i%3==0?M1UISkin.Jade:M1UISkin.Gold;c.a=pulse*.55f;
            M1UISkin.GlowDot(new Vector2(fx*Screen.width+sway,fy*Screen.height),10f+(i%5)*5f,c);
        }
    }
    void DrawTitle(float cx)
    {
        M1UISkin.Backdrop();DrawFloatingLights();
        float titleY=Mathf.Max(24f,Screen.height*.07f);
        M1UISkin.ShadowLabel(new Rect(0,titleY,Screen.width,72),"山 海 驭 灵 录",M1UISkin.Title);
        GUI.Label(new Rect(0,titleY+74,Screen.width,26),"星金营地 · M1 试玩版 "+Version,M1UISkin.Subtitle);
        M1UISkin.Divider(cx,titleY+110,Mathf.Min(560,Screen.width-60));
        float w=Mathf.Min(860,Screen.width-40),top=titleY+128,bottom=Screen.height-96;
        DrawManual(new Rect((Screen.width-w)*.5f,top,w,Mathf.Max(160,bottom-top)));
        if(GUI.Button(new Rect(cx-250,Screen.height-78,240,46),"开始探索"))StartGame();
        if(GUI.Button(new Rect(cx+10,Screen.height-78,240,46),"退出游戏"))QuitGame();
        GUI.Label(new Rect(0,Screen.height-26,Screen.width,20),AimLabel()+"  ·  按 Esc 随时打开菜单",St(ref sSmallC,()=>new GUIStyle(M1UISkin.Small){alignment=TextAnchor.MiddleCenter}));
    }
    void DrawPause(float cx)
    {
        M1UISkin.Dim(.55f);
        float w=400,h=430,x=cx-w*.5f,y=Screen.height*.5f-h*.5f;
        GUI.Box(new Rect(x,y,w,h),GUIContent.none);
        GUI.Label(new Rect(x,y+18,w,32),"暂  停",M1UISkin.Heading);M1UISkin.Divider(cx,y+58,w-60);
        string[] labels={"继续游戏  (Esc)","操作说明","重新开始","返回标题页","退出游戏"};
        for(int i=0;i<labels.Length;i++)
        {
            if(!GUI.Button(new Rect(x+60,y+80+i*58,w-120,44),labels[i]))continue;
            if(i==0)ResumeGame();else if(i==1)OpenInstructions();else if(i==2)RestartGame();else if(i==3)ReturnToTitle();else QuitGame();
        }
        GUI.Label(new Rect(x,y+h-34,w,20),"重新开始会清空本局资源与已结契的异兽",St(ref sSmallC,()=>new GUIStyle(M1UISkin.Small){alignment=TextAnchor.MiddleCenter}));
    }
    void DrawInstructions(float cx)
    {
        M1UISkin.Dim(.65f);
        float w=Mathf.Min(900,Screen.width-40),h=Mathf.Min(760,Screen.height-32),x=(Screen.width-w)*.5f,y=(Screen.height-h)*.5f;
        GUI.Label(new Rect(x,y,w,32),"操 作 说 明",M1UISkin.Heading);M1UISkin.Divider(cx,y+38,w*.6f);
        DrawManual(new Rect(x,y+50,w,h-110));
        if(GUI.Button(new Rect(cx-110,y+h-50,220,42),"返回暂停菜单"))CloseInstructions();
    }
    void DrawHud(float cx)
    {
        float w=Mathf.Min(320,Screen.width-24),x=14,y=14;
        GUI.Box(new Rect(x,y,w,118),GUIContent.none);
        GUI.Label(new Rect(x+14,y+8,w-28,22),"<b>山海驭灵录</b>  <color=#8FA9A4>"+Version+" · "+AimLabel()+"</color>",St(ref sBody14,()=>new GUIStyle(M1UISkin.Body){fontSize=14}));
        float hp=Mathf.Clamp01(playerHealth/100f);
        GUI.Label(new Rect(x+14,y+34,60,18),"生命",M1UISkin.Small);
        M1UISkin.Bar(new Rect(x+52,y+37,w-120,12),hp,Color.Lerp(M1UISkin.Danger,M1UISkin.Jade,hp));
        GUI.Label(new Rect(x+w-64,y+32,54,20),Mathf.CeilToInt(playerHealth)+"/100",M1UISkin.Small);
        int pets=M1PetSystem.Instance?M1PetSystem.Instance.Count:0;
        GUI.Label(new Rect(x+14,y+58,w-28,20),"灵兽 "+pets+"   灵木 "+M1ResourceCrafting.wood+"   青玉 "+M1ResourceCrafting.ore+"   灵契符 "+M1ResourceCrafting.seals,St(ref sBody14,()=>new GUIStyle(M1UISkin.Body){fontSize=14}));
        GUI.Label(new Rect(x+14,y+84,w-28,30),"左键/J 蓄力 · R 换杖 · F 采集 · B 工坊\nE 结契 · P 队伍 · F1 图鉴 · Esc 菜单",M1UISkin.Small);
        if(messageTimer>0f)
        {
            var content=new GUIContent(message);float tw=Mathf.Min(Screen.width-40,M1UISkin.Toast.CalcSize(content).x+40);
            float fade=Mathf.Clamp01(messageTimer/.4f);var old=GUI.color;GUI.color=new Color(1,1,1,fade);
            GUI.Box(new Rect(cx-tw*.5f,18,tw,40),content,M1UISkin.Toast);GUI.color=old;
        }
    }
    GUIStyle sBody14,sSmallC;
    static GUIStyle St(ref GUIStyle cache,System.Func<GUIStyle> make)=>cache??(cache=make());
    static string AimLabel()=>M1ChargeWeaponSystem.UsePointerAim?"鼠标准星 · 右键拖动转镜头":"中心准星";
    void OnGUI()
    {
        M1UISkin.Apply();
        GUI.depth=(titleOpen||instructionsOpen||paused)?-100:0; // 数值越小越靠前：菜单盖住名牌和准星
        float cx=Screen.width*.5f;
        if(titleOpen){DrawTitle(cx);return;}
        if(instructionsOpen){DrawInstructions(cx);return;}
        if(paused){DrawPause(cx);return;}
        DrawHud(cx);
    }
}
