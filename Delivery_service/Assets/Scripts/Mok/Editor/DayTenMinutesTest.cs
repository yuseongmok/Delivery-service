#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class DayTenMinutesTest {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static IEnumerator RunCore(Action<string> log){
  void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();var orders=UnityEngine.Object.FindFirstObjectByType<OrderManager>();
  Check(clock.timeMultiplier==108&&Mathf.Abs(18*3600/clock.timeMultiplier-600)<.01f,"씬의 하루 06~24시 = 현실 600초");
  clock.isShopOpen=true;orders.ResetOrdersForNewDay();orders.OnShopOpened();yield return null;Check(orders.TryAcceptOrder(orders.PendingOrders[0]),"실제 주문 수락");
  float start=Time.time,world=clock.currentTime,delivery=orders.ElapsedDeliveryMinutes;while(Time.time<start+2)yield return null;
  float real=Time.time-start,worldDelta=(clock.currentTime-world)*60,deliveryDelta=orders.ElapsedDeliveryMinutes-delivery;
  Check(Mathf.Abs(worldDelta-real*1.8f)<.3f,"실제 프레임에서 세계 시계 108배속");Check(Mathf.Abs(deliveryDelta-real*.9f)<.3f,"실제 프레임에서 배달 시계 기존 54배속");
  clock.isShopOpen=false;float stopped=orders.ElapsedDeliveryMinutes;float end=Time.time+.25f;while(Time.time<end)yield return null;Check(Mathf.Abs(orders.ElapsedDeliveryMinutes-stopped)<.01f,"마감 중 배달 시계 정지");clock.isShopOpen=true;
  Time.timeScale=0;stopped=orders.ElapsedDeliveryMinutes;double until=Time.realtimeSinceStartupAsDouble+.2;while(Time.realtimeSinceStartupAsDouble<until)yield return null;Check(Mathf.Abs(orders.ElapsedDeliveryMinutes-stopped)<.01f,"일시정지 중 배달 시계 정지");Time.timeScale=1;
  foreach(float promised in new[]{50f,70f,90f}){
   float seconds=promised*60/OrderManager.DeliveryTimeMultiplier;Check(Mathf.Abs(seconds-promised/.9f)<.001f,"기존 배달 실제 여유 유지 "+promised+"분 = "+seconds+"초");
   Check(DeliveryPricing.ApplyDelay(10000,promised,promised)==10000&&DeliveryPricing.ApplyDelay(10000,promised+15,promised)==6500&&DeliveryPricing.ApplyDelay(10000,promised+30,promised)==3000,"기존 감액 곡선 유지 "+promised);
  }
  clock.enabled=false;orders.enabled=false;float limit=orders.RecommendedDeliveryMinutes;
  var timeField=typeof(OrderManager).GetField("deliveryClockMinutes",F);double now=(double)timeField.GetValue(orders);timeField.SetValue(orders,now+limit+15-orders.ElapsedDeliveryMinutes);
  Check(Mathf.Abs(orders.ElapsedDeliveryMinutes-limit-15)<.01f&&orders.HasActiveOrder,"지연 주문 경과 시간·주문 유지");
  var order=orders.ActiveOrder;int quote=orders.Quote(order,orders.CurrentTargetDeliveryPoint),before=MoneyManager.Instance.currentMoney;int expected=DeliveryPricing.ApplyDelay(quote,orders.ElapsedDeliveryMinutes,limit);
  Check(orders.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(order.toppings,true)},order.destinationID)&&MoneyManager.Instance.currentMoney==before+expected,"실제 배달 지급액에 기존 지연 기준 적용");
  Check(ShopReputation.Instance.Reviews[0].stars==2,"실제 리뷰도 기존 지연 별점 유지");
  orders.ResetOrdersForNewDay();Check(!orders.HasActiveOrder&&orders.ElapsedDeliveryMinutes==0,"다음 날 주문 시간 초기화");
 }
}
}
#endif
