#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DeliveryService.Mok.Editor {
public static class BrokerMissionCheck {
 static string log;static void Check(bool ok,string message){if(!ok)throw new Exception("Broker: "+message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Broker-check.txt",log);}
 static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
 static IEnumerator Key(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
 static void Place(PlayerCombat p,Vector3 point){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=point+Vector3.up*(cc.height*.5f-cc.center.y+.12f);cc.enabled=true;Physics.SyncTransforms();}
 static IEnumerator Walk(PlayerCombat p,Vector3 point,float speed){
  var path=new NavMeshPath();Check(NavMesh.CalculatePath(p.transform.position,point,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"complete walk route");
  var cc=p.GetComponent<CharacterController>();float end=Time.time+65;
  foreach(var corner in path.corners.Skip(1))while(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(corner.x,corner.z))>.35f){
   if(Time.time>end)throw new Exception("Physical route blocked at "+p.transform.position+" toward "+corner+" obstacles "+string.Join(",",Physics.OverlapSphere(p.transform.position,.85f).Select(c=>c.name)));
   if(!cc.enabled||p.IsDown||(p.avatar&&p.avatar.IsPhysical)){yield return null;continue;}Vector3 d=corner-p.transform.position;d.y=0;p.View.transform.rotation=Quaternion.LookRotation(d.normalized);cc.Move((d.normalized*speed+Vector3.down*3)*Time.deltaTime);yield return null;
  }
 }
 public static void Capture(BrokerMission m,string name,Camera overrideCamera=null){
  var cam=overrideCamera?overrideCamera:m.player.View;var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();var rt=new RenderTexture(1280,720,24);var previous=cam.targetTexture;var active=RenderTexture.active;
  try{foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=Mathf.Max(.04f,cam.nearClipPlane+.01f);}cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;cam.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 public static IEnumerator Extra(){
  log="Broker failure and presentation checks\n";var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();yield return Wait(.4f);m.day.EnterNightPreparation();var ui=m.day.pcUIPanel.GetComponent<NightPreparationView>();foreach(var pc in UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsSortMode.None))pc.interactRange=1000;m.day.pcUIPanel.SetActive(true);yield return Wait(.2f);ui.SelectMission(0);Capture(m,"Broker-PC");m.Begin();yield return Wait(.1f);
  Place(m.player,m.meeting.position+Vector3.right*2);m.player.View.transform.LookAt(m.broker.position+Vector3.up*1.3f);yield return Wait(.7f);Check(m.Talk(),"nearby broker dialogue available state="+m.State+" canAct="+m.player.CanAct+" position="+m.player.transform.position+" broker="+m.broker.position+" camera="+m.player.View.transform.position);m.player.View.transform.LookAt(m.broker.position+Vector3.up*1.3f);Capture(m,"Broker-meeting");m.AdvanceDialogue(true);yield return Wait(.5f);
  int cash=MoneyManager.Instance.currentMoney;foreach(var e in m.GetComponentsInChildren<CitizenCombat>())if(e!=m.enemyTemplate)e.ExternalAttackControl=true;
  for(int i=0;i<3;i++){float end=Time.time+20;while(!m.player.CanReceiveHit&&Time.time<end)yield return null;yield return Wait(1.5f);m.player.ReceivePunch(34,Vector3.forward);yield return null;while(m.player.IsDown&&Time.time<end)yield return null;}
  Check(m.State==BrokerMission.Phase.Failed&&!m.broker.gameObject.activeSelf,"three hits fail mission and remove guide");Check(MoneyManager.Instance.currentMoney==cash-20000,"hospital charges existing fee exactly once");Check(!m.GetComponentsInChildren<CitizenCombat>().Any(e=>e!=m.enemyTemplate),"failure removes mission pursuers");
  yield return Wait(2);Check(m.Begin(),"retry works after hospital recovery");m.Cancel();
 }
 public static IEnumerator Verify(){
  StoryPhoneInbox.Current.AutoOpenEnabled=false;
  log="Broker Play Mode integration checks\n";var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();Check(m&&m.broker&&m.enemyTemplate&&m.ambushPosts.Length==4,"saved actors, UI and posts exist");
  yield return Wait(.4f);var ui=m.day.pcUIPanel.GetComponent<NightPreparationView>();var player=m.player;var move=player.GetComponent<PlayerMove>();
  Check(!m.Begin(),"daytime cannot start night mission");m.day.EnterNightPreparation();
  foreach(var pc in UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsSortMode.None))pc.interactRange=1000;
  m.day.pcUIPanel.SetActive(true);yield return Wait(.1f);ui.SelectMission(0);m.Begin();yield return null;
  Check(m.State==BrokerMission.Phase.Find&&!m.day.pcUIPanel.activeSelf&&Cursor.lockState==CursorLockMode.Locked,"mission start releases gameplay controls");
  int day=m.day.currentDay,cash=MoneyManager.Instance.currentMoney;m.day.OnClick_ConfirmClose();Check(m.day.currentDay==day&&!m.day.IsTransitioning,"active mission blocks next-day settlement");
  m.Cancel();yield return null;Check(m.State==BrokerMission.Phase.Failed&&!m.broker.gameObject.activeSelf,"cancellation cleans up and permits retry");
  Check(m.Begin(),"failed mission can restart");Check(!m.Begin(),"duplicate start rejected");
  move.enabled=false;Place(player,m.exit.position);yield return Walk(player,m.meeting.position+Vector3.right*1.8f,6);
  player.View.transform.LookAt(m.broker.position+Vector3.up*1.2f);yield return null;yield return Key(UnityEngine.InputSystem.Key.F);
  Check(m.State==BrokerMission.Phase.Conversation,"physical F starts broker dialogue after actual walking path");
  Time.timeScale=0;var before=m.State;yield return null;yield return null;Check(m.State==before,"pause preserves mission phase");Time.timeScale=1;
  yield return Key(UnityEngine.InputSystem.Key.Space);yield return Wait(.3f);
  Check(m.State==BrokerMission.Phase.Escape,"skip hands off into playable escape");
  var enemies=m.GetComponentsInChildren<CitizenCombat>().Where(c=>c!=m.enemyTemplate).ToArray();Check(enemies.Length==2&&enemies.All(e=>e.IsAggressive&&e.missionEnemy),"opening wave contains two hostile combat actors");
  enemies[0].knockdownSeconds=90;Check(enemies[0].ReceiveKick(player,Vector3.forward,30)&&enemies[0].Npc.IsKnockedDown,"existing kick and ragdoll work on mission pursuer");
  // 이동 경로 검증에서는 적을 잠깐 기절시켜 벽/계단 충돌과 동행을 따로 확인한다.
  foreach(var e in enemies){e.knockdownSeconds=60;if(!e.Npc.IsKnockedDown)e.ReceiveKick(player,Vector3.forward,30);}
  m.reinforcementDelay=1;yield return Wait(1.2f);enemies=m.GetComponentsInChildren<CitizenCombat>().Where(c=>c!=m.enemyTemplate).ToArray();Check(enemies.Length==4,"support wave joins after delay");
  foreach(var e in enemies.Where(e=>!e.Npc.IsKnockedDown)){e.knockdownSeconds=60;e.ReceiveKick(player,Vector3.forward,30);}
  yield return Walk(player,m.exit.position,6);float rendezvous=Time.time+30;while(m.State==BrokerMission.Phase.Escape&&Time.time<rendezvous)yield return null;
  Check(Vector3.Distance(m.broker.position,m.exit.position)<6,"broker follows through city route without teleporting state="+m.State+" guide="+m.broker.position+" player="+player.transform.position+" reason="+m.FailureReason);
  Check(m.State==BrokerMission.Phase.Debrief,"both actors reaching clear exit completes escape");yield return Key(UnityEngine.InputSystem.Key.Space);
  Check(m.Completed&&!BrokerMission.Active&&!m.broker.gameObject.activeSelf,"debrief skip records completion and cleans actor");
  Check(MoneyManager.Instance.currentMoney==cash&&m.day.currentDay==day,"mission success does not change cash or day");
  m.day.pcUIPanel.SetActive(true);yield return Wait(.2f);ui.SelectMission(0);Check(m.Completed&&!m.CanStart&&!ui.startButton.gameObject.activeSelf,"completed mission prevents repeat rewards; PC start stays hidden");
  Capture(m,"Broker-completed");MoneyManager.Instance.currentMoney=200000;m.day.fadeDuration=0;ui.restButton.onClick.Invoke();ui.restButton.onClick.Invoke();float deadline=Time.time+8;while(m.day.currentDay==day&&Time.time<deadline)yield return null;Check(m.day.currentDay==day+1&&m.Completed,"completion survives next-day transition");move.enabled=true;
 }
}
}
#endif
