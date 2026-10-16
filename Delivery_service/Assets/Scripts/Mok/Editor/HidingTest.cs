#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class HidingTest {
const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
public static IEnumerator RunCore(Action<string> log){
var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var hide=p.GetComponent<PlayerHiding>();var cc=p.GetComponent<CharacterController>();var move=p.GetComponent<PlayerMove>();var cam=p.View;foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;move.enabled=false;move.isControllable=true;Cursor.lockState=CursorLockMode.Locked;UnityEngine.Object.FindFirstObjectByType<PhoneController>().ClosePhone();
var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.timeMultiplier=0;clock.currentTime=12;
var bins=UnityEngine.Object.FindObjectsByType<HidingDumpster>(FindObjectsSortMode.None).OrderBy(b=>b.name).ToArray();Check(bins.Length==6,"Six persistent dumpsters saved in scene",log);
foreach(var bin in bins){cc.enabled=false;p.transform.SetPositionAndRotation(bin.ExitPosition,Quaternion.LookRotation(-bin.transform.forward));cc.enabled=true;Physics.SyncTransforms();yield return null;Check(NavMesh.SamplePosition(bin.ExitPosition,out var nav,1,NavMesh.AllAreas),bin.name+" has reachable exit",log);var before=p.transform.position;Check(hide.Enter(bin),bin.name+" enter succeeds",log);Check(hide.IsHidden&&!p.CanAct&&!cc.enabled,"Hiding suppresses movement and combat",log);yield return null;hide.Leave();Check(!hide.IsHidden&&cc.enabled&&Vector3.Distance(p.transform.position,before)<.01f,"Exit restores entry position and controller",log);}
var first=bins[0];cc.enabled=false;p.transform.position=first.ExitPosition;p.transform.rotation=Quaternion.LookRotation(-first.transform.forward);cc.enabled=true;cam.transform.LookAt(first.transform.position+Vector3.up);yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-dumpster.png");yield return null;
var mgr=UnityEngine.Object.FindFirstObjectByType<PoliceManager>();mgr.SpawnPoliceOnAccidentRefused();yield return null;var police=UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None);foreach(var cop in police){Set(cop,"viewDistance",1000f);Set(cop,"viewAngle",360f);Set(cop,"obstacleMask",(LayerMask)0);}
Check(hide.Enter(first),"Enter while pursued",log);yield return null;yield return null;
Check(police.All(c=>!c.HasVisualContact),"All police lose visual contact with hidden player",log);
var hud=UnityEngine.Object.FindFirstObjectByType<WantedHUD>();float alpha=hud.label.color.a;float until=Time.time+.25f;while(Time.time<until)yield return null;Check(Mathf.Abs(alpha-hud.label.color.a)>.01f,"Wanted warning blinks while unseen",log);
foreach(var cop in police){var agent=cop.GetComponent<NavMeshAgent>();agent.Warp(first.ExitPosition);}
yield return null;yield return null;Check(!PoliceControl.IsArrestInProgress&&hide.IsHidden,"Nearby police cannot arrest hidden player",log);
ScreenCapture.CaptureScreenshot("Temp/Mok-hidden.png");yield return null;
foreach(var cop in police)Set(cop,"arrestDistance",.01f);hide.Leave();foreach(var cop in police)cop.GetComponent<NavMeshAgent>().Warp(first.ExitPosition+Vector3.forward*4);until=Time.time+.3f;while(Time.time<until)yield return null;Check(police.Any(c=>c.HasVisualContact)&&hud.label.color.a==1,"Reappearance restores steady wanted warning",log);
Check(hide.Enter(first),"Can hide again",log);foreach(var cop in police)Set(cop,"outOfSightTimer",.1f);until=Time.time+.5f;while(Time.time<until)yield return null;
Check(UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None).Length==0&&hud.label.text=="","Out-of-sight timeout dismisses police and warning",log);hide.Leave();
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();Check(bike.EnterBike(p.gameObject),"Bike boarding succeeds",log);yield return null;yield return null;Canvas.ForceUpdateCanvases();
foreach(var gauge in new[]{bike.GetComponent<MotorcycleFuel>().fuelGaugeUI,UnityEngine.Object.FindFirstObjectByType<MotorcycleDurability>().durabilityGaugeUI}){var corners=new Vector3[4];gauge.rectTransform.GetWorldCorners(corners);Check(gauge.gameObject.activeInHierarchy&&corners[0].x>=0&&corners[0].y>=0&&corners[2].x<=Screen.width&&corners[2].y<=Screen.height,gauge.name+" visible inside viewport while riding",log);}
ScreenCapture.CaptureScreenshot("Temp/Mok-gauges-fixed.png");yield return null;bike.ExitBike();yield return null;Check(!bike.bikeUIPanel.activeSelf,"Gauges hide after dismount",log);
cam.transform.position=first.transform.position+new Vector3(3,2.7f,3);cam.transform.LookAt(first.transform.position+Vector3.up*.8f);yield return null;yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-dumpster-exterior.png");yield return null;

}
}}
#endif
