#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor {public static class RatingOrdersTest {
 public static IEnumerator RunCore(Action<string> log){
 void Check(bool b,string s){if(!b)throw new Exception(s);log("PASS: "+s);}
 var saved=UnityEngine.Random.state;try{UnityEngine.Random.InitState(7182);int Cheap(float rating){int count=0;for(int i=0;i<10000;i++){var o=PizzaRecipes.Create(null,rating);if(o.recipeName=="치즈피자"||o.recipeName=="페페로니피자")count++;}return count;}int low=Cheap(3),high=Cheap(5);Check(low>7200&&low<7800&&high>2200&&high<2800,$"Weighted sampling: cheap pizzas at 3 stars {low}/10000; at 5 stars {high}/10000");}finally{UnityEngine.Random.state=saved;}
 Check(OrderManager.CapacityForRating(3.99f)==3&&OrderManager.CapacityForRating(4)==4&&OrderManager.CapacityForRating(4.99f)==4&&OrderManager.CapacityForRating(5)==5,"Capacity boundaries 3/4/5");
 var manager=UnityEngine.Object.FindFirstObjectByType<OrderManager>();var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.enabled=false;cycle.currentTime=6;cycle.isShopOpen=true;var rep=ShopReputation.Instance;var rating=typeof(ShopReputation).GetProperty("Rating");var refresh=typeof(OrderManager).GetMethod("RefreshPrices",BindingFlags.NonPublic|BindingFlags.Instance);var update=typeof(OrderManager).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
 rating.SetValue(rep,3f);manager.ResetOrdersForNewDay();manager.OnShopOpened();Check(manager.PendingOrders.Count==3,"Initial three choices");
 foreach(float stars in new[]{4f,5f}){rating.SetValue(rep,stars);refresh.Invoke(manager,null);cycle.currentTime+=6f/3600;update.Invoke(manager,null);Check(manager.PendingOrders.Count==(int)stars,"Capacity grows after five game seconds at "+stars);var queue=UnityEngine.Object.FindFirstObjectByType<ShopPcOrderQueue>(FindObjectsInactive.Include);Check(queue!=null,"PC queue exists");queue.Refresh();Check(queue.VisibleCount==(int)stars&&queue.Rows.Take((int)stars).All(r=>r.gameObject.activeSelf),"PC exposes all rows at "+stars);}
 var existing=manager.PendingOrders.ToArray();rating.SetValue(rep,3f);refresh.Invoke(manager,null);Check(manager.PendingOrders.Count==5&&existing.SequenceEqual(manager.PendingOrders),"Rating drop preserves existing orders");manager.TryDeclineOrder(manager.PendingOrders[0]);manager.TryDeclineOrder(manager.PendingOrders[0]);cycle.currentTime+=10f/3600;update.Invoke(manager,null);Check(manager.PendingOrders.Count==3,"No replacement while above reduced cap");manager.TryDeclineOrder(manager.PendingOrders[0]);cycle.currentTime+=6f/3600;update.Invoke(manager,null);Check(manager.PendingOrders.Count==3,"Refill restores reduced three-slot cap");
 rating.SetValue(rep,4.999999f);Check(OrderManager.PendingCapacity==5,"Displayed 5.00 rating unlocks five slots despite float interpolation");yield return null;
 }
}}
#endif
