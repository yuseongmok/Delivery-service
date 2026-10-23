using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 행사장 경비만 순찰시킨다. 경계가 끝나면 기존 시민 전투를 해제하고 순찰로 돌린다.
public sealed class BackdoorGuard : MonoBehaviour {
 public BackdoorMission mission;public Transform[] posts;public float patrolSpeed=1.5f,sightDistance=11f,viewAngle=85f;
 CitizenCombat combat;NavMeshAgent agent;Animator animator;NPCControl npc;int post;float waitUntil,nextPath;
 public bool IsFighting=>combat&&combat.IsAggressive;
 void Cache(){if(combat)return;combat=GetComponent<CitizenCombat>();agent=GetComponent<NavMeshAgent>();animator=GetComponent<Animator>();npc=GetComponent<NPCControl>();}
 public void Activate(){gameObject.SetActive(true);Cache();if(npc.IsDead)return;agent.enabled=true;agent.Warp(posts[0].position);post=1%posts.Length;StandDown();}
 public void Deactivate(){Cache();if(combat)combat.EndFight();gameObject.SetActive(false);}
 public void Chase(){Cache();if(!npc.IsDead)combat.Provoke(mission.Player);}
 public void StandDown(){Cache();combat.EndFight();npc.ExternalControl=true;if(agent.enabled&&agent.isOnNavMesh){agent.speed=patrolSpeed;agent.stoppingDistance=.25f;agent.isStopped=false;}waitUntil=Time.time+2;nextPath=0;}
 public bool CanSee(PlayerCombat player){
  Cache();if(!gameObject.activeInHierarchy||npc.IsDead||npc.IsKnockedDown)return false;
  var hiding=player.GetComponent<PlayerHiding>();if(hiding&&hiding.IsHidden)return false;
  var from=transform.position+Vector3.up*1.45f;var to=player.View.transform.position;var d=to-from;
  if(d.magnitude>sightDistance||Vector3.Angle(transform.forward,d)>viewAngle*.5f)return false;
  return !Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(player.transform)&&!(player.RiddenBike&&h.transform.IsChildOf(player.RiddenBike.transform)));
 }
 void Update(){
  Cache();if(!mission.Running||Time.timeScale<=0||combat.IsAggressive||npc.IsDead||npc.IsKnockedDown||!agent.enabled||!agent.isOnNavMesh)return;
  npc.ExternalControl=true;animator.SetBool("IsWalking",agent.velocity.sqrMagnitude>.05f);
  if(Time.time<waitUntil){agent.isStopped=true;return;}
  if(Time.time>=nextPath){nextPath=Time.time+.4f;agent.isStopped=false;agent.SetDestination(posts[post].position);}
  if(!agent.pathPending&&Vector3.Distance(transform.position,posts[post].position)<.65f){post=(post+1)%posts.Length;waitUntil=Time.time+2;}
 }
}
}
