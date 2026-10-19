using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 개점할 때 하루에 한 번만 배치한다. 처치한 자리에는 다음 날까지 충원하지 않는다.
public sealed class SlumGuardRoster : MonoBehaviour {
 public GameObject robberPrefab;
 public SlumRock projectilePrefab;
 public Transform[] posts;
 readonly List<SlumRobberCombat> guards=new();
 DayNightCycle clock;int spawnedDay=-1;
 public IReadOnlyList<SlumRobberCombat> Guards=>guards;
 void Start(){clock=FindFirstObjectByType<DayNightCycle>();}
 void Update(){if(clock!=null&&clock.isShopOpen&&spawnedDay!=clock.currentDay)SpawnDay(clock.currentDay);}
 public bool SpawnDay(int day){
  if(spawnedDay==day)return false;
  if(robberPrefab==null||projectilePrefab==null||posts==null||posts.Length!=5)return false;
  var positions=new Vector3[5];
  for(int i=0;i<5;i++){if(posts[i]==null||!NavMesh.SamplePosition(posts[i].position,out var hit,2,NavMesh.AllAreas))return false;positions[i]=hit.position;}
  foreach(var guard in guards)if(guard!=null){guard.gameObject.SetActive(false);Destroy(guard.gameObject);}guards.Clear();
  foreach(var rock in FindObjectsByType<SlumRock>(FindObjectsSortMode.None))rock.Expire();
  spawnedDay=day;
  for(int i=0;i<5;i++){
   var go=Instantiate(robberPrefab,positions[i],posts[i].rotation);
   go.name=i==4?"Slum Guard 05 - Rock Thrower":"Slum Guard "+(i+1).ToString("00");
   go.GetComponent<NavMeshAgent>().Warp(positions[i]);
   var guard=go.AddComponent<SlumRobberCombat>();guard.SetPost(positions[i]);guards.Add(guard);
   if(i==4){var thrower=go.AddComponent<SlumRockThrower>();thrower.projectilePrefab=projectilePrefab;}
  }
  return true;
 }
}
}
