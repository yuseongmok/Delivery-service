#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;
using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class MissionPolishCheck {
 static string log;static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/MissionPolish-check.txt",log);}
 static IEnumerator Wait(float s){float end=Time.time+s;while(Time.time<end)yield return null;}
 public static IEnumerator Verify(){
  StoryPhoneInbox.Current.AutoOpenEnabled=false;
  log="Mission polish integration\n";yield return Wait(1);var m=BrokerMission.Current;var p=m.player;
  foreach(var role in new[]{"Mingi","Broker","Gang"})Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.name=="Story costume "+role),"saved distinctive costume "+role);
  int cash=MoneyManager.Instance.currentMoney;Check(DailyRent.Cost(1)==0&&DailyRent.Cost(100)==0&&DailyRent.Current.SettleDay(1)==0&&MoneyManager.Instance.currentMoney==cash,"daily rent removed at all day levels");Check(!DailyRent.Preview(1).Contains("임대료"),"rent removed from closing preview");
  m.day.EnterNightPreparation();Check(m.Begin(),"mission starts");var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();Check(bike.EnterBike(p.gameObject),"mount motorcycle");yield return Wait(.2f);
  Check(!p.gameObject.activeSelf&&Vector3.Distance(p.transform.position,bike.transform.position)<.5f,"inactive rider position follows motorcycle");Check(p.View==bike.bikeCamera.GetComponentInChildren<Camera>(),"mission uses motorcycle camera");Check(p.CanReceiveHit&&!p.CanAct,"mounted player is targetable without enabling foot attacks");
  var observer=UnityEngine.Object.Instantiate(m.enemyTemplate,m.meeting.position,Quaternion.identity);observer.gameObject.SetActive(true);observer.Provoke(p);yield return Wait(.5f);var nav=observer.GetComponent<NavMeshAgent>();Check(nav.pathStatus==NavMeshPathStatus.PathComplete&&Vector3.Distance(nav.destination,bike.transform.position)<13,"hostile NPC pursues current motorcycle position: "+nav.destination+" bike="+bike.transform.position+" active="+observer.IsAggressive+" onNav="+nav.isOnNavMesh+" enabled="+observer.enabled+" down="+p.IsDown+" npcDown="+observer.Npc.IsKnockedDown);UnityEngine.Object.Destroy(observer.gameObject);
  p.ReceivePunch(34,Vector3.forward);yield return null;Check(!bike.isDriven&&p.gameObject.activeSelf&&p.HitsTaken==1&&p.IsDown,"mounted hit dismounts and enters existing knockdown once");yield return Wait(6);
  Check(!p.IsDown&&p.CanAct,"player controls recover after mounted hit");
  var accident=bike.GetComponentInChildren<MotorcycleAccident>();var victim=UnityEngine.Object.Instantiate(m.enemyTemplate,m.meeting.position+Vector3.right*2,Quaternion.identity);victim.gameObject.SetActive(true);yield return null;
  var collider=victim.GetComponent<Collider>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(MotorcycleAccident).GetField("npcLayer",flags).SetValue(accident,(LayerMask)(1<<collider.gameObject.layer));typeof(MotorcycleAccident).GetField("policeSpawnChance",flags).SetValue(accident,0f);typeof(MotorcycleController).GetField("currentSpeed",flags).SetValue(bike,8f);cash=MoneyManager.Instance.currentMoney;typeof(MotorcycleAccident).GetMethod("OnTriggerEnter",flags).Invoke(accident,new object[]{collider});Check(victim.Npc.IsDead&&MoneyManager.Instance.currentMoney==cash,"actual motorcycle collision retains NPC reaction without automatic fee");UnityEngine.Object.Destroy(victim.gameObject);
  var avatar=p.avatar;Check(avatar.leftFist&&avatar.rightFist,"saved fist meshes assigned");avatar.PoseKick(PlayerAvatar.HitTime);yield return null;yield return null;Check(avatar.leftFist.gameObject.activeSelf&&avatar.rightFist.gameObject.activeSelf,"punch shows both hands");Check(avatar.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"punch no longer plays kick leg clip");BrokerMissionCheck.Capture(m,"Mission-punch");avatar.StopKick();Check(!avatar.leftFist.gameObject.activeSelf&&!avatar.rightFist.gameObject.activeSelf,"hands hide after attack");
  // 저장된 동행 컨트롤러를 실제로 샘플링하여 다리 포즈가 움직이는지 확인한다.
  var a=m.broker.GetComponentInChildren<Animator>();a.SetFloat("MoveSpeed",4);a.Update(.3f);var leg=a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var before=leg.localRotation;a.Update(.2f);Check(a.GetCurrentAnimatorStateInfo(0).IsName("Walk")&&Quaternion.Angle(before,leg.localRotation)>1,"broker walking clip animates leg bones");a.SetFloat("MoveSpeed",0);
  var lesson=m.GetComponent<FirstWantedLesson>();UnityEngine.Object.FindFirstObjectByType<PoliceManager>().SpawnPoliceOnAccidentRefused();yield return Wait(.3f);Check(lesson.Shown,"real police spawn triggers first wanted tutorial");int messages=StoryPhoneInbox.Current.Messages.Count;lesson.Notify();Check(lesson.panel.activeSelf&&StoryPhoneInbox.Current.Messages.Count==messages,"first wanted lesson and text sent only once");
  // 카메라만 이동해 저장된 실장 외형을 확인한다.
  var oldPos=p.View.transform.position;var oldRot=p.View.transform.rotation;p.View.transform.position=m.broker.position+m.broker.forward*3+Vector3.up*1.3f;p.View.transform.LookAt(m.broker.position+Vector3.up*.9f);BrokerMissionCheck.Capture(m,"Mission-broker-style");p.View.transform.SetPositionAndRotation(oldPos,oldRot);
  m.Cancel();
 }
}
}
#endif
