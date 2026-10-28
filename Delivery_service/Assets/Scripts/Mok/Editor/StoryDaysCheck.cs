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
 Check(f.DeveloperStart(),"restart for homecoming check");f.SkipCinematic();typeof(VenueFinale).GetProperty("State").SetValue(f,VenueFinale.Phase.StreetEscape);typeof(VenueFinale).GetField("turned",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(f,true);f.PlacePlayer(f.carStand);yield return null;Check(f.Interact(),"board for return check");f.SkipCinematic();f.car.position=f.driveEnd.position;yield return null;f.AbandonCar();yield return Wait(2.6f);Check(f.State==VenueFinale.Phase.Debrief&&Vector3.Distance(f.Player.transform.position,f.safePoint.position)<2,"player reaches shop before dialogue");yield return Wait(1.5f);var mingi=f.preparation.preparation.delivery.kitchen.actor;Check(mingi.gameObject.activeInHierarchy&&Vector3.Angle(f.cinema.transform.forward,mingi.position+Vector3.up*1.45f-f.cinema.transform.position)<3,"first-person camera faces Mingi");Check(!f.fade.gameObject.activeSelf&&f.subtitle.text.Contains("실장도 우리도"),"dialogue starts after return fade");BrokerMissionCheck.Capture(b,"Venue-homecoming",f.cinema);f.SkipCinematic();Check(f.Completed&&!f.cinema.enabled,"skip restores gameplay at shop");
 }
 public static IEnumerator Verify(){
 log="Story dialogue and daily progression\n";yield return Wait(.5f);
 var b=BrokerMission.Current;var s=SpecialOrderMission.Current;var r=BackdoorMission.Current;var m=MechanicMission.Current;var f=VenueFinale.Current;var day=b.day;var inbox=StoryPhoneInbox.Current;inbox.AutoOpenEnabled=false;MoneyManager.Instance.currentMoney=200000;day.EnterNightPreparation();
 var story=AssetDatabase.LoadAssetAtPath<PrologueStory>("Assets/Scripts/Mok/Prologue/Art/BankPrologueStory.asset");Check(story.carConversation.Any(x=>x.text.Contains("조직이 시킨"))&&story.shopConversation.Any(x=>x.text.Contains("조직에 넘길")),"saved prologue explains commission and inaccessible job money");
 Check(b.meetingLines.Any(x=>x.Contains("이유는 나도 몰라"))&&f.overheard.Any(x=>x.Contains("새 사업 자금"))&&f.overheard.Any(x=>x.Contains("전부 없애")),"broker uncertainty leads to revealed betrayal at venue");Check(inbox.Messages.Any(x=>x.sender=="실장"&&x.body.Contains("내일")),"first contact is broker message");
 // 실제 완료 프로퍼티와 실제 다음 날 전환으로 검사한다.
 int start=day.currentDay;Complete(b,BrokerMission.Phase.Complete);yield return null;
 Check(b.CompletedDay==start&&!s.DayUnlocked&&!s.Begin(),"same-night special order is blocked");
 var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);app.Open(false);app.Move(1);app.Confirm();app.Refresh();Check(app.action.text.Contains("DAY "+(start+1)),"phone displays next eligible day");app.gameObject.SetActive(false);
 yield return NextDay(day);Check(s.DayUnlocked&&!s.CanStart,"next day still requires night");day.EnterNightPreparation();yield return null;Check(s.Begin(),"special order begins next night");s.Cancel();Check(s.Begin(),"failed mission can retry same night");s.Cancel();Complete(s,SpecialOrderMission.Phase.Complete);Check(!r.DayUnlocked&&!r.Begin(),"recon blocked on delivery completion night");
 yield return NextDay(day);day.EnterNightPreparation();yield return null;Check(r.Begin(),"recon starts following night");r.Cancel();Complete(r,BackdoorMission.Phase.Complete);Check(!m.DayUnlocked&&!m.Begin(),"mechanic blocked on recon completion night");
 yield return NextDay(day);day.EnterNightPreparation();yield return null;Check(m.Begin(),"mechanic starts following night");m.Cancel();Complete(m,MechanicMission.Phase.Complete);Check(f.Ready,"all four separate preparation nights unlock finale");
 int eventBefore=day.currentDay;Check(f.Begin(),"finale departure schedules event day");float until=Time.time+12;while(f.State==VenueFinale.Phase.Scheduling&&Time.time<until)yield return null;Check(day.currentDay==eventBefore+1&&f.State==VenueFinale.Phase.Opening&&!day.isShopOpen,"finale starts next day without pizza sales");f.Cancel();
 Check(inbox.Messages.Count(x=>x.title=="오늘의 약속 · 특별 주문")==1&&inbox.Messages.Count(x=>x.title=="오늘의 준비 · 도시의 뒷문")==1&&inbox.Messages.Count(x=>x.title=="오늘의 약속 · 빚진 정비사")==1,"each next-night invitation sent once");
 Check(f.DeveloperStart(),"presentation cheat still enters finale directly");f.Cancel();
 }
}
}
#endif
