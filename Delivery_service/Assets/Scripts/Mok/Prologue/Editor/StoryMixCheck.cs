#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class StoryMixCheck {
 static string log;static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Story-mix-check.txt",log);}
 static IEnumerator Wait(float s){float until=Time.time+s;while(Time.time<until)yield return null;}
 public static IEnumerator Finale(){
  log="Finale sound state verification\n";yield return Wait(.5f);
  var m=VenueFinale.Current;Check(m!=null,"main city finale loaded");m.enabled=false;
  var music=m.GetComponent<PrologueMissionMusic>();var state=typeof(VenueFinale).GetProperty("State");
  Check(music&&music.Source.clip==SuppliedAudio.Current.missionMusic&&!music.Source.isPlaying,"no mission music before detection");
  state.SetValue(m,VenueFinale.Phase.Exposed);yield return Wait(.3f);Check(music.Source.isPlaying,"detection starts supplied music");float at=music.Source.time;
  state.SetValue(m,VenueFinale.Phase.StreetEscape);yield return Wait(.3f);Check(music.Source.time>at,"escape keeps playback position");
  StorySoundMix.Play(m.sound,m.doorOpen,.4f);Time.timeScale=0;yield return null;yield return null;Check(!m.sound.isPlaying&&!music.Source.isPlaying,"finale effects and music both pause");Time.timeScale=1;yield return null;
  state.SetValue(m,VenueFinale.Phase.Complete);yield return Wait(1);Check(!music.Source.isPlaying,"completion fades and stops music");m.enabled=true;
 }
 public static IEnumerator Verify(){
  log="Mission sound mix verification\n";yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.BeginRobbery();d.AcceptInput=false;foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}d.accomplice.enabled=false;d.accomplice.performance.StopAllCoroutines();
  var music=d.GetComponent<PrologueMissionMusic>();float setting=PlayerPrefs.GetFloat("Yun.Settings.BgmVolume",1);PlayerPrefs.SetFloat("Yun.Settings.BgmVolume",1);
  try{
   yield return Wait(3);float baseline=music.Source.volume;float position=music.Source.time;
   Check(baseline>.12f&&baseline<=.141f,"combat music has conservative gain");
   StorySoundMix.Duck(.6f);yield return Wait(.3f);Check(music.Source.volume<baseline*.6f,"important effect ducks music");
   Check(music.Source.time>position,"ducking does not restart music");yield return Wait(2);Check(music.Source.volume>.12f,"music smoothly recovers after effect");
   d.player.gunFeedback.Fire();yield return null;d.SetPaused(true);yield return null;yield return null;
   var gun=d.player.gunFeedback.GetComponent<AudioSource>();Check(!gun.isPlaying&&!music.Source.isPlaying,"pause freezes gun effect and music together");d.SetPaused(false);yield return null;
   var escape=d.GetComponent<PrologueEscapePerformance>();escape.StartCoroutine(escape.BoardFirstPerson());yield return null;
   var boarding=(AudioSource)typeof(PrologueEscapePerformance).GetField("boardingAudio",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(escape);
   var alarm=d.GetComponents<AudioSource>().Single(s=>s.clip==SuppliedAudio.Current.bankAlarm);
   Check(boarding&&boarding!=alarm&&boarding!=music.Source&&!boarding.loop,"boarding sound has dedicated nonlooping source");
   escape.StopAllCoroutines();d.accomplice.performance.StopAllCoroutines();
   Check(d.GetComponents<PrologueMissionMusic>().Length==1,"only one mission music controller");
  }finally{PlayerPrefs.SetFloat("Yun.Settings.BgmVolume",setting);d.SetPaused(false);}
 }
}
}
#endif
