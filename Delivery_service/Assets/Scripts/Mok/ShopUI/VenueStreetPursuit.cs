using System.Linq;
using UnityEngine;
namespace DeliveryService.Mok {
// 행사장 밖에서만 작동하는 유한한 추격 세 파동. 일반 도시 스폰에는 영향을 주지 않는다.
public sealed class VenueStreetPursuit:MonoBehaviour {
 public VenueFinale mission;public Transform[] firstWave,secondWave,thirdWave;
 public Transform secondTrigger,thirdTrigger;public float speed=4.7f;public int maximumActive=12;
 public int WavesStarted{get;private set;}float started;
 public void Begin(){WavesStarted=0;started=Time.time;Wave(firstWave);}
 public void ResetPursuit(){WavesStarted=0;}
 void Wave(Transform[] points){int free=Mathf.Max(0,maximumActive-mission.Enemies.Count(e=>e.Health>0&&!e.Npc.IsDead));mission.SpawnStreetPursuers(points,free,speed);WavesStarted++;}
 void Update(){if(Time.timeScale<=0||!mission||mission.State!=VenueFinale.Phase.StreetEscape)return;float elapsed=Time.time-started;
 if(WavesStarted==1&&elapsed>2&&(Vector3.Distance(mission.Player.transform.position,secondTrigger.position)<9||elapsed>10))Wave(secondWave);
 if(WavesStarted==2&&elapsed>5&&(Vector3.Distance(mission.Player.transform.position,thirdTrigger.position)<9||elapsed>18))Wave(thirdWave);
 }
}
}
