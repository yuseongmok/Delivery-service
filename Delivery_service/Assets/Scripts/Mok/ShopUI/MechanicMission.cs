using System.Linq;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.UI;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 회수한 부품과 차량 지원은 현재 게임 세션의 준비 성과로 유지한다.
public sealed class MechanicMission:MonoBehaviour {
 public enum Phase{Available,Meet,Briefing,Retrieve,Return,Handover,Debrief,Complete,Failed}
 public static MechanicMission Current{get;private set;}public static bool Active=>Current&&Current.Running;
 public Phase State{get;private set;}public bool Running=>State>=Phase.Meet&&State<=Phase.Debrief;public bool Completed=>State==Phase.Complete;
 public bool InDialogue=>State==Phase.Briefing||State==Phase.Handover||State==Phase.Debrief;
 public bool PartRecovered{get;private set;}public bool VehicleSupportUnlocked=>Completed;
 public BackdoorMission preparation;public Transform mechanic,approach,pickupStand,caseRest,benchRest,supportVehicle,rendezvous;
 public GameObject parts,keys,ui;public Text objective,subtitle,marker;public Transform[] enemyPosts;public CitizenCombat enemyTemplate;
 public AudioSource sound;public AudioClip pickupSound,repairSound;public float handoverSeconds=2.4f;
 [TextArea(2,5)]public string[] briefing={"정비사: 민기가 보냈지? 행사장 뒤에서 기다려 줄 차가 필요하다면서.","나: 눈에 안 띄고, 확실하게 움직이는 차면 돼.\n정비사: 차는 있어. 문제는 시동 제어기야. 빚은 다 갚았는데 그놈들이 부품을 담보라고 안 돌려줘.","정비사: 표시한 골목에 공구 상자가 있어. 노란 띠에 내 정비소 표시가 붙어 있지. 그것만 찾아와 줘.","나: 가져오면?\n정비사: 부품을 달고 직접 운전할게. 행사 당일 네가 확인한 합류 지점에서 기다리겠다."};
 [TextArea(2,5)]public string[] ending={"정비사: 맞아, 이 부품이야. 이제 저 차를 다시 굴릴 수 있겠어.","나: 행사장 골목 끝, 큰길 합류 지점이야. 민기가 위치를 보내 줄 거야.\n정비사: 받았어. 신호가 오면 문 열어 두고 기다릴게.","정비사: 이건 예비 열쇠야. 이번 빚은 내가 진 걸로 하자.\n나: 그날 꼭 갚아. 시간이 없을 테니까."};
 public string FailureReason{get;private set;}public CitizenCombat[] Pursuers=>enemies;
 CitizenCombat[] enemies=new CitizenCombat[0];bool engaged;int day,line;float entered,noticeUntil;Vector3 boxStart;Quaternion boxRotation;
 public PlayerCombat Player=>preparation.Player;DayNightCycle Day=>preparation.delivery.broker.day;
 public bool CanStart=>preparation&&preparation.Completed&&!BrokerMission.Active&&!Running&&!Completed&&Day.IsNightPreparation&&!Day.IsTransitioning&&!StoryTutorial.Active&&!Player.IsDown&&!Player.WaterRescueActive&&!PoliceControl.IsArrestInProgress&&!(preparation.delivery.broker.orders&&preparation.delivery.broker.orders.HasActiveOrder);
 void Awake(){Current=this;ui.SetActive(false);parts.SetActive(false);keys.SetActive(false);mechanic.gameObject.SetActive(false);}
 void OnDestroy(){Clean();if(Current==this)Current=null;}
 public bool Begin(){if(!CanStart)return false;Clean();day=Day.currentDay;FailureReason="";engaged=false;mechanic.gameObject.SetActive(true);ui.SetActive(true);keys.SetActive(false);
  preparation.ui.SetActive(false);preparation.delivery.ui.SetActive(false);
  FindFirstObjectByType<PhoneController>()?.ClosePhone();Day.pcUIPanel.SetActive(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  foreach(var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))if(pc.pcUIPanel==Day.pcUIPanel&&pc.cameraLookScript)pc.cameraLookScript.enabled=true;
  parts.SetActive(true);parts.transform.SetPositionAndRotation(caseRest.position,caseRest.rotation);Set(PartRecovered?Phase.Return:Phase.Meet);Notice(PartRecovered?"회수한 부품을 정비사에게 전달하세요.":"민기: 정비사에게 연락해 뒀어. 수리점에서 만나 봐. 차량은 그 사람이 잘 알아.");return true;
 }
 void Set(Phase value){State=value;line=0;entered=Time.time;}
 void Notice(string text){subtitle.text=text;noticeUntil=Time.time+8;}
 bool Near(Transform target,float height=1.1f){if(!Player.CanAct||Vector3.Distance(Player.transform.position,target.position)>3.3f)return false;var from=Player.View.transform.position;var delta=target.position+Vector3.up*height-from;var inv=Player.GetComponent<ToppingInventory>();return !Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(Player.transform)&&!h.transform.IsChildOf(target)&&!h.transform.IsChildOf(parts.transform)&&!(inv&&inv.HoldPoint&&h.transform.IsChildOf(inv.HoldPoint)));}
 void ShowLine(){var lines=State==Phase.Briefing?briefing:ending;subtitle.text=lines[line]+"\n[F] 계속 · [Space] 대화 건너뛰기";}
 public bool ThreatNearby=>enemies.Any(e=>e&&e.gameObject.activeInHierarchy&&!e.GetComponent<NPCControl>().IsDead&&Vector3.Distance(e.transform.position,Player.transform.position)<16);
 public bool Interact(){
  if(!Running||Time.timeScale<=0)return false;
  if(State==Phase.Briefing||State==Phase.Debrief){AdvanceDialogue();return true;}
  if(State==Phase.Meet&&Near(mechanic)){Set(Phase.Briefing);ShowLine();return true;}
  if(State==Phase.Retrieve&&Near(parts.transform,.15f)){PartRecovered=true;Set(Phase.Return);if(sound&&pickupSound)sound.PlayOneShot(pickupSound,.4f);Engage();Notice("조직원: 그 상자 내려놔!\n민기: 받았으면 돌아와! 싸우든 따돌리든, 수리점까지 끌고 오진 마.");return true;}
  if(State==Phase.Return&&Near(mechanic)){if(ThreatNearby){Notice("정비사: 뒤에 놈들부터 떼어 내! 여기까지 데려오면 안 돼.");return false;}Clean();boxStart=parts.transform.position;boxRotation=parts.transform.rotation;Set(Phase.Handover);Notice("정비사: 잘 가져왔어. 단자부터 확인해 볼게.");if(sound&&repairSound)sound.PlayOneShot(repairSound,.3f);return true;}return false;
 }
 public void AdvanceDialogue(bool skip=false){if(Time.timeScale<=0||(State!=Phase.Briefing&&State!=Phase.Debrief)||!Near(mechanic))return;line++;var lines=State==Phase.Briefing?briefing:ending;if(!skip&&line<lines.Length){ShowLine();return;}
  if(State==Phase.Briefing){Set(Phase.Retrieve);enemies=enemyPosts.Select(t=>{var e=Instantiate(enemyTemplate,t.position,t.rotation,transform);e.gameObject.SetActive(true);e.missionEnemy=true;e.GetComponent<NPCControl>().ExternalControl=true;return e;}).ToArray();Notice("정비소 표시가 붙은 부품 상자를 회수하세요. 조직원과 싸우거나 빠져나와 돌아올 수 있습니다.");}
  else{Set(Phase.Complete);keys.SetActive(false);parts.transform.SetPositionAndRotation(benchRest.position,benchRest.rotation);Notice("빚진 정비사 · 준비 완료\n행사장 탈출 차량·운전 지원 확보");StoryPhoneInbox.Current?.Send("탈출 차량 지원 확보","정비사가 차량과 운전을 맡기로 했어. 행사 당일 네가 확인한 골목 끝 합류 지점에서 기다릴 거야. ‘빚진 정비사’ 준비 성과에 기록했어.",true);}
 }
 void Engage(){if(engaged)return;engaged=true;foreach(var e in enemies)if(e)e.Provoke(Player);}
 void Clean(){foreach(var e in enemies)if(e){e.EndFight();Destroy(e.gameObject);}enemies=new CitizenCombat[0];}
 public void DeveloperComplete(){if(Completed)return;Clean();PartRecovered=true;Set(Phase.Complete);FailureReason="";parts.SetActive(false);keys.SetActive(false);mechanic.gameObject.SetActive(false);supportVehicle.gameObject.SetActive(true);ui.SetActive(false);}
 public void Cancel(){if(Running)Fail("미션을 중단했습니다.");}
 public void Fail(string reason){if(!Running)return;Set(Phase.Failed);FailureReason=reason;Clean();parts.SetActive(false);keys.SetActive(false);mechanic.gameObject.SetActive(false);Notice("빚진 정비사 중단 · "+reason+(PartRecovered?"\n회수한 부품은 유지됩니다. 휴대폰에서 다시 시작하세요.":"\n휴대폰에서 다시 시작할 수 있습니다."));}
 void Update(){if(Time.timeScale<=0)return;if(!Running){marker.gameObject.SetActive(false);if(Time.time>noticeUntil)ui.SetActive(false);return;}
  if(Day.currentDay!=day||!Day.IsNightPreparation){Fail("밤 준비 시간이 끝났습니다.");return;}if(Player.HitsTaken>=3||Player.WaterRescueActive||PoliceControl.IsArrestInProgress){Fail("안전하게 복귀하지 못했습니다.");return;}
  if(State==Phase.Retrieve&&!engaged&&Vector3.Distance(Player.transform.position,caseRest.position)<12){Engage();Notice("조직원: 정비사 놈이 보냈냐? 그 상자에 손대지 마!");}
  if(PrologueKeyboard.Pressed(Key.F))Interact();if(PrologueKeyboard.Pressed(Key.Space))AdvanceDialogue(true);
  if(State==Phase.Handover){float t=Mathf.Clamp01((Time.time-entered)/handoverSeconds);parts.transform.position=Vector3.Lerp(boxStart,benchRest.position,Mathf.SmoothStep(0,1,t))+Vector3.up*Mathf.Sin(t*Mathf.PI)*.15f;parts.transform.rotation=Quaternion.Slerp(boxRotation,benchRest.rotation,t);if(t>=1){keys.SetActive(true);Set(Phase.Debrief);ShowLine();}}
  if(!InDialogue&&Time.time>noticeUntil)subtitle.text="";var target=State==Phase.Retrieve?parts.transform:mechanic;
  objective.text=State==Phase.Meet?"빚진 정비사 · 수리점에서 정비사 만나기":State==Phase.Retrieve?"빚진 정비사 · 부품 상자 회수하기":State==Phase.Return?"추격자를 따돌리고 정비사에게 부품 전달":State==Phase.Handover?"정비사가 회수한 부품을 확인하는 중":"빚진 정비사 · 정비사와 대화";
  if(Player.RiddenBike)objective.text+="\n도착하면 내려서 상호작용하세요";else if(State!=Phase.Handover)objective.text+="\n[F] "+(State==Phase.Retrieve?"상자 회수":State==Phase.Return?"부품 전달":"대화");
  marker.gameObject.SetActive(true);StoryTargetDiamond.Place(marker,Player.View,target.position+Vector3.up*(State==Phase.Retrieve?.55f:1.8f),Player.transform.position,State==Phase.Retrieve?"정비사의 부품":"정비사");
  var direction=Player.transform.position-mechanic.position;direction.y=0;if(direction.sqrMagnitude>.1f&&direction.sqrMagnitude<100)mechanic.rotation=Quaternion.RotateTowards(mechanic.rotation,Quaternion.LookRotation(direction),100*Time.deltaTime);
 }
 void LateUpdate(){if(!Running)return;if(State==Phase.Return){parts.SetActive(!Player.RiddenBike&&!Player.IsDown);if(parts.activeSelf)parts.transform.SetPositionAndRotation(Player.View.transform.TransformPoint(new Vector3(-.32f,-.37f,.67f)),Player.View.transform.rotation);}else if(State==Phase.Handover){parts.SetActive(true);var a=mechanic.GetComponentInChildren<Animator>();if(!a||!a.isHuman)return;var upper=a.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=a.GetBoneTransform(HumanBodyBones.RightLowerArm);var hand=a.GetBoneTransform(HumanBodyBones.RightHand);if(!upper||!lower||!hand)return;float weight=Mathf.Sin(Mathf.Clamp01((Time.time-entered)/handoverSeconds)*Mathf.PI);var direction=parts.transform.position+Vector3.up*.1f-upper.position;upper.rotation=Quaternion.Slerp(upper.rotation,Quaternion.FromToRotation(hand.position-upper.position,direction)*upper.rotation,weight*.7f);}}
}
}
