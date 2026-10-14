#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class MokOrderFlowCheck {
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
static OrderManager manager;static DayNightCycle cycle;static string log="Sequential order flow verification\n";
static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
static void Call(string n,params object[] args)=>typeof(OrderManager).GetMethod(n,F).Invoke(manager,args);
static void Check(bool ok,string text){if(!ok)throw new Exception(text);log+="PASS: "+text+"\n";}
static void Wait(float seconds)=>Call("AdvanceOrderWait",seconds);
public static string RunCore(){
log="Sequential order flow verification\n";
manager=UnityEngine.Object.FindFirstObjectByType<OrderManager>();cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
Set(manager,"orderUI",null);Set(manager,"dayNightCycle",cycle);Set(manager,"orderGenerator",UnityEngine.Object.FindFirstObjectByType<OrderGenerator>());
cycle.currentDay=1;cycle.currentTime=6;cycle.isShopOpen=true;
manager.OnShopOpened();Check(manager.PendingOrders.Count==1&&manager.OrdersGeneratedToday==1,"one immediate opening order");
Wait(500);manager.OnShopOpened();Check(manager.PendingOrders.Count==1&&manager.OrdersGeneratedToday==1,"waiting and repeat opening do not accumulate orders");
manager.TryAcceptOrder(manager.PendingOrders[0]);Check(Mathf.Abs(manager.RemainingDeliveryMinutes-80)<.001f,"80 game-minute delivery limit retained");Wait(500);Check(manager.PendingOrders.Count==0,"no new order during delivery");
var accepted=manager.ActiveOrder;cycle.OnClick_CloseShop();Check(!cycle.isShopOpen&&cycle.summaryPanel.activeSelf,"manual closing available during active delivery before daily quota");cycle.OnClick_CancelClose();Check(cycle.isShopOpen&&manager.ActiveOrder==accepted,"cancel closing preserves active delivery");
Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(new List<string>{"Dough","Sauce","Cheese"},true)},manager.CurrentTargetDeliveryID),"normal delivery resolves order");
Wait(29);Check(manager.PendingOrders.Count==0,"no next order before 30 real seconds");
cycle.isShopOpen=false;Wait(500);cycle.isShopOpen=true;manager.OnShopOpened();Wait(1);Check(manager.PendingOrders.Count==1&&manager.OrdersGeneratedToday==2,"closing pause preserves cooldown; next order at 30 seconds");
manager.TryDeclineOrder(manager.PendingOrders[0]);Wait(30);Check(manager.PendingOrders.Count==1&&manager.OrdersGeneratedToday==3,"decline schedules next opportunity");
manager.TryAcceptOrder(manager.PendingOrders[0]);cycle.currentTime+=81f/60f;Call("UpdateDeliveryDeadline");Check(!manager.HasActiveOrder,"deadline cancels active order");Wait(30);Check(manager.OrdersGeneratedToday==4&&manager.PendingOrders.Count==1,"expiry schedules next opportunity");
while(manager.OrdersGeneratedToday<7){manager.TryDeclineOrder(manager.PendingOrders[0]);Wait(30);}
Check(manager.PendingOrders[0].orderName.EndsWith("07"),"sequential numbering reaches 07");manager.TryDeclineOrder(manager.PendingOrders[0]);Wait(500);manager.OnShopOpened();Call("GenerateSingleOrder");Check(manager.DailyOrdersExhausted&&manager.PendingOrders.Count==0&&manager.OrdersGeneratedToday==7,"hard daily cap includes declined and cancelled orders");
cycle.currentDay++;cycle.isShopOpen=false;manager.ResetOrdersForNewDay();Check(manager.OrdersGeneratedToday==0,"new day resets count");cycle.isShopOpen=true;manager.OnShopOpened();Check(manager.PendingOrders.Count==1&&manager.PendingOrders[0].orderName.EndsWith("01"),"new day starts with one order numbered 01");
Check(Mathf.Abs(cycle.timeMultiplier-54f)<.001f,"20-minute day preserved");
log+="RESULT: PASS\n";return log;
}
}
#endif
