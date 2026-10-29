using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok {
// 행사장 탈출 전용 운전. 목적지에 정차하고 내려야 이야기가 이어진다.
public sealed class VenueGetawayDrive : MonoBehaviour {
 public MeshFilter bodyShell;public Mesh exteriorShell,cockpitShell;
 public VenueRoadInterception interception;public VenueVehiclePursuit pursuit;public VenueFinale mission;public Transform[] route,wheels;public AudioClip impactClip;
 public float maximumSpeed=18,acceleration=8,steering=65;public Vector3 eye=new Vector3(-.38f,1.17f,.1f);
 public float Speed{get;private set;}public bool Driving{get;private set;}public bool ExternalInput;
 public float SteeringInput=>smoothTurn;float smoothTurn;
 public bool CanAbandon=>Driving&&Vector3.Distance(transform.position,mission.driveEnd.position)<6&&Mathf.Abs(Speed)<1;
 public string LastBlock{get;private set;}
 public int PedestrianHits{get;private set;}
 readonly EscapeRouteGuide guidance=new();
 public int RouteIndex{get;private set;}public int Impacts{get;private set;}
 float gas,turn,yaw,pitch,oldFov,oldNear,shake,nextImpact;bool paused;Vector3 lastSafe;Quaternion lastRotation;
 readonly System.Collections.Generic.List<GameObject> hiddenTraffic=new(),hiddenPolice=new();
 public int HiddenTrafficCount=>hiddenTraffic.Count;
 public float ShakeRemaining=>shake;
 public void SuppressTraffic(){foreach(var cop in FindObjectsByType<PoliceControl>(FindObjectsSortMode.None))if(cop.gameObject.activeSelf&&!hiddenPolice.Contains(cop.gameObject)){hiddenPolice.Add(cop.gameObject);cop.gameObject.SetActive(false);}foreach(var car in FindObjectsByType<CarMov>(FindObjectsSortMode.None))if(car.gameObject.activeSelf&&!hiddenTraffic.Contains(car.gameObject)){hiddenTraffic.Add(car.gameObject);car.gameObject.SetActive(false);}}
 public void RestoreTraffic(){foreach(var cop in hiddenPolice)if(cop)cop.SetActive(true);hiddenPolice.Clear();foreach(var car in hiddenTraffic)if(car)car.SetActive(true);hiddenTraffic.Clear();}
 // 짧은 밀림만 적용하고 벽이나 물 쪽으로 강제 이동시키지 않는다.
 public bool ReceiveSideImpact(Vector3 direction,float strength=1){if(!Driving||Time.timeScale<=0||Time.time<nextImpact)return false;nextImpact=Time.time+1.2f;shake=.35f;Impacts++;Speed*=.82f;
  var push=Vector3.ProjectOnPlane(direction,Vector3.up).normalized*.35f*Mathf.Clamp(strength,.5f,1);
  bool clear=!Physics.BoxCastAll(transform.position+Vector3.up*.85f,new Vector3(.8f,.55f,1.65f),push.normalized,transform.rotation,push.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(mission.Player.transform)&&h.normal.y<.6f);
  bool floor=Physics.RaycastAll(transform.position+push+Vector3.up*2,Vector3.down,3,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name.Contains("Road"));if(clear&&floor)transform.position+=push;
  if(impactClip)StorySoundMix.Play(mission.sound,impactClip,.65f);return true;
 }
 public void Input(float throttle,float direction){gas=Mathf.Clamp(throttle,-1,1);turn=Mathf.Clamp(direction,-1,1);}
 public void Begin(){guidance.Reset();if(SuppliedAudio.Current&&SuppliedAudio.Current.carCrash)impactClip=SuppliedAudio.Current.carCrash;if(bodyShell&&cockpitShell)bodyShell.sharedMesh=cockpitShell;Driving=true;Speed=0;RouteIndex=0;yaw=pitch=0;lastSafe=transform.position;lastRotation=transform.rotation;oldFov=mission.cinema.fieldOfView;oldNear=mission.cinema.nearClipPlane;mission.cinema.fieldOfView=80;mission.cinema.nearClipPlane=.03f;mission.passenger.gameObject.SetActive(false);if(mission.engine){if(SuppliedAudio.Current&&SuppliedAudio.Current.playerEngine)mission.engine.clip=SuppliedAudio.Current.playerEngine;mission.engine.Play();}// 기존 도보 추격자가 차량을 가로막는다. 별도 사격 웨이브는 생성하지 않는다.
}
 public void Stop(){if(bodyShell&&exteriorShell)bodyShell.sharedMesh=exteriorShell;if(Driving){mission.cinema.fieldOfView=oldFov;mission.cinema.nearClipPlane=oldNear;}Driving=false;Speed=gas=turn=smoothTurn=shake=0;if(pursuit)pursuit.Stop();if(interception)interception.Stop();if(mission.engine)mission.engine.Stop();paused=false;}
 void Update(){if(!Driving)return;if(Time.timeScale<=0){if(!paused&&mission.engine)mission.engine.Pause();paused=true;}else if(paused){if(mission.engine)mission.engine.UnPause();paused=false;}}
 public void Tick(){if(!Driving||Time.timeScale<=0)return;
  if(!ExternalInput)Input(PrologueKeyboard.Vertical,PrologueKeyboard.Horizontal);
  if(PrologueKeyboard.Pressed(Key.R)){transform.SetPositionAndRotation(lastSafe,lastRotation);Speed=0;}
  while(RouteIndex<route.Length&&Vector3.Distance(transform.position,route[RouteIndex].position)<7)RouteIndex++;
  var goal=guidance.Marker(transform.position,mission.driveEnd.position,true);
  bool chased=false;
  bool close=Vector3.Distance(transform.position,mission.driveEnd.position)<6;
  mission.objective.text=chased?("조직원 차량 추격 · "+(pursuit.LostSeconds>0?"추격을 따돌리는 중":"거리를 벌리고 시야를 끊으세요")):close?(CanAbandon?"추적을 끊기 · [F] 차를 버리고 가게로 돌아가기":"차를 버릴 장소 · 완전히 정차하세요"):"추적을 끊기 · 지정 장소까지 직접 운전";
  mission.marker.gameObject.SetActive(true);StoryTargetDiamond.Place(mission.marker,mission.cinema,goal+Vector3.up,transform.position,(goal-mission.driveEnd.position).sqrMagnitude<1||!guidance.HasPath?"수리점 인근 차량 폐기":"도주 경로");
  if(close&&PrologueKeyboard.Pressed(Key.F))mission.AbandonCar();
  if(mission.engine){float speed=Mathf.Clamp01(Mathf.Abs(Speed)/maximumSpeed);mission.engine.pitch=Mathf.Lerp(.75f,1.65f,speed);mission.engine.volume=Mathf.Lerp(.12f,.26f,speed)*SuppliedAudio.Volume;}
 }
 // 몸통 한 곳에만 충격을 주면 관절에 힘이 흡수되므로 전신에 같은 이동 속도를 전달한다.
 void LaunchPedestrian(NPCControl npc,Vector3 direction){
 npc.Die(direction,0);
 float speed=Mathf.Abs(Speed);var forward=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
 var launch=forward*Mathf.Clamp(6+speed*.9f,8,25)+Vector3.up*Mathf.Clamp(2+speed*.18f,2.5f,5.5f);
 foreach(var body in npc.GetComponentsInChildren<Rigidbody>()){
 if(body.gameObject==npc.gameObject||body.isKinematic)continue;
 body.linearVelocity=launch;body.angularVelocity=Vector3.Cross(Vector3.up,forward)*2.5f;
 }
 }
 void FixedUpdate(){if(!Driving||Time.timeScale<=0)return;float dt=Time.fixedDeltaTime;
  float desired=gas*(gas<0?6:maximumSpeed);Speed=Mathf.MoveTowards(Speed,desired,(Mathf.Abs(desired)<Mathf.Abs(Speed)?20:acceleration)*dt);
  if(!ExternalInput&&PrologueKeyboard.Held(Key.Space))Speed=Mathf.MoveTowards(Speed,0,30*dt);
  smoothTurn=Mathf.MoveTowards(smoothTurn,turn,(Mathf.Abs(turn)<.01f?5:3.5f)*dt);
  float speedSteering=Mathf.Lerp(1,.48f,Mathf.InverseLerp(7,maximumSpeed,Mathf.Abs(Speed)));
  var rot=transform.rotation*Quaternion.Euler(0,smoothTurn*steering*speedSteering*Mathf.Clamp(Speed/7,-1,1)*dt,0);var delta=rot*Vector3.forward*Speed*dt;
  bool blocked=false;LastBlock="";
  // 낮은 인도 턱은 차체를 들어 통과하되 높은 벽은 충돌을 유지한다.
  float step=.4f;

  if(delta.sqrMagnitude>.000001f)foreach(var hit in Physics.BoxCastAll(transform.position+Vector3.up*(.85f+step),new Vector3(.8f,.55f,1.65f),delta.normalized,rot,delta.magnitude+.05f,~0,QueryTriggerInteraction.Collide)){
   if(hit.transform.IsChildOf(transform)||hit.transform.IsChildOf(mission.Player.transform)||hit.normal.y>.6f)continue;var npc=hit.collider.GetComponentInParent<NPCControl>();if(npc){if(!npc.IsDead&&Mathf.Abs(Speed)>=2){LaunchPedestrian(npc,delta.normalized);PedestrianHits++;if(Time.time>nextImpact){nextImpact=Time.time+.4f;shake=.15f;Impacts++;if(impactClip)StorySoundMix.Play(mission.sound,impactClip,.4f);}}continue;}if(hit.collider.isTrigger)continue;blocked=true;LastBlock=hit.collider.name+" at "+hit.point;break;}
  if(blocked){if(Mathf.Abs(Speed)>2.5f&&Time.time>nextImpact){nextImpact=Time.time+.7f;shake=.25f;Impacts++;if(impactClip)StorySoundMix.Play(mission.sound,impactClip,.45f);}Speed=0;delta=Vector3.zero;}
  var next=transform.position+delta;
  // 가장자리와 물로 떨어지지 않도록 바퀴 아래 지면이 있는 곳만 이동한다.
  var ground=Physics.RaycastAll(next+Vector3.up*2,Vector3.down,4,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(mission.Player.transform)&&h.normal.y>.7f&&(h.collider.gameObject.isStatic||h.collider.name.Contains("Road")||h.collider.name.Contains("Sidewalk")||h.collider.name.Contains("Pavement"))).OrderBy(h=>h.distance).ToArray();
  if(ground.Length==0){LastBlock="No floor at "+next;Speed=0;return;}
  if(Mathf.Abs(ground[0].point.y-transform.position.y)>.55f){LastBlock="Floor height "+ground[0].point+" "+ground[0].collider.name;Speed=0;return;}
  next.y=Mathf.Lerp(transform.position.y,ground[0].point.y+.04f,dt*10);transform.SetPositionAndRotation(next,rot);
  if(!blocked&&Mathf.Abs(Speed)>2){lastSafe=next;lastRotation=rot;}
  if(wheels!=null)foreach(var wheel in wheels)if(wheel)wheel.Rotate(Vector3.right,Speed*dt*130,Space.Self);
 }
 void LateUpdate(){if(!Driving||Time.timeScale<=0)return;
  if(!ExternalInput&&Mouse.current!=null){var d=Mouse.current.delta.ReadValue();yaw=Mathf.Clamp(yaw+d.x*.08f,-85,85);pitch=Mathf.Clamp(pitch-d.y*.08f,-20,25);}
  var cam=mission.cinema.transform;cam.SetPositionAndRotation(transform.TransformPoint(eye),transform.rotation*Quaternion.Euler(pitch,PrologueKeyboard.Held(Key.C)?180:yaw,0));
  if(shake>0){shake-=Time.deltaTime;cam.position+=cam.up*(Mathf.Sin(Time.time*70)*.02f*shake/.35f)+cam.right*(Mathf.Sin(Time.time*53)*.025f*shake/.35f);cam.rotation*=Quaternion.Euler(Mathf.Sin(Time.time*48)*.5f*shake/.35f,0,Mathf.Sin(Time.time*65)*.8f*shake/.35f);}
 }
 void OnDisable(){Stop();}
}}
