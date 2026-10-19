#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class OrderRecipeTest {
static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
public static IEnumerator RunCore(Action<string> log){
 void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
 var manager=UnityEngine.Object.FindFirstObjectByType<OrderManager>();var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.currentTime=6;clock.isShopOpen=true;manager.enabled=false;
 void Tick()=>typeof(OrderManager).GetMethod("Update",F).Invoke(manager,null);
 void Advance(float seconds){clock.currentTime+=seconds/3600f;Tick();}
 manager.ResetOrdersForNewDay();Tick();Check(manager.PendingOrders.Count==3,"Opening presents three offers");
 var policy=manager.GetComponent<DistrictDeliveryPolicy>();Check(policy.eastCity.Count==24&&policy.westQuarter.Count>0,"New city and slum destinations registered");
 var original=manager.PendingOrders.ToArray();manager.TryDeclineOrder(original[0]);Check(manager.PendingOrders.Count==2,"Decline removes only chosen offer");Advance(4);Check(manager.PendingOrders.Count==2,"No refill before five game seconds");Advance(1.2f);Check(manager.PendingOrders.Count==3&&manager.PendingOrders.Contains(original[1]),"Five game seconds refill preserves other offers");
 for(int i=0;i<12;i++){manager.TryDeclineOrder(manager.PendingOrders[0]);Advance(5.2f);}Check(manager.OrdersGeneratedToday>7&&manager.PendingOrders.Count==3&&!manager.DailyOrdersExhausted,"No daily seven-order cap");
 PizzaRecipes.TryType("Pepperoni",out var pepperoni);Check(pepperoni==ToppingType.Pepperoni,"Pepperoni does not alias Pepper");
 Check(PizzaRecipes.Matches(new List<string>{"도우","소스","치즈","페페로니"},new List<string>{"Dough","Sauce","Cheese","Pepperoni"}),"Actual Korean topping names match recipe");
 var inventory=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();
 foreach(int district in new[]{0,1,2}){
  clock.isShopOpen=false;manager.ResetOrdersForNewDay();Tick();policy.oldCityWeight=district==0?100:0;policy.eastCityWeight=district==1?100:0;policy.westQuarterWeight=district==2?100:0;clock.isShopOpen=true;Tick();
  var order=manager.PendingOrders[0];Check(manager.TryAcceptOrder(order),"Accept region "+order.districtName);Advance(5.2f);Check(manager.PendingOrders.Count==3&&manager.HasActiveOrder,"Refill while delivering");
  int before=MoneyManager.Instance.currentMoney;var raw=new PizzaData(order.toppings,false);var ruined=new PizzaData(order.toppings,true,true);var wrong=new PizzaData(order.toppings.Concat(new[]{"unknown"}).ToList(),true);
  Check(!manager.TryDeliverPackagedStack(new List<PizzaData>{raw,ruined,wrong},order.destinationID)&&manager.HasActiveOrder&&MoneyManager.Instance.currentMoney==before,"Reject raw, ruined and wrong recipe without payment or cancellation");
  var target=manager.CurrentTargetDeliveryPoint;inventory.ClearPackagedPizzas();inventory.AddPackagedDataStack(new List<PizzaData>{wrong,new PizzaData(order.toppings,true)});target.Interact(inventory);
  Check(!manager.HasActiveOrder&&inventory.GetHeldPackagedPizzas().Count==1&&ReferenceEquals(inventory.GetHeldPackagedPizzas()[0],wrong),"Deliver only matching box in "+order.districtName);
  Check(MoneyManager.Instance.currentMoney==before+order.quotedPrice,"Pay quoted region price "+order.quotedPrice);inventory.ClearPackagedPizzas();
 }
 var cheese=new PizzaOrder{toppings=new List<string>{"Dough","Sauce","Cheese"}};
 Check(manager.Quote(cheese,policy.oldCity[0])==10000&&manager.Quote(cheese,policy.eastCity[0])==13000&&manager.Quote(cheese,policy.westQuarter[0])==16000,"Cheese prices old 10000 / east 13000 / slum 16000");
 cheese.toppings.Add("Pepperoni");Check(manager.Quote(cheese,policy.oldCity[0])==13000,"Pepperoni ingredient premium applied");
 manager.TryDeclineOrder(manager.PendingOrders[0]);clock.isShopOpen=false;Advance(6);Check(manager.PendingOrders.Count==2,"No new offers when closed");manager.ResetOrdersForNewDay();Check(manager.PendingOrders.Count==0&&!manager.HasActiveOrder,"New day clears active and pending orders");
  clock.currentDay++;clock.currentTime=6;clock.isShopOpen=true;Tick();
 var pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;
 typeof(ShopPC).GetMethod("OpenPanel",F).Invoke(pc,null);
 var view=pc.pcUIPanel.GetComponent<DeliveryService.Yun.ShopUI.ShopPcUiView>();view.SelectTab(0);
 var queue=pc.pcUIPanel.GetComponentInChildren<DeliveryService.Mok.ShopUI.ShopPcOrderQueue>(true);queue.Refresh();Canvas.ForceUpdateCanvases();
 Check(queue.VisibleCount==3&&queue.Rows.Take(3).All(r=>r.description.text.Contains(r.Order.recipeName)&&r.description.text.Contains(r.Order.quotedPrice.ToString("N0"))),"PC shows three requested pizzas and quoted prices");
 var chosen=queue.Rows[0].Order;queue.Rows[0].accept.onClick.Invoke();Check(manager.ActiveOrder==chosen,"PC accept button connected");Advance(5.2f);
 Check(queue.VisibleCount==3&&queue.Rows.Take(3).All(r=>!r.accept.interactable),"PC replenishes and retains one active delivery");
 manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(chosen.toppings,true)},chosen.destinationID);
 queue.Rows[0].decline.onClick.Invoke();Advance(5.2f);Check(queue.VisibleCount==3,"PC decline button replenishes queue");
 Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Temp/Mok-order-recipe-preview.png");float until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
 log("Order regression complete");yield return null;
}
}}
#endif
