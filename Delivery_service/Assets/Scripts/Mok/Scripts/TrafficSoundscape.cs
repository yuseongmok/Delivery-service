using System.Linq;using UnityEngine;
namespace DeliveryService.Mok {
// 가까운 교통 차량 여섯 대만 재생한다. 차량 이동 로직은 건드리지 않는다.
public sealed class TrafficSoundscape:MonoBehaviour {
 sealed class Voice {public CarMov car;public AudioSource audio;public Vector3 previous;}
 System.Collections.Generic.List<Voice> voices=new();float scan,updateAt;AudioListener listener;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Create(){var g=new GameObject("Traffic audio budget");DontDestroyOnLoad(g);g.AddComponent<TrafficSoundscape>();}
 void Update(){
  var bank=SuppliedAudio.Current;if(!bank||bank.traffic==null||bank.traffic.Length==0)return;
  if(Time.unscaledTime>=scan){scan=Time.unscaledTime+3;voices.RemoveAll(v=>!v.car);foreach(var car in FindObjectsByType<CarMov>(FindObjectsSortMode.None)){if(voices.Any(v=>v.car==car))continue;var a=car.gameObject.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=true;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=3;a.maxDistance=30;a.volume=0;a.clip=bank.traffic[voices.Count%bank.traffic.Length];voices.Add(new Voice{car=car,audio=a,previous=car.transform.position});}}
  if(Time.unscaledTime<updateAt)return;float dt=Mathf.Max(.01f,Time.unscaledTime-updateAt+.25f);updateAt=Time.unscaledTime+.25f;
  listener=FindObjectsByType<AudioListener>(FindObjectsSortMode.None).FirstOrDefault(l=>l.isActiveAndEnabled);
  var audible=listener?voices.Where(v=>v.car&&v.car.isActiveAndEnabled&&Vector3.Distance(v.car.transform.position,listener.transform.position)<30).OrderBy(v=>(v.car.transform.position-listener.transform.position).sqrMagnitude).Take(6).ToArray():new Voice[0];
  foreach(var v in voices){if(!v.car||!v.audio)continue;float speed=Vector3.Distance(v.previous,v.car.transform.position)/dt;v.previous=v.car.transform.position;if(Time.timeScale<=0||!audible.Contains(v)){v.audio.Stop();continue;}v.audio.pitch=Mathf.Lerp(.85f,1.2f,Mathf.Clamp01(speed/15));v.audio.volume=.16f*SuppliedAudio.Volume;if(!v.audio.isPlaying){v.audio.time=Random.Range(0,v.audio.clip.length);v.audio.Play();}}
 }
 void OnDestroy(){foreach(var v in voices)if(v.audio)Destroy(v.audio);}
}
}
