using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok {
// 프롤로그 전용 직접 운전. 본편 교통 차량의 자동 경로 이동과 분리한다.
public sealed class PrologueDrive : MonoBehaviour {
    public BankPrologue director;
    public Transform finish;
    public Transform[] wheels;
    public float maxSpeed=23, acceleration=10, steering=65;
    public Vector3 driverEye=new Vector3(-.38f,1.17f,.1f);
    public float cockpitFov=80;float previousFov,previousNear;
    public float Speed {get;private set;}
    public bool Driving {get;private set;}
    readonly RaycastHit[] hits=new RaycastHit[64];
    Rigidbody body;PrologueDriveFeedback feedback;
    float throttle,turn,viewYaw,viewPitch;
    public float SteeringInput => turn;
    public bool ExternalInput {get;set;}
    public void SetInput(float gas,float steer){throttle=Mathf.Clamp(gas,-1,1);turn=Mathf.Clamp(steer,-1,1);}
    void Awake(){body=GetComponent<Rigidbody>();feedback=GetComponent<PrologueDriveFeedback>();}
    public void Begin(){Driving=true;Speed=0;viewYaw=viewPitch=0;body.isKinematic=true;previousFov=director.filmCamera.fieldOfView;previousNear=director.filmCamera.nearClipPlane;director.filmCamera.fieldOfView=cockpitFov;director.filmCamera.nearClipPlane=.03f;}
    public void Stop(){if(Driving){director.filmCamera.fieldOfView=previousFov;director.filmCamera.nearClipPlane=previousNear;}Driving=false;Speed=0;if(feedback)feedback.StopEffects();}
    void Update(){
        if(!Driving||director.Paused)return;
        if(!ExternalInput)SetInput(director.AcceptInput?PrologueKeyboard.Vertical:0,director.AcceptInput?PrologueKeyboard.Horizontal:0);
        if(!ExternalInput&&director.AcceptInput&&PrologueKeyboard.Pressed(Key.R))ResetToRoad();
        var support=director.GetComponent<PrologueEscapePerformance>(); director.objective.text=support&&support.PursuitEnded?"추격을 따돌렸습니다. 도시 입구로 이동하세요":"추격을 따돌리고 경찰 저지선을 돌파하세요";
        director.prompt.text="";
        director.ammo.text=Mathf.Abs(Speed*3.6f).ToString("F0")+" km/h";
        if(Vector3.Distance(transform.position,finish.position)<9)director.FinishDriving();
    }
    void FixedUpdate(){
        if(!Driving||director.Paused)return;
        float target=throttle>=0?throttle*maxSpeed:throttle*7;
        Speed=Mathf.MoveTowards(Speed,target,acceleration*Time.fixedDeltaTime);
        if(!ExternalInput&&director.AcceptInput&&PrologueKeyboard.Held(Key.Space))Speed=Mathf.MoveTowards(Speed,0,30*Time.fixedDeltaTime);
        var rotation=body.rotation*Quaternion.Euler(0,turn*steering*Mathf.Clamp(Speed/7,-1,1)*Time.fixedDeltaTime,0);
        Vector3 forward=rotation*Vector3.forward,delta=forward*Speed*Time.fixedDeltaTime;
        // 저지선 경찰은 밀어내고 건물·차량에는 막히도록 이동 전에 검사한다.
        int count=Physics.BoxCastNonAlloc(body.position+Vector3.up*.85f,new Vector3(.9f,.65f,1.9f),delta.normalized,hits,rotation,delta.magnitude+.05f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        bool blocked=false;
        for(int i=0;i<count;i++){
            var hit=hits[i];if(hit.transform.IsChildOf(transform))continue;
            var officer=hit.collider.GetComponentInParent<PrologueOfficer>();
            if(officer){if(Mathf.Abs(Speed)>2){officer.VehicleHit(forward*Mathf.Abs(Speed)*.65f+Vector3.up*4);if(feedback)feedback.Collision(Mathf.Abs(Speed)*.5f);}continue;}
            if(hit.normal.y<.6f)blocked=true;
        }
        if(blocked){if(feedback)feedback.Collision(Mathf.Abs(Speed));Speed=0;delta=Vector3.zero;}
        body.MoveRotation(rotation);body.MovePosition(body.position+delta);
        if(wheels!=null)foreach(var wheel in wheels)wheel.Rotate(Vector3.right,Speed*Time.fixedDeltaTime*130,Space.Self);
        if(transform.position.z < -78||transform.position.z > -52)ResetToRoad();
    }
    public void ResetToRoad(){
        body.position=new Vector3(Mathf.Clamp(transform.position.x,18,230),0,-65);
        body.rotation=Quaternion.Euler(0,90,0);Speed=0;
    }
    void LateUpdate(){
        if(!Driving||director.Paused)return;
        var camera=director.filmCamera;
        bool rear=!ExternalInput&&director.AcceptInput&&PrologueKeyboard.Held(Key.C);
        // 차량을 따라가는 운전석 시점. 뒤보기 역시 같은 좌석에서 고개만 돌린다.
        if(!ExternalInput&&director.AcceptInput&&Mouse.current!=null){var delta=Mouse.current.delta.ReadValue();viewYaw=Mathf.Clamp(viewYaw+delta.x*.08f,-85,85);viewPitch=Mathf.Clamp(viewPitch-delta.y*.08f,-20,25);}
        camera.transform.SetPositionAndRotation(transform.TransformPoint(driverEye),transform.rotation*Quaternion.Euler(viewPitch,rear?180:viewYaw,0));
    }
}
}
