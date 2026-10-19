#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class FootRobberTest {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
 static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
 public static IEnumerator RunCore(Action<string> log){
 var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.timeMultiplier=0;clock.isShopOpen=true;
 var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();player.GetComponent<PlayerMove>().enabled=false;player.GetComponent<CharacterController>().enabled=false;
 var inv=player.GetComponent<ToppingInventory>();inv.RemoveItem();inv.RemovePizza();inv.ClearPackagedPizzas();
 Check(NavMesh.SamplePosition(new Vector3(7.5f,0,47.5f),out var road,20,NavMesh.AllAreas),"검증 도로",log);
 player.transform.position=road.position;Cursor.lockState=CursorLockMode.Locked;
 var data=new PizzaData(new List<string>{"도우","소스","치즈","고추","고추"},true);inv.AddPackagedDataStack(new List<PizzaData>{data});
 Check(manager.CarriedToppingCount==1&&manager.CheckInterval==30&&manager.SpawnChance==45,"도보 기본: 30초 / 45%, 중복 토핑 제외",log);
 data.toppings.Add("젤리");Check(manager.CheckInterval==22.5f&&manager.SpawnChance==55,"도보 토핑 2개: 22.5초 / 55%",log);
 data.toppings.AddRange(new[]{"초콜릿","파인애플"});Check(manager.CheckInterval==15&&manager.SpawnChance==65,"도보 토핑 4개: 15초 / 65%",log);
 Check(manager.CanSpawnNow,"영업 중 도보 출몰 허용",log);
 clock.isShopOpen=false;Check(!manager.CanSpawnNow,"개점 전 보호",log);clock.isShopOpen=true;
 Cursor.lockState=CursorLockMode.None;Check(!manager.CanSpawnNow,"메뉴 조작 중 보호",log);Cursor.lockState=CursorLockMode.Locked;
 player.transform.position=GameObject.Find("PizzaStore").transform.Find("Mok Pizza Shop/Foundation").position;Check(manager.IsInsideShop&&!manager.CanSpawnNow,"매장 안 보호",log);
 var district=UnityEngine.Object.FindFirstObjectByType<RobberyDistrict>();player.transform.position=district.transform.TransformPoint(district.localCenter);
 Check(manager.InHighRiskDistrict&&manager.CheckInterval==8&&manager.SpawnChance==100&&manager.ActiveLimit==3,"빈민촌 8초 / 100%, 최대 3명 유지",log);
 player.transform.position=road.position;
 var storage=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();Set(storage,"storedPizzas",new List<PizzaData>{data});
 inv.ClearPackagedPizzas();var bike=storage.bikeController;Check(bike.EnterBike(player.gameObject),"실제 탑승",log);
 Check(manager.CheckInterval==20&&manager.SpawnChance==50,"탑승 토핑 4개: 20초 / 50%",log);
 data.toppings=new List<string>{"도우","소스","치즈","고추","젤리"};Check(manager.CheckInterval==30&&manager.SpawnChance==40,"탑승 토핑 2개: 30초 / 40%",log);
 data.toppings=new List<string>{"도우","소스","치즈"};Check(manager.CheckInterval==40&&manager.SpawnChance==30,"탑승 기본: 40초 / 30%",log);
 bike.ExitBike();player.transform.position=road.position;Cursor.lockState=CursorLockMode.Locked;
 Check(manager.CheckInterval==30&&manager.SpawnChance==45,"하차 직후 도보 보정 복귀",log);
 Set(manager,"encounterProgress",.5f);RobberyManager.NotifyEncounterEnded();
 float until=(float)typeof(RobberyManager).GetField("protectedUntil",Flags).GetValue(manager);
 Check(until-Time.unscaledTime>44&&until-Time.unscaledTime<=45.1f&&!manager.CanSpawnNow,"교전 후 45초 보호 (남은 시간: "+(until-Time.unscaledTime)+", 출몰 허용: "+manager.CanSpawnNow+")",log);
 Set(manager,"protectedUntil",0f);Check(manager.CanSpawnNow,"보호 종료 후 재출몰 허용",log);
 Check(manager.TrySpawnForTesting(),"실제 강도 생성 경로",log);yield return null;
 Check(!manager.CanSpawnNow,"일반 지역 동시 중복 출몰 방지",log);
 }
}}
#endif
