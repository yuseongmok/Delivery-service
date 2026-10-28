using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 현장 조사 결과는 재시도에도 유지한다. 최종 보고 전에는 피날레 준비 완료로 처리하지 않는다.
public sealed class BackdoorMission : MonoBehaviour {
 Phase state;public int CompletedDay{get;private set;}
 public enum Phase { Available, Briefing, Survey, Return, Debrief, Complete, Failed }
 public static BackdoorMission Current {get;private set;}
 public static bool Active=>Current&&Current.Running;
 public Phase State{get=>state;private set{if(value==Phase.Complete&&state!=Phase.Complete)CompletedDay=Day.currentDay;state=value;}}
 public bool Running=>State>=Phase.Briefing&&State<=Phase.Debrief;
 public bool Completed=>State==Phase.Complete;
 public bool InDialogue=>State==Phase.Briefing||State==Phase.Debrief;
 public SpecialOrderMission delivery;
 public Transform[] surveyTargets,standPoints;
 public Transform reportPoint;public BackdoorGuard[] guards;
 public GameObject ui;public Text objective,subtitle,marker;
 public float observeSeconds=2f,observeRange=8f,loseSightSeconds=4f;
 public AudioSource sound;public AudioClip recordedSound,warningSound;
 public string[] labels={"서비스 출입구","경비 사각지대","골목 탈출로","차량 합류 지점"};
 [TextArea(2,5)] public string[] findings={
  "나: 납품 승인서를 확인하는 출입구가 여기야. 지금은 잠겨 있어.\n민기: 행사 당일엔 피자 들고 저 문으로 들어가는 거네. 다음은 경비가 어디를 보는지 확인해.",
  "나: 경비가 출입구 앞을 왕복해. 물품 적재함 뒤는 가려져 있어.\n민기: 좋네. 숨을 곳은 찾았고, 이제 들켰을 때 빠져나갈 길을 직접 걸어 봐.",
  "나: 모퉁이를 돌아 나오니 출입구가 안 보여. 여기서 시선을 끊으면 되겠어.\n민기: 막다른 길은 아니지? 끝까지 가서 차가 기다릴 자리도 확인해.",
  "나: 큰길까지 이어져. 이 지점이면 차를 타고 빠져나올 수 있어.\n민기: 좋아. 이제 돌아와서 동선을 정리하자."};
 [TextArea(2,5)] public string[] briefing={
  "민기: 납품 승인은 받았지만, 나오는 길까지 허락받은 건 아니잖아.",
  "나: 행사장 주변부터 돌아보고 올게.\n민기: 오늘은 조사만. 출입구 표지, 경비가 못 보는 자리, 골목 끝의 차량 합류 지점을 확인해.",
  "민기: 가까이 가서 표식을 바라보고 F를 누르고 있으면 기록할 수 있어. 경비가 의심하면 벽 뒤로 빠져. 필요하면 Ctrl로 몸을 낮추고.",
  "나: 들어갈 문보다 나올 길부터 기억하라는 거지.\n민기: 그래. 무리하지 말고 살아서 돌아와."};
 [TextArea(2,5)] public string[] debrief={
  "민기: 동선은?\n나: 납품 출입구와 숨을 곳을 확인했어. 들키면 들어온 문으로 나와 모퉁이를 돌아 큰길로 빠지면 돼.",
  "민기: 박진웅에게 들키면 설명할 기회도 없을 거야. 알아낼 것만 알아내고 바로 빠져.",
  "나: 둘러싸이면 오토바이로는 어렵겠어.\n민기: 내일 정비사를 만나 봐. 차를 구해 보자. 낮에 기름값이랑 준비 비용도 더 벌어 두고."};
 public int ConfirmedCount {get;private set;}
 public float ObservationProgress {get;private set;}
 public bool Alerted {get;private set;}
 public float Suspicion {get;private set;}
 public string FailureReason {get;private set;}
 BackdoorGuard[] activeGuards=new BackdoorGuard[0];
 public BackdoorGuard[] ActiveGuards=>activeGuards;
 int line,startDay;float noticeUntil,hiddenFor,senseElapsed;bool warned;Vector3 mingiPosition;Quaternion mingiRotation;bool mingiWasActive,showingMingi;
 public PlayerCombat Player=>delivery.broker.player;
 DayNightCycle Day=>delivery.broker.day;
 Transform Mingi=>delivery.kitchen.actor;
 public int EarliestDay=>delivery.CompletedDay+1;public bool DayUnlocked=>Day.currentDay>=EarliestDay;
 public bool CanStart=>DayUnlocked&&!VenueFinale.Active&&!MechanicMission.Active&&delivery&&delivery.Completed&&!delivery.Running&&!delivery.broker.Running&&!Running&&!Completed&&Day.IsNightPreparation&&!Day.IsTransitioning&&!StoryTutorial.Active&&!Player.IsDown&&!Player.WaterRescueActive&&!PoliceControl.IsArrestInProgress&&!(delivery.broker.orders&&delivery.broker.orders.HasActiveOrder);
 void Awake(){Current=this;ui.SetActive(false);foreach(var g in guards)g.gameObject.SetActive(false);}
 void OnDestroy(){StopGuards();RestoreMingi();if(Current==this)Current=null;}
 void OnDisable(){RestoreMingi();}
 public bool Begin(){
  if(!CanStart)return false;FailureReason="";startDay=Day.currentDay;Alerted=false;Suspicion=0;hiddenFor=0;senseElapsed=0;warned=false;ObservationProgress=0;
  FindFirstObjectByType<PhoneController>()?.ClosePhone();Day.pcUIPanel.SetActive(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  foreach(var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))if(pc.pcUIPanel==Day.pcUIPanel&&pc.cameraLookScript)pc.cameraLookScript.enabled=true;
  ShowMingi();ui.SetActive(true);State=Phase.Briefing;line=0;ShowLine();return true;
 }
 void ShowMingi(){if(showingMingi)return;showingMingi=true;mingiPosition=Mingi.position;mingiRotation=Mingi.rotation;mingiWasActive=Mingi.gameObject.activeSelf;Mingi.position=reportPoint.position;Mingi.gameObject.SetActive(true);}
 void RestoreMingi(){if(!showingMingi||!delivery)return;Mingi.gameObject.SetActive(false);Mingi.SetPositionAndRotation(mingiPosition,mingiRotation);Mingi.gameObject.SetActive(mingiWasActive);showingMingi=false;}
 bool Clear(Vector3 target,Transform root){
  var view=Player.View;if(!view)return false;var origin=view.transform.position;var d=target-origin;
  var inv=Player.GetComponent<ToppingInventory>();
  return !Physics.RaycastAll(origin,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(Player.transform)&&!h.transform.IsChildOf(root)&&!(inv&&inv.HoldPoint&&h.transform.IsChildOf(inv.HoldPoint)));
 }
 bool NearMingi()=>Player.CanAct&&Vector3.Distance(Player.transform.position,Mingi.position)<3.5f&&Clear(Mingi.position+Vector3.up*1.2f,Mingi);
 void ShowLine(){var lines=State==Phase.Briefing?briefing:debrief;subtitle.text=lines[line]+"\n[F] 계속 · [Space] 대화 건너뛰기";}
 public bool Talk(){if(State!=Phase.Return||Alerted||!NearMingi())return false;State=Phase.Debrief;line=0;ShowLine();return true;}
 public void AdvanceDialogue(bool skip=false){
  if(!InDialogue||!NearMingi())return;var lines=State==Phase.Briefing?briefing:debrief;line++;
  if(!skip&&line<lines.Length){ShowLine();return;}
  if(State==Phase.Briefing){State=ConfirmedCount>=surveyTargets.Length?Phase.Return:Phase.Survey;activeGuards=guards.Select(g=>Instantiate(g,transform)).ToArray();foreach(var g in activeGuards)g.Activate();Notice("주황색 표식을 따라 행사장 주변을 조사하세요. 오토바이는 접근할 때 이용하고, 조사는 내려서 진행하세요.");}
  else{State=Phase.Complete;StopGuards();RestoreMingi();Notice("도시의 뒷문 · 준비 완료\n서비스 출입구와 탈출 경로 확보");StoryPhoneInbox.Current?.Send("현장 조사 완료","현장 조사는 끝났어. 정비사와 내일 밤 약속을 잡았어. 낮에는 가게를 열고, 마감 후 수리점에서 만나 봐. 차량만 확보하면 준비가 끝나.",true);}
 }
 void Notice(string message){subtitle.text=message;noticeUntil=Time.time+Mathf.Clamp(message.Length*.065f,6,11);}
 public bool CanObserve(){
  if(State!=Phase.Survey||Alerted||Suspicion>.65f||ConfirmedCount>=surveyTargets.Length||!Player.CanAct)return false;
  var target=surveyTargets[ConfirmedCount];var delta=target.position-Player.View.transform.position;
  return delta.magnitude<=observeRange&&Vector3.Dot(Player.View.transform.forward,delta.normalized)>.80f&&Clear(target.position,target);
 }
 public void Observe(bool held,float seconds){
  if(Time.timeScale<=0)return;
  if(!held||!CanObserve()){ObservationProgress=0;return;}
  ObservationProgress+=Mathf.Max(0,seconds)/Mathf.Max(.25f,observeSeconds);
  if(ObservationProgress<1)return;ObservationProgress=0;int index=ConfirmedCount++;
  if(sound&&recordedSound)sound.PlayOneShot(recordedSound,.22f);Notice(findings[index]);
  if(ConfirmedCount>=surveyTargets.Length){State=Phase.Return;StopGuards();}
 }
 public void RaiseAlert(){
  if(!Running||State!=Phase.Survey||Alerted)return;Alerted=true;Suspicion=1;hiddenFor=0;ObservationProgress=0;
  foreach(var guard in activeGuards)if(guard)guard.Chase();if(sound&&warningSound)sound.PlayOneShot(warningSound,.22f);
  Notice("경비: 거기! 허가도 없이 뭘 살피는 거야!\n민기: 뒤로 빠져! 벽이나 적재함 뒤로 돌아서 시야를 끊어!");
 }
 void Sense(float elapsed){
  if(State!=Phase.Survey)return;bool seen=activeGuards.Any(g=>g&&g.CanSee(Player));
  bool attacked=activeGuards.Any(g=>g&&g.IsFighting);
  if(Alerted){hiddenFor=seen?0:hiddenFor+elapsed;if(hiddenFor>=Mathf.Min(loseSightSeconds,2.5f)){Alerted=false;Suspicion=0;hiddenFor=0;warned=false;foreach(var g in activeGuards)if(g)g.StandDown();Notice("민기: 놓친 것 같아. 확인한 정보는 남아 있어. 조심해서 이어가자.");}return;}
  Suspicion=Mathf.Clamp01(Suspicion+(seen?elapsed/6f:-elapsed/.8f));
  if(Suspicion>.15f&&!warned){warned=true;Notice("경비: 납품은 접수대에서 하세요. 안쪽을 기웃거리지 마세요.\n민기: 의심받고 있어. 시야에서 벗어나서 기다려.");}
  if(Suspicion<=0)warned=false;if(Suspicion>=1||attacked)RaiseAlert();
 }
 void StopGuards(){foreach(var g in activeGuards)if(g){g.Deactivate();Destroy(g.gameObject);}activeGuards=new BackdoorGuard[0];Alerted=false;Suspicion=0;}
 public void DeveloperComplete(){if(Completed)return;StopGuards();RestoreMingi();ConfirmedCount=surveyTargets.Length;ObservationProgress=0;State=Phase.Complete;FailureReason="";ui.SetActive(false);}
 public void Cancel(){if(Running)Fail("조사를 중단했습니다.");}
 public void Fail(string reason){if(!Running)return;FailureReason=reason;State=Phase.Failed;ObservationProgress=0;StopGuards();RestoreMingi();Notice("도시의 뒷문 중단 · "+reason+"\n확인한 정보 "+ConfirmedCount+"/4 · 휴대폰에서 재시도할 수 있습니다.");}
 void Update(){
  if(Time.timeScale<=0)return;if(!Running){marker.gameObject.SetActive(false);if(Time.time>noticeUntil)ui.SetActive(false);return;}
  if(Day.currentDay!=startDay||!Day.IsNightPreparation){Fail("밤 준비 시간이 끝났습니다.");return;}
  if(Player.HitsTaken>=3||Player.WaterRescueActive||PoliceControl.IsArrestInProgress){Fail("현장 조사를 계속할 수 없습니다.");return;}
  senseElapsed+=Time.deltaTime;if(senseElapsed>=.1f){Sense(senseElapsed);senseElapsed=0;}
  if(InDialogue){if(PrologueKeyboard.Pressed(Key.F))AdvanceDialogue();else if(PrologueKeyboard.Pressed(Key.Space))AdvanceDialogue(true);}
  else if(State==Phase.Return&&PrologueKeyboard.Pressed(Key.F))Talk();
  else if(State==Phase.Survey)Observe(Keyboard.current!=null&&Keyboard.current.fKey.isPressed,Time.deltaTime);
  if(!InDialogue&&Time.time>noticeUntil)subtitle.text="";
  var target=State==Phase.Survey?surveyTargets[Mathf.Min(ConfirmedCount,surveyTargets.Length-1)]:Mingi;string title=State==Phase.Survey?labels[Mathf.Min(ConfirmedCount,labels.Length-1)]:"민기";
  objective.text=Alerted?"경비에게서 벗어나 시야 밖에서 숨기":State==Phase.Survey?"도시의 뒷문 · "+title+" ("+ConfirmedCount+"/4)":State==Phase.Return?"가게로 돌아가 민기에게 동선 보고":"도시의 뒷문 · 민기와 대화";
  if(State==Phase.Survey&&!Alerted)objective.text+=Suspicion>.65f?"\n경비가 의심하고 있습니다":Player.RiddenBike?"\n내려서 주변을 조사하세요":ObservationProgress>0?"\n조사 중 "+Mathf.RoundToInt(ObservationProgress*100)+"%":"\n가까이에서 표식을 바라보고 [F] 길게 누르기";
  else if(!Alerted&&NearMingi())objective.text+="\n[F] 대화";
  marker.gameObject.SetActive(!Alerted);if(!Alerted)StoryTargetDiamond.Place(marker,Player.View,target.position+(State==Phase.Survey?Vector3.zero:Vector3.up*1.8f),Player.transform.position,title);
  if(State!=Phase.Survey){Vector3 d=Player.transform.position-Mingi.position;d.y=0;if(d.sqrMagnitude>.1f&&d.sqrMagnitude<100)Mingi.rotation=Quaternion.RotateTowards(Mingi.rotation,Quaternion.LookRotation(d),Time.deltaTime*120);}
 }
}
}
