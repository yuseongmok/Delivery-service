#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Linq;using UnityEngine;using UnityEditor;
namespace DeliveryService.Mok.Editor {
public static class StoryDaysCheck {
 static string log;static void Check(bool pass,string text){log+=(pass?"PASS ":"FAIL ")+text+"\n";File.WriteAllText("Logs/Story-days-check.txt",log);if(!pass)throw new Exception(text);}
 static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
 static void Complete<T>(T mission,object value){typeof(T).GetProperty("State").SetValue(mission,value);}
 static IEnumerator NextDay(DayNightCycle day){int old=day.currentDay;day.fadeDuration=.01f;day.OnClick_ConfirmClose();float limit=Time.time+10;while((day.currentDay==old||day.IsTransitioning)&&Time.time<limit)yield return null;Check(day.currentDay==old+1&&!day.IsNightPreparation,"rest advances one day and returns to daytime");yield return null;}
 public static IEnumerator Homecoming(){
 log="Delivery message and homecoming check\n";yield return Wait(.5f);var f=VenueFinale.Current;var b=f.preparation.preparation.delivery.broker;var inbox=StoryPhoneInbox.Current;
 b.DeveloperComplete();SpecialOrderMission.Current.DeveloperComplete();BackdoorMission.Current.DeveloperComplete();MechanicMission.Current.DeveloperComplete();inbox.AutoOpenEnabled=false;
 Check(f.DeveloperStart(),"start finale");f.SkipCinematic();Check(!inbox.Messages.Any(x=>x.sender=="정비사"&&x.title=="차량 전달 완료"),"vehicle message not sent before depositing pizza");
 f.PlacePlayer(f.entry);yield return null;Check(f.Interact(),"admission begins");f.SkipCinematic();f.stealth.enabled=false;f.PlacePlayer(f.deliveryTable);yield return null;inbox.AutoOpenEnabled=true;Check(f.Interact(),"deposit pizza triggers vehicle message");yield return Wait(1);
 var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);
 Check(phone.IsPhoneOpen()&&app.MessagesMode&&app.Detail&&app.body.text.Contains("골목에 차와 열쇠"),"phone automatically displays mechanic vehicle message");Check(inbox.Messages.Count(x=>x.sender=="정비사"&&x.title=="차량 전달 완료")==1,"vehicle message sent exactly once");BrokerMissionCheck.Capture(b,"Venue-deposit-phone");phone.ClosePhone();inbox.AutoOpenEnabled=false;f.Cancel();
 Check(f.DeveloperStart(),"restart for homecoming check");f.SkipCinematic();typeof(VenueFinale).GetProperty("State").SetValue(f,VenueFinale.Phase.StreetEscape);typeof(VenueFinale).GetField("turned",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(f,true);f.PlacePlayer(f.carStand);yield return null;Check(f.Interact(),"board for return check");f.SkipCinematic();f.car.position=f.driveEnd.position;yield return null;f.AbandonCar();yield return Wait(2.6f);Check(f.State==VenueFinale.Phase.Debrief&&Vector3.Distance(f.Player.transform.position,f.safePoint.position)<2,"player reaches shop before dialogue");yield return Wait(1.5f);var mingi=f.preparation.preparation.delivery.kitchen.actor;Check(mingi.gameObject.activeInHierarchy&&Vector3.Angle(f.cinema.transform.forward,mingi.position+Vector3.up*1.45f-f.cinema.transform.position)<3,"first-person camera faces Mingi");Check(!f.fade.gameObject.activeSelf&&f.subtitle.text.Contains("차는 버리고"),"dialogue starts after return fade");BrokerMissionCheck.Capture(b,"Venue-homecoming",f.cinema);f.SkipCinematic();Check(f.State==VenueFinale.Phase.Aftermath&&!f.cinema.enabled,"skip starts shop escape at shop");f.Cancel();
 }
 public static IEnumerator Verify(){
 log="Story sequence without calendar gates\n";yield return Wait(.5f);
 var b=BrokerMission.Current;var s=SpecialOrderMission.Current;var r=BackdoorMission.Current;var m=MechanicMission.Current;var f=VenueFinale.Current;var day=b.day;var inbox=StoryPhoneInbox.Current;inbox.AutoOpenEnabled=false;
 int initial=day.currentDay;var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);
 void Daytime(){day.currentTime=9;day.isShopOpen=false;}
 void Open(int index){app.Open(false);app.Move(index);app.Confirm();app.Refresh();}
 Daytime();Check(!s.CanStart&&!r.CanStart&&!m.CanStart&&!f.CanStart,"future missions locked before story prerequisites");Open(0);Check(app.action.text=="미션 시작","broker phone action has no business gate");app.Confirm();Check(b.Running,"broker starts without closing shop");b.Cancel();b.DeveloperComplete();yield return Wait(.2f);
 Daytime();Open(1);Check(app.action.text=="미션 시작"&&s.CanStart,"rescue unlocks special order immediately");app.Confirm();Check(s.Running,"special order starts through phone");s.Cancel();s.DeveloperComplete();yield return Wait(.2f);
 Daytime();Open(2);Check(app.action.text=="미션 시작"&&r.CanStart,"approval unlocks reconnaissance immediately");app.Confirm();Check(r.Running,"reconnaissance starts through phone");r.Cancel();r.DeveloperComplete();yield return Wait(.2f);
 Daytime();Open(3);Check(app.action.text=="미션 시작"&&m.CanStart,"reconnaissance unlocks mechanic immediately");app.Confirm();Check(m.Running,"mechanic starts through phone");m.Cancel();m.DeveloperComplete();yield return Wait(.2f);
 Daytime();Open(4);Check(app.action.text=="행사장으로 출발"&&f.CanStart,"vehicle support unlocks finale immediately");app.Confirm();Check(f.State==VenueFinale.Phase.Opening&&day.currentDay==initial,"all missions connect in same calendar day");f.Cancel();
 foreach(var title in new[]{"다음 준비 · 특별 주문","다음 준비 · 도시의 뒷문","다음 준비 · 빚진 정비사","작전 준비 완료 · 마지막 배달"})Check(inbox.Messages.Count(x=>x.title==title)==1,"single invitation: "+title);
 Check(f.Begin(),"finale retries without losing preparation");f.Cancel();Check(day.currentDay==initial,"retry never advances calendar");
 }
}
}
#endif