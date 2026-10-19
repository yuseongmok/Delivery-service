#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class ArrestViewTest {
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
public static IEnumerator RunCore(Action<string> log){
 void Check(bool ok,string s){if(!ok)throw new Exception(s);log("PASS: "+s);}
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var move=player.GetComponent<PlayerMove>();var cc=player.GetComponent<CharacterController>();
 var inv=player.GetComponent<ToppingInventory>();inv.RemoveItem();inv.RemovePizza();inv.ClearPackagedPizzas();
 var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
 var pm=UnityEngine.Object.FindFirstObjectByType<PoliceManager>();
 foreach(bool riding in new[]{false,true}){
  if(riding)Check(bike.EnterBike(player.gameObject),"탑승 체포 준비");
  pm.SpawnPoliceOnAccidentRefused();yield return null;
  var group=SceneActors.Police.ToArray();Check(group.Length>0,"실제 경찰 소환");foreach(var p in group)p.enabled=false;
  var officer=group[0];var ag=officer.GetComponent<NavMeshAgent>();if(ag!=null)ag.enabled=false;
  officer.transform.position=(riding?bike.transform.position:player.transform.position)+Vector3.back*2;
  var station=new GameObject("체포 검증 도착점");station.transform.position=player.transform.position+Vector3.right*12;
  Set(officer,"policeStationSpawn",station.transform);Set(officer,"fadeDuration",.2f);
  var routine=(IEnumerator)typeof(PoliceControl).GetMethod("ArrestRoutine",F).Invoke(officer,new object[]{riding?bike.transform:player.transform});
  officer.StartCoroutine(routine);yield return null;
  Check(PoliceControl.IsArrestInProgress&&!bike.isDriven&&!move.enabled&&!cc.enabled,"체포 시작: 하차 및 이동 잠금");
  var start=player.transform.position;float until=Time.unscaledTime+.65f;while(Time.unscaledTime<until)yield return null;
  var animator=officer.GetComponentInChildren<Animator>();var head=animator.GetBoneTransform(HumanBodyBones.Head);
  Check(Vector3.Angle(player.View.transform.forward,head.position-player.View.transform.position)<5,"체포 경찰 얼굴을 정면으로 바라봄");
  Check((player.transform.position-start).sqrMagnitude<.01f,"경찰을 보여주는 동안 순간이동하지 않음");
  Check(player.GetComponentsInChildren<CameraMove>().All(x=>!x.enabled),"마우스 시점 잠금");
  until=Time.unscaledTime+5;while(PoliceControl.IsArrestInProgress&&Time.unscaledTime<until)yield return null;
  Check(!PoliceControl.IsArrestInProgress&&move.enabled&&cc.enabled,"연출 종료 후 이동 복원");
  Check((player.transform.position-station.transform.position).sqrMagnitude<1,"경찰서 도착");
  Check(player.GetComponentsInChildren<CameraMove>().All(x=>x.enabled),"시점 조작 복원");
  UnityEngine.Object.Destroy(station);yield return null;
 }
}
}}
#endif
