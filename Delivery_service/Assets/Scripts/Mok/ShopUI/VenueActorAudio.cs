using UnityEngine;
namespace DeliveryService.Mok {
// 행사장 적에게만 보행과 쓰러짐 효과를 추가한다. 일반 시민 시스템은 변경하지 않는다.
public sealed class VenueActorAudio:MonoBehaviour {
 CitizenCombat combat;AudioSource fall;bool down;
 public static void Attach(CitizenCombat actor){if(actor&&!actor.GetComponent<VenueActorAudio>())actor.gameObject.AddComponent<VenueActorAudio>();}
 void Start(){combat=GetComponent<CitizenCombat>();StoryActorFootsteps.Attach(transform,GetComponentInChildren<Animator>(true),null);fall=gameObject.AddComponent<AudioSource>();fall.playOnAwake=false;fall.spatialBlend=1;fall.minDistance=1;fall.maxDistance=14;fall.rolloffMode=AudioRolloffMode.Linear;StorySoundPause.Attach(fall);}
 void Update(){if(!combat||!combat.Npc||!fall||Time.timeScale<=0)return;bool now=combat.Npc.IsDead||combat.Npc.IsKnockedDown;if(now&&!down)StorySoundMix.Play(fall,SuppliedAudio.Current?SuppliedAudio.Current.bodyFall:null,.22f);down=now;}
}
}
