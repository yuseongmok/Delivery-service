#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DeliveryService.Mok.Navigation;
public static class MokNavigationPhoneTest
{
 static string log;
 static void Check(bool ok,string message){if(!ok)throw new Exception(log+"FAIL: "+message);log+="PASS: "+message+"\n";}
 static void Tick(object o,string method="LateUpdate")=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static byte[] Capture(Camera camera,string name){foreach(var pins in UnityEngine.Object.FindObjectsByType<NavigationMapPins>(FindObjectsSortMode.None)) Tick(pins);foreach(var distance in UnityEngine.Object.FindObjectsByType<NavigationDistanceUI>(FindObjectsSortMode.None)) Tick(distance,"Update");var rt=camera.targetTexture;var previous=RenderTexture.active;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();var pixels=tex.GetRawTextureData().ToArray();File.WriteAllBytes("Temp/Mok-phone-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=previous;return pixels;}
 public static string RunCore(){
 log="Phone / bike navigation Play Mode checks\n";
 var p=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();var nav=p.manager;
 var bike=UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None).First(b=>b.name=="Bike 1");
 var ui=GameObject.Find("NavigationUICamera").GetComponent<Camera>();var map=GameObject.Find("NavigationCamera").GetComponent<Camera>();
 Cursor.lockState=CursorLockMode.Locked;
 phone.TogglePhone();Tick(p);Check(p.CurrentScreen==NavigationPlanner.Screen.Home,"Opening handheld phone shows Home");Capture(ui,"home");
 p.MoveSelection(1);p.Confirm();Check(p.CurrentScreen==NavigationPlanner.Screen.Home,"Stocks placeholder stays on Home");p.MoveSelection(-1);p.Confirm();Check(p.CurrentScreen==NavigationPlanner.Screen.Destinations,"Map app opens destination list");Capture(ui,"destinations");
 p.MoveSelection(1);p.Confirm();Check(nav.SelectedPlace!=null&&nav.SelectedPlace.kind==NavigationPlaceKind.Pizza,"Enter selects pizza shop guidance");
 p.Back();p.Back();Check(p.CurrentScreen==NavigationPlanner.Screen.Home,"Backspace returns handheld to Home");
 phone.ClosePhone();bike.isDriven=true;Tick(p);Check(p.CurrentScreen==NavigationPlanner.Screen.Map&&p.IsOpen,"Mounted navigation opens directly to Map");
 p.Show(NavigationPlanner.Screen.Home);Check(p.CurrentScreen==NavigationPlanner.Screen.Map,"Mounted display cannot open Home");
 p.MoveSelection(1);Check(p.CurrentScreen==NavigationPlanner.Screen.Destinations,"Arrow from mounted map opens list");p.MoveSelection(1);p.MoveSelection(1);p.Confirm();Check(nav.SelectedPlace.kind==NavigationPlaceKind.Repair,"Mounted Enter selects repair shop");Check(nav.DisplayPath.Count>=2,"Repair shop has complete road route: "+nav.RouteStatus);Capture(map,"gray-map");Capture(ui,"bike-map");
 p.Back();p.MoveSelection(-1);p.Confirm();Check(p.CurrentScreen==NavigationPlanner.Screen.Destinations,"Own mounted bike is unavailable");Capture(ui,"bike-destinations");
 Check(Time.timeScale==1&&bike.enabled&&phone.enabled,"Menus preserve running game and driving scripts");
 var orders=UnityEngine.Object.FindFirstObjectByType<OrderManager>();UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().isShopOpen=true;orders.OnShopOpened();Check(orders.PendingOrders.Count>0,"Order generated");orders.TryAcceptOrder(orders.PendingOrders[0]);var order=orders.ActiveOrder;float remaining=orders.RemainingDeliveryMinutes;
 p.Show(NavigationPlanner.Screen.Destinations);p.Confirm();Check(nav.SelectedPlace==null&&nav.CurrentDestination!=null,"Delivery guidance selected");
 p.Show(NavigationPlanner.Screen.Destinations);p.MoveSelection(1);p.MoveSelection(1);p.Confirm();Check(orders.ActiveOrder==order&&orders.RemainingDeliveryMinutes==remaining,"POI change preserves active delivery and deadline");
 var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=6;Tick(cycle,"UpdateLighting");var dawn=Capture(map,"dawn");cycle.currentTime=12;Tick(cycle,"UpdateLighting");var noon=Capture(map,"noon");cycle.currentTime=22;Tick(cycle,"UpdateLighting");var night=Capture(map,"night");Check(dawn.SequenceEqual(noon)&&noon.SequenceEqual(night),"Map pixels identical at 06:00, 12:00 and 22:00");
 bike.isDriven=false;phone.TogglePhone();Tick(p);Check(p.CurrentScreen==NavigationPlanner.Screen.Home,"After dismount handheld returns to Home");
 p.Confirm();p.MoveSelection(-1);p.Confirm();Check(nav.SelectedPlace!=null&&nav.SelectedPlace.kind==NavigationPlaceKind.Bike,"Parked bike can be selected from handheld");
 var input=File.ReadAllText("ProjectSettings/InputManager.asset");Check(!input.Contains("negativeButton: left")&&!input.Contains("negativeButton: down")&&input.Contains("altPositiveButton: w")&&input.Contains("altPositiveButton: d"),"Arrows removed from movement axes; WASD preserved");
 Check(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).All(c=>c.name!="Navigation destination planner"),"No fullscreen planner");
 return log;
 }
}
#endif


