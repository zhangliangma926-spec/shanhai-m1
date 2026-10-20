using UnityEngine;

/// <summary>Occasional quiet spatial calls for wild and collected creatures.</summary>
[RequireComponent(typeof(AudioSource))]
public class M1CreatureAudio : MonoBehaviour
{
    AudioSource source; M1Actor actor; float timer;
    void Start()
    {
        actor=GetComponent<M1Actor>();if(!actor||!actor.isTameable){enabled=false;return;}
        source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=1f;source.volume=.16f;source.minDistance=3f;source.maxDistance=20f;source.rolloffMode=AudioRolloffMode.Logarithmic;source.clip=M1AudioDirector.CreatureClip(actor.speciesId);
        timer=Random.Range(2f,8f);
    }
    void Update()
    {
        if(!actor||!source)return;timer-=Time.deltaTime;
        if(timer>0||source.isPlaying)return;
        var gm=M1GameManager.Instance;float d=gm&&gm.player?Vector3.Distance(transform.position,gm.player.transform.position):1000f;
        timer=Random.Range(14f,25f);if(d<23f)source.PlayOneShot(source.clip,actor.tamed?.22f:.14f);
    }
}
