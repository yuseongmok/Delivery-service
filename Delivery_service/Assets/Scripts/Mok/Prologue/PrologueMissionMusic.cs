using UnityEngine;
namespace DeliveryService.Mok {
// 같은 곡을 끊지 않고 이어가며 대화·컷씬에서는 볼륨만 낮춘다.
[DisallowMultipleComponent]
public sealed class PrologueMissionMusic : MonoBehaviour {
 BankPrologue director; VenueFinale venue; AudioSource source; bool paused;
 public AudioSource Source=>source;
 public static float PrologueLevel(BankPrologue.Phase phase)=>phase==BankPrologue.Phase.Complete||phase==BankPrologue.Phase.Failed||phase==BankPrologue.Phase.CityReveal?0:phase==BankPrologue.Phase.Opening||phase==BankPrologue.Phase.Getaway||phase==BankPrologue.Phase.CityReveal?.07f:.14f;
 public static float FinaleLevel(VenueFinale.Phase phase)=>phase<VenueFinale.Phase.Exposed||phase>VenueFinale.Phase.Debrief?0:phase==VenueFinale.Phase.Exposed||phase==VenueFinale.Phase.Boarding||phase==VenueFinale.Phase.Abandoning||phase==VenueFinale.Phase.Debrief?.07f:.14f;
 void Start(){director=GetComponent<BankPrologue>();venue=GetComponent<VenueFinale>();source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.volume=0;if(SuppliedAudio.Current)source.clip=SuppliedAudio.Current.missionMusic;}
 void Update(){
  if(!source||!source.clip)return;
  if(Time.timeScale<=0||(director&&director.Paused)){if(!paused){source.Pause();paused=true;}return;}
  if(paused){source.UnPause();paused=false;}
  float level=director?PrologueLevel(director.Current):venue?FinaleLevel(venue.State):0;
  bool active=level>0;
  float volume=level*(StorySoundMix.DuckActive?.42f:1)*Mathf.Clamp01(PlayerPrefs.GetFloat("Yun.Settings.BgmVolume",1));
  if(active&&!source.isPlaying)source.Play();
  source.volume=Mathf.MoveTowards(source.volume,volume,Time.deltaTime*(volume<source.volume?.45f:.06f));
  if(!active&&source.volume<=0)source.Stop();
 }
 void OnDisable(){if(source)source.Stop();}
}
}
