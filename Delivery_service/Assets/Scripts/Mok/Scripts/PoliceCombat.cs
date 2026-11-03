using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 시민의 관절 물리를 재사용하되 경찰 추격과 체포는 기존 컨트롤러가 맡는다.
public sealed class PoliceCombat:MonoBehaviour {
 public int Health {get;private set;}=100;public bool Armed {get;private set;}
 public bool IsDead=>Health<=0;public bool IsDown=>npc!=null&&(npc.IsKnockedDown||npc.IsDead);
 NPCControl npc;PoliceControl police;NavMeshAgent agent;Animator animator;PlayerCombat target;Transform gun;
 float nextShot;bool aiming;Light flash;
 void Awake(){police=GetComponent<PoliceControl>();agent=GetComponent<NavMeshAgent>();animator=GetComponent<Animator>();}
 void EnsureRagdoll(){if(npc!=null)return;npc=GetComponent<NPCControl>()??gameObject.AddComponent<NPCControl>();npc.ExternalControl=true;npc.enabled=false;foreach(var body in GetComponentsInChildren<Rigidbody>())if(body.gameObject!=gameObject)body.useGravity=true;if(animator!=null&&animator.isHuman)npc.SetCombatRagdollCenter(animator.GetBoneTransform(HumanBodyBones.Hips));}
 public bool ReceiveHit(PlayerCombat attacker,Vector3 direction,int damage){
  if(attacker==null||damage<=0||IsDead||IsDown||PoliceControl.IsArrestInProgress)return false;
  target=attacker;
  Health=Mathf.Max(0,Health-damage);EnsureRagdoll();aiming=false;
  if(IsDead){FindFirstObjectByType<PoliceManager>()?.ReportDeath(police,Armed,attacker.transform);npc.Die(direction,8);return true;}npc.KnockDown(direction,5);StartCoroutine(Recover());return true;
 }
 IEnumerator Recover(){yield return new WaitForSeconds(3);while(!IsDead&&!npc.RecoverFromKnockDown())yield return new WaitForSeconds(.5f);nextShot=Time.time+1.5f;}
 // 기존 지원 요청은 비무장 체포 병력으로 처리한다.
 public void Arm(){Armed=false;}
 public bool HoldForShot(Transform activeTarget)=>false;
 public bool ClearShot(){
  if(target==null||target.View==null)return false;Vector3 from=transform.position+Vector3.up*1.35f,to=target.View.transform.position;
  if(Vector3.Distance(from,to)>18)return false;
  foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   if(hit.transform.IsChildOf(transform))continue;return hit.transform.IsChildOf(target.transform)||(target.RiddenBike&&hit.transform.IsChildOf(target.RiddenBike.transform));
  }return true;
 }
 void OnDestroy(){if(flash!=null)Destroy(flash.gameObject);}
}
}
