using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 문자와 읽음 상태는 현재 게임 세션 동안 유지한다.
public sealed class StoryPhoneInbox : MonoBehaviour {
 [Serializable] public class Message {public string sender,title,body;public int day;public bool read,localConversation,missionUpdate;}
 public static StoryPhoneInbox Current {get;private set;}
 public DayNightCycle cycle;public BrokerMission mission;public GameObject toast;public Text toastText;
 public Transform mingi;public GameObject nearbyPanel;public Text nearbySubtitle;
 public bool AutoOpenEnabled {get;set;}=true;
 readonly Queue<Message> pending=new();float nearbyUntil;
 DeliveryService.Mok.Navigation.NavigationPlanner planner;PhoneController phone;
 public bool IsMingiNearby=>mingi&&mingi.gameObject.activeInHierarchy&&mission&&Vector3.Distance(mingi.position,mission.player.transform.position)<8;
 [TextArea] public string invitation="나 실장이야 우리 조직 움직임이 이상해 놈들이 날 없애려고 해\n내일 밤에 표시한 장소로 와 민기와 너 말고는 아무에게도 말하지 말고.";
 [TextArea] public string nightMessage="어젯밤 실장이 보낸 약속, 오늘이야. 왜 연락을 끊었는지 직접 들어 보자. 남은 배달을 정리하고 미션 앱에서 ‘연락이 끊긴 중개인’을 시작해.";
 [TextArea] public string followup="실장이 준 연락처로 내일 시식 납품을 잡았어. 박진웅에게 대놓고 다가갈 순 없어. 정식 배달원으로 들어가 먼저 속내를 알아내자. 내일 낮에는 가게를 열고, 마감 후 ‘특별 주문’을 준비해.";
 public readonly List<Message> Messages=new();
 public int Unread=>Messages.FindAll(m=>!m.read).Count;
 readonly HashSet<string> readyNotices=new();
 bool introduced,nightSent;BrokerMission.Phase previous;float until;AudioSource sound;AudioClip tone;
 void Awake(){Current=this;toast.SetActive(false);sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;sound.volume=.18f;var samples=new float[11025];for(int i=0;i<samples.Length;i++){float t=i/22050f;float envelope=Mathf.Sin(Mathf.PI*Mathf.Clamp01((t% .25f)/.18f));samples[i]=t% .25f<.18f?Mathf.Sin(2*Mathf.PI*(t<.25f?660:880)*t)*envelope*.4f:0;}tone=AudioClip.Create("Story message chime",samples.Length,1,22050,false);tone.SetData(samples,0);}
 void OnDestroy(){if(Current==this)Current=null;if(tone)Destroy(tone);}
 public void Send(string title,string body,bool missionUpdate=false){
  SendMessage("민기",title,body,missionUpdate,IsMingiNearby);
 }
 public void SendFrom(string sender,string title,string body,bool missionUpdate=false){SendMessage(sender,title,body,missionUpdate,false);}
 // 납품 직후 도착한 차량 안내는 다른 대기 문자보다 먼저 보여 준다.
 public void SendVehicleReady(){SendFrom("정비사","차량 전달 완료","골목에 차와 열쇠를 두고 먼저 빠졌어. 들키면 표시한 차로 와. 직접 운전해서 수리점 인근 폐차 대기 지점에 버려. 가게까지 몰고 가면 추적당할 수 있어.");var latest=Messages[0];var older=pending.ToArray();pending.Clear();pending.Enqueue(latest);foreach(var item in older)if(item!=latest)pending.Enqueue(item);nearbyUntil=0;}
 void SendMessage(string sender,string title,string body,bool missionUpdate,bool local){
  var message=new Message{sender=sender,title=title,body=body,day=cycle.currentDay,localConversation=local,read=local,missionUpdate=missionUpdate};Messages.Insert(0,message);
  if(local&&nearbyPanel){nearbySubtitle.text="민기: "+body;nearbyPanel.SetActive(true);nearbyUntil=Time.time+Mathf.Clamp(body.Length*.065f,6,14);if(missionUpdate)pending.Enqueue(message);}
  else{toastText.text="새 문자 · "+sender+"\n"+title;toast.SetActive(true);until=Time.unscaledTime+5;sound.PlayOneShot(tone);pending.Enqueue(message);}
 }
 void PresentPending(){
  if(nearbyPanel&&nearbyPanel.activeSelf&&Time.time>=nearbyUntil)nearbyPanel.SetActive(false);
  if(nearbyPanel&&nearbyPanel.activeSelf&&IsMingiNearby&&!StoryTutorial.Active){Vector3 direction=mission.player.transform.position-mingi.position;direction.y=0;if(direction.sqrMagnitude>.1f)mingi.rotation=Quaternion.RotateTowards(mingi.rotation,Quaternion.LookRotation(direction),180*Time.deltaTime);}
  if(!AutoOpenEnabled||pending.Count==0||StoryTutorial.Active||!mission||cycle.IsTransitioning||cycle.pcUIPanel.activeSelf||PoliceControl.IsArrestInProgress||mission.player.IsDown||mission.player.WaterRescueActive)return;
  if(VenueFinale.Active&&VenueFinale.Current.State!=VenueFinale.Phase.Entrance&&!(VenueFinale.Current.State==VenueFinale.Phase.Observe&&pending.Peek().sender=="정비사"&&pending.Peek().title=="차량 전달 완료"))return;
  if(SpecialOrderMission.Active&&SpecialOrderMission.Current.InDialogue)return;
  if(MechanicMission.Active&&(MechanicMission.Current.InDialogue||MechanicMission.Current.ThreatNearby))return;
  if(BackdoorMission.Active&&(BackdoorMission.Current.InDialogue||BackdoorMission.Current.Alerted))return;
  if(mission.State==BrokerMission.Phase.Conversation||mission.State==BrokerMission.Phase.Debrief||Time.time<nearbyUntil)return;
  if(!planner)planner=FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>();if(!phone)phone=FindFirstObjectByType<PhoneController>();
  // 주행 중에는 기존 전용 네비를 유지하고 하차하면 자동으로 연락을 펼친다.
  if(!planner||!phone||planner.IsRiding||phone.IsPhoneOpen()||!mission.player.CanAct||mission.player.IsKicking)return;
  var next=pending.Peek();if(next.read&&!next.localConversation){pending.Dequeue();return;}
  if(planner.OpenIncoming(next))pending.Dequeue();
 }
 void Update(){
  if(Time.timeScale<=0)return;
  PresentPending();
  if(toast.activeSelf&&Time.unscaledTime>=until)toast.SetActive(false);
  if(StoryTutorial.Active||!mission)return;
  if(!introduced){introduced=true;SendFrom("실장","연락이 끊긴 중개인",invitation,true);}
  if(cycle.IsNightPreparation&&!nightSent&&!mission.Completed){nightSent=true;Send("실장을 만나러 가자",nightMessage,true);}
  if(cycle.IsNightPreparation){
 var special=SpecialOrderMission.Current;var backdoor=BackdoorMission.Current;var mechanic=MechanicMission.Current;
 if(mission.Completed&&special&&!special.Completed&&special.DayUnlocked&&!special.Running&&readyNotices.Add("special"))Send("오늘의 약속 · 특별 주문","시식 납품 약속이 오늘이야. 가게 이름으로 승인받으면 박진웅에게 접근할 수 있어. 미션 앱에서 ‘특별 주문’을 시작해.",true);
 else if(special&&special.Completed&&backdoor&&!backdoor.Completed&&backdoor.DayUnlocked&&!backdoor.Running&&readyNotices.Add("backdoor"))Send("오늘의 준비 · 도시의 뒷문","오늘은 행사장 주변을 살피자. 납품 출입구와 경비 시야, 탈출로를 확인해야 해. 미션 앱에서 ‘도시의 뒷문’을 시작해.",true);
 else if(backdoor&&backdoor.Completed&&mechanic&&!mechanic.Completed&&mechanic.DayUnlocked&&!mechanic.Running&&readyNotices.Add("mechanic"))Send("오늘의 약속 · 빚진 정비사","정비사와 약속한 날이야. 수리점에서 만나 탈출에 쓸 차를 부탁해 봐. 미션 앱에서 ‘빚진 정비사’를 시작해.",true);
 }
 if(previous==mission.State)return;previous=mission.State;
  if(mission.Completed)Send("다음 준비 · 특별 주문",followup,true);
  else if(mission.State==BrokerMission.Phase.Failed)Send("괜찮아? 다시 준비하자",mission.FailureReason+"\n몸을 추스르고 미션 앱에서 다시 시작해. 밤이 끝나기 전이라면 다시 만날 수 있어.");
 }
}
}
