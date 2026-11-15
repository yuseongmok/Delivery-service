using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 씬에서 연결한 프리팹을 기존 휴대폰 화면에 표시한다.
public sealed class StoryPhoneApp : MonoBehaviour {
 int originalBodySize;float originalLineSpacing;bool originalBestFit;int originalMin,originalMax;
 void Awake(){EditableStoryText.Bind(this);originalBodySize=body.fontSize;originalLineSpacing=body.lineSpacing;originalBestFit=body.resizeTextForBestFit;originalMin=body.resizeTextMinSize;originalMax=body.resizeTextMaxSize;}
 public NightPreparationPlan plan;public GameObject panel;public Text heading,body,action;public Text[] rows;public Image[] cards;public Image detailPaper;
 public bool MessagesMode {get;private set;}public int Selection {get;private set;}public bool Detail {get;private set;}
 public int VisibleMissionCount { get { if(plan==null||plan.missions==null||StoryTutorial.Active)return 0;int n=1;if(BrokerMission.Current&&BrokerMission.Current.Completed){n=2;if(SpecialOrderMission.Current&&SpecialOrderMission.Current.Completed){n=3;if(BackdoorMission.Current&&BackdoorMission.Current.Completed){n=4;if(MechanicMission.Current&&MechanicMission.Current.Completed)n=5;}}}return Mathf.Min(n,plan.missions.Length); } }
 bool Finished(int i){switch(plan.missions[i].id){case "finale":return VenueFinale.Current&&VenueFinale.Current.Completed;case "mechanic":return MechanicMission.Current&&MechanicMission.Current.Completed;case "backdoor":return BackdoorMission.Current&&BackdoorMission.Current.Completed;case "special_order":return SpecialOrderMission.Current&&SpecialOrderMission.Current.Completed;default:return BrokerMission.Current&&BrokerMission.Current.Completed;}}
 string ListStatus(int index){switch(plan.missions[index].id){case "finale":return FinaleStatus();case "mechanic":return MechanicStatus();case "backdoor":return BackdoorStatus();case "special_order":return SpecialStatus();default:var b=BrokerMission.Current;return b&&b.Running?"진행 중":b&&b.CanStart?"시작 가능":"튜토리얼 완료 후 시작";}}
 bool cancelConfirm;float nextRefresh,readAt;StoryPhoneInbox.Message displayedMessage;
 public void Open(bool messages){MessagesMode=messages;Selection=messages?0:Mathf.Max(0,VisibleMissionCount-1);Detail=false;displayedMessage=null;cancelConfirm=false;gameObject.SetActive(true);Refresh();}
 public void OpenMessage(StoryPhoneInbox.Message message){Open(true);displayedMessage=message;Detail=true;readAt=Time.unscaledTime+1;Refresh();}
 public void Move(int delta){if(Detail)return;int count=MessagesMode?(StoryPhoneInbox.Current?.Messages.Count??0):VisibleMissionCount;if(count>0)Selection=Mathf.Clamp(Selection+delta,0,count-1);Refresh();}
 public bool Back(){if(!Detail)return false;Detail=false;displayedMessage=null;cancelConfirm=false;Refresh();return true;}
 public void Confirm(){ if(GameModeSession.IsChallenge)return; if(PrisonMission.Current){Detail=!Detail;Refresh();return;}
  if(!MessagesMode&&VisibleMissionCount==0)return;
  if(!Detail){Detail=true;if(MessagesMode&&StoryPhoneInbox.Current.Messages.Count>0){displayedMessage=StoryPhoneInbox.Current.Messages[Selection];readAt=Time.unscaledTime+1;}Refresh();return;}
  if(MessagesMode){if(displayedMessage!=null&&displayedMessage.missionUpdate)FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>().Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);return;}
  var m=BrokerMission.Current;
  if(Finished(Selection))return;
  if(plan.missions[Selection].id=="finale"){var job=VenueFinale.Current;if(!job)return;if(job.Running){if(!cancelConfirm){cancelConfirm=true;Refresh();return;}job.Cancel();cancelConfirm=false;Refresh();return;}if(job.Begin()){FindFirstObjectByType<PhoneController>()?.ClosePhone();gameObject.SetActive(false);}else Refresh();return;}
  if(plan.missions[Selection].id=="mechanic"){var job=MechanicMission.Current;if(!job)return;if(job.Running){if(!cancelConfirm){cancelConfirm=true;Refresh();return;}job.Cancel();cancelConfirm=false;Refresh();return;}if(job.Begin()){FindFirstObjectByType<PhoneController>()?.ClosePhone();gameObject.SetActive(false);}else Refresh();return;}
  if(plan.missions[Selection].id=="backdoor"){
   var recon=BackdoorMission.Current;if(!recon)return;
   if(recon.Running){if(!cancelConfirm){cancelConfirm=true;Refresh();return;}recon.Cancel();cancelConfirm=false;Refresh();return;}
   if(recon.Begin()){FindFirstObjectByType<PhoneController>()?.ClosePhone();gameObject.SetActive(false);}else Refresh();return;
  }
  if(plan.missions[Selection].id=="special_order"){
   var special=SpecialOrderMission.Current;if(!special)return;
   if(special.Running){if(!cancelConfirm){cancelConfirm=true;Refresh();return;}special.Cancel();cancelConfirm=false;Refresh();return;}
   if(special.Begin()){FindFirstObjectByType<PhoneController>()?.ClosePhone();gameObject.SetActive(false);}else Refresh();return;
  }
  if(Selection!=0||!m)return;
  if(m.Running){if(!cancelConfirm){cancelConfirm=true;Refresh();return;}m.Cancel();cancelConfirm=false;Refresh();return;}
  if(m.Begin()){FindFirstObjectByType<PhoneController>()?.ClosePhone();gameObject.SetActive(false);}else Refresh();
 }
 void Update(){if(Detail&&MessagesMode&&displayedMessage!=null&&Time.timeScale>0&&Time.unscaledTime>=readAt){var phone=FindFirstObjectByType<PhoneController>();if(phone&&phone.IsPhoneOpen())displayedMessage.read=true;}if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}}
 string SpecialStatus(bool detail=false){
  var s=SpecialOrderMission.Current;var b=BrokerMission.Current;
  if(!s)return "준비 중";if(s.Completed)return "정식 납품 승인 완료";
  if(s.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):"진행 중";
  if(!b||!b.Completed)return "실장 구출 후 시작";
  if(!s.DayUnlocked)return "이전 준비 완료 후 시작";
  if(!s.CanStart)return "지금은 시작할 수 없습니다";
  return s.State==SpecialOrderMission.Phase.Failed?"다시 시작":detail?"미션 시작":"시작 가능";
 }
 string FinaleStatus(bool detail=false){var f=VenueFinale.Current;if(!f)return "준비 중";if(f.Completed)return "첫 번째 사건 완료";if(f.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 작전 중단"):"작전 진행 중";if(!f.Ready)return "납품 승인·현장 조사·차량 지원 필요";if(!f.CanStart)return "지금은 시작할 수 없습니다. 탑승 중이면 내려 주세요.";return f.State==VenueFinale.Phase.Failed?"작전 재시도":detail?"행사장으로 출발":"준비 완료 · 출발 가능";}
 string MechanicStatus(bool detail=false){var job=MechanicMission.Current;if(!job)return "준비 중";if(job.Completed)return "탈출 차량 지원 확보 완료";if(job.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):"진행 중";if(!job.preparation.Completed)return "도시의 뒷문 완료 후 시작";if(!job.DayUnlocked)return "이전 준비 완료 후 시작";if(!job.CanStart)return "지금은 시작할 수 없습니다";return job.State==MechanicMission.Phase.Failed?"다시 시작":detail?"미션 시작":"시작 가능";}
 string BackdoorStatus(bool detail=false){
  var m=BackdoorMission.Current;if(!m)return "준비 중";if(m.Completed)return "출입구·탈출 동선 확보 완료";
  if(m.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):"조사 중 · "+m.ConfirmedCount+"/4";
  if(!m.delivery.Completed)return "정식 납품 승인 후 시작";if(!m.DayUnlocked)return "이전 준비 완료 후 시작";
  if(!m.CanStart)return "지금은 시작할 수 없습니다";
  return m.State==BackdoorMission.Phase.Failed?"다시 시작 · 확인한 정보 유지":detail?"미션 시작":"시작 가능";
 }
 void RefreshPrison(){
  var presentation=FindFirstObjectByType<PrisonSharedPresentation>();
  heading.text=MessagesMode?"연락함":"감옥 · 현재 이야기";
  if(detailPaper)detailPaper.gameObject.SetActive(true);
  action.transform.parent.gameObject.SetActive(false);
  foreach(var card in cards)card.gameObject.SetActive(false);
  body.rectTransform.anchoredPosition=new Vector2(44,-170);body.rectTransform.sizeDelta=new Vector2(452,580);
  if(MessagesMode){body.text="아직 도착한 문자가 없습니다.";return;}
  string goal=presentation?presentation.objective.text:PrisonMission.Current.dialogue.objectives[(int)PrisonMission.Current.State];
  string reason="감옥에서 벗어날 방법을 찾기 위해 민기와 정보를 모으자.";
  if(presentation){
   if(presentation.escape&&presentation.escape.Active)reason="확보한 간수복과 차량 열쇠로 검문을 통과하고 민기와 함께 탈출하자.";
   else if(presentation.office&&presentation.office.Active)reason="탈출에 필요한 간수복과 차량 열쇠, 검문 정보를 찾아야 한다.";
   else if(presentation.keys&&presentation.keys.Active)reason="창고 재료로 피자를 만들어 간수들에게 대접하고 간수장실에 접근하자.";
   else if(presentation.medicine&&presentation.medicine.Active)reason="간수들의 경계를 피해 간수장실에 들어갈 수단이 필요하다. 수면제를 가진 죄수를 찾자.";
   else if(presentation.chicken&&presentation.chicken.Active)reason="최형욱은 치킨을 대가로 정보를 주겠다고 했다. 주방장을 깨우지 않고 치킨을 준비하자.";
   else if(presentation.mission.introduction)reason="민기가 알아낸 정보통 최형욱을 만나 탈출의 단서를 얻자.";
  }
  bool complete=presentation&&presentation.escape&&presentation.escape.State==PrisonEscapeMission.Stage.Complete;
  body.text=complete?"<color=#317260><b>탈옥 완료</b></color>\n\n민기와 함께 감옥을 빠져나왔습니다.":"<b>현재 목표</b>\n"+goal+"\n\n<b>이 미션을 하는 이유</b>\n"+reason;
 }
 public void Refresh(){ body.fontSize=originalBodySize;body.lineSpacing=originalLineSpacing;body.resizeTextForBestFit=originalBestFit;body.resizeTextMinSize=originalMin;body.resizeTextMaxSize=originalMax; if(GameModeSession.IsChallenge){
  heading.text=MessagesMode?"연락함":"도전 모드";
  foreach(var card in cards)card.gameObject.SetActive(false);
  if(detailPaper)detailPaper.gameObject.SetActive(true);
  action.transform.parent.gameObject.SetActive(false);
  body.rectTransform.anchoredPosition=new Vector2(44,-170);body.rectTransform.sizeDelta=new Vector2(452,580);
  body.text=MessagesMode?"새로운 스토리 연락은 없습니다.":"PC에서 영업을 시작하세요.\n\n주문 수락 → 피자 제작 → 포장 → 배달\n\n재료비와 연료비를 관리하고 매일 고정비를 납부하며 오래 살아남으세요.\n\n마감 정산 후 잔액이 음수가 되면 도전이 종료됩니다.";return;
 } if(PrisonMission.Current){RefreshPrison();return;}
 if(!MessagesMode&&StoryTutorial.Active){
  heading.text="위장 생활";foreach(var card in cards)card.gameObject.SetActive(false);
  if(detailPaper)detailPaper.gameObject.SetActive(true);action.transform.parent.gameObject.SetActive(false);
  body.rectTransform.anchoredPosition=new Vector2(44,-170);body.rectTransform.sizeDelta=new Vector2(452,580);
  body.text="<b>현재 목표</b>\n"+StoryTutorial.Current.objective.text+"\n\n<b>이 미션을 하는 이유</b>\n얼굴이 알려진 두 사람은 피자 가게에서 위장 생활을 시작했다. 첫 주문을 배달하며 가게 일을 익히자.";return;
 }
  var inbox=StoryPhoneInbox.Current;var m=BrokerMission.Current;if(detailPaper)detailPaper.gameObject.SetActive(Detail);
  action.transform.parent.gameObject.SetActive(Detail&&(!MessagesMode||displayedMessage!=null&&displayedMessage.missionUpdate));
  body.rectTransform.anchoredPosition=new Vector2(Detail?44:28,Detail?-170:-770);body.rectTransform.sizeDelta=new Vector2(Detail?452:484,Detail?580:110);
  heading.text=MessagesMode?"연락함 · 안읽음 "+(inbox?.Unread??0):"미션 기록";body.text="";action.text="";
  int count=MessagesMode?(inbox?.Messages.Count??0):VisibleMissionCount;
  Selection=Mathf.Clamp(Selection,0,Mathf.Max(0,count-1));int first=Mathf.Max(0,Selection-rows.Length+1);
  for(int i=0;i<rows.Length;i++){int index=first+i;cards[i].gameObject.SetActive(!Detail&&index<count);if(index>=count)continue;cards[i].color=index==Selection?new Color(1,.76f,.43f):Color.white;
   if(MessagesMode){var msg=inbox.Messages[index];rows[i].supportRichText=true;rows[i].text="<size=22>"+msg.sender+"  ·  "+(msg.read?"읽음":"새 문자")+"</size>\n"+msg.title+"\n<size=20>"+(msg.read?"읽음":"● 안읽음")+" · "+(msg.localConversation?"대화":"문자")+"</size>";rows[i].color=msg.read?new Color(.30f,.36f,.40f):new Color(.07f,.13f,.20f);}
   else if(plan.missions[index].id=="finale"){rows[i].text=plan.missions[index].title+"\n"+FinaleStatus();}
   else if(plan.missions[index].id=="mechanic"){rows[i].text=plan.missions[index].title+"\n"+MechanicStatus();}
   else if(plan.missions[index].id=="backdoor"){rows[i].text=plan.missions[index].title+"\n"+BackdoorStatus();}
   else if(plan.missions[index].id=="special_order"){rows[i].text=plan.missions[index].title+"\n"+SpecialStatus();}
   else {var item=plan.missions[index];rows[i].text=item.title+"\n"+(index==0&&m?(m.Completed?"완료":m.Running?"진행 중":m.State==BrokerMission.Phase.Failed?"재시도":m.CanStart?"시작 가능":"튜토리얼 완료 후 시작"):"준비 중");}
  }
  for(int i=0;i<rows.Length;i++){int index=first+i;if(index>=count)continue;rows[i].supportRichText=true;if(!MessagesMode){bool done=Finished(index);rows[i].text="<size=20>"+(done?"완료한 미션":"현재 이야기")+"</size>\n"+plan.missions[index].title+"\n<size=20>"+(done?"완료한 결과 보기":ListStatus(index))+"</size>";rows[i].color=done?new Color(.18f,.42f,.34f):new Color(.1f,.17f,.21f);if(done)cards[i].color=index==Selection?new Color(.72f,.86f,.79f):new Color(.85f,.91f,.87f);}}
  if(!Detail){body.text=count==0?(MessagesMode?"아직 도착한 문자가 없습니다.":"진행 중인 첫 배달을 마치면 다음 미션이 열립니다."):MessagesMode?"최근 연락부터 표시됩니다.\n"+(Selection+1)+" / "+count:"완료 "+System.Linq.Enumerable.Count(System.Linq.Enumerable.Range(0,count),Finished)+"개   ·   선택 "+(Selection+1)+" / "+count+"\n방향키로 선택 · Enter로 목표 확인";return;}
  if(count==0)return;
  if(MessagesMode){var msg=displayedMessage??inbox.Messages[Selection];heading.text=msg.sender+" · "+(msg.localConversation?"대화 기록":msg.read?"읽음":"안읽음");body.text="<size=22><color=#597077>"+(msg.localConversation?"대화 기록":"문자 메시지")+"</color></size>\n\n<b>"+msg.title+"</b>\n\n"+msg.body;action.text=msg.missionUpdate?"미션 확인":"";return;}
  body.supportRichText=true;body.fontSize=25;body.lineSpacing=1.12f;body.resizeTextForBestFit=true;body.resizeTextMinSize=21;body.resizeTextMaxSize=25;
  var selected=plan.missions[Selection];heading.text=(Finished(Selection)?"완료  ·  ":"")+selected.title;body.text=StoryMissionBrief.Detail(selected,Finished(Selection));
  if(Finished(Selection)){action.text="완료한 미션";action.transform.parent.gameObject.SetActive(false);return;}
  if(selected.id=="finale"){action.text=FinaleStatus(true);return;}
  if(selected.id=="mechanic"){action.text=MechanicStatus(true);return;}
  if(selected.id=="backdoor"){action.text=BackdoorStatus(true);return;}
  if(selected.id=="special_order"){action.text=SpecialStatus(true);return;}
  action.text=Selection!=0?"아직 준비 중인 미션입니다":!m?"미션을 불러올 수 없습니다":m.Completed?"준비 완료":m.Running?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):StoryTutorial.Active?"튜토리얼 완료 후 시작할 수 있습니다":!m.CanStart?"지금은 시작할 수 없습니다":m.State==BrokerMission.Phase.Failed?"다시 시작":"미션 시작";
 }
}
}
