#if UNITY_EDITOR
using System;using System.Linq;using System.Collections;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class TrafficStunCheck {
 public static IEnumerator Verify(Action<bool,string> check){
  yield return null;yield return null;
  RacerMergeCheck.Verify(check);
  var source=UnityEngine.Object.FindObjectsByType<NPCControl>(FindObjectsSortMode.None).First(n=>!n.IsDead&&!n.IsKnockedDown&&!n.GetComponent<RobberAI>()&&n.GetComponent<NavMeshAgent>().enabled&&n.GetComponent<NavMeshAgent>().isOnNavMesh);
  var prefab=source.gameObject;var originalAgent=source.GetComponent<NavMeshAgent>();
  check(NavMesh.SamplePosition(source.transform.position+Vector3.right*2,out var spot,5,new NavMeshQueryFilter{agentTypeID=originalAgent.agentTypeID,areaMask=originalAgent.areaMask}),"test starts on a live citizen navigation surface");
  var go=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity);var npc=go.GetComponent<NPCControl>();npc.ExternalControl=true;npc.enabled=false;
  var fighter=go.GetComponent<CitizenCombat>();if(fighter)fighter.enabled=false;
  var agent=go.GetComponent<NavMeshAgent>();agent.Warp(spot.position);agent.isStopped=true;
  int deaths=0;npc.OnNPCDeath+=_=>deaths++;
  var carObject=new GameObject("Traffic stun test car");carObject.transform.position=spot.position+Vector3.left*20;var car=carObject.AddComponent<CarMov>();car.enabled=false;var collider=carObject.AddComponent<BoxCollider>();
  typeof(CarMov).GetField("<CurrentTravelSpeed>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(car,12f);
  typeof(CarMov).GetMethod("OnTriggerEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(car,new object[]{go.GetComponent<Collider>()});
  yield return null;
  var effect=go.GetComponent<TrafficKnockout>();check(effect&&npc.IsKnockedDown&&!npc.IsDead&&deaths==0,"traffic-car collision produces knockout instead of death event");
  check(go.GetComponent<StunStars>().Visible,"citizen accident shows orbiting stun stars");
  check(!TrafficKnockout.TryHit(npc,Vector3.back,50,car.transform)&&effect.HitCount==1,"repeated accident cannot relaunch downed citizen");
  check(Physics.GetIgnoreCollision(go.GetComponent<Collider>(),collider),"vehicle contact temporarily ignored while citizen is down");
  float limit=Time.time+8;while(npc.IsKnockedDown&&Time.time<limit)yield return null;yield return null;
  check(!npc.IsDead&&!npc.IsKnockedDown&&!go.GetComponent<StunStars>().Visible&&agent.enabled&&agent.isOnNavMesh,"citizen stands on navmesh and stun stars clear");
  check(fighter&&!fighter.IsAggressive&&!npc.ExternalControl,"AI traffic victim returns to normal civilian control");
  check(!TrafficKnockout.TryHit(npc,Vector3.forward,12,car.transform),"brief recovery grace blocks immediate second accident");
  limit=Time.time+2.3f;while(Time.time<limit)yield return null;
  check(go&&deaths==0&&!Physics.GetIgnoreCollision(go.GetComponent<Collider>(),collider),"citizen remains alive and separated vehicle collision is restored");
  var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
  var riderField=typeof(MotorcycleController).GetField("rider",BindingFlags.NonPublic|BindingFlags.Instance);
  var oldRider=riderField.GetValue(bike);bool oldDriven=bike.isDriven;
  bike.isDriven=false;check(TrafficKnockout.FindDriver(bike.transform)==null,"unoccupied bike is not attributed to player");
  riderField.SetValue(bike,player.gameObject);bike.isDriven=true;
  check(TrafficKnockout.FindDriver(bike.transform)==player,"occupied bike resolves its actual rider");
  check(TrafficKnockout.TryHit(npc,Vector3.forward,12,bike.transform)&&effect.HitCount==2,"player bike starts a distinct accident");
  bike.isDriven=oldDriven;riderField.SetValue(bike,oldRider);
  check(!TrafficKnockout.TryHit(npc,Vector3.back,12,car.transform),"AI repeat hit cannot replace player attribution");
  limit=Time.time+8;while(npc.IsKnockedDown&&Time.time<limit)yield return null;yield return null;
  check(!npc.IsKnockedDown&&fighter.IsAggressive&&npc.ExternalControl,"citizen pursues player after recovery even after dismount");
  check(!go.GetComponent<StunStars>().Visible&&!TrafficKnockout.TryHit(npc,Vector3.forward,12,car.transform),"retaliating citizen keeps recovery grace and clears stars");
  limit=Time.time+2.3f;while(Time.time<limit)yield return null;
  check(TrafficKnockout.TryHit(npc,Vector3.forward,12,car.transform),"AI vehicle can cause later independent accident");
  limit=Time.time+8;while(npc.IsKnockedDown&&Time.time<limit)yield return null;yield return null;
  check(!npc.IsKnockedDown&&!fighter.IsAggressive&&!npc.ExternalControl,"AI-caused recovery resumes wandering instead of retaliation");
  limit=Time.time+2.3f;while(Time.time<limit)yield return null;
  var escapeObject=new GameObject("Escape vehicle collision test");var escape=escapeObject.AddComponent<VenueGetawayDrive>();escape.enabled=false;
  fighter.missionEnemy=true;int health=fighter.Health;
  check(TrafficKnockout.TryHit(npc,Vector3.forward,8,escape.transform),"escape vehicle hits mission gang");
  check(fighter.Health==health-32&&!npc.IsDead,"nonlethal escape impact subtracts speed-based damage");
  limit=Time.time+8;while(npc.IsKnockedDown&&Time.time<limit)yield return null;
  check(!npc.IsKnockedDown&&!npc.IsDead,"surviving mission gang recovers");
  limit=Time.time+2.3f;while(Time.time<limit)yield return null;
  check(TrafficKnockout.TryHit(npc,Vector3.forward,18,escape.transform),"later escape impact accepted");
  var bone=go.GetComponentsInChildren<Rigidbody>().First(rb=>rb.gameObject!=go&&!rb.isKinematic);
  check(bone.linearVelocity.z>12&&bone.linearVelocity.y>=3,"escape impact uses stronger forward launch");
  yield return null;
  check(fighter.Health==0&&npc.IsDead&&go.GetComponent<StunStars>().Visible,"depleted gang remains ragdolled with stun stars");
  check(!TrafficKnockout.TryHit(npc,Vector3.back,30,escape.transform),"depleted gang cannot be struck repeatedly");
  limit=Time.time+5;while(go&&Time.time<limit)yield return null;
  check(!go,"depleted gang disappears after knockout delay");
  UnityEngine.Object.Destroy(escapeObject);UnityEngine.Object.Destroy(carObject);
 }
}}
#endif
