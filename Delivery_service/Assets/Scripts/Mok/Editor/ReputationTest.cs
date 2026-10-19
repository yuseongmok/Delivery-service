#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using UnityEngine;using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
public static class ReputationTest {
static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
public static IEnumerator RunCore(Action<string> log){
 void Check(bool value,string message){if(!value)throw new Exception(message);log("PASS: "+message);}
 var manager=UnityEngine.Object.FindFirstObjectByType<OrderManager>();var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.currentTime=6;clock.isShopOpen=true;
 void Tick()=>typeof(OrderManager).GetMethod("Update",F).Invoke(manager,null);
 var shop=ShopReputation.Instance;Check(Mathf.Approximately(shop.Rating,3)&&shop.Reviews.Count==0,"New game begins at 3.0 with no fabricated reviews");
 Check(ShopReputation.PriceMultiplier(1)==.15f&&ShopReputation.PriceMultiplier(3)==1&&ShopReputation.PriceMultiplier(5)==1.8f,"Rating price endpoints 15% / 100% / 180%");
 manager.ResetOrdersForNewDay();Tick();var rejected=manager.PendingOrders[0];int priceBefore=manager.PendingOrders[1].quotedPrice;
 Check(manager.TryDeclineOrder(rejected)&&Mathf.Abs(shop.Rating-2.95f)<.001f,"Real decline deducts 0.05");float after=shop.Rating;Check(!manager.TryDeclineOrder(rejected)&&shop.Rating==after,"Stale duplicate decline cannot double-charge rating");Check(manager.PendingOrders[0].quotedPrice<priceBefore,"Pending prices update when reputation changes");
 var accepted=manager.PendingOrders[0];manager.TryAcceptOrder(accepted);Check(Mathf.Abs(manager.SignedDeliveryMinutes-30)<.01f,"Accepted order starts at 30 game minutes");
 int expected=manager.Quote(accepted,manager.CurrentTargetDeliveryPoint);int money=MoneyManager.Instance.currentMoney;
 Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(accepted.toppings,true)},accepted.destinationID),"Correct pizza accepted");Check(MoneyManager.Instance.currentMoney==money+expected&&shop.Reviews[0].stars==5&&shop.Rating>after,"Fast delivery pays pre-review rating and earns 5-star review");
 Check(shop.Reviews[0].address==accepted.destinationID&&shop.Reviews[0].pizza==accepted.recipeName,"Review names actual delivery address and requested pizza");
 for(int i=0;i<5;i++)Check(ShopReputation.DeliveryStars(new[]{15f,30f,40f,50f,51f}[i],true,true,false)==5-i,"Time boundary star "+(5-i));
 clock.currentTime+=6f/3600f;Tick();var late=manager.PendingOrders[0];manager.TryAcceptOrder(late);clock.currentTime+=55f/60f;Tick();Check(manager.HasActiveOrder&&manager.SignedDeliveryMinutes<0&&manager.CurrentTargetDeliveryPoint!=null,"Late order remains deliverable and keeps navigation target");
 Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(late.toppings,true)},late.destinationID)&&shop.Reviews[0].stars==1,"55-minute delivery succeeds with 1-star late review");
 clock.currentTime+=6f/3600f;Tick();var wrong=manager.PendingOrders[0];manager.TryAcceptOrder(wrong);var toppings=new List<string>(wrong.toppings){"invalid topping"};float beforeWrong=shop.Rating;expected=Mathf.RoundToInt(manager.Quote(wrong,manager.CurrentTargetDeliveryPoint)*.25f);money=MoneyManager.Instance.currentMoney;
 Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(toppings,true)},wrong.destinationID),"Wrong recipe is delivered rather than blocked");Check(shop.Reviews[0].stars==1&&shop.Rating<=Mathf.Max(1,beforeWrong-.799f)&&MoneyManager.Instance.currentMoney==money+expected,"Wrong pizza gets 1-star, large rating loss and 25% payout");
 // 실제 소매치기 성공 처리에서만 감점되는지 확인한다.
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var citizen=UnityEngine.Object.FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None).FirstOrDefault(c=>c.CanBePickpocketed);
 // 낮아진 평점을 회복시켜 감점이 하한에 가려지지 않도록 한다.
 for(int i=0;i<3;i++)shop.Record("House Test","치즈피자",10,true,true,false,10000,1);
 if(citizen!=null)player.transform.position=citizen.transform.position+Vector3.back;
 float previous=shop.Rating;Check(citizen!=null&&citizen.Pickpocket(player),"Citizen pickpocket success path exercised");Check(Mathf.Abs(shop.Rating-(previous-.1f))<.001f,"Successful pickpocket deducts 0.1");previous=shop.Rating;Check(!citizen.Pickpocket(player)&&shop.Rating==previous,"Failed repeat pickpocket does not deduct rating");
 foreach(var npc in UnityEngine.Object.FindObjectsByType<NPCControl>(FindObjectsSortMode.None))npc.enabled=false;foreach(var npc in UnityEngine.Object.FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None))npc.enabled=false;
 int count=shop.Reviews.Count;float rating=shop.Rating;clock.currentDay++;manager.ResetOrdersForNewDay();Check(shop.Reviews.Count==count&&shop.Rating==rating,"Next day preserves reviews and rating");
 var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.TogglePhone();yield return null;var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();planner.Show(NavigationPlanner.Screen.Home);planner.MoveSelection(3);planner.Confirm();Check(planner.CurrentScreen==NavigationPlanner.Screen.Reviews,"Fourth home app opens reviews via existing selection flow");
 var app=UnityEngine.Object.FindFirstObjectByType<ReviewPhoneApp>();Check(app!=null&&app.isActiveAndEnabled,"Review app active in existing phone");app.Move(1);app.Move(-1);app.Refresh();
 float until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;app.Move(3);typeof(MokNavigationPhoneTest).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{GameObject.Find("NavigationUICamera").GetComponent<Camera>(),"reviews"});ScreenCapture.CaptureScreenshot("Temp/Mok-reputation-preview.png");until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
 planner.Show(NavigationPlanner.Screen.Home);typeof(MokNavigationPhoneTest).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{GameObject.Find("NavigationUICamera").GetComponent<Camera>(),"reviews-home"});ScreenCapture.CaptureScreenshot("Temp/Mok-reputation-home.png");until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
 var police=new GameObject("Test police").AddComponent<PoliceControl>();police.enabled=false;var second=new GameObject("Second test police").AddComponent<PoliceControl>();second.enabled=false;
 var fade=new GameObject("Test fade",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();typeof(PoliceControl).GetField("fadeScreen",F).SetValue(police,fade);
 var routine=(IEnumerator)typeof(PoliceControl).GetMethod("ArrestRoutine",F).Invoke(police,new object[]{null});previous=shop.Rating;Check(routine.MoveNext(),"Actual arrest coroutine begins");Check(Mathf.Abs(shop.Rating-(previous-.2f))<.001f,"Actual arrest deducts 0.2");
 var duplicate=(IEnumerator)typeof(PoliceControl).GetMethod("ArrestRoutine",F).Invoke(second,new object[]{null});Check(!duplicate.MoveNext()&&Mathf.Abs(shop.Rating-(previous-.2f))<.001f,"Concurrent police cannot double-charge arrest penalty");
 for(int step=0;step<1000&&routine.MoveNext();step++){}Check(!PoliceControl.IsArrestInProgress,"Arrest routine finishes normally");UnityEngine.Object.Destroy(fade.gameObject);
 for(int i=0;i<100;i++)shop.Declined();Check(shop.Rating==1,"Rating cannot fall below one");for(int i=0;i<100;i++)shop.Record("House Test","치즈피자",10,true,true,false,10000,2);Check(shop.Rating<=5&&shop.Rating>4.99&&shop.Reviews.Count==100,"Rating capped at five; review history bounded");
 log("Reputation regression complete");
}
}}
#endif
