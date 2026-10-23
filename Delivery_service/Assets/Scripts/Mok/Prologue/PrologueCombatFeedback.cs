using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 작은 고정 풀로 피격 먼지와 소리를 재사용한다. 명중 표시는 실제 피해가 적용될 때만 켠다.
public sealed class PrologueCombatFeedback : MonoBehaviour {
    public BankPrologue director;
    public Material particleMaterial;
    public AudioClip bodyHit, wallHit, fatalHit;
    public CanvasGroup hitMarker;
    public Image[] markerStrokes;
    public int ConfirmedHits {get;private set;}
    public int SurfaceHits {get;private set;}
    ParticleSystem[] particles;
    AudioSource[] sounds;
    int next;
    float markerTime;
    void Awake(){
        particles=new ParticleSystem[12];sounds=new AudioSource[12];
        for(int i=0;i<particles.Length;i++){
            var g=new GameObject("Pooled shot impact "+i);g.transform.SetParent(transform,false);
            var p=g.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.playOnAwake=false;main.loop=false;main.maxParticles=16;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.22f;main.gravityModifier=.5f;
            var emission=p.emission;emission.enabled=false;
            var shape=p.shape;shape.enabled=false;
            var color=p.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particleMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            particles[i]=p;sounds[i]=g.AddComponent<AudioSource>();
            sounds[i].playOnAwake=false;sounds[i].spatialBlend=.7f;sounds[i].minDistance=5;sounds[i].maxDistance=40;
            sounds[i].rolloffMode=AudioRolloffMode.Linear;sounds[i].volume=.8f;
        }
        hitMarker.alpha=0;hitMarker.blocksRaycasts=false;hitMarker.interactable=false;
    }
    public void Impact(Vector3 point,Vector3 normal,bool person,bool fatal=false){
        int i=next++%particles.Length;var p=particles[i];p.transform.position=point;
        p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        Color color=person?new Color(.45f,.22f,.16f):new Color(.8f,.69f,.48f);
        for(int n=0;n<9;n++){
            var velocity=normal.normalized*Random.Range(.5f,1.7f)+Random.insideUnitSphere*.9f;
            p.Emit(new ParticleSystem.EmitParams{position=point+normal*.015f,velocity=velocity,startSize=Random.Range(.025f,.065f),startLifetime=Random.Range(.12f,.3f),startColor=color},1);
        }
        sounds[i].pitch=Random.Range(.93f,1.07f);sounds[i].Stop();
        var clip=person?(fatal?fatalHit:bodyHit):wallHit;if(clip)sounds[i].PlayOneShot(clip);
        if(person){
            ConfirmedHits++;markerTime=.16f;hitMarker.alpha=1;
            foreach(var stroke in markerStrokes)stroke.color=fatal?new Color(1,.47f,.16f):new Color(1,.95f,.79f);
        }else SurfaceHits++;
    }
    void Update(){
        if(!director.CanPlay&&!director.Paused){markerTime=0;hitMarker.alpha=0;return;}
        markerTime=Mathf.Max(0,markerTime-Time.deltaTime);hitMarker.alpha=Mathf.Clamp01(markerTime/.07f);
    }
}
}
