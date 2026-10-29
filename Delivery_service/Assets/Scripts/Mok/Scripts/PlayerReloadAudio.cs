using UnityEngine;
namespace DeliveryService.Mok {
public sealed class PlayerReloadAudio:MonoBehaviour {
 AudioSource sound;
 public void Play(){var b=SuppliedAudio.Current;if(!b||!b.reload)return;if(!sound){sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;}sound.clip=b.reload;sound.volume=.35f*SuppliedAudio.Volume;sound.Play();}
 public void Stop(){if(sound)sound.Stop();}
 void Update(){if(!sound)return;bool paused=Time.timeScale<=0;if(paused)sound.Pause();else sound.UnPause();}
 void OnDisable(){Stop();}
}
public sealed class BulletFlyby:MonoBehaviour {
 static float next;static AudioListener listener;AudioSource sound;
 public static void TryPlay(Vector3 from,Vector3 to){
  var bank=SuppliedAudio.Current;if(!bank||!bank.flyby||Time.time<next)return;
  if(!listener||!listener.isActiveAndEnabled){listener=null;foreach(var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))if(l.isActiveAndEnabled){listener=l;break;}}
  if(!listener)return;var ear=listener.transform.position;if(Vector3.Distance(from,ear)<3)return;
  var ray=to-from;float t=Vector3.Dot(ear-from,ray)/Mathf.Max(ray.sqrMagnitude,.001f);if(t<=0||t>1.08f)return;var closest=from+ray*Mathf.Clamp01(t);float distance=Vector3.Distance(closest,ear);if(distance>2||distance<.12f)return;
  next=Time.time+.35f;var fx=new GameObject("Nearby bullet audio");fx.transform.position=closest;var f=fx.AddComponent<BulletFlyby>();f.sound=fx.AddComponent<AudioSource>();f.sound.playOnAwake=false;f.sound.spatialBlend=1;f.sound.minDistance=1;f.sound.maxDistance=5;f.sound.clip=bank.flyby;f.sound.volume=.24f*SuppliedAudio.Volume;f.sound.Play();Object.Destroy(fx,bank.flyby.length+.1f);
 }
 void Update(){if(Time.timeScale<=0)sound.Pause();else sound.UnPause();}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){next=0;listener=null;}
}
}
