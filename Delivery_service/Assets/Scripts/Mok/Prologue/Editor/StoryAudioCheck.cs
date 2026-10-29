#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class StoryAudioCheck {
 static string log;static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);log+="PASS "+msg+"\n";File.WriteAllText("Logs/Story-audio-check.txt",log);}
 public static IEnumerator Verify(){
  log="Story music and footsteps\n";yield return null;yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var music=d.GetComponent<PrologueMissionMusic>();
  Check(music.Source.clip==SuppliedAudio.Current.missionMusic&&music.Source.isPlaying,"supplied music plays in opening cutscene");
  d.SetPaused(true);yield return null;yield return null;Check(!music.Source.isPlaying,"music pauses with cutscene");d.SetPaused(false);yield return null;Check(music.Source.isPlaying,"music resumes");
  Check(PrologueMissionMusic.PrologueLevel(BankPrologue.Phase.CityReveal)==0&&PrologueMissionMusic.PrologueLevel(BankPrologue.Phase.Getaway)>0,"arrival stops music while boarding retains it");
  Check(PrologueMissionMusic.FinaleLevel(VenueFinale.Phase.Observe)==0&&PrologueMissionMusic.FinaleLevel(VenueFinale.Phase.Exposed)>0&&PrologueMissionMusic.FinaleLevel(VenueFinale.Phase.Driving)>0&&PrologueMissionMusic.FinaleLevel(VenueFinale.Phase.Debrief)>0&&PrologueMissionMusic.FinaleLevel(VenueFinale.Phase.Complete)==0,"finale music starts on exposure and ends after debrief");
  d.StopAllCoroutines();var a=d.accomplice;a.enabled=false;a.performance.StopAllCoroutines();a.performance.enabled=false;var nav=a.GetComponent<UnityEngine.AI.NavMeshAgent>();nav.enabled=false;
  var f=a.GetComponent<StoryActorFootsteps>();Check(f&&d.shopArrival.actor.GetComponent<StoryActorFootsteps>(),"both companion and shop cutscene have footsteps");a.animator.SetFloat("MoveSpeed",1);
  int before=f.Steps;for(int i=0;i<12;i++){a.transform.position+=Vector3.forward*.1f;yield return null;}Check(f.Steps>before,"real animated movement plays steps");
  before=f.Steps;a.transform.position+=Vector3.forward*20;yield return null;yield return null;Check(f.Steps==before,"teleports do not trigger footsteps");
  a.animator.SetFloat("MoveSpeed",0);for(int i=0;i<12;i++){a.transform.position+=Vector3.forward*.1f;yield return null;}Check(f.Steps==before,"seated or idle actor movement does not trigger footsteps");
 }
}
}
#endif
