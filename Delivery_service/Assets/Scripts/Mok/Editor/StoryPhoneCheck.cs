#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
public static class StoryPhoneCheck {
 static string log;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/StoryPhone-check.txt",log);}
 static IEnumerator Wait(float time){float until=Time.time+time;while(Time.time<until)yield return null;}
 static IEnumerator Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
 public static IEnumerator Readability(){
 log="Phone readability and unlock checks\n";yield return Wait(.5f);var inbox=StoryPhoneInbox.Current;inbox.AutoOpenEnabled=false;var b=BrokerMission.Current;var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();phone.ClosePhone();phone.TogglePhone();yield return Wait(.2f);planner.Show(NavigationPlanner.Screen.Missions);var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>();Check(app.VisibleMissionCount==1,"initial list hides all future missions");app.Move(-1);Check(app.Selection==0,"list upper edge does not wrap");yield return Wait(.2f);BrokerMissionCheck.Capture(b,"Phone-missions-locked");typeof(BrokerMission).GetProperty("State").SetValue(b,BrokerMission.Phase.Complete);yield return null;app.Refresh();Check(app.VisibleMissionCount==2,"completion reveals next mission below");app.Move(1);Check(app.Selection==1&&app.rows[0].text.Contains("COMPLETED"),"next mission selected downward and prior mission marked complete");yield return Wait(.2f);BrokerMissionCheck.Capture(b,"Phone-missions-redesign");inbox.SendFrom("정비사","탈출 차량 대기 중","합류 지점에서 기다릴게. 서비스 통로를 빠져나와 골목 끝 차량으로 와.");var msg=inbox.Messages[0];Check(msg.sender=="정비사"&&!msg.read&&!msg.localConversation,"remote mechanic message remains unread");planner.Show(NavigationPlanner.Screen.Messages);app.Refresh();yield return Wait(.2f);BrokerMissionCheck.Capture(b,"Phone-message-list");app.OpenMessage(msg);yield return Wait(1.2f);Check(msg.read&&app.heading.text.Contains("정비사")&&app.detailPaper.gameObject.activeSelf,"reading card uses actual sender and marks message read");yield return Wait(.2f);BrokerMissionCheck.Capture(b,"Phone-message-redesign");phone.ClosePhone();
 }
 public static IEnumerator Verify(){
  StoryPhoneInbox.Current.AutoOpenEnabled=false;
  UnityEngine.Object.FindFirstObjectByType<PhoneController>().ClosePhone();
  log="Story phone integration\n";yield return Wait(1);var m=BrokerMission.Current;var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();var inbox=StoryPhoneInbox.Current;
  Check(planner.storyAppsPrefab&&inbox,"saved phone prefab and inbox references");Check(inbox.Messages.Count==1&&inbox.Unread==1,"daytime invitation sent once");
  phone.TogglePhone();yield return Wait(.4f);planner.Show(NavigationPlanner.Screen.Missions);var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>();Check(app,"mission app opens on handset");yield return Press(Key.Enter);Check(app.Detail&&app.action.text.Contains("18시"),"Enter opens details; daytime start locked");yield return Press(Key.Enter);Check(!m.Running,"daytime confirm cannot start mission");
  planner.Show(NavigationPlanner.Screen.Messages);app.Confirm();yield return Wait(1.2f);Check(inbox.Unread==0&&app.body.text.Contains("실장"),"text body readable and marked read");
  m.day.EnterNightPreparation();yield return Wait(.3f);Check(inbox.Messages.Count==2,"night readiness notification delivered once");
  planner.Show(NavigationPlanner.Screen.Missions);yield return Press(Key.Enter);yield return Press(Key.Enter);yield return Wait(.2f);Check(m.Running&&!phone.IsPhoneOpen()&&m.player.CanAct,"phone starts mission and releases movement/combat");
  Check(m.marker.GetComponentInChildren<StoryTargetDiamond>(true),"saved orange target diamond");Check(m.subtitle.font.name.Contains("Nanum")&&m.objective.font.name.Contains("Nanum"),"different Korean font on story dialogue and objectives");
  var p=m.marker.rectTransform.anchorMin;Check(p.x>=.08f&&p.x<=.92f&&p.y>=.16f&&p.y<=.82f,"target marker stays in screen safe area");BrokerMissionCheck.Capture(m,"StoryPhone-target");
  var pc=m.day.pcUIPanel.GetComponent<NightPreparationView>();pc.Refresh();Check(!pc.startButton.gameObject.activeSelf&&pc.missionButtons.All(b=>!b.gameObject.activeSelf),"PC mission controls removed");pc.StartSelected();Check(m.Running,"old PC start path cannot cancel active mission");
  m.Cancel();yield return Wait(.2f);Check(inbox.Messages.Count==3&&inbox.Messages[0].title.Contains("괜찮아"),"failure sends retry text");
  phone.TogglePhone();yield return Wait(.3f);planner.Show(NavigationPlanner.Screen.Missions);app.Confirm();app.Confirm();yield return Wait(.1f);Check(m.Running,"phone retry starts mission");
  // 전투와 실제 보행 경로는 별도 BrokerMissionCheck에서 검증한다.
  var cc=m.player.GetComponent<CharacterController>();cc.enabled=false;m.player.transform.position=m.broker.position+Vector3.right*2+Vector3.up;cc.enabled=true;m.player.View.transform.LookAt(m.broker.position+Vector3.up*1.2f);yield return Wait(.3f);Check(m.Talk(),"broker dialogue still reachable after phone start");m.AdvanceDialogue(true);yield return Wait(.1f);
  typeof(BrokerMission).GetMethod("SetPhase",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(m,new object[]{BrokerMission.Phase.Debrief});m.AdvanceDialogue(true);yield return Wait(.2f);
  Check(m.Completed&&inbox.Messages[0].title.Contains("특별 주문"),"completion sends next preparation message");int count=inbox.Messages.Count;yield return Wait(.3f);Check(inbox.Messages.Count==count,"notifications do not repeat every frame");
  phone.TogglePhone();yield return Wait(.4f);planner.Show(NavigationPlanner.Screen.Messages);app.Confirm();yield return Wait(.3f);BrokerMissionCheck.Capture(m,"StoryPhone-message");planner.Show(NavigationPlanner.Screen.Missions);app.Move(1);app.Confirm();Check(app.action.text.Contains("준비 중"),"unimplemented followup clearly locked");app.Confirm();Check(m.Completed,"future mission cannot reset completion");
  inbox.Send("연락 확인","테스트 문자");planner.Show(NavigationPlanner.Screen.Home);yield return Wait(.3f);Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t=>t.text.StartsWith("문자 (")),"home shows unread message badge");planner.MoveSelection(-2);Check(planner.Selection==6,"home grid reaches mission app with up arrow movement");planner.Confirm();Check(planner.CurrentScreen==NavigationPlanner.Screen.Missions,"home selection opens mission app");planner.Show(NavigationPlanner.Screen.Home);yield return Wait(.3f);BrokerMissionCheck.Capture(m,"StoryPhone-home");
 }
}
}
#endif
