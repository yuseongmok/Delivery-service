#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class BikeRobberTest {
const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
public static IEnumerator RunCore(Action<string> log){
var screen=new Vector2(1920,1080);
var right=RobberTheftFeedback.ProjectMarker(new Vector3(1.5f,.5f,1),screen,40);
var left=RobberTheftFeedback.ProjectMarker(new Vector3(-.5f,.5f,1),screen,40);
var back=RobberTheftFeedback.ProjectMarker(new Vector3(.5f,.5f,-1),screen,40);
var behindRight=RobberTheftFeedback.ProjectMarker(new Vector3(-.5f,.5f,-1),screen,40);
Check(right.x==1880&&Mathf.Abs(right.z+90)<.01f,"화면 오른쪽 바깥 방향 표시",log);
Check(left.x==40&&Mathf.Abs(left.z-90)<.01f,"화면 왼쪽 바깥 방향 표시",log);
Check(back.y==1040&&behindRight.x==1880,"정후방 및 오른쪽 뒤 방향 보정",log);
var above=RobberTheftFeedback.ProjectMarker(new Vector3(.5f,2,1),screen,40);
Check(above.y==40,"화면 위쪽 가장자리 표시",log);

var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.currentTime=6;clock.timeMultiplier=0;clock.isShopOpen=false;
var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();player.GetComponent<PlayerMove>().enabled=false;player.GetComponent<CharacterController>().enabled=false;
var inv=player.GetComponent<ToppingInventory>();inv.RemoveItem();inv.RemovePizza();inv.ClearPackagedPizzas();
var storage=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();var visual=storage.GetComponent<BikeCargoVisual>();var bike=storage.bikeController;
Check(NavMesh.SamplePosition(new Vector3(7.5f,0,47.5f),out var road,20,NavMesh.AllAreas),"검증 도로 확인",log);
var start=road.position+Vector3.up*.5f;bike.transform.position=start;var offset=(Vector3)typeof(MotorcycleController).GetField("localOffset",Flags).GetValue(bike);bike.sphereRB.position=start-bike.transform.rotation*offset;bike.sphereRB.transform.position=bike.sphereRB.position;Physics.SyncTransforms();bike.sphereRB.constraints=RigidbodyConstraints.FreezeAll;bike.sphereRB.linearVelocity=Vector3.zero;
var original=new PizzaData(new List<string>{"dough","sauce","cheese"},true);Set(storage,"storedPizzas",new List<PizzaData>{original,new PizzaData(original),new PizzaData(original)});visual.Refresh();
Cursor.lockState=CursorLockMode.Locked;Check(bike.EnterBike(player.gameObject),"실제 오토바이 탑승",log);yield return null;
Check(manager.TrySpawnForTesting(),"탑승 중 F8 소환 경로",log);var robber=UnityEngine.Object.FindFirstObjectByType<RobberAI>();
void Place(RobberAI r){var wanted=storage.transform.position-bike.transform.forward*1.2f;log("POSITION bike="+bike.transform.position+" cargo="+storage.transform.position+" road="+road.position);wanted.y=road.position.y;Check(NavMesh.SamplePosition(wanted,out var spot,3,NavMesh.AllAreas),"배달통 뒤 접근 위치",log);r.GetComponent<NavMeshAgent>().Warp(spot.position);Physics.SyncTransforms();}
RobberAI Spawn(){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Robbery.prefab");var r=UnityEngine.Object.Instantiate(prefab).GetComponent<RobberAI>();r.IsTestSpawn=true;Place(r);return r;}
Place(robber);
var feedback=robber.GetComponent<RobberTheftFeedback>();Check(!feedback.WarningVisible&&!feedback.MarkerVisible&&feedback.AlertCount==0,"강탈 전 경고·표시·효과음 없음",log);
var update=typeof(RobberAI).GetMethod("Update",Flags);
bike.SetControllable(false);update.Invoke(robber,null);Check(storage.StoredCount==3,"조작 제한 중 강탈 차단",log);bike.SetControllable(true);
Set(bike,"currentSpeed",.5f);update.Invoke(robber,null);
Check(storage.StoredCount==2&&robber.StolenCount==1,"천천히 움직이는 오토바이에서 대기 없이 한 상자 강탈",log);
Check(feedback.WarningVisible&&feedback.MarkerVisible&&feedback.AlertCount==1&&feedback.AlarmPlaying,"강탈 즉시 경고·추적 표시·효과음 재생",log);
Check(robber.State==RobberAI.Phase.Escape,"강탈 즉시 도주",log);Check(!visual.IsBusy&&Quaternion.Angle(visual.Lid.localRotation,Quaternion.identity)<1,"뚜껑 연출 없이 잠금 해제",log);
update.Invoke(robber,null);Check(storage.StoredCount==2,"한 강도가 연속으로 여러 상자를 훔치지 않음",log);Check(feedback.AlertCount==1,"경고 효과음 중복 재생 방지",log);
bike.ExitBike();player.transform.position=robber.transform.position-robber.transform.forward*1.2f;player.View.transform.position=player.transform.position+Vector3.up*1.2f;player.View.transform.LookAt(robber.transform.position+Vector3.up*1.1f);Cursor.lockState=CursorLockMode.Locked;Physics.SyncTransforms();
Check(player.ResolveKick(),"하차 후 발차기 적중",log);Check(!feedback.MarkerVisible,"피자를 떨어뜨리면 강도 표시 제거",log);var loot=UnityEngine.Object.FindFirstObjectByType<RobberLoot>();Check(loot!=null&&loot.Count==1,"도난 상자 반환",log);loot.Interact(inv);Check(inv.HasPackagedPizzas()&&ReferenceEquals(inv.GetHeldPackagedPizzas()[0],original),"원본 피자 데이터 회수",log);
UnityEngine.Object.Destroy(robber.gameObject);yield return null;inv.ClearPackagedPizzas();Check(bike.EnterBike(player.gameObject),"재탑승",log);
robber=Spawn();robber.enabled=false;yield return null;robber.enabled=true;typeof(RobberAI).GetMethod("Start",Flags).Invoke(robber,null);Place(robber);Set(bike,"currentSpeed",8f);update.Invoke(robber,null);Check(storage.StoredCount==1&&robber.StolenCount==1,"속도 제한 없이 가까이 접근하면 강탈",log);
Check(robber.ReceiveBikeImpact(Vector3.forward,8),"차량 제압 연결",log);Check(UnityEngine.Object.FindFirstObjectByType<RobberLoot>()!=null,"차량 제압 후 회수 피자 생성",log);UnityEngine.Object.Destroy(robber.gameObject);yield return null;
robber=Spawn();yield return null;robber.GetComponent<NavMeshAgent>().Warp(road.position+Vector3.forward*8);Physics.SyncTransforms();Check(!robber.CanReachCargo(storage),"멀리서 강탈하지 않음",log);UnityEngine.Object.Destroy(robber.gameObject);yield return null;
robber=Spawn();yield return null;
var stolen=(List<PizzaData>)typeof(RobberAI).GetField("stolen",Flags).GetValue(robber);
if(stolen.Count==0)stolen.Add(original);
typeof(RobberAI).GetMethod("BeginEscape",Flags).Invoke(robber,null);
Set(robber,"stolenElapsed",0f);Check(robber.RecoverySecondsRemaining==45,"기본 회수 제한 현실 45초",log);
Set(robber,"stateStarted",Time.time-50);Set(robber,"hasExit",false);
player.View.transform.LookAt(player.View.transform.position-Vector3.forward);
update.Invoke(robber,null);yield return null;Check(robber!=null&&robber.StolenCount>0,"상태 경과나 시야 이탈만으로 조기 폐기하지 않음",log);
int lootCount=UnityEngine.Object.FindObjectsByType<RobberLoot>(FindObjectsSortMode.None).Length;
Set(robber,"stolenElapsed",45f);update.Invoke(robber,null);
Check(robber.StolenCount==0&&!robber.GetComponent<RobberTheftFeedback>().MarkerVisible,"제한시간 종료 시 도난 피자와 화살표 제거",log);
yield return null;Check(robber==null,"제한시간 종료 시 강도 퇴장",log);
Check(UnityEngine.Object.FindObjectsByType<RobberLoot>(FindObjectsSortMode.None).Length==lootCount,"폐기 피자를 회수 상자로 생성하지 않음",log);
bike.ExitBike();
}
}}
#endif
