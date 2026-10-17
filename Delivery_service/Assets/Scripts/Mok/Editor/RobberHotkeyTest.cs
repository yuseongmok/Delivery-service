#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class RobberHotkeyTest {
static void Check(bool ok,string message,Action<string> log){if(!ok)throw new Exception(message);log("PASS: "+message);}
public static IEnumerator RunCore(Action<string> log){
var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.currentTime=6;clock.timeMultiplier=0;clock.isShopOpen=false;
var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();player.GetComponent<PlayerMove>().enabled=false;player.GetComponent<CharacterController>().enabled=false;
Check(NavMesh.SamplePosition(new Vector3(7.5f,0,47.5f),out var spot,20,NavMesh.AllAreas),"테스트 도로 확인",log);player.transform.position=spot.position;Cursor.lockState=CursorLockMode.Locked;
var inv=player.GetComponent<ToppingInventory>();inv.RemoveItem();inv.RemovePizza();inv.ClearPackagedPizzas();inv.AddPackagedDataStack(new List<PizzaData>{new PizzaData(new List<string>{"dough","sauce","cheese"},true)});
Check(!manager.CanSpawnNow,"아침·개점 전 정식 소환 제한 유지",log);Check(!RobberyManager.HasCompletedDelivery,"첫 배달 전 상태 확인",log);
Check(manager.TrySpawnForTesting(),"F8 호출 경로가 아침·개점 전에도 소환",log);yield return null;
var robber=UnityEngine.Object.FindFirstObjectByType<RobberAI>();Check(robber!=null&&robber.IsTestSpawn,"테스트 강도가 영업 종료로 즉시 삭제되지 않음",log);
Check(!manager.TrySpawnForTesting(),"중복 소환 차단",log);
var field=typeof(RobberyManager).GetField("currentDay",BindingFlags.NonPublic|BindingFlags.Instance);Check((int)field.GetValue(manager)==-1&&!RobberyManager.HasCompletedDelivery&&!clock.isShopOpen,"날짜 상태·첫 배달·영업 상태 유지",log);
float end=Time.time+.5f;while(Time.time<end)yield return null;Check(robber!=null,"테스트 강도 생존 확인",log);UnityEngine.Object.Destroy(robber.gameObject);yield return null;
Check(manager.TrySpawnForTesting(),"이전 강도 제거 후 다시 소환 가능",log);robber=UnityEngine.Object.FindFirstObjectByType<RobberAI>();UnityEngine.Object.Destroy(robber.gameObject);
}
}}
#endif
