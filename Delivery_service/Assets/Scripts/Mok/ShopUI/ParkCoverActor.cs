using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 공원 전투에서만 사용하는 엄폐 이동과 사격 간격. 일반 시민 AI에는 영향을 주지 않는다.
[DefaultExecutionOrder(2050)]
public sealed class ParkCoverActor : MonoBehaviour {
 public Transform cover;public float phase;
 public bool AtCover=>cover&&Vector3.ProjectOnPlane(transform.position-cover.position,Vector3.up).magnitude<.65f;
 public bool CanFire{get;private set;}
 CitizenCombat combat;NavMeshAgent agent;Animator animator;GangFirearm gun;
 Transform hips,leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot;
 Vector3 hipRest;Quaternion[] rotations;Transform[] legs;bool posed;float crouch;bool activePose;
 NavMeshPath path;int corner;float nextPath;bool requested,allow;PlayerCombat playerTarget;
 void Awake(){combat=GetComponent<CitizenCombat>();agent=GetComponent<NavMeshAgent>();animator=GetComponentInChildren<Animator>();path=new NavMeshPath();
  if(animator&&animator.isHuman){hips=animator.GetBoneTransform(HumanBodyBones.Hips);leftThigh=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);leftShin=animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightThigh=animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);rightShin=animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);legs=new[]{leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot};rotations=new Quaternion[6];}}
 void Update(){RestorePose();if(requested&&Time.timeScale>0)Step(allow,playerTarget);}
 public void Tick(bool allowFire,PlayerCombat player){requested=true;allow=allowFire;playerTarget=player;}
 void Step(bool allowFire,PlayerCombat player){
  activePose=true;CanFire=false;if(combat){combat.ExternalAttackControl=true;gun=GetComponent<GangFirearm>();if(gun)gun.LowerWeapon();if(combat.Health<=0||combat.Npc.IsDead||combat.Npc.IsKnockedDown){crouch=0;return;}}
  if(!cover)return;
  if(!AtCover){crouch=Mathf.MoveTowards(crouch,0,Time.deltaTime*4);Move();return;}
  if(agent&&agent.enabled&&agent.isOnNavMesh){agent.isStopped=true;agent.ResetPath();}Speed(0);
  var aim=player?player.View.transform.position:cover.position+cover.forward*10;
  if(!combat)aim=cover.position+cover.forward*10;
  var d=Vector3.ProjectOnPlane(aim-transform.position,Vector3.up);if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*180);
  bool expose=allowFire&&Mathf.Repeat(Time.time+phase,5.4f)>3.1f;
  crouch=Mathf.MoveTowards(crouch,expose?0:1,Time.deltaTime*3.5f);CanFire=expose&&crouch<.08f;
  if(CanFire&&gun&&agent&&agent.enabled&&agent.isOnNavMesh){gun.ControlledAim=true;gun.Tick(player,agent);}
 }
 void Move(){if(agent&&agent.enabled&&agent.isOnNavMesh){agent.isStopped=false;agent.speed=3.8f;agent.stoppingDistance=.15f;if(!agent.hasPath||Time.time>nextPath){agent.SetDestination(cover.position);nextPath=Time.time+.6f;}Speed(agent.velocity.magnitude);return;}
  if(Time.time>nextPath){NavMesh.CalculatePath(transform.position,cover.position,NavMesh.AllAreas,path);corner=1;nextPath=Time.time+1;}
  if(path.status!=NavMeshPathStatus.PathComplete||path.corners.Length<2){Speed(0);return;}
  while(corner<path.corners.Length-1&&Vector3.Distance(transform.position,path.corners[corner])<.2f)corner++;
  var goal=path.corners[Mathf.Min(corner,path.corners.Length-1)];var delta=Vector3.ProjectOnPlane(goal-transform.position,Vector3.up);if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*220);transform.position=Vector3.MoveTowards(transform.position,goal,Time.deltaTime*3);Speed(3);
 }
 void Speed(float v){if(!animator)return;foreach(var p in animator.parameters){if(p.name=="MoveSpeed")animator.SetFloat(p.name,v);else if(p.name=="IsWalking")animator.SetBool(p.name,v>.1f);else if(p.name=="RunRate")animator.SetFloat(p.name,Mathf.Clamp(v/4.2f,.8f,1.3f));}}
 float lastHit=-10;
 public void ReactToHit(){lastHit=Time.time;}
 void LateUpdate(){if(animator&&animator.isHuman&&animator.enabled&&Time.time-lastHit<.25f){var chest=animator.GetBoneTransform(HumanBodyBones.Chest);if(chest)chest.localRotation*=Quaternion.Euler(-8*Mathf.Sin((Time.time-lastHit)/.25f*Mathf.PI),0,0);}
if(!activePose||!hips||!animator.enabled||crouch<=0||combat&&(combat.Npc.IsKnockedDown||combat.Npc.IsDead))return;
  hipRest=hips.localPosition;for(int i=0;i<legs.Length;i++)rotations[i]=legs[i].localRotation;var lf=leftFoot.position;var rf=rightFoot.position;var lr=leftFoot.rotation;var rr=rightFoot.rotation;
  hips.position-=Vector3.up*(.57f*crouch);Bend(leftThigh,leftShin,leftFoot,lf);Bend(rightThigh,rightShin,rightFoot,rf);leftFoot.rotation=lr;rightFoot.rotation=rr;posed=true;
  // 총의 월드 위치도 낮아진 손을 따라가게 해 엄폐 중 공중에 남지 않도록 한다.
  if(gun&&gun.gun)ArmedHandPose.Apply(animator,gun.gun,playerTarget?playerTarget.View.transform.position:transform.position+transform.forward*10,CanFire,false);
 }
 void Bend(Transform a,Transform b,Transform c,Vector3 target){float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);var delta=target-a.position;float length=Mathf.Clamp(delta.magnitude,.01f,l1+l2-.001f);var axis=delta.normalized;float x=(l1*l1-l2*l2+length*length)/(2*length);float h=Mathf.Sqrt(Mathf.Max(0,l1*l1-x*x));var knee=a.position+axis*x+Vector3.ProjectOnPlane(transform.forward,axis).normalized*h;a.rotation=Quaternion.FromToRotation(b.position-a.position,knee-a.position)*a.rotation;b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;}
 void RestorePose(){if(!posed||!hips)return;hips.localPosition=hipRest;for(int i=0;i<legs.Length;i++)if(legs[i])legs[i].localRotation=rotations[i];posed=false;}
 public void Stop(){requested=false;RestorePose();activePose=false;crouch=0;CanFire=false;if(gun)gun.LowerWeapon();Speed(0);}
 void OnDisable(){Stop();}
}
}
