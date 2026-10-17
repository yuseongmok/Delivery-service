#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.AI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Editor {
public static class RobberTest {
static void Set(object o,string key,object value)=>o.GetType().GetField(key,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(o,value);
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
static void Capture(Transform target,string name){
var lamp=new GameObject("검증 조명").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=2;lamp.transform.rotation=Quaternion.Euler(35,210,0);
var o=new GameObject("강도 검증 카메라");var c=o.AddComponent<Camera>();c.enabled=false;c.nearClipPlane=.05f;c.farClipPlane=60;c.fieldOfView=38;c.cullingMask=~((1<<5)|(1<<17)|(1<<18)|(1<<20)|(1<<22));c.transform.position=target.position+target.rotation*new Vector3(1.7f,1.5f,3.1f);c.transform.LookAt(target.position+Vector3.up*1.05f);lamp.transform.rotation=c.transform.rotation;
var rt=new RenderTexture(1000,1000,24);rt.Create();c.targetTexture=rt;var previous=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-robber-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(o);UnityEngine.Object.Destroy(lamp.gameObject);}}
public static IEnumerator RunCore(Action<string> log){
var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.currentTime=18;clock.timeMultiplier=0;clock.isShopOpen=true;
var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var move=player.GetComponent<PlayerMove>();move.enabled=false;var cc=player.GetComponent<CharacterController>();cc.enabled=false;
var inv=player.GetComponent<ToppingInventory>();inv.RemoveItem();inv.RemovePizza();inv.ClearPackagedPizzas();
Check(NavMesh.SamplePosition(new Vector3(7.5f,0,47.5f),out var spot,20,NavMesh.AllAreas),"테스트 도로 위치 확인",log);player.transform.position=spot.position;Cursor.lockState=CursorLockMode.Locked;
var data=new PizzaData(new List<string>{"dough","sauce","cheese","pineapple"},true);inv.AddPackagedDataStack(new List<PizzaData>{data});
var bikeStorage=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();Set(bikeStorage,"storedPizzas",new List<PizzaData>{new PizzaData(data)});
Check(manager.CanSpawnNow,"첫 배달 전에도 운반 중 등장 가능",log);RobberyManager.NotifyDeliveryCompleted();Check(RobberyManager.HasCompletedDelivery,"배달 완료 알림 연결",log);
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Robbery.prefab");
RobberAI Spawn(float distance){var r=UnityEngine.Object.Instantiate(prefab,spot.position+Vector3.forward*distance,Quaternion.Euler(0,180,0)).GetComponent<RobberAI>();r.GetComponent<NavMeshAgent>().Warp(spot.position+Vector3.forward*distance);return r;}
var robber=Spawn(5);yield return null;
Check(robber.GetComponent<NPCControl>().ExternalControl,"시민 배회와 강도 이동 충돌 방지",log);
Check(robber.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh==AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scripts/Mok/Art/Robber/RobberBody.asset"),"전용 외형 메시 저장",log);
Check(robber.GetComponentsInChildren<Transform>().Any(t=>t.name=="Robber mask"),"마스크가 프리팹에 저장됨",log);
float limit=Time.time+10;while(robber.State==RobberAI.Phase.Approach&&Time.time<limit)yield return null;
log($"진단: state={robber.State}, pos={robber.transform.position}, player={player.transform.position}, available={player.CanReceiveHit}, held={inv.HasPackagedPizzas()}, path={robber.GetComponent<NavMeshAgent>().pathStatus}, velocity={robber.GetComponent<NavMeshAgent>().velocity}, remaining={robber.GetComponent<NavMeshAgent>().remainingDistance}"); Capture(robber.transform,"approach"); Check(robber.State==RobberAI.Phase.Windup,"실제 경로 추격 후 강탈 준비 진입",log);
Check(inv.HasPackagedPizzas(),"준비 동작 중에는 피자 보존",log);Capture(robber.transform,"windup");
player.transform.position=spot.position-Vector3.forward*5;
{float end=Time.time+1.2f;while(Time.time<end)yield return null;}Check(inv.HasPackagedPizzas()&&robber.StolenCount==0,"범위를 벗어나면 강탈 실패",log);UnityEngine.Object.Destroy(robber.gameObject);yield return null;
player.transform.position=spot.position;robber=Spawn(1.3f);yield return null;
// 실제 벽 콜라이더로 시야 차단을 확인한다.
var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=spot.position+new Vector3(0,1,.65f);wall.transform.localScale=new Vector3(3,2,.15f);Physics.SyncTransforms();
Check(!robber.CanReachVictim(),"벽 너머 강탈 차단",log);UnityEngine.Object.Destroy(wall);yield return null;
limit=Time.time+4;while(robber.StolenCount==0&&Time.time<limit)yield return null;
Check(robber.StolenCount==1&&!inv.HasPackagedPizzas(),"강탈 시 피자 소유권 이동",log);
Check(robber.State==RobberAI.Phase.StolenPause,"강탈 직후 반격 시간 제공",log);yield return null;Capture(robber.transform,"carrying");
Check(robber.transform.Find("Stolen pizza visual").gameObject.activeSelf,"도주 박스 표시",log);
{float end=Time.time+1.2f;while(Time.time<end)yield return null;}Check(robber.State==RobberAI.Phase.Escape,"강탈 후 즉시 삭제하지 않고 도주",log);
Check(bikeStorage.StoredCount==1,"강탈 후에도 배달통 피자 보존",log);
{float end=Time.time+.35f;while(Time.time<end)yield return null;}
Check(robber.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Run"),"도주 달리기 애니메이션 전환",log);Capture(robber.transform,"running");
Check(robber.ReceiveKick(-Vector3.forward),"강도에게 발차기 피격 연결",log);
yield return null;Check(robber.GetComponent<NPCControl>().IsKnockedDown,"기존 레그돌 작동",log);
var loot=UnityEngine.Object.FindFirstObjectByType<RobberLoot>();Check(loot!=null&&loot.Count==1&&robber.StolenCount==0,"피자를 바닥에 한 번만 떨어뜨림",log);
Check(!robber.ReceiveKick(Vector3.forward),"넘어진 중 중복 피격 차단",log);
inv.AddItem(AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g))).First());loot.Interact(inv);Check(loot.Count==1,"손이 차 있으면 회수 보류",log);inv.RemoveItem();
player.transform.position=loot.transform.position-Vector3.forward*1.3f;player.View.transform.position=player.transform.position+Vector3.up*1.2f;player.View.transform.LookAt(loot.transform.position+Vector3.up*.06f);Physics.SyncTransforms();
typeof(PlayerInteraction).GetMethod("TryInteract",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(player.GetComponent<PlayerInteraction>(),null);
Check(inv.HasPackagedPizzas()&&ReferenceEquals(inv.GetHeldPackagedPizzas()[0],data),"레시피와 굽기 상태 그대로 회수",log);loot.Interact(inv);Check(inv.GetHeldPackagedPizzas().Count==1,"중복 회수 방지",log);
{float end=Time.time+3.2f;while(Time.time<end)yield return null;}Check(!robber.GetComponent<NPCControl>().IsKnockedDown&&robber.State==RobberAI.Phase.Escape,"일어난 강도는 재강탈 없이 도주",log);
UnityEngine.Object.Destroy(robber.gameObject);yield return null;
player.transform.position=spot.position;robber=Spawn(1.3f);yield return null;
player.View.transform.position=player.transform.position+Vector3.up*1.2f;player.View.transform.LookAt(robber.transform.position+Vector3.up*1.1f);Physics.SyncTransforms();
Check(player.ResolveKick(),"플레이어 실제 발차기 판정이 강도에게 적중",log);Check(inv.HasPackagedPizzas()&&robber.StolenCount==0,"강탈 전 저지하면 피자 보존",log);
Check(UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None).Length==0,"강도 방어 발차기로 경찰이 출동하지 않음",log);
UnityEngine.Object.Destroy(robber.gameObject);yield return null;
var orders=UnityEngine.Object.FindFirstObjectByType<OrderManager>();Set(orders,"activeOrder",new PizzaOrder{orderName="주문 01",toppings=new List<string>{"pineapple"}});Set(orders,"currentTargetDeliveryID","RobberTestDestination");Set(orders,"deliveryDeadlineMinutes",(double)(clock.currentDay*1440+clock.currentTime*60+80));
Check(orders.TryDeliverPackagedStack(inv.GetHeldPackagedPizzas(),"RobberTestDestination"),"회수한 피자로 기존 배달 완료",log);
var bike=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();Check(bike!=null,"기존 배달통 유지",log);
inv.ClearPackagedPizzas();robber=Spawn(1.3f);yield return null;int money=MoneyManager.Instance.currentMoney;{float end=Time.time+1.3f;while(Time.time<end)yield return null;}Check(MoneyManager.Instance.currentMoney==money,"피자가 없을 때 현금 강탈하지 않음",log);UnityEngine.Object.Destroy(robber.gameObject);
yield return null;inv.AddPackagedDataStack(new List<PizzaData>{data});player.transform.position=spot.position;Cursor.lockState=CursorLockMode.Locked;
Set(manager,"protectedUntil",0f);Check(manager.CanSpawnNow,"보호시간 이후 등장 자격 확인",log);
Set(manager,"protectedUntil",Time.unscaledTime+30);Check(!manager.CanSpawnNow,"병원 회복 보호시간 등장 차단",log);Set(manager,"protectedUntil",0f);
clock.isShopOpen=false;Check(!manager.CanSpawnNow,"마감 상태 등장 차단",log);clock.isShopOpen=true;
Set(manager,"lowChance",100f);Set(manager,"currentDay",clock.currentDay);Set(manager,"encounterProgress",1f);typeof(RobberyManager).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(manager,null);
yield return null;var spawned=UnityEngine.Object.FindObjectsByType<RobberAI>(FindObjectsSortMode.None);Check(spawned.Length==1,"기존 매니저가 새 프리팹을 실제 생성",log);Check(Vector3.Distance(spawned[0].transform.position,player.transform.position)>9,"바로 옆 생성 방지",log);
Check(!manager.CanSpawnNow,"기존 강도가 있는 동안 중복 등장 차단",log);UnityEngine.Object.Destroy(spawned[0].gameObject);

}
}}
#endif
