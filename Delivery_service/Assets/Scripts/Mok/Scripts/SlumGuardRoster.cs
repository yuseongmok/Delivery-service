using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 개점할 때 하루에 한 번만 배치한다. 처치한 자리에는 다음 날까지 충원하지 않는다.
public sealed class SlumGuardRoster : MonoBehaviour {
 public GameObject robberPrefab;
 // 이전 씬·에디터 도구 호환용 참조이며 전투에서는 사용하지 않는다.
 public SlumRock projectilePrefab;
 public const int GuardCount=3;
 public Transform[] posts;
 readonly List<SlumRobberCombat> guards=new();
 DayNightCycle clock;int spawnedDay=-1;
 public IReadOnlyList<SlumRobberCombat> Guards=>guards;
 void Start(){clock=FindFirstObjectByType<DayNightCycle>();}
 void Update(){if(clock!=null&&clock.isShopOpen&&spawnedDay!=clock.currentDay)SpawnDay(clock.currentDay);}
 public bool SpawnDay(int day){
  if(spawnedDay==day)return false;
  if(robberPrefab==null||posts==null||posts.Length<GuardCount)return false;
  var positions=new Vector3[GuardCount];
  for(int i=0;i<GuardCount;i++){if(posts[i]==null||!NavMesh.SamplePosition(posts[i].position,out var hit,2,NavMesh.AllAreas))return false;positions[i]=hit.position;}
  foreach(var guard in guards)if(guard!=null){guard.gameObject.SetActive(false);Destroy(guard.gameObject);}guards.Clear();
  foreach(var rock in FindObjectsByType<SlumRock>(FindObjectsSortMode.None))rock.Expire();
  spawnedDay=day;
  for(int i=0;i<GuardCount;i++){
   var go=Instantiate(robberPrefab,positions[i],posts[i].rotation);
   go.name="Slum Guard "+(i+1).ToString("00");
   go.GetComponent<NavMeshAgent>().Warp(positions[i]);
   var guard=go.AddComponent<SlumRobberCombat>();guard.SetPost(positions[i]);guards.Add(guard);
  }
  return true;
 }
}
}
