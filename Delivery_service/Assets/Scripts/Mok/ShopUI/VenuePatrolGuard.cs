using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 기존 시민 전투·래그돌을 유지하며 잠입 중의 순찰만 담당한다.
[DefaultExecutionOrder(300)]
public sealed class VenuePatrolGuard:MonoBehaviour {
 public RuntimeAnimatorController walkController,chaseController;
 public VenueStealth mission;public Transform[] posts;public float speed=1.45f,sightDistance=10,viewAngle=75,pauseSeconds=2;
 public CitizenCombat Combat{get;private set;}
 NavMeshAgent agent;Animator animator;int post;float wait,nextPath;
 void Cache(){if(Combat)return;Combat=GetComponent<CitizenCombat>();agent=GetComponent<NavMeshAgent>();animator=GetComponentInChildren<Animator>(true);}
 public void Activate(){gameObject.SetActive(true);Cache();if(walkController)animator.runtimeAnimatorController=walkController;Combat.EndFight();Combat.Npc.ExternalControl=true;agent.enabled=true;agent.Warp(posts[0].position);transform.rotation=posts[0].rotation;post=1%posts.Length;wait=Time.time+pauseSeconds;nextPath=0;agent.speed=speed;agent.stoppingDistance=.2f;}
 public void BeginChase(){Cache();if(chaseController)animator.runtimeAnimatorController=chaseController;enabled=false;Combat.ExternalAttackControl=false;}
 public void Deactivate(){Cache();if(Combat&&Combat.Npc)Combat.EndFight();gameObject.SetActive(false);}
 public static bool ClearSight(Transform observer,PlayerCombat p){var from=observer.position+Vector3.up*1.45f;var d=p.View.transform.position-from;return !Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(observer)&&!h.transform.IsChildOf(p.transform));}
 public bool CanSee(PlayerCombat p){Cache();if(!gameObject.activeInHierarchy||Combat.Npc.IsDead||Combat.Npc.IsKnockedDown)return false;var d=p.View.transform.position-(transform.position+Vector3.up*1.45f);return d.magnitude<sightDistance&&Vector3.Angle(transform.forward,d)<viewAngle*.5f&&ClearSight(transform,p);}
 void Update(){Cache();if(!mission.Active||Time.timeScale<=0||Combat.IsAggressive||Combat.Npc.IsDead||Combat.Npc.IsKnockedDown||!agent.enabled||!agent.isOnNavMesh)return;
 Combat.Npc.ExternalControl=true;
 if(mission.Seen&&CanSee(mission.mission.Player)){agent.isStopped=true;var d=mission.mission.Player.transform.position-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),100*Time.deltaTime);return;}
 agent.isStopped=Time.time<wait;if(agent.isStopped)return;
 if(Time.time>=nextPath){nextPath=Time.time+.5f;var path=new NavMeshPath();if(agent.CalculatePath(posts[post].position,path)&&path.status==NavMeshPathStatus.PathComplete)agent.SetPath(path);}
 if(Vector3.Distance(transform.position,posts[post].position)<.6f){post=(post+1)%posts.Length;wait=Time.time+pauseSeconds;}
 }
 void LateUpdate(){if(!animator||!animator.enabled||!agent||!agent.enabled||Combat.Npc.IsKnockedDown||Combat.Npc.IsDead||Combat.IsAggressive)return;float v=agent.velocity.magnitude;foreach(var p in animator.parameters){if(p.name=="MoveSpeed")animator.SetFloat(p.name,v);if(p.name=="RunRate")animator.SetFloat(p.name,Mathf.Max(.8f,v/4.2f));if(p.name=="IsWalking")animator.SetBool(p.name,v>.1f);}}
}
}
