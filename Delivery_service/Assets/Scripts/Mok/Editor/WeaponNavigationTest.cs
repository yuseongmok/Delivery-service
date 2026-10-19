#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.UI;using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {public static class WeaponNavigationTest {
 static void Check(bool value,string message,Action<string> log){if(!value)throw new Exception(message);log("PASS: "+message);}
 public static IEnumerator RunCore(Action<string> log){var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();var nav=planner.manager;var place=UnityEngine.Object.FindObjectsByType<NavigationPlace>(FindObjectsSortMode.None).Single(p=>p.kind==NavigationPlaceKind.Weapon);var bikes=UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);foreach(var b in bikes)b.isDriven=false;
 planner.Show(NavigationPlanner.Screen.Destinations);planner.MoveSelection(-1);Check(planner.Selection==4,"Handheld list wraps to weapon shop",log);planner.Confirm();Check(nav.SelectedPlace==place&&nav.RouteDestination==place.entrance,"Handheld selects shop entrance",log);Check(nav.DisplayPath.Count>=2,"Handheld complete path: "+nav.RouteStatus,log);
 var bike=bikes.First();bike.isDriven=true;planner.Show(NavigationPlanner.Screen.Map);planner.MoveSelection(1);planner.MoveSelection(-1);planner.Confirm();Check(planner.IsRiding&&nav.SelectedPlace==place&&planner.CurrentScreen==NavigationPlanner.Screen.Map,"Bike arrows and confirm select weapon shop",log);nav.SetNavigationTarget(bike.transform);Check(nav.DisplayPath.Count>=2,"Bike complete path: "+nav.RouteStatus,log);
 planner.Show(NavigationPlanner.Screen.Destinations);var rows=planner.GetComponentsInChildren<Text>(true);Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.text.Contains("무기점")&&!t.text.Contains("없음")),"Weapon shop row available",log);Check(place.Symbol=="W"&&place.IsAvailable,"Weapon shop has available map pin",log);
 foreach(var pins in UnityEngine.Object.FindObjectsByType<NavigationMapPins>(FindObjectsSortMode.None)){pins.Rebuild();Check(pins.transform.Find("MapPin W")!=null,"Weapon pin created on map display",log);}bike.isDriven=false;yield return null;
 }
}}
#endif
