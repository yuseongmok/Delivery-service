using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 첫 밤 미션의 상태는 해당 게임 세션 동안 유지한다. 실패하면 휴대폰 미션 앱에서 다시 시작한다.
public sealed class BrokerMission : MonoBehaviour {
 public enum Phase { Available, Find, Conversation, Escape, Debrief, Complete, Failed }
 public static BrokerMission Current {get;private set;}
 public static bool Active=>(Current&&Current.Running)||SpecialOrderMission.Active||BackdoorMission.Active||MechanicMission.Active||VenueFinale.Active;
 public Phase State {get;private set;}
 public bool Running=>State==Phase.Find||State==Phase.Conversation||State==Phase.Escape||State==Phase.Debrief;
 public bool Completed=>State==Phase.Complete;
 public DayNightCycle day;public PlayerCombat player;public OrderManager orders;
 public Transform meeting,exit,broker;public CitizenCombat enemyTemplate;public Transform[] ambushPosts;
 public GameObject missionUI;public Text objective,subtitle,marker;
 public float followSpeed=5.4f,pursuitSpeed=4.5f,reinforcementDelay=14f,safeSeconds=3f;
 [TextArea(2,5)]public string[] meetingLines={
  "실장: 조용히 해. 뒤에 누가 따라오진 않았지?",
  "나: 연락을 끊은 건 그쪽이잖아. 은행 일, 대체 누가 시킨 거야?",
  "실장: 나도 버림받았어. 박진웅… 그 사람이 곧 비공개 행사에 나타나.",
  "조직원: 저기 있다! 둘 다 잡아!\n실장: 여기서 얘기할 때가 아니야. 먼저 길을 열어 줘!"};
 [TextArea(2,5)]public string[] endingLines={
  "실장: 따돌렸군. 박진웅은 행사장 안쪽에서만 거래해. 평범하게는 못 들어가.",
  "나: 피자를 시키게 만들면 되겠네.\n실장: 배달원? …그래, 관계자 연락처를 넘겨 줄게.",
  "민기: 정보는 받았어. 가게로 돌아와. 다음에는 정식 납품부터 뚫자."};
 CitizenCombat[] enemies=new CitizenCombat[4];NavMeshAgent guide;Animator animator;
 int line,startDay;float entered,repath,safeFor,messageUntil;bool reinforced;
 public string FailureReason {get;private set;}
 Vector3 brokerHome;Quaternion brokerRotation;
 public BrokerPassenger passenger;
 void Awake(){Current=this;brokerHome=broker.position;brokerRotation=broker.rotation;guide=broker.GetComponent<NavMeshAgent>();animator=broker.GetComponentInChildren<Animator>();animator.enabled=true;animator.speed=1;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;broker.gameObject.SetActive(false);missionUI.SetActive(false);}
 void OnDestroy(){if(Current==this)Current=null;CleanEnemies();}
 public bool CanStart=>!VenueFinale.Active&&!MechanicMission.Active&&!BackdoorMission.Active&&!SpecialOrderMission.Active&&!Running&&!Completed&&day&&day.IsNightPreparation&&!day.IsTransitioning&&!StoryTutorial.Active&&player&&!player.IsDown&&!PoliceControl.IsArrestInProgress&&!(orders&&orders.HasActiveOrder);
 public bool Begin(){
  if(!CanStart)return false;CleanEnemies();FailureReason="";startDay=day.currentDay;reinforced=false;safeFor=0;
  broker.gameObject.SetActive(false);broker.SetPositionAndRotation(brokerHome,brokerRotation);broker.gameObject.SetActive(true);
  guide.enabled=true;if(!guide.isOnNavMesh){Fail("실장이 이동할 길을 찾지 못했습니다.");return false;}guide.Warp(brokerHome);guide.isStopped=true;
  FindFirstObjectByType<PhoneController>()?.ClosePhone();day.pcUIPanel.SetActive(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  // PC의 별도 시점 잠금도 함께 해제한다.
  foreach(var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))if(pc.pcUIPanel==day.pcUIPanel&&pc.cameraLookScript)pc.cameraLookScript.enabled=true;
  missionUI.SetActive(true);SetPhase(Phase.Find);subtitle.text="민기: 실장이 마지막으로 연락한 곳을 표시했어. 만나서 은행 일부터 물어봐.";messageUntil=Time.time+7;return true;
 }
 void SetPhase(Phase value){State=value;entered=Time.time;line=0;safeFor=0;if(value!=Phase.Escape){if(guide&&guide.enabled&&guide.isOnNavMesh)guide.isStopped=true;if(animator)animator.SetFloat("MoveSpeed",0);}}
 public bool Talk(){
  if(State!=Phase.Find||!player.CanAct||Vector3.Distance(player.transform.position,broker.position)>3||!VisibleBroker())return false;
  SetPhase(Phase.Conversation);ShowLine();return true;
 }
 bool VisibleBroker(){var from=player.View.transform.position;var to=broker.position+Vector3.up*1.2f;return !Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(player.transform)&&!h.transform.IsChildOf(broker));}
 void ShowLine(){var lines=State==Phase.Conversation?meetingLines:endingLines;subtitle.text=lines!=null&&line<lines.Length?lines[line]+"\n[F] 계속 · [Space] 대화 건너뛰기":"";}
 public void AdvanceDialogue(bool skip=false){
  if(State!=Phase.Conversation&&State!=Phase.Debrief)return;if(Vector3.Distance(player.transform.position,broker.position)>5)return;
  var lines=State==Phase.Conversation?meetingLines:endingLines;line++;
  if(!skip&&lines!=null&&line<lines.Length){ShowLine();return;}
  if(State==Phase.Conversation){SetPhase(Phase.Escape);Spawn(0);Spawn(1);subtitle.text="실장: 앞장서! 뒤처지면 잠깐만 기다려 줘!";messageUntil=Time.time+6;}
  else {SetPhase(Phase.Complete);broker.gameObject.SetActive(false);subtitle.text="준비 완료 · 박진웅의 행사 정보와 관계자 연락처 확보\n휴대폰 미션 앱에서 다음 준비를 확인하세요.";messageUntil=Time.time+9;}
 }
 void Spawn(int index){
  if(index>=ambushPosts.Length||!enemyTemplate)return;
  var e=Instantiate(enemyTemplate,ambushPosts[index].position,ambushPosts[index].rotation,transform);e.name="Broker pursuer "+index;
  e.gameObject.SetActive(true);e.chaseSpeed=pursuitSpeed;e.punchCooldown=1.9f+index*.15f;e.Provoke(player);enemies[index]=e;
 }
 public void Fail(string reason){if(Completed)return;FailureReason=reason;SetPhase(Phase.Failed);CleanEnemies();if(broker)broker.gameObject.SetActive(false);if(missionUI)missionUI.SetActive(true);subtitle.text="미션 실패 · "+reason+"\n휴대폰 미션 앱에서 다시 시도할 수 있습니다.";messageUntil=Time.time+8;}
 public void Cancel(){if(Running)Fail("준비를 중단했습니다.");}
 void CleanEnemies(){foreach(var e in enemies)if(e)Destroy(e.gameObject);enemies=new CitizenCombat[4];}
 void Update(){
  if(Time.timeScale<=0)return;
  if(!Running){if(missionUI&&Time.time>messageUntil)missionUI.SetActive(false);if(objective)objective.text=Completed?"연락이 끊긴 중개인 · 준비 완료":"";if(marker)marker.gameObject.SetActive(false);return;}
  if(day.currentDay!=startDay||!day.IsNightPreparation){Fail("준비 시간이 종료되었습니다.");return;}
  if(player.HitsTaken>=3||player.WaterRescueActive||PoliceControl.IsArrestInProgress){Fail("추격을 벗어나지 못했습니다.");return;}
  if(Time.time>messageUntil&&State!=Phase.Conversation&&State!=Phase.Debrief)subtitle.text="";
  if(player.CanAct){
   if(PrologueKeyboard.Pressed(Key.F)){if(State==Phase.Find)Talk();else AdvanceDialogue();}
   if(PrologueKeyboard.Pressed(Key.Space))AdvanceDialogue(true);
  }
  if(State==Phase.Conversation||State==Phase.Debrief){if(!passenger||!passenger.IsRiding)FacePlayer();objective.text=State==Phase.Conversation?"실장에게 은행 의뢰의 배후 묻기":player.RiddenBike?"오토바이에서 내려 확보한 정보 확인하기":"확보한 정보 확인하기";}
  if(State==Phase.Find){objective.text="연락이 끊긴 중개인 · 실장 찾기";if(Vector3.Distance(player.transform.position,broker.position)<3)objective.text+=player.RiddenBike?"\n오토바이에서 내려 실장과 대화하세요":"\n[F] 실장과 대화";FacePlayer();}
  if(State==Phase.Escape){
   Follow();if(!Running)return;if(!reinforced&&Time.time-entered>=reinforcementDelay){reinforced=true;Spawn(2);Spawn(3);subtitle.text="조직원: 강변 쪽을 막아!\n실장: 더 오기 전에 빠져나가자!";messageUntil=Time.time+5;}
   bool arrived=Vector3.Distance(player.transform.position,exit.position)<5&&Vector3.Distance(broker.position,exit.position)<6;
   bool clear=!enemies.Any(e=>e&&e.IsAggressive&&!e.Npc.IsKnockedDown&&Vector3.Distance(e.transform.position,exit.position)<12);
   safeFor=arrived&&clear?safeFor+Time.deltaTime:0;
   objective.text="실장과 탈출 지점으로 이동"+(arrived&&!clear?"\n조직원과 거리를 벌리세요":passenger&&passenger.IsRiding?(passenger.IsBoarding?"\n실장이 뒷자리에 타는 중":"\n실장 탑승 완료"):player.RiddenBike?"\n실장 가까이 멈춰 뒷자리에 태우세요":Vector3.Distance(player.transform.position,broker.position)>13?"\n실장이 따라오도록 기다리세요":arrived?"\n주변을 확인하는 중…":"\n조직원을 모두 쓰러뜨릴 필요는 없습니다");
   if(safeFor>=safeSeconds){CleanEnemies();if(guide.enabled&&guide.isOnNavMesh)guide.isStopped=true;SetPhase(Phase.Debrief);ShowLine();}
  }
  UpdateMarker();
 }
 void FacePlayer(){Vector3 d=player.transform.position-broker.position;d.y=0;if(d.sqrMagnitude>.1f&&d.sqrMagnitude<144)broker.rotation=Quaternion.RotateTowards(broker.rotation,Quaternion.LookRotation(d),Time.deltaTime*180);}
 void Follow(){
  if(passenger&&passenger.IsRiding)return;
  if(passenger&&passenger.TryBoard(player.RiddenBike)){subtitle.text="실장: 탔어! 출발해!";messageUntil=Time.time+4;return;}
  if(!guide.enabled||!guide.isOnNavMesh){Fail("실장이 이동할 길을 잃었습니다.");return;}
  float gap=Vector3.Distance(player.transform.position,broker.position);guide.speed=followSpeed;guide.stoppingDistance=2;
  guide.isStopped=gap<2.2f||player.IsDown;
  if(!guide.isStopped&&Time.time>=repath){repath=Time.time+.3f;if(player.RiddenBike)MountedTargetPath.Set(guide,player.transform.position);else if(NavMesh.SamplePosition(player.transform.position,out var hit,4,NavMesh.AllAreas))guide.SetDestination(hit.position);}
  if(animator){animator.speed=1;animator.SetFloat("MoveSpeed",guide.velocity.magnitude,.12f,Time.deltaTime);}
 }
 void UpdateMarker(){
  var target=State==Phase.Escape?exit:broker;
  marker.gameObject.SetActive(true);StoryTargetDiamond.Place(marker,player.View,target.position+Vector3.up*1.8f,player.transform.position,State==Phase.Escape?"탈출":"실장");
 }
}
}
