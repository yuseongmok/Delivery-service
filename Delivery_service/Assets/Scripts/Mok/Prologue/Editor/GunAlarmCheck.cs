#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class GunAlarmCheck {
 static string log;static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);log+="PASS "+msg+"\n";File.WriteAllText("Logs/Gun-alarm-check.txt",log);}
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
 static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
 public static IEnumerator Verify(){
  log="Gun and bank alarm verification\n";yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.BeginRobbery();d.AcceptInput=false;
  foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}d.accomplice.StopAllCoroutines();d.accomplice.enabled=false;
  var alarm=d.GetComponents<AudioSource>().Single(s=>s.clip==SuppliedAudio.Current.bankAlarm);
  d.alarmCycleSeconds=.1f;d.alarmRepeats=3;d.alarmRestSeconds=.25f;
  Check(alarm.isPlaying,"alarm starts with robbery");yield return Wait(.33f);Check(!alarm.isPlaying&&d.Alarm,"three alarm cycles enter quiet interval");
  d.SetPaused(true);yield return null;yield return null;Check(!alarm.isPlaying,"pause keeps quiet interval silent");d.SetPaused(false);yield return Wait(.3f);Check(alarm.isPlaying,"alarm resumes after interval");
  // 실제 수집 완료 API로 모든 돈을 담고 경보가 즉시 끝나는지 확인한다.
  d.accomplice.performance=null;
  for(int i=0;i<d.cash.Length;i++){d.accomplice.transform.position=d.cash[i].transform.position;Set(d.accomplice,"<CollectionProgress>k__BackingField",1f);Check(d.SecureCash(i,d.accomplice),"cash collected "+i);}
  Check(!d.Alarm&&!alarm.isPlaying,"last bundle immediately stops bank alarm");yield return Wait(.7f);Check(!alarm.isPlaying,"alarm never restarts after collection");d.SetPaused(true);d.SetPaused(false);Check(!alarm.isPlaying,"resume does not revive completed alarm");
  Set(d,"<Current>k__BackingField",BankPrologue.Phase.Robbery);d.player.Teleport(new Vector3(30,4,-65));d.player.view.transform.rotation=Quaternion.LookRotation(Vector3.up);
  foreach(var s in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))s.Stop();
  int count=d.player.gunFeedback.ShotCount;Check(d.player.Fire(),"isolated shot fires");Check(d.player.gunFeedback.ShotCount==count+1&&!d.player.Fire(),"one trigger produces one shot event");
  var active=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(s=>s.isPlaying).ToArray();
  Check(active.Length==1&&active[0].gameObject==d.player.gunFeedback.gameObject&&d.player.gunFeedback.shot==SuppliedAudio.Current.gunshot,"only supplied gunshot plays into open sky");
  d.player.combatFeedback.Impact(d.player.transform.position+Vector3.forward,Vector3.back,false);
  var impacts=d.GetComponentsInChildren<AudioSource>().Where(s=>s.gameObject.name.StartsWith("Pooled shot impact")&&s.isPlaying).ToArray();Check(impacts.Length==1&&impacts[0].volume<=.18f&&impacts[0].spatialBlend==1,"impact is quiet positional feedback, not a second loud gunshot");
 }
}
}
#endif
