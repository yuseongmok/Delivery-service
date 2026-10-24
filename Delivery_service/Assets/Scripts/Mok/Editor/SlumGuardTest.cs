#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class SlumGuardTest {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string text,Action<string> log){if(!ok)throw new Exception(text);log("PASS: "+text);}
 static void Set(object target,string field,object value)=>target.GetType().GetField(field,F).SetValue(target,value);
 public static IEnumerator RunCore(Action<string> log){
  var inbox=UnityEngine.Object.FindFirstObjectByType<StoryPhoneInbox>();if(inbox)inbox.AutoOpenEnabled=false;var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();if(phone)phone.ClosePhone();
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.isShopOpen=false;
  var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
  foreach(var car in UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None))car.enabled=false;
  var roster=UnityEngine.Object.FindFirstObjectByType<SlumGuardRoster>();
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var move=player.GetComponent<PlayerMove>();move.enabled=false;
  foreach(var look in player.GetComponentsInChildren<CameraMove>())look.enabled=false;
  var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(-176,0,-5);Cursor.lockState=CursorLockMode.Locked;
  yield return null;Check(roster.Guards.Count==0,"개점 전 강도 미생성",log);
  clock.isShopOpen=true;yield return null;yield return null;
  var gang=roster.Guards.ToArray();Check(gang.Length==3&&gang.All(g=>g.GetComponent<SlumRockThrower>()==null),"고정 근접 강도 3명",log);
  Check(gang.All(g=>g.FixedGuard&&!g.GetComponent<RobberAI>().enabled&&!g.Combat.CanBePickpocketed),"고정 경비는 접촉 강탈·소매치기 제외",log);
  Check(gang.All(g=>g.Combat.chaseSpeed==7&&g.Combat.punchRange==3.4f&&g.Combat.attackStartRange==3.2f),"속도 7·타격 범위 3.4·공격 시도 3.2",log);
  Check(!roster.SpawnDay(clock.currentDay)&&roster.Guards.SequenceEqual(gang),"같은 날 중복 생성 방지",log);
  NavMesh.SamplePosition(new Vector3(-265,0,-5),out var spot,3,NavMesh.AllAreas);player.transform.position=spot.position;yield return null;yield return null;
  Check(gang.All(g=>g.Engaged),"빈민가 진입 시 세 경비 추격",log);
  Check(!manager.TrySpawnForTesting(),"빈민가 랜덤 무리 중복 소환 차단",log);
  var wounded=gang[0];Check(wounded.Combat.ReceiveKick(player,Vector3.forward,30)&&wounded.Combat.Health==70,"경비에게 발차기 피해 30",log);
  player.transform.position=new Vector3(-176,0,-5);float until=Time.time+4;while(Time.time<until)yield return null;
  Check(gang.All(g=>g!=null&&!g.Engaged)&&wounded.Combat.Health==70,"구역 이탈 후 삭제·체력 리셋 없이 귀환",log);
  player.transform.position=spot.position;yield return null;yield return null;Check(wounded.Engaged&&wounded.Combat.Health==70,"재진입 시 남은 체력으로 재추격",log);
  wounded.Combat.ReceiveKick(player,Vector3.forward,100);player.transform.position=new Vector3(-176,0,-5);until=Time.time+6;while(Time.time<until)yield return null;
  Check(roster.Guards.Count(g=>g!=null)==2&&!roster.SpawnDay(clock.currentDay),"사망 경비는 그날 다시 생성하지 않음",log);
  Check(manager.TrySpawnForTesting(),"고정 경비 두 명이 남아도 일반 지역 강도 생성 가능",log);
  foreach(var ordinary in UnityEngine.Object.FindObjectsByType<RobberAI>(FindObjectsSortMode.None))if(ordinary.GetComponent<SlumRobberCombat>()==null)UnityEngine.Object.Destroy(ordinary.gameObject);
  clock.currentDay++;yield return null;yield return null;gang=roster.Guards.ToArray();Check(gang.Length==3&&gang.All(g=>g!=null&&g.Combat.Health==100),"다음 영업일 세 명 새로 배치",log);
  Check(UnityEngine.Object.FindObjectsByType<SlumRockThrower>(FindObjectsSortMode.None).Length==0&&UnityEngine.Object.FindObjectsByType<SlumRock>(FindObjectsSortMode.None).Length==0,"원거리 투석 강도와 투사체가 생성되지 않음",log);
 }
}
}
#endif
