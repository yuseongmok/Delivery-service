using UnityEngine;
namespace DeliveryService.Mok {
// 프롤로그 전용 직접 운전. 본편 교통 차량의 자동 경로 이동과 분리한다.
public sealed class PrologueDrive : MonoBehaviour {
    public BankPrologue director;
    public Transform finish;
    public Transform[] wheels;
    public float maxSpeed=23, acceleration=10, steering=65;
    public float Speed {get;private set;}
    public bool Driving {get;private set;}
    readonly RaycastHit[] hits=new RaycastHit[64];
    Rigidbody body;
    float throttle,turn;
    public bool ExternalInput {get;set;}
    public void SetInput(float gas,float steer){throttle=Mathf.Clamp(gas,-1,1);turn=Mathf.Clamp(steer,-1,1);}
    void Awake(){body=GetComponent<Rigidbody>();}
    public void Begin(){Driving=true;Speed=0;body.isKinematic=true;}
    public void Stop(){Driving=false;Speed=0;}
    void Update(){
        if(!Driving||director.Paused)return;
        if(!ExternalInput)SetInput(Input.GetAxisRaw("Vertical"),Input.GetAxisRaw("Horizontal"));
        if(Input.GetKeyDown(KeyCode.R))ResetToRoad();
        director.objective.text="경찰 저지선을 돌파하세요";
        director.prompt.text="W/S  가속·후진    A/D  조향    Space  브레이크    R  도로 복귀";
        director.ammo.text=Mathf.Abs(Speed*3.6f).ToString("F0")+" km/h";
        if(Vector3.Distance(transform.position,finish.position)<9)director.FinishDriving();
    }
    void FixedUpdate(){
        if(!Driving||director.Paused)return;
        float target=throttle>=0?throttle*maxSpeed:throttle*7;
        Speed=Mathf.MoveTowards(Speed,target,acceleration*Time.fixedDeltaTime);
        if(!ExternalInput&&Input.GetKey(KeyCode.Space))Speed=Mathf.MoveTowards(Speed,0,30*Time.fixedDeltaTime);
        var rotation=body.rotation*Quaternion.Euler(0,turn*steering*Mathf.Clamp(Speed/7,-1,1)*Time.fixedDeltaTime,0);
        Vector3 forward=rotation*Vector3.forward,delta=forward*Speed*Time.fixedDeltaTime;
        // 저지선 경찰은 밀어내고 건물·차량에는 막히도록 이동 전에 검사한다.
        int count=Physics.BoxCastNonAlloc(body.position+Vector3.up*.85f,new Vector3(.9f,.65f,1.9f),delta.normalized,hits,rotation,delta.magnitude+.05f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        bool blocked=false;
        for(int i=0;i<count;i++){
            var hit=hits[i];if(hit.transform.IsChildOf(transform))continue;
            var officer=hit.collider.GetComponentInParent<PrologueOfficer>();
            if(officer){if(Mathf.Abs(Speed)>2)officer.VehicleHit(forward*Mathf.Abs(Speed)*.65f+Vector3.up*4);continue;}
            if(hit.normal.y<.6f)blocked=true;
        }
        if(blocked){Speed=0;delta=Vector3.zero;}
        body.MoveRotation(rotation);body.MovePosition(body.position+delta);
        if(wheels!=null)foreach(var wheel in wheels)wheel.Rotate(Vector3.right,Speed*Time.fixedDeltaTime*130,Space.Self);
        if(transform.position.z < -78||transform.position.z > -52)ResetToRoad();
    }
    public void ResetToRoad(){
        body.position=new Vector3(Mathf.Clamp(transform.position.x,18,230),0,-65);
        body.rotation=Quaternion.Euler(0,90,0);Speed=0;
    }
    void LateUpdate(){
        if(!Driving)return;
        var camera=director.filmCamera;
        Vector3 target=transform.position+Vector3.up*1.3f;
        Vector3 desired=target-transform.forward*8+Vector3.up*3;
        if(Physics.Linecast(target,desired,out var hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(transform))
            desired=hit.point+hit.normal*.4f;
        camera.transform.position=Vector3.Lerp(camera.transform.position,desired,1-Mathf.Exp(-6*Time.deltaTime));
        camera.transform.LookAt(target+transform.forward*4);
    }
}
}
