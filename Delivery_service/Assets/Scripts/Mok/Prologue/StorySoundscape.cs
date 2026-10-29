using UnityEngine;
namespace DeliveryService.Mok {
// 실내·실외 환경음은 교차 전환하고 발각 효과음은 상태 변경당 한 번만 재생한다.
[DisallowMultipleComponent]
public sealed class StorySoundscape:MonoBehaviour {
 BankPrologue bank;VenueFinale venue;AudioSource city,room,cue;
 VenueFinale.Phase previous;bool started,paused,roof;float nextRoof;
 public AudioSource City=>city;public AudioSource Room=>room;public AudioSource Cue=>cue;
 public int DetectionCues{get;private set;}public int EscalationCues{get;private set;}
 AudioSource Make(AudioClip clip,bool loop){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.clip=clip;s.spatialBlend=0;s.volume=0;return s;}
 void Start(){bank=GetComponent<BankPrologue>();venue=GetComponent<VenueFinale>();var b=SuppliedAudio.Current;city=Make(b?b.cityAmbience:null,true);room=Make(b?b.roomAmbience:null,true);cue=Make(null,false);cue.volume=1;previous=venue?venue.State:VenueFinale.Phase.Available;started=true;}
 void Fade(AudioSource s,float target){if(!s.clip)return;if(target>0&&!s.isPlaying)s.Play();s.volume=Mathf.MoveTowards(s.volume,target,Time.deltaTime*.2f);if(target==0&&s.volume<.001f)s.Stop();}
 void Stinger(AudioClip clip,float gain){if(!clip)return;cue.Stop();cue.clip=clip;cue.volume=gain*SuppliedAudio.Volume;cue.Play();StorySoundMix.Duck(Mathf.Min(3.5f,clip.length));}
 void Update(){
  if(!started)return;bool stop=Time.timeScale<=0||(bank&&bank.Paused);
  if(stop){if(!paused){city.Pause();room.Pause();cue.Pause();paused=true;}return;}
  if(paused){city.UnPause();room.UnPause();cue.UnPause();paused=false;}
  var b=SuppliedAudio.Current;
  if(venue&&venue.State!=previous){
   var state=venue.State;
   if(state==VenueFinale.Phase.Ejected){Stinger(b?b.detected:null,.42f);DetectionCues++;}
   if(state==VenueFinale.Phase.Exposed){Stinger(b?b.escalation:null,.46f);EscalationCues++;}
   if(state==VenueFinale.Phase.Observe&&previous==VenueFinale.Phase.Deposit)StorySoundMix.Play(venue.sound,b?b.bag:null,.16f);
   if(state==VenueFinale.Phase.Complete||state==VenueFinale.Phase.Failed||state==VenueFinale.Phase.Available)cue.Stop();
   previous=state;
  }
  // 일반 매장에서도 큰 천장 아래에 있으면 실내음을 사용한다. 차량·인물 콜라이더는 제외한다.
  if(Time.time>=nextRoof){nextRoof=Time.time+.5f;roof=false;Camera camera=bank?(bank.filmCamera.enabled?bank.filmCamera:bank.player.view):venue?(venue.cinema.enabled?venue.cinema:venue.Player.View):null;
   if(camera){foreach(var h in Physics.RaycastAll(camera.transform.position,Vector3.up,5,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){var size=h.collider.bounds.size;if(size.x>3&&size.z>3&&!h.collider.attachedRigidbody){roof=true;break;}}}
  }
  bool inside=venue?venue.InsideVenue||roof:bank?(bank.Current==BankPrologue.Phase.Opening||bank.Current==BankPrologue.Phase.Robbery||roof):roof;
  if(bank&&bank.Current==BankPrologue.Phase.CityReveal)inside=false;
  bool combat=venue?venue.State>=VenueFinale.Phase.Exposed&&venue.State<=VenueFinale.Phase.Driving:bank&&bank.CanPlay;
  float level=(combat?.055f:.11f)*SuppliedAudio.Volume;
  bool ended=bank&&(bank.Current==BankPrologue.Phase.Complete||bank.Current==BankPrologue.Phase.Failed);
  Fade(city,!ended&&!inside?level:0);Fade(room,!ended&&inside?level:0);
 }
 void OnDisable(){if(city)city.Stop();if(room)room.Stop();if(cue)cue.Stop();paused=false;}
}
}
