using UnityEngine;

public class M1PetNameplates : MonoBehaviour
{
    const float MaxDistance=60f,FadeStart=35f;
    GUIStyle nameStyle, hintStyle, hpStyle;
    readonly System.Collections.Generic.Dictionary<M1Actor,Collider> colliders=new System.Collections.Generic.Dictionary<M1Actor,Collider>();
    void OnGUI()
    {
        if(Event.current.type!=EventType.Repaint)return; // 纯显示，只在绘制事件里画
        var cam=Camera.main;if(!cam)return;
        M1UISkin.Apply();
        if(nameStyle==null)
        {
            nameStyle=new GUIStyle(M1UISkin.Body){alignment=TextAnchor.MiddleCenter,fontSize=14,fontStyle=FontStyle.Bold,wordWrap=false};
            hintStyle=new GUIStyle(M1UISkin.Small){alignment=TextAnchor.MiddleCenter,wordWrap=false};
        }
        foreach(var a in M1Actor.All)
        {
            if(!a||(!a.isTameable&&!a.tamed&&!a.isEnemy)||!a.gameObject.activeInHierarchy)continue; // v0.8.1：营地守卫也显示名牌和血条
            float dist=Vector3.Distance(cam.transform.position,a.transform.position);if(dist>MaxDistance)continue;
            float top=a.transform.position.y+2.05f;if(!colliders.TryGetValue(a,out var col)){col=a.GetComponent<Collider>();colliders[a]=col;}if(col)top=Mathf.Max(top,col.bounds.max.y+.28f);
            Vector3 p=cam.WorldToScreenPoint(new Vector3(a.transform.position.x,top,a.transform.position.z));if(p.z<=0)continue;
            float alpha=1f-Mathf.Clamp01((dist-FadeStart)/(MaxDistance-FadeStart));
            float ratio=Mathf.Clamp01(a.health/Mathf.Max(1,a.maxHealth));bool tameable=a.isTameable&&!a.tamed&&ratio<=.10f;
            const float w=176f;float h=a.tamed?26f:(tameable?46f:34f);float x=p.x-w*.5f,y=Screen.height-p.y-h; // v0.8.2：加宽，名字和生命数值分开放
            var old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            GUI.Box(new Rect(x,y,w,h),GUIContent.none);
            nameStyle.normal.textColor=a.isEnemy?M1UISkin.Danger:a.tamed||tameable?M1UISkin.Jade:M1UISkin.Gold;
            GUI.Label(new Rect(x+8,y+3,w-(a.tamed?16:62),20),a.displayName,nameStyle);
            if(!a.tamed)M1UISkin.Bar(new Rect(x+12,y+24,w-24,5),ratio,a.isEnemy?M1UISkin.Danger:tameable?M1UISkin.Jade:Color.Lerp(M1UISkin.Danger,M1UISkin.Gold,ratio));
            if(!a.tamed)GUI.Label(new Rect(x+w-52,y+4,44,18),Mathf.CeilToInt(a.health)+"",hpStyle??(hpStyle=new GUIStyle(M1UISkin.Small){alignment=TextAnchor.MiddleRight})); // 生命数值靠右，不压住名字
            if(tameable)GUI.Label(new Rect(x,y+29,w,16),"可结契 · E",hintStyle);
            GUI.color=old;
        }
    }
}
