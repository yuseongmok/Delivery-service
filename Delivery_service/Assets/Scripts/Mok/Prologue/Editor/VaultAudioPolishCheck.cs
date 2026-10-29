#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class VaultAudioPolishCheck {
 static string log;static void Check(bool ok,string text){if(!ok)throw new Exception(text);log+="PASS "+text+"\n";File.WriteAllText("Logs/Vault-audio-polish-check.txt",log);}
 public static IEnumerator Verify(){
  log="Vault and supplied audio verification\n";yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.AcceptInput=false;
  var b=SuppliedAudio.Current;Check(b&&b.vaultLatch&&b.vaultSwing&&b.bag&&b.shutter&&b.carOpen&&b.carClose&&b.carCrash&&b.wallImpact&&b.metalImpact&&b.bodyFall&&b.missionMusic,"eleven new sound references load");
  var a=d.accomplice;var p=a.performance;a.enabled=false;p.StopAllCoroutines();foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}
  d.vaultDoor.localRotation=Quaternion.identity;bool done=false;Vector3 stationary=Vector3.zero;bool seen=false;
  p.StartCoroutine(Open());
  IEnumerator Open(){yield return p.OpenVault();done=true;}
  float deadline=Time.time+10;
  while(!done&&Time.time<deadline){
   float angle=Quaternion.Angle(d.vaultDoor.localRotation,Quaternion.identity);
   if(angle>2){if(!seen){stationary=p.transform.position;seen=true;}else Check(Vector3.Distance(stationary,p.transform.position)<.01f,"partner stays still while automatic door swings");}
   yield return null;
  }
  Check(done&&seen&&Quaternion.Angle(d.vaultDoor.localRotation,Quaternion.Euler(0,-105,0))<.1f,"vault finishes opening and restores navigation");
  Check(p.GetComponent<NavMeshAgent>().enabled&&!p.Busy,"partner released for money collection");
  Check(p.vaultLatch==b.vaultLatch&&p.vaultSwing==b.vaultSwing&&p.doorOpen==b.carOpen&&p.doorClose==b.carClose,"vault and boarding use new clips");
  var officer=d.officers[0];officer.Activate();Check(officer.GetComponent<NavMeshAgent>().speed>=5.4f,"police approach speed increased");officer.Stop();officer.gameObject.SetActive(false);
  d.BeginRobbery();yield return null;Check(!d.progress.gameObject.activeInHierarchy,"cash gauge remains hidden during robbery");
  Check(d.player.combatFeedback.wallHit==b.wallImpact&&d.drive.GetComponent<PrologueDriveFeedback>().impact.clip==b.carCrash,"impact recordings assigned");
  d.SetPaused(true);yield return null;d.SetPaused(false);Check(!d.progress.gameObject.activeInHierarchy,"pause and resume keep cash gauge hidden");
 }
}
}
#endif
