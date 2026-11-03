#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class ConspiracyStoryCheck {
 static string log;static void Check(bool ok,string text){log+=(ok?"PASS ":"FAIL ")+text+"\n";File.WriteAllText("Logs/Conspiracy-play-check.txt",log);if(!ok)throw new Exception(text);}
 static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
 public static IEnumerator Verify(){
 log="Conspiracy story and chapter handoff\n";yield return Wait(.5f);var f=VenueFinale.Current;var a=f.aftermath;var b=BrokerMission.Current;
 Check(a&&a.route.Length==3&&a.police.Length==4&&a.prison,"saved scene references complete");
 b.DeveloperComplete();SpecialOrderMission.Current.DeveloperComplete();BackdoorMission.Current.DeveloperComplete();MechanicMission.Current.DeveloperComplete();StoryPhoneInbox.Current.AutoOpenEnabled=false;
 Check(f.overheard.Any(t=>t.Contains("보스가 없어지면 조직은 내가"))&&f.closing.Any(t=>t.Contains("납품 기록")),"conspiracy and traceable shop address are explained");
 Check(f.DeveloperStart(),"existing finale developer entry works");f.SkipCinematic();typeof(VenueFinale).GetProperty("State").SetValue(f,VenueFinale.Phase.StreetEscape);f.PlacePlayer(f.carStand);yield return null;Check(f.Interact(),"existing escape car can be boarded");f.SkipCinematic();f.car.position=f.driveEnd.position;yield return null;f.AbandonCar();yield return Wait(2.7f);
 Check(f.State==VenueFinale.Phase.Debrief,"abandonment returns to shop conversation");f.SkipCinematic();yield return null;Check(f.State==VenueFinale.Phase.Aftermath&&a.State==ShopEscapeChapter.Stage.Packing&&f.Player.CanAct,"homecoming hands control to packing objective");
 f.PlacePlayer(a.route[0]);yield return null;Check(!a.TakeBag(),"bag cannot be taken from outside reach");f.PlacePlayer(a.bag);yield return null;Check(a.TakeBag(),"taking bag starts playable escape");Check(!a.TakeBag(),"bag cannot trigger duplicate waves");f.getaway.SuppressTraffic();
 f.PlacePlayer(a.route[0]);yield return Wait(8);float until=Time.time+6;while(f.Enemies.Length<2&&Time.time<until)yield return null;Check(f.Enemies.Length>=2,"gang spawns outside camera for escape");foreach(var e in f.Enemies)e.ExternalAttackControl=true;
 var guide=a.mingi.GetComponent<NavMeshAgent>();Check(a.CompanionHasPath,"Mingi follows using connected navigation path");var nav=new NavMeshPath();Check(NavMesh.CalculatePath(f.Player.transform.position,a.route.Last().position,NavMesh.AllAreas,nav)&&nav.status==NavMeshPathStatus.PathComplete,"shop exit connects to police encounter");
 float paused=Time.time;Time.timeScale=0;yield return null;yield return null;Check(Time.time==paused&&a.State==ShopEscapeChapter.Stage.Escape,"pause does not advance escape");Time.timeScale=1;
 foreach(var e in f.Enemies.Take(2))Check(e.ReceiveKick(f.Player,Vector3.forward,100),"escape gang can be subdued in combat");yield return null;Check(a.SubduedCount>=2,"combat progress counted");
 foreach(var point in a.route){f.PlacePlayer(point);yield return Wait(.4f);foreach(var e in f.Enemies)e.ExternalAttackControl=true;}
 Check(a.State==ShopEscapeChapter.Stage.Escape,"police capture waits for companion");until=Time.time+45;while(a.State==ShopEscapeChapter.Stage.Escape&&Time.time<until){foreach(var e in f.Enemies)e.ExternalAttackControl=true;yield return null;}
 File.WriteAllText("Logs/Conspiracy-guide-debug.txt","stage="+a.State+" step="+a.RouteStep+" guide="+a.mingi.position+" player="+f.Player.transform.position);BrokerMissionCheck.Capture(b,"Conspiracy-escape");Check(a.State==ShopEscapeChapter.Stage.Arrest,"companion reaches escape point and police surround pair");Check(!f.Player.CanAct&&f.cinema.enabled,"arrest locks combat and uses cinematic camera");int balance=MoneyManager.Instance.currentMoney;
 yield return Wait(2);BrokerMissionCheck.Capture(b,"Conspiracy-arrest",f.cinema);yield return Wait(a.arrestSeconds-1.5f);Check(a.State==ShopEscapeChapter.Stage.Transfer&&f.Player.GetComponent<PlayerWeapon>().Equipped==WeaponKind.None,"prison transfer confiscates weapon");yield return Wait(a.transferSeconds+1);
 Check(a.InPrison&&f.Completed&&f.Player.CanAct&&!f.cinema.enabled,"chapter two cell restores movement");Check(Vector3.Distance(f.Player.transform.position,a.prisonArrival.position)<2,"player arrives inside saved prison set");Check(MoneyManager.Instance.currentMoney==balance,"story arrest does not charge random police fine");Check(!UnityEngine.Object.FindFirstObjectByType<PhoneController>().enabled,"city phone remains unavailable in prison");BrokerMissionCheck.Capture(b,"Conspiracy-prison");
 Check(f.DeveloperStart(),"presentation restart leaves chapter two cleanly");Check(!a.InPrison&&!a.prison.activeSelf&&Vector3.Distance(f.Player.transform.position,f.safePoint.position)<2,"restart resets prison state and returns player to city");f.Cancel();
 }
}
}
#endif
