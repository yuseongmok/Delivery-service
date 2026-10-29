#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class FinaleSoundscapeCheck {
 static string log;static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Finale-soundscape-check.txt",log);}
 static IEnumerator Wait(float t){float end=Time.time+t;while(Time.time<end)yield return null;}
 static void Field(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
 public static IEnumerator Verify(){
  log="Finale soundscape Play Mode check\n";yield return Wait(.5f);var m=VenueFinale.Current;Check(m!=null,"finale scene loaded");m.enabled=false;if(m.stealth)m.stealth.enabled=false;
  var b=SuppliedAudio.Current;Check(b.cityAmbience&&b.roomAmbience&&b.detected&&b.escalation,"all four supplied sounds load");
  var s=m.GetComponent<StorySoundscape>();var state=typeof(VenueFinale).GetProperty("State");Field(s,"roof",false);Field(s,"nextRoof",Time.time+200);
  Field(m,"insideVenue",false);yield return Wait(1);Check(s.City.isPlaying&&s.City.volume>0&&!s.Room.isPlaying,"outdoor ambience plays alone");
  Field(m,"insideVenue",true);state.SetValue(m,VenueFinale.Phase.Deposit);yield return Wait(1);Check(s.Room.isPlaying&&s.Room.volume>0&&!s.City.isPlaying,"entering interior crossfades ambience");
  state.SetValue(m,VenueFinale.Phase.Ejected);yield return null;yield return null;Check(s.DetectionCues==1&&s.Cue.clip==b.detected&&s.Cue.isPlaying,"guard detection cue starts once; count="+s.DetectionCues+" clip="+(s.Cue.clip?s.Cue.clip.name:"none")+" playing="+s.Cue.isPlaying+" state="+m.State);yield return Wait(1);Check(s.DetectionCues==1&&!s.Cue.isPlaying,"held detection state does not repeat cue");
  state.SetValue(m,VenueFinale.Phase.Observe);yield return null;state.SetValue(m,VenueFinale.Phase.Exposed);yield return null;yield return null;Check(s.EscalationCues==1&&s.Cue.clip==b.escalation&&s.Cue.isPlaying,"VIP exposure state plays supplied escalation once");
  Time.timeScale=0;yield return null;yield return null;Check(!s.Cue.isPlaying&&!s.Room.isPlaying,"pause stops ambience and cue together");Time.timeScale=1;yield return null;
  state.SetValue(m,VenueFinale.Phase.StreetEscape);Field(m,"insideVenue",false);yield return Wait(1);Check(s.City.isPlaying&&!s.Room.isPlaying&&s.EscalationCues==1,"escape restores city bed without repeating escalation");
  state.SetValue(m,VenueFinale.Phase.Complete);yield return null;yield return null;Check(!s.Cue.isPlaying,"completion clears stinger");m.enabled=true;
 }
}
}
#endif
