using UnityEngine;

public class M1ResourceCrafting : MonoBehaviour
{
    public static int wood, ore, seals, axes, picks; bool open;
    // v0.7.9 读条采集：按住 F 约 1.1 秒完成一次采集，松手或离开节点则取消
    static M1HarvestNode channelNode; static float channelT; const float ChannelTime = 1.1f;
    static readonly string[] names={"灵木斧","青玉镐","灵契符"};
    public static void Collect(string resource,int amount){if(resource=="矿石")ore+=amount;else wood+=amount;}
    public static int ToolBonus(string resource){return resource=="矿石"&&picks>0?1:resource!="矿石"&&axes>0?1:0;}
    public static bool ConsumeSeal(){if(seals<=0)return false;seals--;return true;}
    // "重新开始" 时由 M1GameManager 调用；同样是因为这些字段是 static，传送门切图要保留，只有重开才清零。
    public static void ResetProgress(){wood=0;ore=0;seals=0;axes=0;picks=0;}
    void Update()
    {
        var gm=M1GameManager.Instance;if(!gm||gm.Paused||(!gm.IsGameplayActive&&!open))return;
        if(Input.GetKeyDown(KeyCode.B)){open=!open;gm.SetCursorForUI(open);}
        if(!gm.IsGameplayActive)return;
        var player=gm.player?gm.player.transform:null;
        if(Input.GetKeyDown(KeyCode.F))
        {
            channelNode=FindReadyNode(player); channelT=0f;
            if(!channelNode)gm.ShowAction("靠近灵木或青玉矿点 3.8 米内，按住 F 采集。");
        }
        if(channelNode)
        {
            if(!Input.GetKey(KeyCode.F)){channelNode=null;channelT=0f;gm.ShowAction("采集已取消。");}
            else if(!channelNode.CanHarvest){channelNode=null;channelT=0f;gm.ShowAction("离开了采集范围，采集中断。");}
            else
            {
                channelT+=Time.deltaTime;
                if(channelT>=ChannelTime){channelNode.Harvest();channelNode=null;channelT=0f;}
            }
        }
    }
    static M1HarvestNode FindReadyNode(Transform player)
    {
        if(!player)return null;M1HarvestNode nearest=null;float d=3.8f;
        foreach(var n in M1HarvestNode.All){if(!n.CanHarvest)continue;float q=Vector3.Distance(player.position,n.transform.position);if(q<d){d=q;nearest=n;}}
        return nearest;
    }
    /// 一张配方卡片：名称 / 材料（不足时标红）/ 用途 / 制作按钮（材料不足时置灰）
    static bool Recipe(Rect r,string title,string cost,string use,bool affordable)
    {
        GUI.Box(r,GUIContent.none);
        GUI.Label(new Rect(r.x+16,r.y+8,r.width-150,24),"<b>"+title+"</b>",new GUIStyle(M1UISkin.Body){fontSize=17});
        GUI.Label(new Rect(r.x+16,r.y+32,r.width-150,20),"<color="+(affordable?"#E0BA66":"#E5534B")+">"+cost+"</color>",new GUIStyle(M1UISkin.Body){fontSize=13});
        GUI.Label(new Rect(r.x+16,r.y+54,r.width-150,20),use,M1UISkin.Small);
        bool was=GUI.enabled;GUI.enabled=affordable;
        bool clicked=GUI.Button(new Rect(r.xMax-120,r.y+22,104,38),affordable?"制作":"材料不足");
        GUI.enabled=was;return clicked;
    }
    void OnGUI()
    {
        if(channelNode&&Event.current.type==EventType.Repaint)
        {
            M1UISkin.Apply();float bw=260f,bx=(Screen.width-bw)*.5f,by=Screen.height*.68f;
            GUI.Box(new Rect(bx-8,by-30,bw+16,46),GUIContent.none);
            GUI.Label(new Rect(bx,by-26,bw,20),"采集 "+channelNode.resource+"…",M1UISkin.Small);
            M1UISkin.Bar(new Rect(bx,by,bw,14),Mathf.Clamp01(channelT/ChannelTime),M1UISkin.Jade);
        }
        if(!open||!M1GameManager.Instance||M1GameManager.Instance.Paused)return;
        M1UISkin.Apply();GUI.depth=-50;
        float w=Mathf.Min(560,Screen.width-30),h=400,x=(Screen.width-w)*.5f,y=(Screen.height-h)*.5f;
        M1UISkin.Dim(.35f);GUI.Box(new Rect(x,y,w,h),GUIContent.none);
        GUI.Label(new Rect(x,y+14,w,28),"星金工坊",M1UISkin.Heading);M1UISkin.Divider(x+w*.5f,y+46,w-60);
        GUI.Label(new Rect(x+24,y+56,w-48,22),$"<color=#E0BA66>灵木</color> {wood}　　<color=#5CDBCC>青玉矿</color> {ore}　　灵契符 {seals}　　斧 {axes}　镐 {picks}",new GUIStyle(M1UISkin.Body){alignment=TextAnchor.MiddleCenter});
        var gm=M1GameManager.Instance;
        if(Recipe(new Rect(x+20,y+92,w-40,82),"灵契符","灵木 × 2 + 青玉矿 × 1","结契必需：异兽生命 ≤10% 时靠近按 E，消耗 1 张",wood>=2&&ore>=1)){wood-=2;ore--;seals++;gm.ShowAction("灵契符制作完成；靠近生命值 ≤10% 的异兽按 E 结契。");}
        if(Recipe(new Rect(x+20,y+182,w-40,82),"灵木斧","灵木 × 3","砍树时每次额外获得 1 份灵木",wood>=3)){wood-=3;axes++;gm.ShowAction("灵木斧制作完成；砍树每次额外获得木材。");}
        if(Recipe(new Rect(x+20,y+272,w-40,82),"青玉镐","灵木 × 2 + 青玉矿 × 2","挖矿时每次额外获得 1 份青玉矿",wood>=2&&ore>=2)){wood-=2;ore-=2;picks++;gm.ShowAction("青玉镐制作完成；挖矿每次额外获得矿石。");}
        if(GUI.Button(new Rect(x+w*.5f-60,y+h-38,120,30),"关闭 (B)")){open=false;gm.SetCursorForUI(false);}
    }
}



