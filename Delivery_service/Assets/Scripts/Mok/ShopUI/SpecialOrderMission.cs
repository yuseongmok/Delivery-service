using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 시식 납품과 승인은 일반 주문/평점과 분리하며, 완료 여부는 현재 게임 세션에서 유지한다.
public sealed class SpecialOrderMission : MonoBehaviour {
 public enum Phase { Available, Meet, Briefing, Prepare, Handover, Approval, Return, Debrief, Complete, Failed }
 public static SpecialOrderMission Current {get;private set;}
 public static bool Active=>Current&&Current.Running;
 public Phase State {get;private set;}
 public bool Completed=>State==Phase.Complete;
 public bool Running=>State>=Phase.Meet&&State<=Phase.Debrief;
 public bool InDialogue=>State==Phase.Briefing||State==Phase.Approval||State==Phase.Debrief||State==Phase.Handover;
 public BrokerMission broker;public StoryTutorial kitchen;public Transform attendant,approach,boxRest,returnPoint;
 public GameObject ui,box,approvalPaper;public Text objective,subtitle,marker;
 public int payment=5000;public float handoverSeconds=1.4f,interactionRange=3f;
 public AudioSource sound;public AudioClip boxSound,paperSound;
 [TextArea(2,5)]public string[] briefing={
  "담당자: 실장님 소개로 오셨죠? 이번 비공개 행사, 납품 업체 한 곳이 빠졌어요.",
  "나: 저희 가게가 맡겠습니다.\n담당자: 먼저 기본부터 봅시다. 치즈피자 한 판. 도우, 소스, 치즈만요.",
  "담당자: 제대로 구워서 상자에 담아 오세요. 멀쩡한 피자를 받으면 납품 명단에 올려 드리죠.",
  "나: 행사장 안까지 가져다드리면 됩니까?\n담당자: 오늘은 여기까지. 승인받은 업체만 다음 행사 때 서비스 출입구를 쓸 수 있어요."};
 [TextArea(2,5)]public string[] approval={
  "담당자: 토핑도 맞고, 상자 안도 멀쩡하네요. 좋아요. 정식 납품 업체로 등록하겠습니다.",
  "담당자: 이 승인서를 가져가세요. 행사 당일 물품 검사까지 면제되는 건 아닙니다. 서비스 출입구로 오세요.",
  "나: 알겠습니다. 시간 맞춰 오죠."};
 [TextArea(2,5)]public string[] ending={
  "민기: 됐어?\n나: 납품 명단에 이름 올렸어. 피자로 문을 연 셈이지.",
  "민기: 잘했어. 박진웅이 있는 행사장에 들어갈 이유는 생겼네. 하지만 들키면 나올 길도 있어야 해.",
  "나: 다음은 뒷문인가.\n민기: 맞아. 승인서는 잘 챙겨. 휴대폰의 ‘도시의 뒷문’에서 현장 조사를 시작하자."};
 public string FailureReason {get;private set;}
 public bool SampleAccepted {get;private set;}
 int line,startDay;float entered,noticeUntil,nextPoll;Vector3 boxStart;Quaternion boxRotation;
 bool mingiWasActive;Vector3 mingiPosition;Quaternion mingiRotation;bool showingMingi;
 ToppingInventory Inventory=>broker.player.GetComponent<ToppingInventory>();
 public bool CanStart=>!VenueFinale.Active&&!MechanicMission.Active&&!BackdoorMission.Active&&broker&&broker.Completed&&!broker.Running&&!Running&&!Completed&&broker.day.IsNightPreparation&&!broker.day.IsTransitioning&&!StoryTutorial.Active&&!broker.player.IsDown&&!broker.player.WaterRescueActive&&!PoliceControl.IsArrestInProgress&&!(broker.orders&&broker.orders.HasActiveOrder);
 void Awake(){Current=this;ui.SetActive(false);attendant.gameObject.SetActive(false);box.SetActive(false);approvalPaper.SetActive(false);}
 void OnDestroy(){RestoreMingi();if(Current==this)Current=null;}
 void OnDisable(){RestoreMingi();}
 public bool Begin(){
  if(!CanStart)return false;startDay=broker.day.currentDay;FailureReason="";
  FindFirstObjectByType<PhoneController>()?.ClosePhone();broker.day.pcUIPanel.SetActive(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  foreach(var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))if(pc.pcUIPanel==broker.day.pcUIPanel&&pc.cameraLookScript)pc.cameraLookScript.enabled=true;
  attendant.gameObject.SetActive(true);ui.SetActive(true);box.SetActive(SampleAccepted);approvalPaper.SetActive(SampleAccepted);box.transform.SetPositionAndRotation(boxRest.position,boxRest.rotation);
  Set(SampleAccepted?Phase.Approval:Phase.Meet);if(SampleAccepted)ShowLine();else Notice("실장이 연결한 행사 납품 담당자를 만나세요. 이번에는 정식 배달원으로 접근합니다.");return true;
 }
 void Set(Phase phase){State=phase;line=0;entered=Time.time;}
 void Notice(string text){subtitle.text=text;noticeUntil=Time.time+7;}
 string[] Lines=>State==Phase.Briefing?briefing:State==Phase.Approval?approval:ending;
 void ShowLine(){var lines=Lines;subtitle.text=(lines!=null&&line<lines.Length?lines[line]:"")+"\n[F] 계속 · [Space] 대화 건너뛰기";}
 bool Near(Transform target){
  var p=broker.player;if(!p.CanAct||Vector3.Distance(p.transform.position,target.position)>interactionRange)return false;
  Vector3 from=p.View.transform.position,to=target.position+Vector3.up*1.1f;
  return !Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(p.transform)&&!h.transform.IsChildOf(target)&&!(Inventory.HoldPoint&&h.transform.IsChildOf(Inventory.HoldPoint)));
 }
 public bool Interact(){
  if(!Running||State==Phase.Handover)return false;
  if(State==Phase.Return){if(!Near(kitchen.actor))return false;Set(Phase.Debrief);ShowLine();return true;}
  if(State==Phase.Debrief||State==Phase.Briefing||State==Phase.Approval){AdvanceDialogue();return true;}
  if(!Near(attendant))return false;
  if(State==Phase.Meet){Set(Phase.Briefing);ShowLine();return true;}
  if(State!=Phase.Prepare)return false;
  var pizzas=Inventory.GetHeldPackagedPizzas();int index=pizzas.FindIndex(Valid);
  if(index<0){Notice(pizzas.Count==0?"담당자: 배달통에 있다면 꺼내서 직접 건네주세요. 구운 치즈피자 한 판입니다.":"담당자: 도우·소스·치즈만 넣어 구운, 손상되지 않은 피자가 필요해요. 다시 준비해 주세요.");return false;}
  // 맞는 한 상자만 소비하고 나머지 상자는 그대로 보존한다.
  var remaining=Inventory.ClearPackagedPizzas();remaining.RemoveAt(index);if(remaining.Count>0)Inventory.AddPackagedDataStack(remaining);
  SampleAccepted=true;boxStart=broker.player.View.transform.position+broker.player.View.transform.forward*.6f-Vector3.up*.4f;boxRotation=broker.player.transform.rotation;
  if(sound&&boxSound)sound.PlayOneShot(boxSound,.35f);
  box.SetActive(true);box.transform.SetPositionAndRotation(boxStart,boxRotation);Set(Phase.Handover);subtitle.text="담당자: 잠시만요. 납품 상태부터 확인하겠습니다.";return true;
 }
 public static bool Valid(PizzaData p)=>p!=null&&p.isBaked&&!p.isRuined&&PizzaRecipes.Matches(p.toppings,new List<string>{"도우","소스","치즈"});
 public void AdvanceDialogue(bool skip=false){
  if(!InDialogue||State==Phase.Handover||!Near(State==Phase.Debrief?kitchen.actor:attendant))return;
  line++;if(!skip&&Lines!=null&&line<Lines.Length){ShowLine();return;}
  if(State==Phase.Briefing){Set(Phase.Prepare);Notice("치즈피자 한 판을 만들어 포장하세요. 재료비는 평소와 같습니다. 이 시식 납품에는 시간 제한이 없습니다.");}
  else if(State==Phase.Approval){approvalPaper.SetActive(false);if(sound&&paperSound)sound.PlayOneShot(paperSound,.3f);ShowMingi();Set(Phase.Return);Notice("정식 납품 승인서 확보 · 가게로 돌아가 민기와 다음 준비를 의논하세요.");}
  else{Set(Phase.Complete);showingMingi=false;MoneyManager.Instance?.AddMoney(payment);Notice("특별 주문 완료 · 정식 납품 승인\n시식 납품 대금 "+payment.ToString("N0")+"원");StoryPhoneInbox.Current?.Send("납품 승인서 확보","승인서는 잘 보관해 둬. 이제 휴대폰에서 ‘도시의 뒷문’을 시작하자. 행사장 출입구와 경비 사각지대, 탈출 동선을 확인해야 해.",true);}
 }
 void ShowMingi(){if(showingMingi)return;showingMingi=true;mingiWasActive=kitchen.actor.gameObject.activeSelf;mingiPosition=kitchen.actor.position;mingiRotation=kitchen.actor.rotation;kitchen.actor.position=returnPoint.position;kitchen.actor.gameObject.SetActive(true);}
 void RestoreMingi(){if(!showingMingi||!kitchen||!kitchen.actor)return;kitchen.actor.gameObject.SetActive(false);kitchen.actor.SetPositionAndRotation(mingiPosition,mingiRotation);kitchen.actor.gameObject.SetActive(mingiWasActive);showingMingi=false;}
 public void Cancel(){if(Running)Fail("준비를 중단했습니다.");}
 public void Fail(string reason){if(!Running)return;Set(Phase.Failed);FailureReason=reason;attendant.gameObject.SetActive(false);box.SetActive(false);approvalPaper.SetActive(false);RestoreMingi();Notice("특별 주문 중단 · "+reason+"\n휴대폰에서 재시도할 수 있습니다."+(SampleAccepted?"\n접수된 피자는 다시 제출하지 않아도 됩니다.":""));}
 void Update(){
  if(Time.timeScale<=0)return;
  if(!Running){marker.gameObject.SetActive(false);if(Time.time>noticeUntil)ui.SetActive(false);return;}
  if(broker.day.currentDay!=startDay||!broker.day.IsNightPreparation){Fail("밤 준비 시간이 끝났습니다.");return;}
  if(broker.player.HitsTaken>=3||broker.player.WaterRescueActive||PoliceControl.IsArrestInProgress){Fail("안전하게 납품하지 못했습니다.");return;}
  if(PrologueKeyboard.Pressed(Key.F))Interact();if(PrologueKeyboard.Pressed(Key.Space))AdvanceDialogue(true);
  if(State==Phase.Handover){float t=Mathf.Clamp01((Time.time-entered)/Mathf.Max(.3f,handoverSeconds));box.transform.position=Vector3.Lerp(boxStart,boxRest.position,Mathf.SmoothStep(0,1,t))+Vector3.up*Mathf.Sin(t*Mathf.PI)*.18f;box.transform.rotation=Quaternion.Slerp(boxRotation,boxRest.rotation,t);if(t>=1){approvalPaper.SetActive(true);Set(Phase.Approval);ShowLine();}}
  if(!InDialogue&&Time.time>noticeUntil)subtitle.text="";
  Transform target=State==Phase.Return||State==Phase.Debrief?kitchen.actor:attendant;
  string label=State==Phase.Return||State==Phase.Debrief?"민기":"납품 담당자";
  objective.text=State==Phase.Meet?"특별 주문 · 행사 납품 담당자 만나기":State==Phase.Prepare?"특별 주문 · 구운 치즈피자 1판 포장해 전달하기":State==Phase.Handover?"시식 납품 상태 확인 중":State==Phase.Approval?"정식 납품 승인서 받기":State==Phase.Return?"가게로 돌아가 민기와 이야기하기":"특별 주문 · 대화";
  if(State==Phase.Prepare&&!Inventory.HasPackagedPizzas()&&kitchen.cargo.StoredCount==0){target=kitchen.pickup.PackedCount>0?kitchen.pickup.transform:kitchen.maker.transform;label=kitchen.pickup.PackedCount>0?"포장 피자":"피자 제작대";}
  if(broker.player.RiddenBike)objective.text+="\n도착하면 내려서 대화하세요";
  else if(State!=Phase.Handover&&Vector3.Distance(broker.player.transform.position,(State==Phase.Return||State==Phase.Debrief?kitchen.actor:attendant).position)<interactionRange)objective.text+="\n[F] "+(State==Phase.Prepare?"피자 전달":"대화");
  marker.gameObject.SetActive(true);StoryTargetDiamond.Place(marker,broker.player.View,target.position+Vector3.up*1.8f,broker.player.transform.position,label);
  if(Time.time>=nextPoll){nextPoll=Time.time+.15f;var who=State==Phase.Return||State==Phase.Debrief?kitchen.actor:attendant;var d=broker.player.transform.position-who.position;d.y=0;if(d.sqrMagnitude>.1f&&d.sqrMagnitude<100)who.rotation=Quaternion.RotateTowards(who.rotation,Quaternion.LookRotation(d),25);}
 }
 void LateUpdate(){
  if(State!=Phase.Handover)return;var a=attendant.GetComponentInChildren<Animator>();if(!a||!a.isHuman)return;
  var upper=a.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=a.GetBoneTransform(HumanBodyBones.RightLowerArm);var hand=a.GetBoneTransform(HumanBodyBones.RightHand);
  float weight=Mathf.Sin(Mathf.Clamp01((Time.time-entered)/Mathf.Max(.3f,handoverSeconds))*Mathf.PI);
  Vector3 goal=Vector3.Lerp(hand.position,box.transform.position+Vector3.up*.1f,weight),origin=upper.position,axis=(goal-origin).normalized;
  float x=Vector3.Distance(origin,lower.position),y=Vector3.Distance(lower.position,hand.position),d=Mathf.Clamp(Vector3.Distance(origin,goal),.01f,x+y-.005f);goal=origin+axis*d;
  float along=(x*x-y*y+d*d)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,x*x-along*along));var elbow=origin+axis*along+Vector3.ProjectOnPlane(attendant.right,axis).normalized*height;
  upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,goal-lower.position)*lower.rotation;
 }
}
}
