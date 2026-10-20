using UnityEngine;

public class M1TamingGuide : MonoBehaviour
{
    bool open; Vector2 scroll;
    static readonly string[] Entries={
        "岚角灵兽 · 125 HP\n风雷守卫，稳步巡游；灵契：降至 10% 后用灵契符并按 E。",
        "赤尾灵狐 · 90 HP\n敏捷林狐，靠近会警觉；灵契：控制蓄力，避免过度攻击。",
        "幽蓝翼兽 · 115 HP\n幽谷飞兽，守护水晶；灵契：瞄准身体，降至 10% 后按 E。",
        "岩甲灵龟 · 150 HP\n厚甲慢行，耐打；灵契：需要多次攻击，低血后用灵契符。",
        "青羽灵雀 · 75 HP\n轻巧灵鸟，生命偏低；灵契：使用轻点射，留意 10% 血线。",
        "赤焰山猫 · 95 HP\n火纹猎手，靠近会反击；灵契：使用低蓄力攻击控制血量。",
        "雾角灵鹿 · 100 HP\n雾林行者，生命均衡；灵契：削弱至 10% 后靠近按 E。",
        "玄岩獠猪 · 140 HP\n岩地冲锋者，生命较厚；灵契：保持距离逐次削弱。",
        "琉璃水貂 · 82 HP\n溪谷灵兽，行动敏捷；灵契：轻攻击，低血时迅速靠近。",
        "月羽灵蝶 · 65 HP\n月光精灵，生命最低；灵契：点射一至数次，别使用满蓄力。",
        "六目灵狐 · 88 HP\n六瞳洞察林间动静；灵契：轻攻击，降至 10% 后结契。",
        "七面幻狐 · 108 HP\n多面幻灵，能够迷惑敌人；灵契：控制蓄力并用灵契符。",
        "赤霄朱雀 · 118 HP\n火羽飞兽，攻击性强；灵契：保持距离，逐步削弱到 10%。",
        "青蛟 · 165 HP\n溪谷长躯灵龙，生命最高；灵契：使用较高伤害武器，低血后按 E。",
        "苍羽仙鹤 · 92 HP\n高空巡林的长喙灵鸟；灵契：轻攻击，低血后靠近结契。",
        "琥珀雷豹 · 132 HP\n雷纹猎手，速度迅捷；灵契：先瞄准再蓄力，低血后用符。",
        "霜牙灵狼 · 98 HP\n雪原狼群的首领，奔袭迅猛；灵契：先拉开距离，降至 10% 后按 E。",
        "玉角灵牛 · 140 HP\n温顺却极耐打的玉角巨兽；灵契：耐心削弱，低血后用灵契符。",
        "焰驼 · 112 HP\n背负火纹的沙驼，性情慵懒；灵契：轻攻击即可，低血后靠近结契。",
        "白玉灵犬 · 84 HP\n通灵白犬，嗅觉敏锐；灵契：点射削弱，留意 10% 血线再按 E。"
    };
    void Update(){if(!M1GameManager.Instance||M1GameManager.Instance.Paused||(!M1GameManager.Instance.IsGameplayActive&&!open))return;if(Input.GetKeyDown(KeyCode.F1)){open=!open;scroll=Vector2.zero;M1GameManager.Instance.SetCursorForUI(open);}}
    void OnGUI()
    {
        if(!open||!M1GameManager.Instance||M1GameManager.Instance.Paused)return;
        M1UISkin.Apply();GUI.depth=-50;
        float w=Mathf.Min(900,Screen.width-36),h=Mathf.Min(740,Screen.height-30),x=(Screen.width-w)*.5f,y=(Screen.height-h)*.5f;
        M1UISkin.Dim(.45f);GUI.Box(new Rect(x,y,w,h),GUIContent.none);
        GUI.Label(new Rect(x,y+14,w,28),"灵 兽 图 鉴",M1UISkin.Heading);M1UISkin.Divider(x+w*.5f,y+46,w-80);
        GUI.Label(new Rect(x+24,y+54,w-48,40),"结契方法：削弱至生命 10% 以下 → 带灵契符靠近 3.25 米内按 E。F 采集材料，B 制作灵契符。　已结契 <color=#5CDBCC>"+(M1PetSystem.Instance?M1PetSystem.Instance.Count:0)+"</color> / "+Entries.Length,new GUIStyle(M1UISkin.Body){fontSize=13,alignment=TextAnchor.MiddleCenter});
        int cols=w>=700?2:1;const float cardH=76f,gap=10f;
        Rect view=new Rect(x+18,y+100,w-36,h-156);float cardW=(view.width-16-gap*(cols-1))/cols;
        int rows=Mathf.CeilToInt(Entries.Length/(float)cols);
        scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,view.width-16,rows*(cardH+gap)));
        var nameStyle=new GUIStyle(M1UISkin.Body){fontSize=16,fontStyle=FontStyle.Bold};nameStyle.normal.textColor=M1UISkin.Gold;
        var hpStyle=new GUIStyle(M1UISkin.Small){alignment=TextAnchor.UpperRight};
        for(int i=0;i<Entries.Length;i++)
        {
            Rect card=new Rect((i%cols)*(cardW+gap),(i/cols)*(cardH+gap),cardW,cardH);GUI.Box(card,GUIContent.none);
            string[] lines=Entries[i].Split('\n');string[] head=lines[0].Split('·');
            GUI.Label(new Rect(card.x+14,card.y+8,card.width-100,24),head[0].Trim(),nameStyle);
            if(head.Length>1)GUI.Label(new Rect(card.xMax-96,card.y+11,82,20),head[1].Trim(),hpStyle);
            if(lines.Length>1)GUI.Label(new Rect(card.x+14,card.y+34,card.width-28,40),lines[1],new GUIStyle(M1UISkin.Body){fontSize=13});
        }
        GUI.EndScrollView();
        if(GUI.Button(new Rect(x+w*.5f-70,y+h-46,140,34),"关闭图鉴 (F1)")){open=false;M1GameManager.Instance.SetCursorForUI(false);}
    }
}
