#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Linq;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class SuppliedAudioCheck {
 static string log;static void Check(bool pass,string message){if(!pass)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Supplied-audio-check.txt",log);}
 public static IEnumerator Verify(){
  log="Supplied audio Play Mode verification\n";yield return null;yield return null;
  var b=SuppliedAudio.Current;Check(b&&b.gunshot&&b.reload&&b.flyby&&b.bankAlarm&&b.policeSiren&&b.playerEngine,"all supplied effect references load");
  Check(b.indoorSteps.Length==8&&b.streetSteps.Length==9&&b.boots.Length==7,"24 footstep variations referenced");
  Check(b.gunshot.length<.7f&&b.reload.length<.8f&&b.playerEngine.length<5,"trimmed effects and engine loop lengths");
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.BeginRobbery();d.AcceptInput=false;
  Check(d.GetComponents<AudioSource>().Any(s=>s.clip==b.bankAlarm&&s.isPlaying),"bank alarm uses supplied recording");
  foreach(var o in d.officers)o.Stop();
  d.player.gunFeedback.Fire();yield return null;Check(d.player.gunFeedback.shot==b.gunshot&&d.player.gunFeedback.GetComponents<AudioSource>().Any(s=>s.isPlaying),"rifle uses supplied gunshot");
  GunshotSound.Play(d.player.gameObject,false);Check(d.player.GetComponents<AudioSource>().Any(s=>s.isPlaying),"shared pistol sound plays");
  var reload=d.player.gameObject.AddComponent<PlayerReloadAudio>();reload.Play();yield return null;Check(d.player.GetComponents<AudioSource>().Any(s=>s.clip==b.reload&&s.isPlaying),"reload recording plays");reload.Stop();
  var cars=UnityEngine.Object.FindObjectsByType<ProloguePursuitCar>(FindObjectsInactive.Include,FindObjectsSortMode.None);Check(cars.All(c=>c.siren.clip==b.policeSiren),"both pursuit cars use new siren");
  Check(d.drive.GetComponent<PrologueDriveFeedback>().engine.clip==b.playerEngine,"getaway engine uses prepared loop");
  var go=new GameObject("Supplied footsteps test");var f=go.AddComponent<PlayerWalkFeedback>();f.Tick(true,true,false,0,.02f);for(int i=0;i<30;i++){go.transform.position+=Vector3.forward*.08f;f.Tick(true,true,false,0,.02f);}Check(f.Footsteps>0&&go.GetComponent<AudioSource>().isPlaying,"imported footstep variants play");f.Tick(false,false,false,0,0);Check(!go.GetComponent<AudioSource>().isPlaying,"footstep audio stops when control is locked");UnityEngine.Object.Destroy(go);
 }
}
}
#endif
