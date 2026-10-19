#if UNITY_EDITOR
using System;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class SlumWaveTest {
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
public static IEnumerator RunCore(Action<string> log){
 void Check(bool ok,string s){if(!ok)throw new Exception(s);log("PASS: "+s);}
 var m=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();m.enabled=false;
 var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.isShopOpen=true;clock.timeMultiplier=0;
 var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();p.GetComponent<CharacterController>().enabled=false;p.GetComponent<PlayerMove>().enabled=false;
 var d=UnityEngine.Object.FindFirstObjectByType<RobberyDistrict>();
 Check(NavMesh.SamplePosition(d.transform.TransformPoint(d.localCenter),out var point,40,NavMesh.AllAreas),"빈민촌 이동 가능 위치");
 p.transform.position=point.position;Cursor.lockState=CursorLockMode.Locked;
 Check(m.InHighRiskDistrict&&m.CheckInterval==8&&m.SpawnChance==100&&m.ActiveLimit==3,"빈민촌 8초/100%, 최대 3명");
 Set(m,"districtWaveMin",2);Set(m,"districtWaveMax",2);
 Check(m.TrySpawnForTesting(),"2명 무리 실제 생성");
 var wave=SceneActors.Robbers.ToArray();Check(wave.Length==2&&wave.All(x=>x.GetComponent<SlumRobberCombat>()!=null),"같은 프레임 2명 생성 및 빈민촌 전투 연결");
 foreach(var r in wave){var combat=r.GetComponent<SlumRobberCombat>().Combat;Check(Mathf.Approximately(combat.chaseSpeed,5.75f),"빈민촌 추격 설정 5.75");combat.Provoke(p);Check(Mathf.Approximately(r.GetComponent<NavMeshAgent>().speed,5.75f),"실제 추격 이동 속도 5.75 적용");Check(Mathf.Approximately(combat.punchCooldown,1.4f),"공격 주기 유지");}
 Check((wave[0].transform.position-wave[1].transform.position).sqrMagnitude>=6.25f,"무리끼리 위치 겹침 방지");
 Check(!m.TrySpawnForTesting()&&!m.CanSpawnNow&&SceneActors.Robbers.Count()==2,"슬롯 1개만 남으면 단독 증원 차단");
 float until=(float)typeof(RobberyManager).GetField("districtProtectedUntil",F).GetValue(m);
 Check(until-Time.unscaledTime>9&&until-Time.unscaledTime<=10,"빈민촌 재출몰 보호 10초");
 foreach(var r in wave)UnityEngine.Object.Destroy(r.gameObject);yield return null;
 // 경로가 없는 높이에서는 일부 인원도 생성되지 않는다.
 p.transform.position+=Vector3.up*100;Check(!m.TrySpawnForTesting()&&!SceneActors.Robbers.Any(),"경로 확보 실패 시 무리 전체 생성 보류");
 p.transform.position=point.position;Set(m,"districtProtectedUntil",0f);Set(m,"districtWaveMax",3);
 for(int i=0;i<5;i++){
  Check(m.TrySpawnForTesting(),"반복 무리 생성 "+i);var batch=SceneActors.Robbers.ToArray();Check(batch.Length>=2&&batch.Length<=3,"매번 2~3명 보장 "+i);
  foreach(var r in batch)UnityEngine.Object.Destroy(r.gameObject);yield return null;
 }
 var other=FootRobberTest.RunCore(log);while(other.MoveNext())yield return other.Current;
}
}}
#endif
