using UnityEngine;
namespace DeliveryService.Mok {
// 신도시 전용 경로를 달리는 일부 차량만 과속한다. 교차로에서는 감속해 회전한다.
public sealed class EastRecklessDriver : MonoBehaviour {
    public float cruiseSpeed=22f, turnSpeed=12f;
    public float hornDistance=30f;
    AudioSource horn; AudioClip clip; float nextHorn;
    public float Speed(Vector3 target) {
        Vector3 delta=target-transform.position;delta.y=0;
        return Vector3.Angle(transform.forward,delta)>25f?turnSpeed:cruiseSpeed;
    }
    void Awake(){
        horn=gameObject.AddComponent<AudioSource>();horn.playOnAwake=false;horn.spatialBlend=1;
        horn.rolloffMode=AudioRolloffMode.Linear;horn.minDistance=4;horn.maxDistance=55;horn.volume=.35f;
        int count=11025;float[] samples=new float[count];
        for(int i=0;i<count;i++){float t=i/22050f;float envelope=Mathf.Min(1,t*30)*Mathf.Clamp01((.5f-t)*15);samples[i]=(Mathf.Sin(t*2*Mathf.PI*440)+.5f*Mathf.Sin(t*2*Mathf.PI*554))*.3f*envelope;}
        clip=AudioClip.Create("Reckless horn",count,1,22050,false);clip.SetData(samples,0);horn.clip=clip;
    }
    void Update(){
        if(Time.time<nextHorn||Camera.main==null)return;
        Vector3 delta=Camera.main.transform.position-transform.position;delta.y=0;
        if(delta.magnitude<hornDistance&&Vector3.Dot(transform.forward,delta.normalized)>.55f){horn.Play();nextHorn=Time.time+4;}
    }
    void OnDestroy(){if(clip!=null)Destroy(clip);}
}
}
