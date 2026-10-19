using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
[DefaultExecutionOrder(40)]
public sealed class SlumRockThrower : MonoBehaviour {
 public SlumRock projectilePrefab;
 public float attackRange=18f,preferredDistance=10f,windupSeconds=.85f,cooldown=4f;
 SlumRobberCombat guard;CitizenCombat combat;NavMeshAgent agent;Animator animator;
 Transform hand,upper,lower,held;float releaseAt,nextThrow,repath;Vector3 aim;bool winding;
 public bool IsWinding=>winding;
 void Start(){
  guard=GetComponent<SlumRobberCombat>();combat=guard.Combat;combat.ExternalAttackControl=true;agent=GetComponent<NavMeshAgent>();animator=GetComponent<Animator>();
  var bones=GetComponentsInChildren<Transform>();hand=bones.FirstOrDefault(t=>t.name=="Hand.R");upper=bones.FirstOrDefault(t=>t.name=="Upper Arm.R");lower=bones.FirstOrDefault(t=>t.name=="Lower Arm.R");
  if(hand!=null&&projectilePrefab!=null){var model=projectilePrefab.GetComponentInChildren<MeshFilter>();held=new GameObject("Held throwing stone").transform;held.SetParent(hand,false);held.localScale=model.transform.localScale;held.gameObject.AddComponent<MeshFilter>().sharedMesh=model.sharedMesh;held.gameObject.AddComponent<MeshRenderer>().sharedMaterial=model.GetComponent<MeshRenderer>().sharedMaterial;held.gameObject.SetActive(false);}
  nextThrow=Time.time+1.5f;
 }
 bool CanAttack=>guard.Engaged&&guard.Target!=null&&guard.Target.CanAct&&!combat.Npc.IsDead&&!combat.Npc.IsKnockedDown&&agent.enabled&&agent.isOnNavMesh;
 void Cancel(){winding=false;if(held!=null)held.gameObject.SetActive(false);}
 void Update(){
  if(!CanAttack){Cancel();if(guard.Engaged&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;return;}
  Vector3 delta=guard.Target.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
  if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),400*Time.deltaTime);
  if(winding){
   agent.isStopped=true;
   if(Time.time>=releaseAt){
    Vector3 origin=transform.position+Vector3.up*1.8f+transform.forward*.5f;
    // 처음 조준한 지점은 발사 후에도 바꾸지 않는다.
    var rock=Instantiate(projectilePrefab,origin,Quaternion.identity);
    rock.Launch(transform,aim,Mathf.Clamp(Vector3.Distance(origin,aim)/10f,1.2f,1.8f));
    nextThrow=Time.time+cooldown;Cancel();
   }
  }else{
   agent.stoppingDistance=preferredDistance;agent.isStopped=distance<=preferredDistance;
   if(!agent.isStopped&&Time.time>=repath){repath=Time.time+.3f;if(NavMesh.SamplePosition(guard.Target.transform.position,out var dest,3,NavMesh.AllAreas))agent.SetDestination(dest.position);}
   if(distance<=attackRange&&Time.time>=nextThrow&&ClearSight()){
    aim=guard.Target.transform.position;
    if(Physics.Raycast(aim+Vector3.up*.3f,Vector3.down,out var ground,3,~0,QueryTriggerInteraction.Ignore))aim.y=ground.point.y;
    aim.y+=.2f;winding=true;releaseAt=Time.time+windupSeconds;if(held!=null)held.gameObject.SetActive(true);
   }
  }
  if(animator!=null&&animator.enabled)animator.SetFloat("MoveSpeed",winding?0:agent.velocity.magnitude);
 }
 bool ClearSight(){Vector3 a=transform.position+Vector3.up*1.4f,b=guard.Target.transform.position+Vector3.up;
  return !Physics.RaycastAll(a,(b-a).normalized,(b-a).magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(guard.Target.transform));}
 void LateUpdate(){
  if(!winding||upper==null||lower==null||hand==null||!CanAttack)return;
  float t=Mathf.Clamp01(1-(releaseAt-Time.time)/windupSeconds);
  Vector3 goal=transform.position+transform.rotation*Vector3.Lerp(new Vector3(.35f,1.6f,-.25f),new Vector3(.3f,1.9f,.3f),Mathf.SmoothStep(0,1,t));
  float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
  Vector3 d=goal-upper.position;float length=Mathf.Clamp(d.magnitude,.03f,a+b-.005f),x=(a*a-b*b+length*length)/(2*length);
  Vector3 elbow=upper.position+d.normalized*x+Vector3.ProjectOnPlane(transform.right,d.normalized).normalized*Mathf.Sqrt(Mathf.Max(0,a*a-x*x));
  upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
  lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,goal-lower.position)*lower.rotation;
 }
 void OnDisable(){Cancel();}
}
}
