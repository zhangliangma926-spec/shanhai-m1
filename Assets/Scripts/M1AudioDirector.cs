using System.Collections.Generic;
using UnityEngine;

/// <summary>All trial audio is synthesized at runtime; no external or paid audio is required.</summary>
[RequireComponent(typeof(AudioSource))]
public class M1AudioDirector : MonoBehaviour
{
    static M1AudioDirector instance;
    static readonly Dictionary<string,AudioClip> creatureClips=new Dictionary<string,AudioClip>();
    AudioSource music;
    AudioClip chargeClip, releaseClip, hitClip;
    const int Rate=22050;
    void Awake()
    {
        instance=this;music=GetComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.volume=.13f;
        music.clip=MakeAmbient();music.Play();chargeClip=MakeTone("灵杖蓄力",.24f,510,770);releaseClip=MakeTone("灵杖释放",.32f,350,1040);hitClip=MakeTone("灵弹命中",.18f,820,280);
    }
    void OnDestroy(){if(instance==this)instance=null;}
    public static void PlayUI(string sound)
    {
        if(!instance)return;var clip=sound=="charge"?instance.chargeClip:sound=="release"?instance.releaseClip:instance.hitClip;
        if(clip)instance.music.PlayOneShot(clip,sound=="charge"?.45f:.65f);
    }
    public static void PlayCreatureAction(string species,Vector3 at,float volume=.28f)
    {
        var clip=CreatureClip(species);if(clip)AudioSource.PlayClipAtPoint(clip,at,volume);
    }
    public static AudioClip CreatureClip(string species)
    {
        if(creatureClips.TryGetValue(species,out var c)&&c)return c;
        float baseTone=M1SpeciesDatabase.AudioBaseTone(species);
        c=MakeTone("异兽灵息_"+species,.48f,baseTone,baseTone*.72f);creatureClips[species]=c;return c;
    }
    static AudioClip MakeAmbient()
    {
        const int seconds=20;int n=Rate*seconds;var data=new float[n];float[] notes={130.81f,164.81f,196f,261.63f};
        for(int i=0;i<n;i++)
        {
            float t=(float)i/Rate;float fade=Mathf.Min(1f,t*4f,(seconds-t)*4f);
            float pad=0;for(int k=0;k<notes.Length;k++){float wobble=1f+.0025f*Mathf.Sin(t*.31f+k);pad+=Mathf.Sin(2*Mathf.PI*notes[k]*wobble*t+k*.7f)*(.014f/(1f+k*.22f));}
            float breeze=(Mathf.Sin(t*1.7f)+Mathf.Sin(t*.73f+1.2f))*.0032f;
            float birds=(Mathf.Sin(2*Mathf.PI*1480*t)+.45f*Mathf.Sin(2*Mathf.PI*1910*t))*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*.19f)),12)*.006f;
            data[i]=(pad+breeze+birds)*fade;
        }
        var clip=AudioClip.Create("森林灵韵_原创程序氛围",n,1,Rate,false);clip.SetData(data,0);return clip;
    }
    static AudioClip MakeTone(string name,float seconds,float start,float end)
    {
        int n=Mathf.CeilToInt(Rate*seconds);var data=new float[n];
        for(int i=0;i<n;i++){float t=(float)i/Rate;float x=t/seconds;float env=Mathf.Sin(Mathf.PI*x)*Mathf.Sin(Mathf.PI*x);float f=Mathf.Lerp(start,end,x);float tone=Mathf.Sin(2*Mathf.PI*f*t)+.22f*Mathf.Sin(2*Mathf.PI*f*2.01f*t);data[i]=tone*env*.16f;}
        var clip=AudioClip.Create(name,n,1,Rate,false);clip.SetData(data,0);return clip;
    }
}
