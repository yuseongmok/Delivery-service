using UnityEngine;
namespace DeliveryService.Mok {
// 결투 전용 믹스. 기존 도시 오디오나 공용 효과음을 변경하지 않는다.
public sealed class PrisonDuelAudio : MonoBehaviour {
 public AudioClip punchSwing,punchHit,crowdCheer,battleMusic;
 [Range(0,1)] public float swingVolume=.5f,hitVolume=.65f,crowdVolume=.1f,musicVolume=.13f;
 AudioSource swing,hit,crowd,music; PrisonMedicineDuel duel;bool running,paused;float duck;
 public int Swings {get;private set;} public int Hits {get;private set;}
 public bool PlayingMusic=>music&&music.isPlaying;
 AudioSource Source(bool loop){var a=gameObject.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=loop;a.spatialBlend=0;return a;}
 public void Begin(PrisonMedicineDuel owner){duel=owner;if(!music){swing=Source(false);hit=Source(false);crowd=Source(true);music=Source(true);}StopImmediate();running=true;music.clip=battleMusic;crowd.clip=crowdCheer;music.volume=crowd.volume=0;if(battleMusic)music.Play();if(crowdCheer)crowd.Play();}
 public void Swing(){if(!running||!punchSwing)return;Swings++;swing.Stop();swing.clip=punchSwing;swing.volume=swingVolume*SuppliedAudio.Volume;swing.pitch=Random.Range(.96f,1.04f);swing.Play();duck=.22f;}
 public void Hit(){if(!running||!punchHit)return;Hits++;hit.Stop();hit.clip=punchHit;hit.volume=hitVolume*SuppliedAudio.Volume;hit.pitch=Random.Range(.97f,1.03f);hit.Play();duck=.3f;}
 public void Finish(){running=false;}
 public void StopImmediate(){running=false;paused=false;foreach(var a in new[]{swing,hit,crowd,music})if(a){a.Stop();a.volume=0;}}
 void Update(){if(!music)return;if(Time.timeScale<=0){if(!paused){foreach(var a in new[]{swing,hit,crowd,music})a.Pause();paused=true;}return;}if(paused){foreach(var a in new[]{swing,hit,crowd,music})a.UnPause();paused=false;}duck=Mathf.Max(0,duck-Time.unscaledDeltaTime);var manager=DeliveryService.Yun.Audio.AudioManager.Instance;float bgm=manager?manager.MasterVolume*manager.GetCategoryVolume(DeliveryService.Yun.Audio.SoundCategory.Bgm):PlayerPrefs.GetFloat("Yun.Settings.BgmVolume",1);if(manager&&(manager.MasterMuted||manager.GetCategoryMuted(DeliveryService.Yun.Audio.SoundCategory.Bgm)))bgm=0;
 bool lost=duel&&duel.Current==PrisonMedicineDuel.Phase.Lost;bool dialogue=duel&&duel.Current==PrisonMedicineDuel.Phase.Inactive;float target=running&&!lost?musicVolume*bgm*(duck>0?.55f:1)*(dialogue?.5f:1):0;music.volume=Mathf.MoveTowards(music.volume,target,Time.unscaledDeltaTime*.25f);float cheer=running&&!lost?crowdVolume*SuppliedAudio.Volume*(dialogue?.5f:1)*(duel.Current==PrisonMedicineDuel.Phase.Won?1.8f:1):0;crowd.volume=Mathf.MoveTowards(crowd.volume,cheer,Time.unscaledDeltaTime*.3f);if(running&&!lost){if(music.clip&&!music.isPlaying)music.Play();if(crowd.clip&&!crowd.isPlaying)crowd.Play();}else{if(music.volume==0)music.Stop();if(crowd.volume==0)crowd.Stop();}}
 void OnDisable(){StopImmediate();}
}
}
