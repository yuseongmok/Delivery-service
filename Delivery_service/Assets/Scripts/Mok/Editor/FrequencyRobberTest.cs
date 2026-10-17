#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class FrequencyRobberTest {
const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
public static IEnumerator RunCore(Action<string> log){
var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.currentTime=8;clock.timeMultiplier=0;clock.isShopOpen=true;
var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();player.GetComponent<PlayerMove>().enabled=false;player.GetComponent<CharacterController>().enabled=false;
var inv=player.GetComponent<ToppingInventory>();inv.RemoveItem();inv.RemovePizza();inv.ClearPackagedPizzas();
NavMesh.SamplePosition(new Vector3(7.5f,0,47.5f),out var road,20,NavMesh.AllAreas);player.transform.position=road.position;Cursor.lockState=CursorLockMode.Locked;
var data=new PizzaData(new List<string>{"도우","소스","치즈","고추","고추"},true);inv.AddPackagedDataStack(new List<PizzaData>{data});
Check(manager.CarriedToppingCount==1&&manager.CheckInterval==60&&manager.SpawnChance==15,"기본 재료 제외 및 중복 토핑 제외: 60초/15%",log);
data.toppings.Add("젤리");Check(manager.CheckInterval==45&&manager.SpawnChance==25,"토핑 2개: 45초/25%",log);
data.toppings.AddRange(new[]{"초콜릿","파인애플"});Check(manager.CheckInterval==30&&manager.SpawnChance==35,"토핑 4개: 30초/35%",log);
Check(manager.CanSpawnNow,"첫 배달 전 오전에도 출몰 가능",log);
player.transform.position=GameObject.Find("PizzaStore").transform.Find("Mok Pizza Shop/Foundation").position;Check(manager.IsInsideShop&&!manager.CanSpawnNow,"매장 안 출몰 차단",log);player.transform.position=road.position;
var update=typeof(RobberyManager).GetMethod("Update",Flags);Set(manager,"currentDay",clock.currentDay);Set(manager,"encounterProgress",.5f);inv.ClearPackagedPizzas();update.Invoke(manager,null);Check((float)typeof(RobberyManager).GetField("encounterProgress",Flags).GetValue(manager)==.5f,"피자를 내려놓아도 진행률 보존",log);inv.AddPackagedDataStack(new List<PizzaData>{data});
Set(manager,"highChance",100f);Set(manager,"encounterProgress",1f);update.Invoke(manager,null);yield return null;
var robber=UnityEngine.Object.FindFirstObjectByType<RobberAI>();Check(robber!=null,"등장 간격 종료 후 실제 강도 생성",log);Check(!manager.CanSpawnNow,"강도 중복 등장 차단",log);
Check(robber.GetComponent<NavMeshAgent>().speed==5,"프리팹 추격 속도 5 적용",log);
robber.ReceiveKick(Vector3.forward);UnityEngine.Object.Destroy(robber.gameObject);yield return null;Check(!manager.CanSpawnNow,"저지 후 90초 보호",log);
float until=(float)typeof(RobberyManager).GetField("protectedUntil",Flags).GetValue(manager);Check(until-Time.unscaledTime>88&&until-Time.unscaledTime<=90,"현실 90초 보호시간 설정",log);Set(manager,"protectedUntil",0f);Check(manager.CanSpawnNow,"같은 날 다시 출몰 가능",log);
var storage=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();Set(storage,"storedPizzas",new List<PizzaData>{new PizzaData(new List<string>{"dough","sauce","cheese"},true),data});inv.ClearPackagedPizzas();var bike=storage.bikeController;
Check(bike.EnterBike(player.gameObject),"오토바이 탑승",log);Check(manager.CarriedToppingCount==4,"여러 배달 상자 중 최대 토핑 수 사용",log);
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Robbery.prefab");robber=UnityEngine.Object.Instantiate(prefab,road.position,Quaternion.identity).GetComponent<RobberAI>();robber.IsTestSpawn=true;yield return null;
var stolen=(List<PizzaData>)typeof(RobberAI).GetField("stolen",Flags).GetValue(robber);stolen.Add(data);
var accident=bike.GetComponentInChildren<MotorcycleAccident>(true);Check(accident!=null,"기존 차량 사고 센서 연결",log);Set(bike,"currentSpeed",8f);
typeof(MotorcycleAccident).GetMethod("OnTriggerEnter",Flags).Invoke(accident,new object[]{robber.GetComponent<Collider>()});
Check(robber.GetComponent<NPCControl>().IsDead,"차량 충돌 콜백으로 강도 레그돌 처리",log);var loot=UnityEngine.Object.FindFirstObjectByType<RobberLoot>();Check(loot!=null&&loot.Count==1&&robber.StolenCount==0,"차량 제압 시 도난 피자 반환",log);
Check(bike.IsControlAvailable,"강도 충돌 후 사고 창으로 조작 차단하지 않음",log);
typeof(MotorcycleAccident).GetMethod("OnTriggerEnter",Flags).Invoke(accident,new object[]{robber.GetComponent<Collider>()});Check(loot.Count==1,"중복 충돌로 피자 복제되지 않음",log);
bike.ExitBike();loot.Interact(inv);Check(inv.HasPackagedPizzas()&&ReferenceEquals(inv.GetHeldPackagedPizzas()[0],data),"차량 제압 후 피자 원본 회수",log);
float end=Time.time+5.3f;while(Time.time<end)yield return null;Check(robber==null,"기존 사고와 동일하게 5초 후 퇴장",log);
}
}}
#endif
