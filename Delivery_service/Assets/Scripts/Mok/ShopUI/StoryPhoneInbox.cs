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
 [TextArea] public string invitation="실장이 연락했어. 은행 일의 배후를 알고 있대. 낮에는 가게를 지키고, 18시 이후에 만나 보자. 미션 앱에 위치와 준비 내용을 보냈어.";
 [TextArea] public string nightMessage="영업 끝났지? 이제 실장을 만나러 가자. 휴대폰 미션 앱에서 ‘연락이 끊긴 중개인’을 시작해. 남은 배달이 있으면 먼저 마쳐 줘.";
 [TextArea] public string followup="박진웅의 행사 정보와 관계자 연락처를 받았어. 다음 목표는 정식 피자 납품으로 행사장에 들어갈 자격을 만드는 거야. 휴대폰에서 ‘특별 주문’을 시작해. 실장이 소개한 납품 담당자를 만나 시식 납품부터 해 보자.";
 public readonly List<Message> Messages=new();
 public int Unread=>Messages.FindAll(m=>!m.read).Count;
 bool introduced,nightSent;BrokerMission.Phase previous;float until;AudioSource sound;AudioClip tone;
 void Awake(){Current=this;toast.SetActive(false);sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;sound.volume=.18f;var samples=new float[11025];for(int i=0;i<samples.Length;i++){float t=i/22050f;float envelope=Mathf.Sin(Mathf.PI*Mathf.Clamp01((t% .25f)/.18f));samples[i]=t% .25f<.18f?Mathf.Sin(2*Mathf.PI*(t<.25f?660:880)*t)*envelope*.4f:0;}tone=AudioClip.Create("Story message chime",samples.Length,1,22050,false);tone.SetData(samples,0);}
 void OnDestroy(){if(Current==this)Current=null;if(tone)Destroy(tone);}
 public void Send(string title,string body,bool missionUpdate=false){
  bool local=IsMingiNearby;var message=new Message{sender="민기",title=title,body=body,day=cycle.currentDay,localConversation=local,read=local,missionUpdate=missionUpdate};Messages.Insert(0,message);
  if(local&&nearbyPanel){nearbySubtitle.text="민기: "+body;nearbyPanel.SetActive(true);nearbyUntil=Time.time+Mathf.Clamp(body.Length*.065f,6,14);if(missionUpdate)pending.Enqueue(message);}
  else{toastText.text="새 문자 · 민기\n"+title;toast.SetActive(true);until=Time.unscaledTime+5;sound.PlayOneShot(tone);pending.Enqueue(message);}
 }
 void PresentPending(){
  if(nearbyPanel&&nearbyPanel.activeSelf&&Time.time>=nearbyUntil)nearbyPanel.SetActive(false);
  if(nearbyPanel&&nearbyPanel.activeSelf&&IsMingiNearby&&!StoryTutorial.Active){Vector3 direction=mission.player.transform.position-mingi.position;direction.y=0;if(direction.sqrMagnitude>.1f)mingi.rotation=Quaternion.RotateTowards(mingi.rotation,Quaternion.LookRotation(direction),180*Time.deltaTime);}
  if(!AutoOpenEnabled||pending.Count==0||StoryTutorial.Active||!mission||cycle.IsTransitioning||cycle.pcUIPanel.activeSelf||PoliceControl.IsArrestInProgress||mission.player.IsDown||mission.player.WaterRescueActive)return;
  if(VenueFinale.Active)return;
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
  if(!introduced){introduced=true;Send("연락이 끊긴 중개인",invitation,true);}
  if(cycle.IsNightPreparation&&!nightSent&&!mission.Completed){nightSent=true;Send("실장을 만나러 가자",nightMessage,true);}
  if(previous==mission.State)return;previous=mission.State;
  if(mission.Completed)Send("다음 준비 · 특별 주문",followup,true);
  else if(mission.State==BrokerMission.Phase.Failed)Send("괜찮아? 다시 준비하자",mission.FailureReason+"\n몸을 추스르고 미션 앱에서 다시 시작해. 밤이 끝나기 전이라면 다시 만날 수 있어.");
 }
}
}
