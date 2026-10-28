using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 준비 성과를 소비하지 않는다. 실패해도 승인·동선·차량 지원을 유지해 재시도한다.
public sealed class VenueFinale : MonoBehaviour {
 public enum Phase { Available, Scheduling, Opening, Entrance, Admission, Deposit, Observe, Conversation, Ejected, Exposed, InteriorEscape, StreetEscape, Boarding, Driving, Abandoning, Debrief, Complete, Failed }
 public static VenueFinale Current{get;private set;} public static bool Active=>Current&&Current.Running;
 public Phase State{get;private set;} public bool Running=>State>=Phase.Scheduling&&State<=Phase.Debrief;
 public bool Completed=>State==Phase.Complete;public bool Cinematic=>State==Phase.Ejected||State==Phase.Opening||State==Phase.Admission||State==Phase.Exposed||State==Phase.Boarding||State==Phase.Driving||State==Phase.Abandoning||State==Phase.Debrief;
 public VenueGetawayDrive getaway;public VenueStealth stealth;public VenueStreetPursuit streetPursuit;
 public MechanicMission preparation;public GameObject interior,ui,pizza,approval,actors;public Transform entryGuard;public Transform entry,inside,deliveryTable,observeStand,target,contact,interiorExit,escapeTurn,carStand,car,door,seat,driver,passenger,driveEnd,safePoint;
 public Transform[] interiorPosts,streetPosts;
 public Transform streetArrival;public Transform[] serviceRoute,alleyRoute;int serviceStep,alleyStep;readonly HashSet<CitizenCombat> subdued=new();public int SubduedCount=>subdued.Count;public bool ExitUnlocked=>SubduedCount>=2;
 public Transform EscapeGoal=>State==Phase.InteriorEscape&&serviceRoute!=null&&serviceStep<serviceRoute.Length?serviceRoute[serviceStep]:State==Phase.StreetEscape&&alleyRoute!=null&&alleyStep<alleyRoute.Length?alleyRoute[alleyStep]:State==Phase.InteriorEscape?interiorExit:turned?carStand:escapeTurn;public Text objective,subtitle,marker;public Image fade;public Camera cinema;
 public AudioSource sound,engine;public AudioClip stamp,alarm,doorOpen,doorClose;
 public float observeSeconds=3,openingSeconds=5,revealSeconds=3.5f,boardingSeconds=3.2f,driveSeconds=6;
 [TextArea(2,6)]public string[] opening={"민기: 오늘은 문 닫아. 납품 승인, 출입구, 탈출 차량까지 준비됐어.","민기: 박진웅한테 따지러 가는 게 아니야. 왜 우릴 쫓는지, 안 들키고 들어 봐.","정비사: 약속한 곳에 차와 열쇠를 뒀어. 난 먼저 빠질게."};
 [TextArea(2,6)]public string[] overheard={"박진웅: 새 사업 자금은 그 은행 돈으로 채워. 민기한테 맡겼던 일, 아직도 마무리가 안 됐나?","조직원: 실장을 놓쳤습니다. 민기와 같이 일한 놈도 살아 있습니다. 약속한 보수를 기다리는 모양입니다.","박진웅: 보수? 금고 열라고 쓴 놈들이야. 돈 회수하면 실장까지 전부 없애. 우리 이름을 아는 놈을 남길 순 없지.","나: 처음부터 돈만 챙기고 우릴 없앨 생각이었어.\n민기: …들었어. 더 듣지 마. 거기서 나와."};
 [TextArea(2,6)]public string[] closing={"나: 여기까지 왔어. 차는 두고 갈게.","나: 실장도 우리도, 처음부터 버릴 패였어. 박진웅이 직접 말했어.","민기: 내가 믿고 널 끌어들였어. 미안하다. …여기선 잠깐 쉬어. 앞으로 어떻게 할지 같이 생각하자."};
 public float ObservationProgress{get;private set;}public bool IntelSecured{get;private set;}public string FailureReason{get;private set;}public int EventDay{get;private set;}public CitizenCombat[] Enemies=>enemies.Where(e=>e).ToArray();
 readonly List<CitizenCombat> enemies=new();bool locked,oldMove,oldControl,oldLook,insideVenue,reinforced,turned,announced;int line;float entered,noticeUntil;Vector3 carOrigin;Quaternion carRotation;Camera gameplay;PlayerMove move;CameraMove look;Renderer[] hiddenRenderers;bool[] hiddenStates;PhoneController lockedPhone;bool phoneWasEnabled,phoneWasVisible;GameObject hiddenPhone;
 public PlayerCombat Player=>preparation.Player;public DayNightCycle Day=>preparation.preparation.delivery.broker.day;
 public bool Ready=>preparation&&preparation.Completed&&preparation.preparation.Completed&&preparation.preparation.delivery.Completed&&preparation.preparation.delivery.broker.Completed;
 public bool CanStart=>Ready&&!BrokerMission.Active&&!Completed&&!Day.IsTransitioning&&Day.IsNightPreparation&&!StoryTutorial.Active&&!Player.IsDown&&!Player.RiddenBike&&!Player.WaterRescueActive&&!PoliceControl.IsArrestInProgress&&!(preparation.preparation.delivery.broker.orders&&preparation.preparation.delivery.broker.orders.HasActiveOrder);
 void Awake(){Current=this;ui.SetActive(false);cinema.enabled=false;actors.SetActive(false);pizza.SetActive(false);approval.SetActive(false);car.gameObject.SetActive(false);fade.gameObject.SetActive(false);carOrigin=car.position;carRotation=car.rotation;if(stealth)stealth.ResetAttempt();}
 void OnDestroy(){if(getaway)getaway.RestoreTraffic();Release();Clean();if(Current==this)Current=null;}
 void OnDisable(){if(getaway)getaway.RestoreTraffic();if(locked)Release();}
 public bool Begin(){if(!CanStart)return false;FailureReason="";Clean();HiddenSpawnCount=0;getaway.SuppressTraffic();IntelSecured=false;ObservationProgress=0;reinforced=turned=false;serviceStep=alleyStep=0;subdued.Clear();if(stealth)stealth.ResetAttempt();
 FindFirstObjectByType<PhoneController>()?.ClosePhone();Day.pcUIPanel.SetActive(false);preparation.ui.SetActive(false);preparation.preparation.ui.SetActive(false);preparation.preparation.delivery.ui.SetActive(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
 foreach(var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))if(pc.pcUIPanel==Day.pcUIPanel&&pc.cameraLookScript)pc.cameraLookScript.enabled=true;
 ui.SetActive(true);actors.SetActive(true);car.gameObject.SetActive(true);car.SetPositionAndRotation(carOrigin,carRotation);door.localRotation=Quaternion.identity;passenger.gameObject.SetActive(false);preparation.supportVehicle.gameObject.SetActive(false);driver.gameObject.SetActive(false);pizza.SetActive(true);approval.SetActive(false);
 if(EventDay==0){int previous=Day.currentDay;Day.OnClick_ConfirmClose();Set(Phase.Scheduling);StartCoroutine(Schedule(previous));}else StartOpening();return true;}
 IEnumerator Schedule(int previous){float until=Time.time+120;while(Day.currentDay==previous&&Time.time<until)yield return null;if(!Running)yield break;if(Day.currentDay==previous){Fail("다음 날 전환을 완료하지 못했습니다.");yield break;}while(Day.fadeScreen&&Day.fadeScreen.gameObject.activeSelf)yield return null;Day.EnterNightPreparation();EventDay=Day.currentDay;StartOpening();}
 void StartOpening(){Day.EnterNightPreparation();Set(Phase.Opening);Lock();subtitle.text=string.Join("\n",opening.Where(t=>!t.StartsWith("정비사:")));Shot(entry.position+Vector3.up*3+Vector3.right*6,entry.position+Vector3.up);}
 void Set(Phase value){State=value;entered=Time.time;line=0;}
 void Notice(string message,float seconds=7){subtitle.text=message;noticeUntil=Time.time+seconds;}
 void Lock(){if(locked)return;locked=true;lockedPhone=FindFirstObjectByType<PhoneController>();if(lockedPhone){lockedPhone.ClosePhone();if(lockedPhone.VisualRoot){hiddenPhone=lockedPhone.VisualRoot.gameObject;phoneWasVisible=hiddenPhone.activeSelf;hiddenPhone.SetActive(false);}phoneWasEnabled=lockedPhone.enabled;lockedPhone.enabled=false;}move=Player.GetComponent<PlayerMove>();gameplay=Player.View;look=gameplay.GetComponent<CameraMove>();oldMove=move.enabled;oldControl=move.isControllable;oldLook=look&&look.enabled;move.enabled=false;move.isControllable=false;if(look)look.enabled=false;gameplay.enabled=false;cinema.enabled=true;}
 void Release(){if(getaway)getaway.Stop();if(hiddenRenderers!=null){for(int i=0;i<hiddenRenderers.Length;i++)if(hiddenRenderers[i])hiddenRenderers[i].forceRenderingOff=hiddenStates[i];hiddenRenderers=null;}if(!locked)return;if(State==Phase.Boarding)cinema.nearClipPlane=boardingNear;if(State==Phase.Abandoning)cinema.nearClipPlane=exitNear;locked=false;if(hiddenPhone){hiddenPhone.SetActive(phoneWasVisible);hiddenPhone=null;}if(lockedPhone)lockedPhone.enabled=phoneWasEnabled;if(cinema)cinema.enabled=false;if(gameplay)gameplay.enabled=true;if(move){move.enabled=oldMove;move.isControllable=oldControl;}if(look){look.enabled=oldLook;look.SyncViewFromTransform();}if(fade)fade.gameObject.SetActive(false);}
 void Shot(Vector3 position,Vector3 at){cinema.transform.position=position;cinema.transform.LookAt(at);}
 public void PlacePlayer(Transform anchor){var cc=Player.GetComponent<CharacterController>();bool enabled=cc.enabled;cc.enabled=false;Player.transform.SetPositionAndRotation(anchor.position+Vector3.up*(cc.height*.5f-cc.center.y+.08f),anchor.rotation);Player.View.transform.localRotation=Quaternion.identity;cc.enabled=enabled;Player.View.GetComponent<CameraMove>()?.SyncViewFromTransform();Physics.SyncTransforms();}
 bool Near(Transform t,float range=3.2f){if(!Player.CanAct||Vector3.Distance(Player.transform.position,t.position)>range)return false;var from=Player.View.transform.position;var d=t.position+Vector3.up*.7f-from;return !Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(Player.transform)&&!h.transform.IsChildOf(t)&&!h.transform.IsChildOf(pizza.transform)&&!h.transform.IsChildOf(car));}
 public bool Interact(){if(!Running||Time.timeScale<=0)return false;
 if(State==Phase.Entrance&&Near(entry)){Set(Phase.Admission);Lock();approval.SetActive(true);approval.transform.position=entryGuard.TransformPoint(new Vector3(0,1.25f,.3f));approval.transform.rotation=entryGuard.rotation;Shot(entryGuard.position+entryGuard.forward*2+Vector3.up*1.7f,entryGuard.position+Vector3.up*1.2f);Notice("경비: 납품 명단 확인했습니다. 피자는 서비스 테이블에 놓고 바로 나오세요.");if(stamp)sound.PlayOneShot(stamp,.4f);return true;}
 if(State==Phase.Deposit&&Near(deliveryTable)){pizza.SetActive(true);pizza.transform.SetPositionAndRotation(deliveryTable.position,deliveryTable.rotation);Set(Phase.Observe);StoryPhoneInbox.Current?.SendVehicleReady();Notice("민기: 안쪽이 VIP 행사장이야. 순찰이 지나가면 움직여. 기둥 옆이나 서비스 통로에서 대화를 들어봐.");return true;}
 if(State==Phase.Conversation){AdvanceConversation();return true;}
 if(State==Phase.InteriorEscape&&Near(interiorExit)){if(!ExitUnlocked){Notice("출구 봉쇄 · 경비를 2명 제압해 탈출로를 확보하세요. ("+SubduedCount+"/2)");return false;}Clean();insideVenue=false;PlacePlayer(streetArrival?streetArrival:entry);Set(Phase.StreetEscape);if(streetPursuit)streetPursuit.Begin();else Spawn(streetPosts,2);return true;}
 if(State==Phase.StreetEscape&&turned&&Near(carStand)){pendingSpawns.Clear();if(streetPursuit)streetPursuit.BoardCar();boardingViewStart=Player.View.transform.position;boardingViewRotation=Player.View.transform.rotation;boardingNear=cinema.nearClipPlane;Set(Phase.Boarding);Lock();cinema.nearClipPlane=.03f;TrafficRecovery.Get(Player.gameObject).Protect(boardingSeconds+driveSeconds+10);hiddenRenderers=Player.GetComponentsInChildren<Renderer>(true);hiddenStates=hiddenRenderers.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hiddenRenderers)r.forceRenderingOff=true;passenger.gameObject.SetActive(false);passenger.position=carStand.position;passenger.rotation=car.rotation;cinema.transform.SetPositionAndRotation(boardingViewStart,boardingViewRotation);if(doorOpen)sound.PlayOneShot(doorOpen,.55f);return true;}return false;}
 public void AbandonCar(){if(State!=Phase.Driving||!getaway.CanAbandon)return;exitViewStart=cinema.transform.position;exitViewRotation=cinema.transform.rotation;getaway.Stop();exitNear=cinema.nearClipPlane;cinema.nearClipPlane=.03f;door.localRotation=Quaternion.identity;passenger.gameObject.SetActive(false);Set(Phase.Abandoning);if(doorOpen)sound.PlayOneShot(doorOpen,.55f);Notice("나: 여기 두고 가자. 이 차의 흔적을 가게까지 끌고 갈 순 없어.");}
 public bool CanObserve(){if(State!=Phase.Observe||!Player.CanAct||(stealth&&!stealth.CanListen))return false;var delta=target.position+Vector3.up*1.4f-Player.View.transform.position;return (stealth?stealth.AtListeningPoint:Vector3.Distance(Player.transform.position,observeStand.position)<3.5f)&&Vector3.Dot(Player.View.transform.forward,delta.normalized)>.9f&&!Physics.RaycastAll(Player.View.transform.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(target)&&!h.transform.IsChildOf(Player.transform));}
 public void Observe(bool held,float seconds){if(Time.timeScale<=0)return;if(!held||!CanObserve())return;ObservationProgress+=seconds/Mathf.Max(.5f,observeSeconds);if(ObservationProgress>=1){ObservationProgress=1;Set(Phase.Conversation);ShowConversation();}}
 void ShowConversation(){subtitle.text=overheard[line]+"\n[F] 계속 · [Space] 대화 건너뛰기";}
 public void AdvanceConversation(bool skip=false){if(State!=Phase.Conversation||Time.timeScale<=0||!Player.CanAct)return;line++;if(!skip&&line<overheard.Length){ShowConversation();return;}IntelSecured=true;Expose(false);}
 public void SecurityWarning(string message){Notice(message,4);}
 Vector3 exitViewStart;Quaternion exitViewRotation;float exitNear;
 public float ExitProgress=>State==Phase.Abandoning?Mathf.Clamp01((Time.time-entered)/2.5f):0;
 Vector3 boardingViewStart;Quaternion boardingViewRotation;float boardingNear;
 public float BoardingProgress=>State==Phase.Boarding?Mathf.Clamp01((Time.time-entered)/boardingSeconds):0;
 Transform ejectGuard;Quaternion ejectView;
 public void EjectFromVenue(Transform caughtBy=null){if(State!=Phase.Deposit&&State!=Phase.Observe&&State!=Phase.Conversation)return;var viewPosition=Player.View.transform.position;ejectView=Player.View.transform.rotation;ejectGuard=caughtBy?caughtBy:stealth.guards.Where(g=>g&&!g.Incapacitated&&g.gameObject.activeInHierarchy).OrderBy(g=>(g.transform.position-Player.transform.position).sqrMagnitude).Select(g=>g.transform).FirstOrDefault();Set(Phase.Ejected);Lock();cinema.transform.SetPositionAndRotation(viewPosition,ejectView);foreach(var guard in stealth.guards){var agent=guard?guard.GetComponent<UnityEngine.AI.NavMeshAgent>():null;if(agent&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;}if(ejectGuard){var direction=Player.transform.position-ejectGuard.position;direction.y=0;if(direction.sqrMagnitude>.01f)ejectGuard.rotation=Quaternion.LookRotation(direction);}pizza.SetActive(false);approval.SetActive(false);Notice("경비: 출입 금지 구역입니다. 당장 밖으로 나가세요.");}
 public void Expose(bool early){
 if(State!=Phase.Deposit&&State!=Phase.Observe&&State!=Phase.Conversation)return;
 if(State==Phase.Deposit)pizza.SetActive(false);
 var patrol=stealth?stealth.TakeCombatGuards():new CitizenCombat[0];
 // 일찍 들켜도 핵심 단서를 놓쳐 다음 이야기로 진행하지 못하는 일이 없게 한다.
 if(early){IntelSecured=true;StoryPhoneInbox.Current?.SendFrom("민기","급히 잡힌 통신","네가 빠져나오는 동안 남은 통신을 들었어. 은행 돈은 사업 자금이고, 회수하면 우리와 실장을 없앨 계획이었어. 지금은 살아서 나와!");}
 Set(Phase.Exposed);Lock();Shot(Player.View.transform.position,target.position+Vector3.up*1.3f);if(patrol.Length>0){foreach(var guard in patrol){guard.Provoke(Player);enemies.Add(guard);}}else Spawn(interiorPosts,2);foreach(var e in enemies)e.ExternalAttackControl=true;
 Shot(Player.View.transform.position,target.position+Vector3.up*1.3f);
 Notice(early?"박진웅: 저 배달원 잡아! 우리 얘기를 들었어. 살려 보내지 마!\n민기: 들켰어! 들어온 문으로 빠져!":"박진웅: 저 배달원, 언제부터 듣고 있었어? 잡아. 밖으로 나가기 전에 없애!\n민기: 뛰어! 들어온 문으로 빠져!");if(alarm)sound.PlayOneShot(alarm,.6f);
 }

 // 화면에 보이는 위치에서는 생성하지 않고 다음 안전한 위치 확인까지 기다린다.
 class PendingEnemy {public Transform post;public float speed;}
 readonly List<PendingEnemy> pendingSpawns=new();float nextSpawnCheck;
 public int PendingSpawnCount=>pendingSpawns.Count;
 public int HiddenSpawnCount{get;private set;}
 public void SpawnStreetPursuers(Transform[] points,int count,float speed){QueueSpawns(points,count,speed);}
 void Spawn(Transform[] points,int count){QueueSpawns(points,count,4.2f);}
 void QueueSpawns(Transform[] points,int count,float speed){foreach(var post in points.Take(count))if(post)pendingSpawns.Add(new PendingEnemy{post=post,speed=speed});TryPendingSpawns();}
 public bool SpawnOutsideView(Vector3 point){
 var camera=cinema.enabled?cinema:Player.View;
 // 몸 전체와 화면 가장자리 여유까지 검사한다. 벽 뒤라도 화면 안이면 생성하지 않는다.
 return !GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),new Bounds(point+Vector3.up, new Vector3(2.4f,2.8f,2.4f)));
 }
 bool FindHiddenSpawn(Transform post,out Vector3 point){
 var path=new NavMeshPath();
 for(int i=0;i<49;i++){
 var candidate=post.position;if(i>0){float angle=(i-1)*Mathf.PI/4;candidate+=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(2+((i-1)/8)*2);}
 if(!NavMesh.SamplePosition(candidate,out var hit,1.5f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-post.position.y)>1.5f||Vector3.Distance(hit.position,Player.transform.position)<6||!SpawnOutsideView(hit.position))continue;
 if(enemies.Any(e=>e&&Vector3.Distance(e.transform.position,hit.position)<1.2f))continue;
 if(!NavMesh.CalculatePath(hit.position,Player.transform.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
 point=hit.position;return true;
 }point=default;return false;
 }
 void TryPendingSpawns(){
 for(int i=pendingSpawns.Count-1;i>=0;i--){var request=pendingSpawns[i];if(!request.post){pendingSpawns.RemoveAt(i);continue;}if(!FindHiddenSpawn(request.post,out var point))continue;
 var e=Instantiate(preparation.enemyTemplate,point,request.post.rotation,transform);e.missionEnemy=true;e.gameObject.SetActive(true);e.chaseSpeed=request.speed;e.punchCooldown=2.1f;e.Provoke(Player);enemies.Add(e);HiddenSpawnCount++;pendingSpawns.RemoveAt(i);
 }
 }
 void Clean(){pendingSpawns.Clear();if(streetPursuit)streetPursuit.ResetPursuit();if(stealth)stealth.EndPatrol();foreach(var e in enemies)if(e){e.EndFight();Destroy(e.gameObject);}enemies.Clear();}
 public void SkipCinematic(){if(State==Phase.Driving)return;if(!Cinematic||Time.timeScale<=0)return;FinishCinematic();}
 void FinishCinematic(){switch(State){case Phase.Opening:Release();Set(Phase.Entrance);Notice("마지막 배달 · 납품 승인서를 제시하고 서비스 출입구로 입장하세요.");break;
 case Phase.Ejected:if(stealth)stealth.ResetAttempt();ObservationProgress=0;IntelSecured=false;insideVenue=false;Release();PlacePlayer(entry);Set(Phase.Entrance);Notice("민기: 정면으로 마주치지 마. 뒤에서 제압하고, 다른 경비가 보는지도 살펴. 입구에서 다시 들어가자.",8);break;
 case Phase.Admission:approval.SetActive(false);insideVenue=true;PlacePlayer(inside);if(stealth)stealth.BeginPatrol();Release();Set(Phase.Deposit);Notice("서비스 테이블에 피자를 놓고 박진웅을 찾으세요.");break;
 case Phase.Exposed:Release();Set(Phase.InteriorEscape);foreach(var e in enemies)if(e)e.ExternalAttackControl=false;Notice("민기: 출구가 봉쇄됐어! 경비 두 명을 제압하고 들어왔던 접수실 입구로 빠져!");break;
 case Phase.Boarding:cinema.nearClipPlane=boardingNear;door.localRotation=Quaternion.identity;passenger.position=seat.position;passenger.rotation=seat.rotation;if(doorClose)sound.PlayOneShot(doorClose,.6f);Set(Phase.Driving);getaway.Begin();Notice("나: 이 차는 추적될 수 있어. 수리점 인근에 버리고 가게로 돌아가자.",7);break;
 case Phase.Abandoning:cinema.nearClipPlane=exitNear;door.localRotation=Quaternion.identity;if(doorClose)sound.PlayOneShot(doorClose,.5f);getaway.Stop();fade.gameObject.SetActive(false);if(engine)engine.Stop();BeginHomecoming();break;
 case Phase.Debrief:Release();Set(Phase.Complete);CleanupProps();Notice("첫 번째 사건 · 마지막 배달 완료\n은행 의뢰의 진실과 조직의 제거 계획 확인",10);StoryPhoneInbox.Current?.Send("첫 번째 사건 완료","돌아와서 다행이야. 은행 일은 조직의 사업 자금을 마련하려던 거였고, 돈을 회수하면 우리까지 없앨 계획이었어. 이제 실장에게도 알려야 해. 오늘은 가게에서 숨을 돌리자.",true);break;}}
 // 차를 버린 뒤 암전 속에서 실제 플레이어를 가게로 옮기고 민기와 마주한다.
 void BeginHomecoming(){
 Set(Phase.Debrief);Clean();PlacePlayer(safePoint);var mingi=preparation.preparation.delivery.kitchen.actor;var stand=preparation.preparation.delivery.returnPoint;
 var agent=mingi.GetComponent<NavMeshAgent>();if(agent)agent.enabled=false;
 var body=mingi.GetComponent<CharacterController>();bool bodyEnabled=body&&body.enabled;if(body)body.enabled=false;
 mingi.position=stand.position;var toward=Player.transform.position-mingi.position;toward.y=0;if(toward.sqrMagnitude>.01f)mingi.rotation=Quaternion.LookRotation(toward);
 mingi.gameObject.SetActive(true);if(body)body.enabled=bodyEnabled;
 var anim=mingi.GetComponentInChildren<Animator>();if(anim){anim.enabled=true;foreach(var parameter in anim.parameters){if(parameter.type==AnimatorControllerParameterType.Float&&parameter.name=="MoveSpeed")anim.SetFloat(parameter.name,0);if(parameter.type==AnimatorControllerParameterType.Bool&&parameter.name=="IsWalking")anim.SetBool(parameter.name,false);}}
 var aim=mingi.position+Vector3.up*1.45f;var facing=aim-Player.View.transform.position;var flat=facing;flat.y=0;if(flat.sqrMagnitude>.01f)Player.transform.rotation=Quaternion.LookRotation(flat);
 Player.View.transform.rotation=Quaternion.LookRotation(aim-Player.View.transform.position);cinema.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);
 subtitle.text="";fade.gameObject.SetActive(true);fade.color=Color.black;
 }
 void CleanupProps(){Clean();if(getaway)getaway.RestoreTraffic();if(getaway)getaway.Stop();pizza.SetActive(false);approval.SetActive(false);actors.SetActive(false);car.gameObject.SetActive(false);preparation.supportVehicle.gameObject.SetActive(true);if(engine)engine.Stop();}
 // 발표용 바로 시작은 날짜 정산 없이 현재 일차에서 도입 장면부터 재생한다.
 public bool DeveloperStart(){if(Day.IsTransitioning||State==Phase.Scheduling)return false;if(Running)Cancel();StopAllCoroutines();Clean();Release();CleanupProps();insideVenue=false;State=Phase.Available;EventDay=Day.currentDay;Day.EnterNightPreparation();return Begin();}
 public void DeveloperComplete(){if(Completed||State==Phase.Scheduling)return;StopAllCoroutines();Clean();if(Player.RiddenBike)Player.RiddenBike.ExitBike();Release();PlacePlayer(safePoint);insideVenue=false;IntelSecured=true;Set(Phase.Complete);FailureReason="";CleanupProps();ui.SetActive(false);}
 public void Cancel(){if(Running)Fail("작전을 중단했습니다.");}
 public void Fail(string reason){if(!Running)return;StopAllCoroutines();Clean();if(insideVenue&&!Player.IsDown&&!Player.WaterRescueActive&&!PoliceControl.IsArrestInProgress)PlacePlayer(entry);insideVenue=false;Release();CleanupProps();Set(Phase.Failed);FailureReason=reason;Notice("마지막 배달 중단 · "+reason+"\n준비 성과는 유지됩니다. 휴대폰에서 재시도하세요.",10);}
 void Update(){if(Time.timeScale>0&&pendingSpawns.Count>0&&Time.time>=nextSpawnCheck){nextSpawnCheck=Time.time+.35f;TryPendingSpawns();}if(Time.timeScale<=0)return;if(!Running){if(Ready&&!Completed&&!announced){announced=true;StoryPhoneInbox.Current?.Send("첫 번째 사건 · 마지막 배달","준비는 끝났어. 내일은 영업을 쉬고 행사장에 들어가자. 휴대폰에서 ‘마지막 배달’을 선택하면 다음 날로 넘어가. 박진웅이 왜 우릴 쫓는지 알아내야 해.",true);}marker.gameObject.SetActive(false);if(Time.time>noticeUntil)ui.SetActive(false);return;}
 if(State==Phase.Scheduling){objective.text="첫 번째 사건 · 행사 당일로 이동";return;}
 if(Player.HitsTaken>=3||Player.WaterRescueActive||PoliceControl.IsArrestInProgress){Fail("작전을 계속할 수 없습니다.");return;}
 if(State==Phase.Driving){if(Time.time>noticeUntil)subtitle.text="";getaway.Tick();return;}
 if(Cinematic){float elapsed=Time.time-entered;if(State==Phase.Boarding){float t=Mathf.Clamp01(elapsed/boardingSeconds);float open=Mathf.SmoothStep(0,1,t/.23f)*(1-Mathf.SmoothStep(0,1,(t-.78f)/.22f));door.localRotation=Quaternion.Euler(0,70*open,0);
 var doorway=car.TransformPoint(new Vector3(-1.05f,1.12f,.16f));var eye=car.TransformPoint(getaway.eye);Vector3 pos=t<.48f?Vector3.Lerp(boardingViewStart,doorway,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.48f,t))):Vector3.Lerp(doorway,eye,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.83f,t)));
 pos.y-=Mathf.Sin(Mathf.PI*Mathf.Clamp01(Mathf.InverseLerp(.25f,.83f,t)))*.1f;cinema.transform.position=pos;
 var doorLook=Quaternion.LookRotation(car.TransformPoint(new Vector3(-.6f,1,.6f))-boardingViewStart);cinema.transform.rotation=t<.4f?Quaternion.Slerp(boardingViewRotation,doorLook,Mathf.SmoothStep(0,1,t/.4f)):Quaternion.Slerp(doorLook,car.rotation,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,.9f,t)));}
 if(State==Phase.Debrief){fade.gameObject.SetActive(elapsed<1.3f);fade.color=new Color(0,0,0,1-Mathf.InverseLerp(.35f,1.3f,elapsed));subtitle.text=elapsed<1.3f?"":closing[elapsed<5.8f?1:2];}
   if(State==Phase.Ejected){if(ejectGuard){var direction=ejectGuard.position+Vector3.up*1.5f-cinema.transform.position;if(direction.sqrMagnitude>.01f)cinema.transform.rotation=Quaternion.Slerp(ejectView,Quaternion.LookRotation(direction),Mathf.SmoothStep(0,1,elapsed/.3f));}fade.gameObject.SetActive(true);fade.color=new Color(0,0,0,Mathf.InverseLerp(.45f,1.1f,elapsed));}
 if(State==Phase.Abandoning){float t=ExitProgress;float open=Mathf.SmoothStep(0,1,t/.22f)*(1-Mathf.SmoothStep(0,1,(t-.78f)/.18f));door.localRotation=Quaternion.Euler(0,70*open,0);
 var doorway=car.TransformPoint(new Vector3(-1.05f,1.12f,.16f));var outside=car.TransformPoint(new Vector3(-1.85f,1.65f,.16f));var position=t<.5f?Vector3.Lerp(exitViewStart,doorway,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.5f,t))):Vector3.Lerp(doorway,outside,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,.78f,t)));cinema.transform.position=position;
 var doorLook=car.rotation;var outLook=car.rotation*Quaternion.Euler(0,-90,0);cinema.transform.rotation=t<.5f?Quaternion.Slerp(exitViewRotation,doorLook,Mathf.SmoothStep(0,1,t/.5f)):Quaternion.Slerp(doorLook,outLook,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.85f,t)));
 fade.gameObject.SetActive(t>.88f);fade.color=new Color(0,0,0,Mathf.InverseLerp(.88f,1,t));}

 float duration=State==Phase.Ejected?1.1f:State==Phase.Opening?openingSeconds:State==Phase.Admission?2.5f:State==Phase.Exposed?revealSeconds:State==Phase.Boarding?boardingSeconds:State==Phase.Abandoning?2.5f:State==Phase.Debrief?12:7;
 if(PrologueKeyboard.Pressed(Key.Space)||elapsed>=duration)FinishCinematic();objective.text="첫 번째 사건 · 마지막 배달";marker.gameObject.SetActive(false);return;}
 if(PrologueKeyboard.Pressed(Key.F))Interact();if(State==Phase.Conversation&&PrologueKeyboard.Pressed(Key.Space))AdvanceConversation(true);
 if(State==Phase.Observe)Observe(Keyboard.current!=null&&Keyboard.current.fKey.isPressed,Time.deltaTime);
 if(State==Phase.InteriorEscape){foreach(var enemy in enemies)if(enemy&&(enemy.Health<=0||enemy.Npc.IsKnockedDown))subdued.Add(enemy);}
 if(State==Phase.InteriorEscape&&!reinforced&&Vector3.Distance(Player.transform.position,inside.position)<8){reinforced=true;Spawn(interiorPosts.Skip(2).ToArray(),Mathf.Max(0,4-enemies.Count));Notice("조직원: 서비스 통로 쪽이다!\n민기: 출입구 바로 앞이야. 멈추지 마!");}
 if(State==Phase.InteriorEscape&&serviceRoute!=null&&serviceStep<serviceRoute.Length&&Vector3.Distance(Player.transform.position,serviceRoute[serviceStep].position)<3)serviceStep++;
 if(State==Phase.StreetEscape&&alleyRoute!=null&&alleyStep<alleyRoute.Length&&Vector3.Distance(Player.transform.position,alleyRoute[alleyStep].position)<4)alleyStep++;
 if(State==Phase.StreetEscape&&!turned&&Vector3.Distance(Player.transform.position,escapeTurn.position)<5){turned=true;StoryPhoneInbox.Current?.SendFrom("정비사","차량 위치 확인","표시된 차가 네 차야. 운전석으로 타. 나는 먼저 빠졌어. 가게로 바로 몰고 가지 말고 폐차 대기 지점에 버려.");}
 Transform goal=State==Phase.Entrance?entry:State==Phase.Deposit?deliveryTable:State==Phase.Observe||State==Phase.Conversation?(stealth?stealth.Guidance:observeStand):EscapeGoal;
 string label=State==Phase.Entrance?"납품 출입구":State==Phase.Deposit?"서비스 테이블":State==Phase.Observe||State==Phase.Conversation?(stealth&&!stealth.AtListeningPoint?"순찰을 피해 관찰 지점으로":"박진웅 대화 엿듣기"):State==Phase.InteriorEscape?(goal==interiorExit?"들어왔던 입구로 나가기":"접수 복도를 따라 입구로"):turned?"정비사 차량 탑승":"뒷골목으로 탈출";
 objective.text="마지막 배달 · "+label+(State==Phase.InteriorEscape&&!ExitUnlocked?"\n출구 봉쇄 · 경비 제압 "+SubduedCount+" / 2":"")+(State==Phase.Observe?"\n엄폐 옆에서 박진웅을 바라보고 [F] 길게 · "+Mathf.RoundToInt(ObservationProgress*100)+"%":State==Phase.Conversation?"":(State==Phase.InteriorEscape&&goal!=interiorExit)||(State==Phase.StreetEscape&&!turned)?"":"\n[F] 상호작용");marker.gameObject.SetActive(true);StoryTargetDiamond.Place(marker,Player.View,(State==Phase.Observe&&Vector3.Distance(Player.transform.position,observeStand.position)<3.5f)?target.position+Vector3.up*2:goal.position+Vector3.up,Player.transform.position,label);if(State==Phase.Conversation)marker.gameObject.SetActive(false);
 if(State!=Phase.Conversation&&Time.time>noticeUntil)subtitle.text="";
 }
 void LateUpdate(){if(!Running)return;if(State==Phase.Entrance||State==Phase.Deposit){pizza.SetActive(!Player.RiddenBike);pizza.transform.SetPositionAndRotation(Player.View.transform.TransformPoint(new Vector3(-.27f,-.35f,.65f)),Player.View.transform.rotation);}if(State==Phase.Exposed){var d=Player.transform.position-target.position;d.y=0;if(d.sqrMagnitude>.1f)target.rotation=Quaternion.RotateTowards(target.rotation,Quaternion.LookRotation(d),120*Time.deltaTime);}if(State==Phase.Driving||(State==Phase.Boarding&&Time.time-entered>boardingSeconds*.7f))Sit(passenger);}
 static void Sit(Transform actor){var a=actor.GetComponentInChildren<Animator>();if(!a||!a.isHuman)return;foreach(var b in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg}){var t=a.GetBoneTransform(b);if(t)t.localRotation*=Quaternion.Euler(-75,0,0);}foreach(var b in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg}){var t=a.GetBoneTransform(b);if(t)t.localRotation*=Quaternion.Euler(85,0,0);}}
}
}
