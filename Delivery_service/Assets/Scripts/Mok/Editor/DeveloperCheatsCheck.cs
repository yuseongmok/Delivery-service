#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
namespace DeliveryService.Mok.Editor {
public static class DeveloperCheatsCheck {
 static string log;static void Check(bool ok,string s){log+=(ok?"PASS ":"FAIL ")+s+"\n";File.WriteAllText("Logs/Developer-cheats-check.txt",log);if(!ok)throw new Exception(s);}
 public static IEnumerator Verify(){
  yield return null;log="Presentation cheat runtime verification\n";var cheat=DeveloperCheats.Current;var b=BrokerMission.Current;var p=b.player;StoryPhoneInbox.Current.AutoOpenEnabled=false;
  Check(cheat&&!DeveloperCheats.Invincible&&!cheat.IsOpen,"runtime menu starts closed and god mode off");
  Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.LeftCtrl,Key.LeftShift,Key.F12));yield return null;yield return null;
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
  Check(cheat.IsOpen&&Time.timeScale==0&&Cursor.visible,"keyboard shortcut opens paused menu");
  int money=MoneyManager.Instance.currentMoney;Check(cheat.GrantMoney(100000)&&MoneyManager.Instance.currentMoney==money+100000,"cash grant uses normal balance");
  cheat.Close();Check(Time.timeScale==1&&Cursor.lockState==CursorLockMode.Locked,"closing restores gameplay");
  DeveloperCheats.SetInvincible(true);int hits=p.HitsTaken,bullets=p.BulletHitsTaken,health=p.Health;p.ReceivePunch(100,Vector3.forward);Check(!p.ReceiveBullet(Vector3.forward),"god mode rejects bullets");
  Check(p.HitsTaken==hits&&p.BulletHitsTaken==bullets&&p.Health==health&&!p.IsDown,"god mode blocks damage and knockdown");Check(p.CanReceiveHit,"god mode does not disable CanAct prerequisite");DeveloperCheats.SetInvincible(false);Check(!DeveloperCheats.Invincible,"god mode can be disabled");
  b.day.EnterNightPreparation();Check(b.Begin(),"broker mission begins");Check(cheat.SkipMission()&&b.Completed&&!b.Running&&!b.broker.gameObject.activeSelf,"active broker skip cleans actor and completes");
  var s=SpecialOrderMission.Current;Check(s.CanStart&&s.Begin(),"next mission unlocked normally");Check(cheat.SkipMission()&&s.Completed&&s.SampleAccepted,"special order skip records approval");
  Check(cheat.SkipMission()&&BackdoorMission.Current.Completed&&BackdoorMission.Current.ConfirmedCount==4,"idle skip completes next preparation");
  Check(cheat.SkipMission()&&MechanicMission.Current.VehicleSupportUnlocked,"mechanic skip grants vehicle support");
  var finale=VenueFinale.Current;Check(finale.Ready,"all preparations unlock finale");typeof(VenueFinale).GetProperty("State").SetValue(finale,VenueFinale.Phase.Entrance);
  Check(cheat.SkipMission()&&finale.Completed&&!finale.Cinematic&&!finale.cinema.enabled,"finale skip completes and returns camera");
  Check(!cheat.SkipMission()&&cheat.SkipTarget.Contains("없음"),"finished chain cannot be skipped repeatedly");Check(MoneyManager.Instance.currentMoney==money+100000,"skips do not add unintended mission rewards");
  cheat.Open();Check(cheat.StartFinalDelivery()&&finale.State==VenueFinale.Phase.Opening&&!cheat.IsOpen&&Time.timeScale==1,"direct finale cheat starts completed finale again and closes menu");finale.SkipCinematic();Check(finale.State==VenueFinale.Phase.Entrance,"direct finale reaches playable entrance");var guard=finale.entryGuard.Find("Original Synty entry guard");Check(guard&&guard.GetComponentInChildren<Animator>().isHuman,"entry guard uses intact native humanoid rig");yield return null;yield return null;var camera=new GameObject("Guard inspection camera").AddComponent<Camera>();camera.transform.position=guard.position+guard.forward*2+guard.right*.5f+Vector3.up*1.5f;camera.transform.LookAt(guard.position+Vector3.up*1.25f);BrokerMissionCheck.Capture(b,"Venue-entry-guard-fixed",camera);UnityEngine.Object.Destroy(camera.gameObject);finale.Cancel();
  cheat.Open();cheat.Close();Check(Time.timeScale==1,"menu remains usable after all skips");
 }
}}
#endif
