#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class RegularRobberPairTest {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string text,Action<string> log){if(!ok)throw new Exception(text);log("PASS: "+text);}
 static RobberAI[] Ordinary()=>UnityEngine.Object.FindObjectsByType<RobberAI>(FindObjectsSortMode.None).Where(r=>r.GetComponent<SlumRobberCombat>()==null).ToArray();
 public static IEnumerator RunCore(Action<string> log){
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.isShopOpen=true;
  var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();player.GetComponent<PlayerMove>().enabled=false;player.GetComponent<CharacterController>().enabled=false;player.transform.position=new Vector3(-176,0,-5);Cursor.lockState=CursorLockMode.Locked;
  var inventory=player.GetComponent<ToppingInventory>();inventory.RemoveItem();inventory.RemovePizza();inventory.ClearPackagedPizzas();
  yield return null;yield return null;
  var roster=UnityEngine.Object.FindFirstObjectByType<SlumGuardRoster>();Check(roster.Guards.Count==5,"빈민가 고정 5명 유지",log);
  foreach(var name in new[]{"lowChance","mediumChance","highChance"}){float expected=name=="lowChance"?45:name=="mediumChance"?55:65;Check((float)typeof(RobberyManager).GetField(name,F).GetValue(manager)==expected,"씬 저장 확률 "+name+"="+expected,log);}
  Check(manager.SpawnChance==60&&manager.ActiveLimit==2,"도보 기본 확률 60%·일반 최대 2명",log);
  inventory.AddPackagedDataStack(new List<PizzaData>{new PizzaData(new List<string>{"dough","sauce","cheese","pepper","pineapple"},true)});Check(manager.SpawnChance==70,"토핑 두 종류 도보 확률 70%",log);
  inventory.ClearPackagedPizzas();inventory.AddPackagedDataStack(new List<PizzaData>{new PizzaData(new List<string>{"dough","sauce","cheese","pepper","pineapple","jelly","chocolate"},true)});Check(manager.SpawnChance==80,"토핑 네 종류 도보 확률 80%",log);
  Check(manager.TrySpawnForTesting(),"고정 경비 5명과 별개로 일반 무리 생성",log);yield return null;
  var pair=Ordinary();Check(pair.Length==2&&pair.All(r=>r.enabled),"일반 강탈형 AI 정확히 두 명 생성",log);Check(Vector3.Distance(pair[0].transform.position,pair[1].transform.position)>=2.4f,"두 강도 겹치지 않게 배치",log);
  Check(!manager.TrySpawnForTesting()&&Ordinary().Length==2,"기존 무리 생존 중 추가 생성 차단",log);
  UnityEngine.Object.Destroy(pair[0].gameObject);yield return null;Check(!manager.TrySpawnForTesting()&&Ordinary().Length==1,"한 명만 남아도 새 무리 충원하지 않음",log);
  UnityEngine.Object.Destroy(pair[1].gameObject);yield return null;Check(manager.TrySpawnForTesting()&&Ordinary().Length==2,"기존 무리 제거 후 다음 두 명 생성 가능",log);
  foreach(var r in Ordinary())UnityEngine.Object.Destroy(r.gameObject);yield return null;
  player.transform.position=new Vector3(-265,0,-5);Check(!manager.TrySpawnForTesting()&&Ordinary().Length==0,"빈민가에서 일반 두 명 생성 차단",log);Check(roster.Guards.Count(g=>g!=null)==5,"빈민가 다섯 명 중복·삭제 없음",log);
 }
}
}
#endif
