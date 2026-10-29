using UnityEngine;
namespace DeliveryService.Mok {
// 보행 거리로 발걸음과 흔들림을 맞춘다. 이동 물리와 조준 방향은 변경하지 않는다.
public sealed class PlayerWalkFeedback : MonoBehaviour {
    [Range(0,1)] public float cameraAmount=.65f;
    [Range(0,1)] public float handAmount=.8f;
    [Range(0,1)] public float footstepVolume=.23f;
    public Vector3 CameraOffset {get;private set;}
    public Vector3 HandOffset {get;private set;}
    public Quaternion HandRotation {get;private set;}=Quaternion.identity;
    public int Footsteps {get;private set;}
    AudioSource sound;AudioClip step;Vector3 previous;float phase,travel,weight;bool tracking;
    void Awake(){previous=transform.position;step=Resources.Load<AudioClip>("MokWalk/BootStep");sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;}
    public void Tick(bool ready,bool grounded,bool crouching,float aim,float dt){
        Vector3 delta=transform.position-previous;previous=transform.position;
        if(!ready||dt<=0){ResetMotion();return;}
        if(!tracking){tracking=true;return;}
        float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
        // 순간 이동이나 컷씬 복귀를 발걸음으로 계산하지 않는다.
        if(delta.magnitude>Mathf.Max(.8f,dt*12)){ResetMotion();return;}
        float speed=distance/Mathf.Max(dt,.001f);bool moving=grounded&&speed>.12f;
        float stride=crouching?1.1f:Mathf.Lerp(1.65f,2.05f,Mathf.InverseLerp(4,6,speed));
        float blend=1-Mathf.Exp(-12*dt);weight=Mathf.Lerp(weight,moving?Mathf.Clamp01(speed/3):0,blend);
        if(moving){phase+=distance/stride*Mathf.PI;travel+=distance;if(travel>=stride){travel%=stride;Step(crouching,speed);}}
        else travel=0;
        float strength=weight*(crouching?.4f:1)*Mathf.Lerp(1,.12f,aim);
        CameraOffset=new Vector3(Mathf.Sin(phase)*.012f,-Mathf.Cos(phase*2)*.018f,0)*strength*cameraAmount;
        HandOffset=new Vector3(Mathf.Sin(phase)*.018f,Mathf.Cos(phase*2)*.013f,Mathf.Sin(phase*2)*.006f)*strength*handAmount;
        HandRotation=Quaternion.Euler(Mathf.Cos(phase*2)*.6f*strength*handAmount,0,Mathf.Sin(phase)*1.1f*strength*handAmount);
    }
    int lastStep=-1;
    void Step(bool crouching,float speed){
        var bank=SuppliedAudio.Current;if(bank){bool street=false;foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up*.5f,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){if(hit.transform.IsChildOf(transform))continue;string n=hit.collider.name.ToLowerInvariant();street|=n.Contains("road")||n.Contains("street")||n.Contains("sidewalk")||n.Contains("pavement");}var clips=street?bank.streetSteps:bank.indoorSteps;if(clips!=null&&clips.Length>0){int index=Random.Range(0,clips.Length);if(clips.Length>1&&index==lastStep)index=(index+1)%clips.Length;lastStep=index;step=clips[index];}}
        Footsteps++;if(!step)return;
        sound.pitch=Random.Range(.94f,1.06f)*(crouching?.92f:speed>4.6f?1.08f:1);
        sound.PlayOneShot(step,footstepVolume*(crouching?.35f:1)*Mathf.Clamp01(PlayerPrefs.GetFloat("Yun.Settings.SfxVolume",1)));
    }
    void ResetMotion(){tracking=false;weight=travel=phase=0;CameraOffset=HandOffset=Vector3.zero;HandRotation=Quaternion.identity;if(sound&&sound.isPlaying)sound.Stop();}
    void OnDisable(){ResetMotion();}
}
}
