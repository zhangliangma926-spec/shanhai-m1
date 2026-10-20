using UnityEditor;
using UnityEngine;

public static class M1V070WorldContent
{
    static readonly string[] Names={"岩甲灵龟","青羽灵雀","赤焰山猫","雾角灵鹿","玄岩獠猪","琉璃水貂","月羽灵蝶"};
    static readonly string[] Ids={"stone_turtle","sky_bird","ember_cat","mist_deer","rock_boar","glass_marten","moon_moth"};
    static readonly Color[] Colors={new Color(.38f,.53f,.42f),new Color(.27f,.72f,.79f),new Color(.82f,.31f,.20f),new Color(.66f,.77f,.73f),new Color(.48f,.35f,.27f),new Color(.32f,.72f,.75f),new Color(.75f,.58f,.91f)};
    static readonly Vector3[] Spots={new Vector3(-18,0,12),new Vector3(18,0,12),new Vector3(-22,0,25),new Vector3(22,0,25),new Vector3(-15,0,36),new Vector3(15,0,36),new Vector3(0,0,43)};
    public static void AddToScene(int kind, Transform player)
    {
        for(int i=0;i<Names.Length;i++)MakeBeast(i,Spots[i]);
        for(int i=0;i<6;i++)MakeResource("木材",new Vector3((i%2==0?-1:1)*(12+(i/2)*6),0,15+(i%3)*9),player,false);
        for(int i=0;i<6;i++)MakeResource("矿石",new Vector3((i%2==0?-1:1)*(10+(i/2)*7),0,22+(i%3)*8),player,true);
        var bench=GameObject.CreatePrimitive(PrimitiveType.Cube);bench.name="星金工坊工作台";bench.transform.position=new Vector3(3,.55f,3);bench.transform.localScale=new Vector3(1.5f,1.1f,.85f);bench.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.30f,.19f,.11f));
        Part(bench.transform,PrimitiveType.Cube,new Vector3(0,.47f,0),new Vector3(1.65f,.12f,1f),Mat(new Color(.55f,.35f,.17f)));
        var anvil=Part(bench.transform,PrimitiveType.Cube,new Vector3(.2f,.68f,.05f),new Vector3(.35f,.22f,.3f),Mat(new Color(.42f,.48f,.49f)));
        AddSimpleLabel(bench.transform,"B · 制作斧、镐与灵契符",new Vector3(0,1.35f,0));
    }
    static void MakeBeast(int i,Vector3 pos)
    {
        var root=new GameObject(Names[i]+" · 野生异兽");root.transform.position=pos;
        var actor=root.AddComponent<M1Actor>();actor.isTameable=true;actor.speciesId=Ids[i];actor.displayName=Names[i];actor.maxHealth=100;actor.health=100;
        var col=root.AddComponent<CapsuleCollider>();col.height=i==0?1.35f:1.55f;col.radius=i==0?.72f:.5f;col.center=new Vector3(0,col.height*.5f,0);
        Material main=Mat(Colors[i]);Material light=Mat(Color.Lerp(Colors[i],Color.white,.30f));Material dark=Mat(Color.Lerp(Colors[i],Color.black,.38f));Material eye=Mat(new Color(.08f,.10f,.13f));
        PrimitiveType bodyType=i==0||i==4?PrimitiveType.Sphere:PrimitiveType.Capsule;
        var body=Part(root.transform,bodyType,new Vector3(0,.72f,0),new Vector3(i==0?1.05f:.83f,i==0?.76f:.92f,i==4?1.08f:.72f),main);
        Part(root.transform,PrimitiveType.Sphere,new Vector3(0,.90f,.46f),new Vector3(.58f,.48f,.42f),light);
        Part(root.transform,PrimitiveType.Sphere,new Vector3(-.17f,1.05f,.80f),new Vector3(.10f,.12f,.07f),eye);
        Part(root.transform,PrimitiveType.Sphere,new Vector3(.17f,1.05f,.80f),new Vector3(.10f,.12f,.07f),eye);
        Part(root.transform,PrimitiveType.Sphere,new Vector3(0,.88f,.89f),new Vector3(.12f,.08f,.08f),dark);
        if(i==0){Part(root.transform,PrimitiveType.Sphere,new Vector3(0,1.25f,-.1f),new Vector3(1.05f,.28f,.95f),dark);for(int k=0;k<4;k++)Part(root.transform,PrimitiveType.Cube,new Vector3((k%2==0?-.5f:.5f),.24f,(k<2?-.34f:.34f)),new Vector3(.22f,.32f,.22f),dark);}
        if(i==1||i==6){Part(root.transform,PrimitiveType.Cylinder,new Vector3(-.32f,1.35f,.02f),new Vector3(.34f,.70f,.22f),light);Part(root.transform,PrimitiveType.Cylinder,new Vector3(.32f,1.35f,.02f),new Vector3(.34f,.70f,.22f),light);Part(root.transform,PrimitiveType.Sphere,new Vector3(0,.78f,-.72f),new Vector3(.20f,.78f,.22f),dark);}
        if(i==2||i==3){Part(root.transform,PrimitiveType.Cylinder,new Vector3(-.22f,1.38f,.1f),new Vector3(.17f,.48f,.17f),light);Part(root.transform,PrimitiveType.Cylinder,new Vector3(.22f,1.38f,.1f),new Vector3(.17f,.48f,.17f),light);}
        if(i==3){Part(root.transform,PrimitiveType.Cylinder,new Vector3(0,1.72f,-.02f),new Vector3(.14f,.68f,.14f),light);Part(root.transform,PrimitiveType.Cylinder,new Vector3(-.10f,1.62f,0),new Vector3(.1f,.55f,.1f),light);}
        if(i==4){Part(root.transform,PrimitiveType.Cylinder,new Vector3(-.34f,.88f,.70f),new Vector3(.22f,.35f,.22f),light);Part(root.transform,PrimitiveType.Cylinder,new Vector3(.34f,.88f,.70f),new Vector3(.22f,.35f,.22f),light);}
        if(i==5){for(int k=-1;k<=1;k+=2)Part(root.transform,PrimitiveType.Sphere,new Vector3(k*.55f,.85f,-.15f),new Vector3(.4f,.18f,.28f),light);Part(root.transform,PrimitiveType.Sphere,new Vector3(0,.62f,-.75f),new Vector3(.25f,.24f,.52f),dark);}
        if(i==6){for(int k=-1;k<=1;k+=2){Part(root.transform,PrimitiveType.Sphere,new Vector3(k*.57f,.92f,-.08f),new Vector3(.40f,.62f,.15f),light);Part(root.transform,PrimitiveType.Sphere,new Vector3(k*.88f,.94f,-.1f),new Vector3(.25f,.42f,.12f),main);}}
        root.AddComponent<M1SpiritBeastMotion>();
    }
    static void MakeResource(string resource,Vector3 pos,Transform player,bool mineral)
    {
        var root=new GameObject(mineral?"青玉矿脉 · 可采集":"灵木树 · 可采集");root.transform.position=pos;
        if(mineral){Part(root.transform,PrimitiveType.Cube,new Vector3(0,.45f,0),new Vector3(.75f,.9f,.7f),Mat(new Color(.24f,.48f,.56f)));Part(root.transform,PrimitiveType.Cube,new Vector3(.1f,1.05f,.08f),new Vector3(.3f,.62f,.32f),Mat(new Color(.28f,.78f,.83f)));}
        else{Part(root.transform,PrimitiveType.Cylinder,new Vector3(0,.85f,0),new Vector3(.23f,1.7f,.23f),Mat(new Color(.36f,.24f,.13f)));Part(root.transform,PrimitiveType.Sphere,new Vector3(0,1.85f,0),new Vector3(1.25f,1.15f,1.15f),Mat(new Color(.16f,.39f,.30f)));}
        var collider=root.AddComponent<BoxCollider>();collider.center=mineral?new Vector3(0,.7f,0):new Vector3(0,1f,0);collider.size=mineral?new Vector3(1.1f,1.6f,1.1f):new Vector3(1.2f,2.2f,1.2f);
        var node=root.AddComponent<M1HarvestNode>();node.resource=mineral?"矿石":"木材";node.amount=2;node.remaining=4;node.player=player;
        AddSimpleLabel(root.transform,mineral?"F · 青玉矿（镐/灵契符）":"F · 灵木（斧/灵契符）",new Vector3(0,2.6f,0));
    }
    static GameObject Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
    {
        var p=GameObject.CreatePrimitive(type);p.transform.SetParent(parent,false);p.transform.localPosition=pos;p.transform.localScale=scale;var r=p.GetComponent<Renderer>();if(r)r.sharedMaterial=mat;var c=p.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);return p;
    }
    static void AddSimpleLabel(Transform parent,string text,Vector3 pos){var go=new GameObject("提示标牌 · "+text);go.transform.SetParent(parent,false);go.transform.localPosition=pos;var tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=24;tm.characterSize=.055f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.96f,.86f,.58f);go.AddComponent<M1BillboardLabel>();}
    static Material Mat(Color color){var m=new Material(Shader.Find("Standard"));m.color=color;return m;}

}




