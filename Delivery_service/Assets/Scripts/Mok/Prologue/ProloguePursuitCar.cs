using UnityEngine;
namespace DeliveryService.Mok {
// 프롤로그 도주로에서만 동작하는 추격차. 정지한 플레이어를 밀어붙이지 않는다.
public sealed class ProloguePursuitCar:MonoBehaviour {
    public PrologueDrive drive;
    public AudioSource siren;
    public Light[] flashes;
    public Light headlights;
    public Renderer[] lenses;
    public Transform[] wheels;
    public float followingDistance=13,laneOffset=-3,delay=.7f;
    public bool Pursuing {get;private set;}
    public float Speed {get;private set;}
    public float Travelled {get;private set;}
    public bool DisabledByPartner {get;private set;}
    public void DisableByPartner(){DisabledByPartner=true;Pursuing=false;if(siren)siren.Stop();foreach(var l in flashes)l.enabled=false;foreach(var r in lenses)r.enabled=false;}
    Rigidbody body;Vector3 start;Quaternion rotation;float began;bool wasDriving,paused;
    readonly RaycastHit[] hits=new RaycastHit[32];
    void Awake(){if(SuppliedAudio.Current&&SuppliedAudio.Current.policeSiren)siren.clip=SuppliedAudio.Current.policeSiren;body=GetComponent<Rigidbody>();start=transform.position;rotation=transform.rotation;Show(false);}
    void Show(bool value){foreach(var r in GetComponentsInChildren<Renderer>(true))r.enabled=value;GetComponent<Collider>().enabled=value;foreach(var l in flashes)l.enabled=value;if(headlights)headlights.enabled=value;}
    void Update(){
        if(siren)siren.volume=.22f*SuppliedAudio.Volume;
        if(!drive.Driving){if(wasDriving){siren.Stop();Show(false);}wasDriving=false;Pursuing=false;Speed=0;return;}
        if(!wasDriving){wasDriving=true;began=Time.time;body.position=start;body.rotation=rotation;Travelled=0;}
        if(drive.director.Paused){if(!paused){siren.Pause();paused=true;}return;}
        if(paused){if(Pursuing)siren.UnPause();paused=false;}
        if(DisabledByPartner)return;
        if(!Pursuing&&Time.time-began>=delay){Pursuing=true;Show(true);siren.Play();siren.time=Mathf.Repeat(delay,siren.clip.length);}
        if(!Pursuing)return;
        for(int i=0;i<flashes.Length;i++){bool lit=Mathf.Sin(Time.time*11+i*Mathf.PI)>0;flashes[i].enabled=lit;lenses[i].enabled=lit;}
    }
    bool Clear(Vector3 delta,float extra){
        if(delta.sqrMagnitude<.0001f)return true;
        var q=Quaternion.LookRotation(delta);
        int n=Physics.BoxCastNonAlloc(body.position+Vector3.up*.85f,new Vector3(.9f,.6f,1.9f),delta.normalized,hits,q,delta.magnitude+extra,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(n==hits.Length)return false;
        for(int i=0;i<n;i++)if(!hits[i].transform.IsChildOf(transform)&&!hits[i].collider.GetComponentInParent<PrologueOfficer>()&&hits[i].normal.y<.6f)return false;
        return true;
    }
    void FixedUpdate(){
        if(DisabledByPartner&&drive.Driving&&!drive.director.Paused){
            Speed=Mathf.MoveTowards(Speed,0,13*Time.fixedDeltaTime);
            body.MovePosition(body.position+transform.forward*Speed*Time.fixedDeltaTime);
            return;
        }
        if(!Pursuing||!drive.Driving||drive.director.Paused)return;
        var player=drive.transform.position;float gap=player.x-body.position.x;
        float target=gap<8?0:Mathf.Clamp(Mathf.Max(0,drive.Speed)+(gap-followingDistance)*1.5f,0,33);
        Speed=Mathf.MoveTowards(Speed,target,18*Time.fixedDeltaTime);
        var goal=new Vector3(Mathf.Max(body.position.x,player.x-followingDistance),0,Mathf.Clamp(player.z+laneOffset,-74,-56));
        Vector3 delta=goal-body.position;delta.y=0;
        if(delta.magnitude<.2f){Speed=0;return;}
        Vector3 heading=delta.normalized;
        if(!Clear(heading*3,0)){
            bool found=false;
            foreach(float z in new[]{-69f,-61f,-65f,-73f,-57f}){var direction=(new Vector3(body.position.x+5,0,z)-body.position).normalized;if(Clear(direction*4,0)){heading=direction;found=true;break;}}
            if(!found){Speed=0;return;}
        }
        var turn=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(heading),100*Time.fixedDeltaTime);
        var step=turn*Vector3.forward*Mathf.Min(Speed*Time.fixedDeltaTime,Mathf.Max(0,gap-7));
        if(!Clear(step,.15f)){Speed=0;return;}
        body.MoveRotation(turn);body.MovePosition(body.position+step);Travelled+=step.magnitude;
        foreach(var wheel in wheels)wheel.Rotate(Vector3.right,Speed*Time.fixedDeltaTime*130,Space.Self);
    }
    void OnDisable(){if(siren)siren.Stop();}
}
}
