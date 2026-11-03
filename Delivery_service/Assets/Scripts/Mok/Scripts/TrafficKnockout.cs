using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 교통사고는 사망 이벤트를 보내지 않는다. 회복까지 차량의 물리 접촉도 임시로 끈다.
public sealed class TrafficKnockout:MonoBehaviour {
 NPCControl npc;PlayerCombat accidentDriver;float immuneUntil;Vector3 safePosition;bool hasSafe;
 readonly List<(Collider own,Collider vehicle)> ignored=new();
 readonly HashSet<long> pairs=new();
 public bool Protected=>npc&&(npc.IsKnockedDown||Time.time<immuneUntil);
 public int HitCount{get;private set;}
 void Awake(){npc=GetComponent<NPCControl>();}
 public static bool TryHit(NPCControl target,Vector3 direction,float speed,Transform vehicle){
  if(!target||target.IsDead||speed<1)return false;
  var effect=target.GetComponent<TrafficKnockout>()??target.gameObject.AddComponent<TrafficKnockout>();
  if(effect.Protected){effect.IgnoreVehicle(vehicle);return false;}
  return effect.Begin(direction,speed,vehicle);
 }
 bool Begin(Vector3 direction,float speed,Transform vehicle){
  accidentDriver=FindDriver(vehicle);
  HitCount++;var agent=GetComponent<NavMeshAgent>();var filter=new NavMeshQueryFilter{agentTypeID=agent?agent.agentTypeID:0,areaMask=agent?agent.areaMask:NavMesh.AllAreas};hasSafe=NavMesh.SamplePosition(transform.position,out var nav,5,filter);safePosition=hasSafe?nav.position:transform.position;
  if(!GetComponent<StunStars>())gameObject.AddComponent<StunStars>();
  var animator=GetComponentInChildren<Animator>();if(animator&&animator.isHuman)npc.SetCombatRagdollCenter(animator.GetBoneTransform(HumanBodyBones.Hips));
  var combat=GetComponent<CitizenCombat>();
  bool escapeGang=vehicle&&vehicle.GetComponentInParent<VenueGetawayDrive>()&&combat&&combat.missionEnemy;
  if(escapeGang)combat.ReceiveEscapeImpact(Mathf.RoundToInt(speed*4),direction);
  if(!npc.IsDead)npc.KnockDown(direction,1);
  var forward=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
  var launch=forward*Mathf.Clamp(3+speed*.4f,4,10)+Vector3.up*Mathf.Clamp(1.7f+speed*.05f,1.8f,2.8f);
  if(escapeGang)launch=forward*Mathf.Clamp(speed*.72f,7,15)+Vector3.up*3.2f;
  foreach(var rb in GetComponentsInChildren<Rigidbody>())if(rb.gameObject!=gameObject&&!rb.isKinematic){rb.useGravity=true;rb.linearVelocity=launch;rb.angularVelocity=Vector3.Cross(Vector3.up,forward)*1.1f;}
  IgnoreVehicle(vehicle);
  foreach(var car in FindObjectsByType<CarMov>(FindObjectsSortMode.None))IgnoreVehicle(car.transform);
  foreach(var bike in FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))IgnoreVehicle(bike.transform);
  foreach(var car in FindObjectsByType<VenueGetawayDrive>(FindObjectsSortMode.None))IgnoreVehicle(car.transform);
  immuneUntil=Time.time+6;if(!npc.IsDead)StartCoroutine(Recover());return true;
 }
 // 충돌 순간의 탑승자를 저장하므로 회복 전에 내려도 책임 대상이 유지된다.
 public static PlayerCombat FindDriver(Transform vehicle){
  if(!vehicle)return null;
  var bike=vehicle.GetComponentInParent<MotorcycleController>();
  if(bike)return bike.Rider?bike.Rider.GetComponent<PlayerCombat>():null;
  var car=vehicle.GetComponentInParent<VenueGetawayDrive>();
  return car&&car.Driving&&car.mission?car.mission.Player:null;
 }
 void IgnoreVehicle(Transform vehicle){if(!vehicle)return;foreach(var own in GetComponentsInChildren<Collider>())foreach(var other in vehicle.GetComponentsInChildren<Collider>()){
  if(other.transform.IsChildOf(transform))continue;long key=((long)(uint)own.GetInstanceID()<<32)|(uint)other.GetInstanceID();
  if(pairs.Contains(key)||Physics.GetIgnoreCollision(own,other))continue;
  Physics.IgnoreCollision(own,other);pairs.Add(key);ignored.Add((own,other));
 }}
 IEnumerator Recover(){
  yield return new WaitForSeconds(3.5f);float deadline=Time.time+2;
  while(npc&&npc.IsKnockedDown&&!npc.IsDead&&!npc.RecoverFromKnockDown()){
   // 도로 밖으로 떨어졌다면 사고 직전 확인한 안전한 보행 위치로 복구한다.
   if(Time.time>=deadline&&hasSafe){var shift=safePosition+Vector3.up*.15f-npc.RagdollCenter.position;foreach(var rb in GetComponentsInChildren<Rigidbody>())if(rb.gameObject!=gameObject&&!rb.isKinematic){rb.position+=shift;rb.linearVelocity=Vector3.zero;}Physics.SyncTransforms();}
   yield return new WaitForSeconds(.25f);
  }
  immuneUntil=Time.time+2;
  // 일반 시민만 사고 책임자를 추격한다. 미션 적의 별도 행동은 유지한다.
  var combat=GetComponent<CitizenCombat>();
  if(npc&&!npc.IsDead&&!npc.IsKnockedDown&&combat&&!combat.missionEnemy&&!combat.ExternalAttackControl&&!GetComponent<RobberAI>()&&!GetComponent<SlumRobberCombat>()&&!GetComponent<PoliceControl>()){
   if(accidentDriver)combat.Provoke(accidentDriver);else combat.EndFight();
  }
  accidentDriver=null;
 }
 void Update(){if(Time.timeScale<=0||Protected)return;Restore(false);}
 void Restore(bool force){for(int i=ignored.Count-1;i>=0;i--){var pair=ignored[i];if(!force&&pair.own&&pair.vehicle&&pair.own.enabled&&pair.vehicle.enabled&&pair.own.bounds.Intersects(pair.vehicle.bounds))continue;
  if(pair.own&&pair.vehicle){Physics.IgnoreCollision(pair.own,pair.vehicle,false);pairs.Remove(((long)(uint)pair.own.GetInstanceID()<<32)|(uint)pair.vehicle.GetInstanceID());}ignored.RemoveAt(i);
 }if(ignored.Count==0)pairs.Clear();}
 void OnDisable(){StopAllCoroutines();Restore(true);}
 void OnDestroy(){Restore(true);}
}}
