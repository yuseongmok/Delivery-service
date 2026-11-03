#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class RamPolishCheck {
 public static IEnumerator Verify(Action<bool,string> check){
  yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.autoEnterGame=false;d.AcceptInput=false;d.player.enabled=false;
  typeof(BankPrologue).GetField("<Current>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,BankPrologue.Phase.Driving);
  foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}foreach(var o in d.roadPolice)o.gameObject.SetActive(true);
  d.player.view.enabled=false;d.filmCamera.enabled=true;d.drive.Begin();d.drive.ExternalInput=true;d.drive.ResetToRoad();d.drive.SetInput(1,0);
  var cars=UnityEngine.Object.FindObjectsByType<PrologueRoadblockCar>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.x).ToArray();var first=cars[0];var start=first.transform.position;var rotation=first.transform.rotation;
  var feedback=d.drive.GetComponent<PrologueDriveFeedback>();float end=Time.time+15;
  while(!first.Cleared&&Time.time<end)yield return null;
  check(first.Cleared,"real driving impacts the first blockade car");
  check(d.drive.Speed<cars.Where(c=>c.Cleared).Max(c=>c.ImpactSpeed)*.9f,"collision briefly reduces player vehicle speed");
  check(feedback.RamRemaining>0&&feedback.impact.isPlaying,"vehicle hit plays crash sound and cockpit impulse");
  check(!Physics.GetIgnoreCollision(first.GetComponent<Collider>(),d.drive.GetComponent<Collider>()),"ram keeps vehicle collision enabled instead of phasing through");
  var speed=d.drive.Speed;int count=feedback.ImpactCount;first.TryRam(Vector3.right*23,d.drive.transform);check(d.drive.Speed==speed&&feedback.ImpactCount==count,"same car contact does not repeatedly brake or replay audio");
  d.SetPaused(true);var parked=first.transform.position;yield return null;yield return null;check(first.transform.position==parked,"pause freezes moving blockade car");d.SetPaused(false);
  end=Time.time+1.2f;while(Time.time<end)yield return null;
  check(Vector3.Distance(start,first.transform.position)>3&&Quaternion.Angle(rotation,first.transform.rotation)>25,"blockade car slides and rotates away after impact");
  check(Mathf.Abs(first.transform.position.y-start.y)<.01f,"suspension bounce settles back on road");
  end=Time.time+25;while(d.Current==BankPrologue.Phase.Driving&&Time.time<end)yield return null;
  check(cars.Any(c=>c.Cleared&&c.transform.position.x>200)&&d.Current==BankPrologue.Phase.CityReveal,"contact-limited final barricade remains driveable to city transition");
 }
}}
#endif

