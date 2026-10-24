using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 씬에서 연결한 프리팹을 기존 휴대폰 화면에 표시한다.
public sealed class StoryPhoneApp : MonoBehaviour {
 public NightPreparationPlan plan;public GameObject panel;public Text heading,body,action;public Text[] rows;public Image[] cards;public Image detailPaper;
 public bool MessagesMode {get;private set;}public int Selection {get;private set;}public bool Detail {get;private set;}
 public int VisibleMissionCount { get { int n=1;if(BrokerMission.Current&&BrokerMission.Current.Completed){n=2;if(SpecialOrderMission.Current&&SpecialOrderMission.Current.Completed){n=3;if(BackdoorMission.Current&&BackdoorMission.Current.Completed){n=4;if(MechanicMission.Current&&MechanicMission.Current.Completed)n=5;}}}return Mathf.Min(n,plan.missions.Length); } }
 bool Finished(int i){switch(plan.missions[i].id){case "finale":return VenueFinale.Current&&VenueFinale.Current.Completed;case "mechanic":return MechanicMission.Current&&MechanicMission.Current.Completed;case "backdoor":return BackdoorMission.Current&&BackdoorMission.Current.Completed;case "special_order":return SpecialOrderMission.Current&&SpecialOrderMission.Current.Completed;default:return BrokerMission.Current&&BrokerMission.Current.Completed;}}
 string ListStatus(int index){switch(plan.missions[index].id){case "finale":return FinaleStatus();case "mechanic":return MechanicStatus();case "backdoor":return BackdoorStatus();case "special_order":return SpecialStatus();default:var b=BrokerMission.Current;return b&&b.Running?"진행 중":b&&b.day.IsNightPreparation?"시작 가능":"18시 이후 시작";}}
 bool cancelConfirm;float nextRefresh,readAt;StoryPhoneInbox.Message displayedMessage;
 public void Open(bool messages){MessagesMode=messages;Selection=0;Detail=false;displayedMessage=null;cancelConfirm=false;gameObject.SetActive(true);Refresh();}
 public void OpenMessage(StoryPhoneInbox.Message message){Open(true);displayedMessage=message;Detail=true;readAt=Time.unscaledTime+1;Refresh();}
 public void Move(int delta){if(Detail)return;int count=MessagesMode?(StoryPhoneInbox.Current?.Messages.Count??0):VisibleMissionCount;if(count>0)Selection=Mathf.Clamp(Selection+delta,0,count-1);Refresh();}
 public bool Back(){if(!Detail)return false;Detail=false;displayedMessage=null;cancelConfirm=false;Refresh();return true;}
 public void Confirm(){
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
  if(!b.day.IsNightPreparation)return "18시 이후 시작";
  if(b.orders&&b.orders.HasActiveOrder)return "진행 중인 배달을 먼저 마치세요";
  if(!s.CanStart)return "지금은 시작할 수 없습니다";
  return s.State==SpecialOrderMission.Phase.Failed?"다시 시작":detail?"미션 시작":"밤 미션";
 }
 string FinaleStatus(bool detail=false){var f=VenueFinale.Current;if(!f)return "준비 중";if(f.Completed)return "첫 번째 사건 완료";if(f.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 작전 중단"):"작전 진행 중";if(!f.Ready)return "모든 밤 준비 미션 완료 필요";if(!f.CanStart)return "밤에 배달을 마치고 내려서 시작";return f.State==VenueFinale.Phase.Failed?"작전 재시도":detail?"행사 당일로 출발":"준비 완료 · 출발 가능";}
 string MechanicStatus(bool detail=false){var job=MechanicMission.Current;if(!job)return "준비 중";if(job.Completed)return "탈출 차량 지원 확보 완료";if(job.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):"진행 중";if(!job.preparation.Completed)return "도시의 뒷문 완료 후 시작";if(!job.preparation.delivery.broker.day.IsNightPreparation)return "18시 이후 시작";if(!job.CanStart)return "지금은 시작할 수 없습니다";return job.State==MechanicMission.Phase.Failed?"다시 시작":detail?"미션 시작":"밤 미션";}
 string BackdoorStatus(bool detail=false){
  var m=BackdoorMission.Current;if(!m)return "준비 중";if(m.Completed)return "출입구·탈출 동선 확보 완료";
  if(m.Running)return detail?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):"조사 중 · "+m.ConfirmedCount+"/4";
  if(!m.delivery.Completed)return "정식 납품 승인 후 시작";if(!m.delivery.broker.day.IsNightPreparation)return "18시 이후 시작";
  if(m.delivery.broker.orders&&m.delivery.broker.orders.HasActiveOrder)return "진행 중인 배달을 먼저 마치세요";
  if(!m.CanStart)return "지금은 시작할 수 없습니다";
  return m.State==BackdoorMission.Phase.Failed?"다시 시작 · 확인한 정보 유지":detail?"미션 시작":"밤 미션";
 }
 public void Refresh(){
  var inbox=StoryPhoneInbox.Current;var m=BrokerMission.Current;if(detailPaper)detailPaper.gameObject.SetActive(Detail);
  action.transform.parent.gameObject.SetActive(Detail&&(!MessagesMode||displayedMessage!=null&&displayedMessage.missionUpdate));
  body.rectTransform.anchoredPosition=new Vector2(Detail?44:28,Detail?-170:-770);body.rectTransform.sizeDelta=new Vector2(Detail?452:484,Detail?580:110);
  heading.text=MessagesMode?"연락함 · 안읽음 "+(inbox?.Unread??0):"밤 준비 미션";body.text="";action.text="";
  int count=MessagesMode?(inbox?.Messages.Count??0):VisibleMissionCount;
  Selection=Mathf.Clamp(Selection,0,Mathf.Max(0,count-1));int first=Mathf.Max(0,Selection-rows.Length+1);
  for(int i=0;i<rows.Length;i++){int index=first+i;cards[i].gameObject.SetActive(!Detail&&index<count);if(index>=count)continue;cards[i].color=index==Selection?new Color(1,.76f,.43f):Color.white;
   if(MessagesMode){var msg=inbox.Messages[index];rows[i].supportRichText=true;rows[i].text="<size=22>"+msg.sender+"  ·  "+(msg.read?"읽음":"새 문자")+"</size>\n"+msg.title+"\n<size=20>"+(msg.read?"읽음":"● 안읽음")+" · "+(msg.localConversation?"대화":"문자")+" · DAY "+msg.day.ToString("00")+"</size>";rows[i].color=msg.read?new Color(.30f,.36f,.40f):new Color(.07f,.13f,.20f);}
   else if(plan.missions[index].id=="finale"){rows[i].text=plan.missions[index].title+"\n"+FinaleStatus();}
   else if(plan.missions[index].id=="mechanic"){rows[i].text=plan.missions[index].title+"\n"+MechanicStatus();}
   else if(plan.missions[index].id=="backdoor"){rows[i].text=plan.missions[index].title+"\n"+BackdoorStatus();}
   else if(plan.missions[index].id=="special_order"){rows[i].text=plan.missions[index].title+"\n"+SpecialStatus();}
   else {var item=plan.missions[index];rows[i].text=item.title+"\n"+(index==0&&m?(m.Completed?"완료":m.Running?"진행 중":m.State==BrokerMission.Phase.Failed?"재시도":m.day.IsNightPreparation?"밤 미션":"18시 이후 시작"):"준비 중");}
  }
  for(int i=0;i<rows.Length;i++){int index=first+i;if(index>=count)continue;rows[i].supportRichText=true;if(!MessagesMode){bool done=Finished(index);rows[i].text="<size=20>"+(done?"완료  /  COMPLETED":"MISSION "+(index+1).ToString("00"))+"</size>\n"+plan.missions[index].title+"\n<size=20>"+(done?"준비 성과 확보":ListStatus(index))+"</size>";rows[i].color=done?new Color(.18f,.42f,.34f):new Color(.1f,.17f,.21f);if(done&&index!=Selection)cards[i].color=new Color(.83f,.92f,.87f);}}
  if(!Detail){body.text=count==0?"아직 도착한 문자가 없습니다.":MessagesMode?"최근 연락부터 표시됩니다.\n"+(Selection+1)+" / "+count:"해금된 미션  "+count+"개   ·   완료 "+System.Linq.Enumerable.Count(System.Linq.Enumerable.Range(0,count),Finished)+"개\n"+(Selection+1)+" / "+count;return;}
  if(count==0)return;
  if(MessagesMode){var msg=displayedMessage??inbox.Messages[Selection];heading.text=msg.sender+" · "+(msg.localConversation?"대화 기록":msg.read?"읽음":"안읽음");body.text="<size=22><color=#597077>DAY "+msg.day.ToString("00")+"  ·  "+(msg.localConversation?"대화 기록":"문자 메시지")+"</color></size>\n\n<b>"+msg.title+"</b>\n\n"+msg.body;action.text=msg.missionUpdate?"미션 확인":"";return;}
  var selected=plan.missions[Selection];heading.text=(Finished(Selection)?"완료  ·  ":"")+selected.title;body.text=selected.description+"\n\n준비 성과\n"+selected.result;
  if(Finished(Selection)){action.text="완료 · 준비 성과 확보";return;}
  if(selected.id=="finale"){action.text=FinaleStatus(true);return;}
  if(selected.id=="mechanic"){action.text=MechanicStatus(true);return;}
  if(selected.id=="backdoor"){action.text=BackdoorStatus(true);return;}
  if(selected.id=="special_order"){body.text+="\n\n치즈피자 준비 중 기본 재료 무료 지원\n진행 중에는 아래에서 중단·재시도 가능";action.text=SpecialStatus(true);return;}
  action.text=Selection!=0?"아직 준비 중인 미션입니다":!m?"미션을 불러올 수 없습니다":m.Completed?"준비 완료":m.Running?(cancelConfirm?"한 번 더 확인하면 중단합니다":"진행 중 · 미션 중단"):!m.day.IsNightPreparation?"18시 이후 시작할 수 있습니다":m.orders&&m.orders.HasActiveOrder?"진행 중인 배달을 먼저 마치세요":!m.CanStart?"지금은 시작할 수 없습니다":m.State==BrokerMission.Phase.Failed?"다시 시작":"미션 시작";
 }
}
}
