#if UNITY_EDITOR
using System;using System.Collections;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class WantedTest {
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
public static IEnumerator RunCore(Action<string> log){
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();bike.EnterBike(player.gameObject);yield return null;yield return null;
foreach(var im in bike.bikeUIPanel.GetComponentsInChildren<UnityEngine.UI.Image>(true)){var corners=new Vector3[4];im.rectTransform.GetWorldCorners(corners);log(im.name+" active="+im.gameObject.activeInHierarchy+" color="+im.color+" fill="+im.fillAmount+" corners="+corners[0]+".."+corners[2]);}
ScreenCapture.CaptureScreenshot("Temp/Mok-bike-gauges-before.png");yield return null;bike.ExitBike();yield return null;
var hud=UnityEngine.Object.FindFirstObjectByType<WantedHUD>();yield return null;
Check(hud!=null&&hud.label.text=="","No wanted label before pursuit",log);
var inv=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();Check(typeof(ToppingInventory).GetField("currentTopping",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(inv)==null,"Inventory HUD disconnected without changing item logic",log);
UnityEngine.Object.FindFirstObjectByType<PoliceManager>().SpawnPoliceOnAccidentRefused();yield return null;yield return null;
Check(hud.label.text.Contains("수배 중")&&hud.label.text.Contains("초"),"Real police spawn shows wanted seconds",log);
var police=UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None);Check(police.Length>0,"Police spawned",log);
foreach(var p in police)typeof(PoliceControl).GetField("totalChaseTimer",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,35f);
yield return null;yield return null;Check(hud.label.text.Contains("35초"),"HUD follows real chase timers",log);
UnityEngine.Object.FindFirstObjectByType<PoliceManager>().SpawnPoliceOnAccidentRefused();yield return null;yield return null;Check(hud.label.text.Contains("120초"),"Repeat offense refreshes displayed timer",log);
ScreenCapture.CaptureScreenshot("Temp/Mok-wanted.png");yield return null;
foreach(var p in police)UnityEngine.Object.Destroy(p.gameObject);yield return null;yield return null;Check(hud.label.text=="","Warning clears when police leave",log);
}
}}
#endif
