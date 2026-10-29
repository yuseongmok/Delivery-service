using UnityEngine;
namespace DeliveryService.Mok {
// 짧은 효과음이 음악에 묻히지 않도록 재생 시점에만 여유를 만든다.
public static class StorySoundMix {
 static float duckUntil;
 public static bool DuckActive=>Time.time<duckUntil;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){duckUntil=0;}
 public static void Duck(float seconds=.35f){duckUntil=Mathf.Max(duckUntil,Time.time+seconds);}
 public static void Play(AudioSource source,AudioClip clip,float gain){if(!source||!clip||Time.timeScale<=0)return;Duck(Mathf.Min(.8f,clip.length));source.PlayOneShot(clip,gain*SuppliedAudio.Volume);}
}
// 개별 효과음의 남은 재생 위치를 유지한다. 정지한 음원을 새로 시작하지 않는다.
public sealed class StorySoundPause:MonoBehaviour {
 public AudioSource source;bool paused;
 public static void Attach(AudioSource source){if(!source)return;var p=source.gameObject.AddComponent<StorySoundPause>();p.source=source;}
 void Update(){if(!source)return;bool stop=Time.timeScale<=0;if(stop&&!paused){source.Pause();paused=true;}else if(!stop&&paused){source.UnPause();paused=false;}}
 void OnDisable(){if(source)source.Stop();paused=false;}
}
}
