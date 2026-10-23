#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;
using UnityEngine;using UnityEngine.AI;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
namespace DeliveryService.Mok.Editor {
public static class BackdoorCheck {
 static string log;static void Check(bool ok,string text){log+=(ok?"PASS ":"FAIL ")+text+"\n";File.WriteAllText("Logs/Backdoor-check.txt",log);if(!ok)throw new Exception("Backdoor: "+text);}
 static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
 static IEnumerator Key(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
 static void Place(PlayerCombat p,Vector3 at){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=at+Vector3.up*(cc.height*.5f-cc.center.y+.12f);cc.enabled=true;Physics.SyncTransforms();}
 static IEnumerator Walk(PlayerCombat p,Vector3 point){
  var path=new NavMeshPath();Check(NavMesh.CalculatePath(p.transform.position,point,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"connected objective walking route");var cc=p.GetComponent<CharacterController>();
  foreach(var corner in path.corners.Skip(1)){float until=Time.time+40;while(Vector2.Distance(new(p.transform.position.x,p.transform.position.z),new(corner.x,corner.z))>.4f){if(Time.time>until)throw new Exception("physical route blocked "+p.transform.position+" -> "+corner);var d=corner-p.transform.position;d.y=0;p.View.transform.rotation=Quaternion.LookRotation(d);cc.Move((d.normalized*6+Vector3.down*3)*Time.deltaTime);yield return null;}}
 }
 static IEnumerator Record(BackdoorMission m){
  m.Player.View.transform.LookAt(m.surveyTargets[m.ConfirmedCount]);yield return null;
  if(!m.CanObserve()){var cam=m.Player.View.transform;var d=m.surveyTargets[m.ConfirmedCount].position-cam.position;log+="Diagnostic "+m.State+" act="+m.Player.CanAct+" suspicion="+m.Suspicion+" camera="+cam.position+" distance="+d.magnitude+" dot="+Vector3.Dot(cam.forward,d.normalized)+" hits="+string.Join(";",Physics.RaycastAll(cam.position,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider.name))+"\n";}
  Check(m.CanObserve(),"current objective visible from authored stand "+m.ConfirmedCount);
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.F));yield return Wait(m.observeSeconds+.3f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
 }
 public static IEnumerator Verify(bool isolated=false){
  log="Backdoor mission full chain\n";yield return Wait(.4f);var m=BackdoorMission.Current;Check(m&&m.surveyTargets.Length==4&&m.guards.Length>0,"saved reconnaissance content exists");Check(!m.Begin(),"locked before catering approval");
  if(isolated){typeof(BrokerMission).GetProperty("State").SetValue(m.delivery.broker,BrokerMission.Phase.Complete);typeof(SpecialOrderMission).GetProperty("State").SetValue(m.delivery,SpecialOrderMission.Phase.Complete);m.delivery.broker.day.EnterNightPreparation();Check(m.delivery.Completed,"isolated approved-catering setup");}
  else yield return SpecialOrderCheck.Verify();
  var p=m.Player;var b=m.delivery.broker;var inbox=StoryPhoneInbox.Current;inbox.AutoOpenEnabled=false;var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.ClosePhone();
  var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);app.Open(false);app.Move(2);app.Confirm();app.Refresh();Check(app.action.text=="미션 시작","approval unlocks backdoor in phone");app.Confirm();yield return null;Check(m.State==BackdoorMission.Phase.Briefing,"phone begins briefing");
  var move=p.GetComponent<PlayerMove>();move.enabled=false;var look=p.View.GetComponent<CameraMove>();if(look)look.enabled=false;
  Place(p,m.reportPoint.position+m.delivery.kitchen.actor.forward*2);p.View.transform.LookAt(m.delivery.kitchen.actor.position+Vector3.up*1.2f);yield return Wait(.2f);yield return Key(UnityEngine.InputSystem.Key.Space);Check(m.State==BackdoorMission.Phase.Survey&&m.ActiveGuards.Length==1,"briefing skip gives control and activates patrol");
  int day=b.day.currentDay;b.day.OnClick_ConfirmClose();Check(!b.day.IsTransitioning&&b.day.currentDay==day,"active reconnaissance blocks settlement");
  var guard=m.ActiveGuards[0];guard.enabled=false;var agent=guard.GetComponent<NavMeshAgent>();agent.isStopped=true;agent.updateRotation=false;var combat=guard.GetComponent<CitizenCombat>();combat.ExternalAttackControl=true;
  Vector3 eyeTest=guard.transform.position+guard.transform.forward*5;if(NavMesh.SamplePosition(eyeTest,out var h,2,NavMesh.AllAreas))eyeTest=h.position;Place(p,eyeTest);guard.transform.LookAt(new Vector3(p.transform.position.x,guard.transform.position.y,p.transform.position.z));yield return null;
  Check(guard.CanSee(p),"guard sees player within view cone");var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=(guard.transform.position+p.transform.position)*.5f+Vector3.up;cover.transform.localScale=new Vector3(3,3,3);Physics.SyncTransforms();Check(!guard.CanSee(p),"solid cover blocks guard vision");UnityEngine.Object.Destroy(cover);yield return null;yield return Wait(3.3f);Check(m.Alerted&&combat.IsAggressive,"sustained detection starts existing NPC pursuit");
  int count=m.ConfirmedCount;m.Observe(true,20);Check(m.ConfirmedCount==count,"cannot record while pursued");Place(p,b.exit.position);yield return Wait(5);Check(!m.Alerted&&!combat.IsAggressive,"breaking sight clears local pursuit");guard.gameObject.SetActive(false);
  if(isolated)Place(p,m.delivery.approach.position);yield return Walk(p,m.standPoints[0].position);Time.timeScale=0;m.Observe(true,20);Check(m.ConfirmedCount==0,"pause does not record objectives");Time.timeScale=1;yield return Record(m);Check(m.ConfirmedCount==1,"held F records service entrance");BrokerMissionCheck.Capture(b,"Backdoor-entrance");
  m.Cancel();Check(m.ConfirmedCount==1&&m.State==BackdoorMission.Phase.Failed,"cancel preserves collected intelligence");Check(m.Begin(),"retry starts");if(look)look.enabled=false;Place(p,m.reportPoint.position+m.delivery.kitchen.actor.forward*2);yield return Wait(.2f);m.AdvanceDialogue(true);Check(m.State==BackdoorMission.Phase.Survey&&m.ConfirmedCount==1,"retry resumes next unrecorded objective");foreach(var g in m.ActiveGuards)g.gameObject.SetActive(false);
  Place(p,isolated?m.standPoints[0].position:b.exit.position);for(int i=1;i<4;i++){yield return Walk(p,m.standPoints[i].position);yield return Record(m);Check(m.ConfirmedCount==i+1,"objective recorded "+i);}
  Check(m.State==BackdoorMission.Phase.Return&&m.ActiveGuards.Length==0,"all four observations unlock return and clean guards");
  yield return Walk(p,b.exit.position);Place(p,m.reportPoint.position+m.delivery.kitchen.actor.forward*2);p.View.transform.LookAt(m.delivery.kitchen.actor.position+Vector3.up*1.2f);yield return Wait(.2f);yield return Key(UnityEngine.InputSystem.Key.F);Check(m.State==BackdoorMission.Phase.Debrief,"return debrief uses physical F");int money=MoneyManager.Instance.currentMoney;yield return Key(UnityEngine.InputSystem.Key.Space);Check(m.Completed&&!BackdoorMission.Active&&!m.Begin(),"completion persists and prevents replay");Check(MoneyManager.Instance.currentMoney==money,"reconnaissance does not change economy");
  app.Open(false);app.Move(2);app.Confirm();app.Refresh();Check(app.action.text.Contains("확보 완료"),"phone displays completed escape preparation");app.gameObject.SetActive(false);if(look)look.enabled=true;move.enabled=true;
 }
}
}
#endif
