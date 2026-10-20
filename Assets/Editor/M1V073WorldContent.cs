using UnityEngine;

/// <summary>Additional original low-poly prototypes, assembled from Unity primitives.</summary>
public static class M1V073WorldContent
{
    static readonly string[] Names={"六目灵狐","七面幻狐","赤霄朱雀","青蛟","苍羽仙鹤","琥珀雷豹"};
    static readonly string[] Ids={"six_eye_fox","seven_face_fox","red_xiao","qing_jiao","azure_crane","amber_leopard"};
    static readonly Color[] Main={new Color(.80f,.48f,.19f),new Color(.56f,.37f,.73f),new Color(.83f,.22f,.13f),new Color(.15f,.62f,.68f),new Color(.77f,.85f,.86f),new Color(.88f,.57f,.18f)};
    static readonly Vector3[] Spots={new Vector3(-42,0,34),new Vector3(42,0,34),new Vector3(-48,0,54),new Vector3(48,0,54),new Vector3(-30,0,76),new Vector3(30,0,76)};
    public static void AddToScene(int kind,Transform player)
    {
        for(int i=0;i<Names.Length;i++)MakeBeast(i,Spots[i]+new Vector3(kind*3f,0,kind*2.5f));
        for(int i=0;i<4;i++)MakeResource(i,new Vector3((i%2==0?-1:1)*(36+i*5),0,96+(i%2)*11),player);
    }
    static void MakeBeast(int i,Vector3 at)
    {
        var root=new GameObject(Names[i]+" · 野生异兽");root.transform.position=at;
        var actor=root.AddComponent<M1Actor>();actor.isTameable=true;actor.displayName=Names[i];actor.speciesId=Ids[i];
        var box=root.AddComponent<CapsuleCollider>();box.height=i==3?3.6f:2.2f;box.radius=i==3?1.05f:.72f;box.center=new Vector3(0,box.height*.48f,0);
        var main=Mat(Main[i]);var light=Mat(Color.Lerp(Main[i],Color.white,.30f));var dark=Mat(Color.Lerp(Main[i],Color.black,.38f));var eye=Mat(new Color(.055f,.075f,.09f));var glow=Mat(new Color(.48f,.94f,1f),true);
        if(i==0)HexEyeFox(root.transform,main,light,dark,eye,glow);
        else if(i==1)SevenFaceFox(root.transform,main,light,dark,eye,glow);
        else if(i==2)Phoenix(root.transform,main,light,dark,eye,glow);
        else if(i==3)Jiaolong(root.transform,main,light,dark,eye,glow);
        else if(i==4)Crane(root.transform,main,light,dark,eye,glow);
        else Leopard(root.transform,main,light,dark,eye,glow);
        root.AddComponent<M1SpiritBeastMotion>();
    }
    static void HexEyeFox(Transform r,Material m,Material l,Material d,Material eye,Material glow)
    {
        Part(r,PrimitiveType.Capsule,"六目灵狐·身躯",new Vector3(0,.72f,-.05f),new Vector3(.76f,.72f,1.12f),m);
        Part(r,PrimitiveType.Sphere,"六目狐首",new Vector3(0,1.22f,.60f),new Vector3(.78f,.63f,.68f),l);
        for(int row=0;row<3;row++)for(int side=-1;side<=1;side+=2){float y=1.42f-row*.19f;Part(r,PrimitiveType.Sphere,"灵瞳",new Vector3(side*(.22f-row*.035f),y,.91f-row*.08f),new Vector3(.105f,.10f,.055f),eye);Part(r,PrimitiveType.Sphere,"瞳光",new Vector3(side*(.22f-row*.035f),y,.945f-row*.08f),new Vector3(.035f,.04f,.018f),glow);}
        for(int side=-1;side<=1;side+=2){Part(r,PrimitiveType.Cylinder,"狐耳",new Vector3(side*.27f,1.68f,.53f),new Vector3(.22f,.54f,.20f),d);for(int t=0;t<3;t++){var tail=Part(r,PrimitiveType.Capsule,"灵尾",new Vector3(side*(.24f+t*.12f),.92f,-.82f-t*.12f),new Vector3(.22f,.72f,.22f),t==2?l:m);tail.transform.localRotation=Quaternion.Euler(-38,0,side*(23+t*5));}}
    }
    static void SevenFaceFox(Transform r,Material m,Material l,Material d,Material eye,Material glow)
    {
        Part(r,PrimitiveType.Capsule,"七面狐躯",new Vector3(0,.68f,-.12f),new Vector3(.78f,.72f,1.12f),m);
        Part(r,PrimitiveType.Sphere,"主狐面",new Vector3(0,1.23f,.60f),new Vector3(.72f,.61f,.58f),l);
        Part(r,PrimitiveType.Sphere,"主面灵瞳",new Vector3(-.17f,1.30f,.87f),new Vector3(.085f,.09f,.045f),eye);Part(r,PrimitiveType.Sphere,"主面灵瞳",new Vector3(.17f,1.30f,.87f),new Vector3(.085f,.09f,.045f),eye);
        Vector3[] masks={new Vector3(-.42f,1.44f,.34f),new Vector3(0,1.59f,.34f),new Vector3(.42f,1.44f,.34f),new Vector3(-.48f,1.02f,.37f),new Vector3(0,1.03f,.45f),new Vector3(.48f,1.02f,.37f)};
        for(int i=0;i<masks.Length;i++){Part(r,PrimitiveType.Sphere,"幻面"+(i+2),masks[i],new Vector3(.30f,.27f,.24f),i%2==0?d:l);Part(r,PrimitiveType.Sphere,"幻面灵光",masks[i]+new Vector3(0,0,.13f),new Vector3(.045f,.045f,.025f),glow);}
        for(int side=-1;side<=1;side+=2){Part(r,PrimitiveType.Cylinder,"七面狐耳",new Vector3(side*.25f,1.67f,.48f),new Vector3(.20f,.42f,.18f),d);var tail=Part(r,PrimitiveType.Capsule,"幻狐灵尾",new Vector3(side*.50f,.86f,-.90f),new Vector3(.27f,.90f,.25f),m);tail.transform.localRotation=Quaternion.Euler(-40,0,side*30);}
    }
    static void Phoenix(Transform r,Material m,Material l,Material d,Material eye,Material glow)
    {
        Part(r,PrimitiveType.Sphere,"赤霄朱雀身",new Vector3(0,.88f,0),new Vector3(.78f,.88f,1.08f),m);Part(r,PrimitiveType.Capsule,"朱雀長頸",new Vector3(0,1.45f,.43f),new Vector3(.34f,.62f,.36f),l);Part(r,PrimitiveType.Sphere,"朱雀首",new Vector3(0,1.78f,.53f),new Vector3(.46f,.43f,.47f),m);
        Part(r,PrimitiveType.Cylinder,"朱雀喙",new Vector3(0,1.69f,.87f),new Vector3(.19f,.45f,.18f),d).transform.localRotation=Quaternion.Euler(90,0,0);
        for(int side=-1;side<=1;side+=2){var wing=Part(r,PrimitiveType.Capsule,"火羽翼",new Vector3(side*.66f,1.10f,-.04f),new Vector3(.42f,.92f,.20f),l);wing.transform.localRotation=Quaternion.Euler(0,0,-side*34);for(int feather=0;feather<3;feather++){var f=Part(r,PrimitiveType.Capsule,"尾焰羽",new Vector3(side*(.20f+feather*.14f),.58f,-.72f-feather*.12f),new Vector3(.15f,.86f,.12f),feather%2==0?d:l);f.transform.localRotation=Quaternion.Euler(-50,0,side*(15+feather*9));}Part(r,PrimitiveType.Sphere,"朱雀眼",new Vector3(side*.15f,1.83f,.76f),new Vector3(.08f,.09f,.04f),eye);Part(r,PrimitiveType.Cylinder,"頭冠火羽",new Vector3(side*.10f,2.08f,.48f),new Vector3(.13f,.38f,.12f),glow);}
    }
    static void Jiaolong(Transform r,Material m,Material l,Material d,Material eye,Material glow)
    {
        for(int k=0;k<6;k++){float z=.95f-k*.48f;float y=1.12f+Mathf.Sin(k*.58f)*.20f;var seg=Part(r,PrimitiveType.Capsule,"青蛟·龍身節",new Vector3(0,y,z),new Vector3(.54f,.34f,.58f),k%2==0?m:l);seg.transform.localRotation=Quaternion.Euler(90,0,0);}
        Part(r,PrimitiveType.Sphere,"青蛟龍首",new Vector3(0,1.48f,1.15f),new Vector3(.78f,.62f,.78f),l);Part(r,PrimitiveType.Cube,"龍吻",new Vector3(0,1.28f,1.61f),new Vector3(.43f,.20f,.50f),m);
        for(int side=-1;side<=1;side+=2){Part(r,PrimitiveType.Cylinder,"龍角",new Vector3(side*.30f,1.92f,1.02f),new Vector3(.16f,.55f,.16f),d).transform.localRotation=Quaternion.Euler(0,0,-side*24);Part(r,PrimitiveType.Sphere,"龍睛",new Vector3(side*.26f,1.54f,1.48f),new Vector3(.09f,.09f,.05f),glow);var whisker=Part(r,PrimitiveType.Cylinder,"龍鬚",new Vector3(side*.42f,1.23f,1.58f),new Vector3(.035f,.57f,.035f),d);whisker.transform.localRotation=Quaternion.Euler(0,0,side*74);}
        for(int k=0;k<5;k++)Part(r,PrimitiveType.Cylinder,"青蛟背棘",new Vector3(0,1.55f-k*.06f,.62f-k*.39f),new Vector3(.16f,.34f,.15f),glow);
        var tail=Part(r,PrimitiveType.Capsule,"青蛟長尾",new Vector3(0,.92f,-1.65f),new Vector3(.32f,1.35f,.34f),m);tail.transform.localRotation=Quaternion.Euler(-54,0,0);
    }
    static void Crane(Transform r,Material m,Material l,Material d,Material eye,Material glow)
    {
        Part(r,PrimitiveType.Capsule,"仙鶴身",new Vector3(0,.88f,0),new Vector3(.72f,.85f,1.0f),m);Part(r,PrimitiveType.Capsule,"仙鶴長頸",new Vector3(0,1.45f,.35f),new Vector3(.25f,.85f,.25f),l);Part(r,PrimitiveType.Sphere,"仙鶴首",new Vector3(0,1.92f,.40f),new Vector3(.40f,.37f,.42f),m);Part(r,PrimitiveType.Cylinder,"仙鶴長喙",new Vector3(0,1.85f,.73f),new Vector3(.07f,.30f,.07f),d).transform.localRotation=Quaternion.Euler(90,0,0);
        for(int side=-1;side<=1;side+=2){var wing=Part(r,PrimitiveType.Capsule,"蒼羽翼",new Vector3(side*.57f,1.13f,-.10f),new Vector3(.29f,.84f,.16f),l);wing.transform.localRotation=Quaternion.Euler(0,0,-side*28);var leg=Part(r,PrimitiveType.Cylinder,"鶴足",new Vector3(side*.18f,.28f,.02f),new Vector3(.05f,.49f,.05f),d);Part(r,PrimitiveType.Sphere,"鶴目",new Vector3(side*.13f,1.98f,.59f),new Vector3(.07f,.07f,.04f),eye);}
        Part(r,PrimitiveType.Cylinder,"仙鶴頭冠",new Vector3(0,2.22f,.35f),new Vector3(.15f,.40f,.14f),glow);
    }
    static void Leopard(Transform r,Material m,Material l,Material d,Material eye,Material glow)
    {
        var body=Part(r,PrimitiveType.Capsule,"琥珀雷豹身",new Vector3(0,.83f,0),new Vector3(.70f,.63f,1.25f),m);body.transform.localRotation=Quaternion.Euler(90,0,0);
        Part(r,PrimitiveType.Sphere,"雷豹頭",new Vector3(0,1.20f,.88f),new Vector3(.72f,.59f,.65f),l);Part(r,PrimitiveType.Sphere,"雷豹吻",new Vector3(0,1.02f,1.20f),new Vector3(.43f,.30f,.30f),d);
        for(int side=-1;side<=1;side+=2){Part(r,PrimitiveType.Cylinder,"豹耳",new Vector3(side*.23f,1.58f,.74f),new Vector3(.18f,.35f,.16f),d);Part(r,PrimitiveType.Sphere,"雷豹眼",new Vector3(side*.18f,1.28f,1.16f),new Vector3(.08f,.09f,.05f),glow);for(int spot=0;spot<4;spot++)Part(r,PrimitiveType.Sphere,"雷紋斑點",new Vector3(side*(.34f+spot%2*.06f),.88f+(spot/2)*.24f,.16f-spot*.24f),new Vector3(.09f,.08f,.08f),d);}
        for(int side=-1;side<=1;side+=2)for(int front=-1;front<=1;front+=2)Part(r,PrimitiveType.Capsule,"豹腿",new Vector3(side*.42f,.40f,front*.66f),new Vector3(.22f,.54f,.24f),m);
        var tail=Part(r,PrimitiveType.Capsule,"雷豹長尾",new Vector3(0,.80f,-1.15f),new Vector3(.16f,.76f,.16f),d);tail.transform.localRotation=Quaternion.Euler(-52,0,0);
    }
    static void MakeResource(int i,Vector3 at,Transform player)
    {
        bool ore=i%2==1;var root=new GameObject(ore?"扩展区·青玉矿": "扩展区·灵木");root.transform.position=at;var mat=Mat(ore?new Color(.25f,.73f,.82f):new Color(.27f,.47f,.29f));
        if(ore){Part(root.transform,PrimitiveType.Cube,"矿脉",new Vector3(0,.7f,0),new Vector3(1.2f,1.4f,1.0f),mat);Part(root.transform,PrimitiveType.Cube,"青玉晶尖",new Vector3(.20f,1.45f,.06f),new Vector3(.35f,.75f,.32f),Mat(new Color(.40f,.95f,1f),true));}
        else{Part(root.transform,PrimitiveType.Cylinder,"灵木树干",new Vector3(0,.95f,0),new Vector3(.26f,1.9f,.26f),Mat(new Color(.38f,.25f,.13f)));Part(root.transform,PrimitiveType.Sphere,"灵木树冠",new Vector3(0,2.1f,0),new Vector3(1.35f,1.2f,1.25f),mat);}
        var col=root.AddComponent<BoxCollider>();col.center=ore?new Vector3(0,.7f,0):new Vector3(0,1.1f,0);col.size=ore?new Vector3(1.5f,1.7f,1.4f):new Vector3(1.5f,2.4f,1.5f);
        var node=root.AddComponent<M1HarvestNode>();node.resource=ore?"矿石":"木材";node.amount=2;node.remaining=4;node.player=player;var label=new GameObject("资源用途提示");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,3f,0);var text=label.AddComponent<TextMesh>();text.text=ore?"F · 青玉矿（镐/灵契符）":"F · 灵木（斧/灵契符）";text.fontSize=24;text.characterSize=.055f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.96f,.86f,.58f);label.AddComponent<M1BillboardLabel>();
    }
    static GameObject Part(Transform parent,PrimitiveType type,string name,Vector3 pos,Vector3 scale,Material mat)
    {var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;var c=o.GetComponent<Collider>();if(c)Object.DestroyImmediate(c);var r=o.GetComponent<Renderer>();if(r)r.sharedMaterial=mat;return o;}
    static Material Mat(Color c,bool emission=false){var m=new Material(Shader.Find("Standard"));m.color=c;if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*2.1f);}return m;}
}
