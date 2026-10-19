#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class LatePaymentTest {
 public static IEnumerator RunCore(Action<string> log){
 void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
 Check(DeliveryPricing.ApplyDelay(10000,0)==10000&&DeliveryPricing.ApplyDelay(10000,30)==10000,"No deduction through 30 minutes");
 Check(DeliveryPricing.ApplyDelay(10000,45)==6500,"45 minutes pays 65 percent");
 Check(DeliveryPricing.ApplyDelay(10000,60)==3000&&DeliveryPricing.ApplyDelay(10000,5000)==3000,"60 minutes and beyond pays 30 percent; cap enforced");
 for(int i=0;i<90;i++)Check(DeliveryPricing.ApplyDelay(10000,i+1)<=DeliveryPricing.ApplyDelay(10000,i),"Monotonic minute "+i);
 var manager=UnityEngine.Object.FindFirstObjectByType<OrderManager>();var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.isShopOpen=true;clock.currentTime=6;
 var update=typeof(OrderManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
 foreach(float duration in new[]{30f,45f,60f,100f}){
  manager.ResetOrdersForNewDay();update.Invoke(manager,null);var order=manager.PendingOrders[0];Check(manager.TryAcceptOrder(order),"Accept order for "+duration+" minute case");
  int quote=manager.Quote(order,manager.CurrentTargetDeliveryPoint);clock.currentTime+=duration/60f;update.Invoke(manager,null);Check(manager.HasActiveOrder,"Late order remains active");
  var ui=UnityEngine.Object.FindFirstObjectByType<CurrentOrderUI>();var text=(UnityEngine.UI.Text)typeof(CurrentOrderUI).GetField("currrentOrderText",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);if(duration>=45)Check(text.text.Contains(duration==45?"-35%":"-70%"),"HUD displays current delay deduction");
  int expected=DeliveryPricing.ApplyDelay(quote,manager.ElapsedDeliveryMinutes),before=MoneyManager.Instance.currentMoney;
  Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(order.toppings,true)},order.destinationID),"Complete delivery at "+duration+" minutes");
  Check(MoneyManager.Instance.currentMoney==before+expected&&ShopReputation.Instance.Reviews[0].payment==expected,"Wallet and review payment match delayed region/rating quote");
 }
 manager.ResetOrdersForNewDay();update.Invoke(manager,null);var wrong=manager.PendingOrders[0];manager.TryAcceptOrder(wrong);clock.currentTime+=1;int normal=manager.Quote(wrong,manager.CurrentTargetDeliveryPoint);int oldMoney=MoneyManager.Instance.currentMoney;
 int badExpected=Mathf.RoundToInt(DeliveryPricing.ApplyDelay(normal,manager.ElapsedDeliveryMinutes)*.25f);
 Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(new List<string>{"도우"},true)},wrong.destinationID),"Wrong pizza remains deliverable");Check(MoneyManager.Instance.currentMoney==oldMoney+badExpected,"Wrong-recipe penalty composes with capped lateness");
 log("Late payment regression complete");yield return null;
 }
}}
#endif
