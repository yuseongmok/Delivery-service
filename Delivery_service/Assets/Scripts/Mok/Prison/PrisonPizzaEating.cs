using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 먹는 동작 동안만 팔과 상체를 제어하고 마지막 자세를 레그돌에 넘긴다.
[DefaultExecutionOrder(500)]
public sealed class PrisonPizzaEating:MonoBehaviour {
 public Transform slice;
 [Min(.2f)] public float receiveSeconds=.9f,raiseSeconds=.8f,chewSeconds=1.6f,lowerSeconds=.7f,drowsySeconds=1.6f;
 public bool Playing{get;private set;}
 public bool Finished{get;private set;}
 public float Duration=>receiveSeconds+raiseSeconds+chewSeconds+lowerSeconds+drowsySeconds;
 Animator animator;Transform upper,lower,hand,head,spine;Transform[] bones;Quaternion[] rest;
 public float Elapsed=>elapsed; float elapsed;Quaternion facingStart,facingEnd,palmBasis=Quaternion.identity;Vector3 source;Action bite;bool bit;
 public IEnumerator Play(Transform tray,Action onBite=null){
  if(Playing||Finished)yield break;
  animator=GetComponentInChildren<Animator>();upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);hand=animator.GetBoneTransform(HumanBodyBones.RightHand);head=animator.GetBoneTransform(HumanBodyBones.Head);spine=animator.GetBoneTransform(HumanBodyBones.Spine);
  bones=new[]{upper,lower,hand,head,spine};rest=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)rest[i]=bones[i].localRotation;
  animator.enabled=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;var fingers=animator.GetComponent<SyntyGripFingers>();if(fingers)fingers.Apply(null,false);var guard=GetComponent<PrisonGuard>();guard.enabled=false;if(guard.gun)guard.gun.gameObject.SetActive(false);
  var agent=GetComponent<NavMeshAgent>();if(agent&&agent.isOnNavMesh){agent.ResetPath();agent.isStopped=true;}
  GetComponent<NPCControl>().ExternalControl=true;var combat=GetComponent<CitizenCombat>();if(combat)combat.ExternalAttackControl=true;
  // 손가락 뼈의 방향으로 손바닥 기준을 잡아 피자를 아래에서 받친다.
  Transform index=null,other=null;foreach(var t in hand.GetComponentsInChildren<Transform>()){if(t.name.StartsWith("Grip IndexFinger_01"))index=t;if(t.name.StartsWith("Grip Finger_01"))other=t;}
  if(index&&other){var f=hand.InverseTransformDirection((index.position+other.position)*.5f-hand.position);var n=hand.InverseTransformDirection(Vector3.Cross(index.position-hand.position,other.position-hand.position));if(f.sqrMagnitude>.00001f&&n.sqrMagnitude>.00000001f)palmBasis=Quaternion.LookRotation(f,n);}
  source=tray?tray.position:hand.position;source=Vector3.Lerp(hand.position,source,Mathf.Min(1,1.1f/Mathf.Max(.01f,Vector3.Distance(hand.position,source))));
  facingStart=transform.rotation;var direction=Vector3.ProjectOnPlane(PrisonMission.Current.player.transform.position-transform.position,Vector3.up);facingEnd=direction.sqrMagnitude>.01f?Quaternion.LookRotation(direction):facingStart; slice.position=hand.position; bite=onBite;bit=false;elapsed=0;Playing=true;slice.gameObject.SetActive(true);
  while(elapsed<Duration){if(Time.timeScale>0)elapsed+=Time.deltaTime;yield return null;}
  Playing=false;slice.gameObject.SetActive(false);Finished=true;
  // 상체가 숙여진 상태에서 힘없이 주저앉도록 현재 뼈 자세를 그대로 넘긴다.
  GetComponent<NPCControl>().KnockDown(Vector3.zero,0);
 }
 void LateUpdate(){if(!Playing)return;transform.rotation=Quaternion.Slerp(facingStart,facingEnd,Mathf.SmoothStep(0,1,elapsed/receiveSeconds));for(int i=0;i<bones.Length;i++)bones[i].localRotation=rest[i];
  float eatStart=receiveSeconds+raiseSeconds;float tiredStart=eatStart+chewSeconds+lowerSeconds;
  float tired=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-tiredStart)/drowsySeconds));
  spine.rotation=Quaternion.AngleAxis(tired*28,transform.right)*spine.rotation;
  head.rotation=Quaternion.AngleAxis(tired*32+((elapsed>eatStart&&elapsed<eatStart+chewSeconds)?Mathf.Sin(elapsed*10)*2:0),transform.right)*head.rotation;
  Vector3 chest=spine.position+Vector3.up*.12f+transform.forward*.38f+transform.right*.12f;
  Vector3 mouth=head.position+transform.forward*.30f-Vector3.up*.15f;
  Vector3 target;
  if(elapsed<receiveSeconds)target=Vector3.Lerp(source,chest,Mathf.SmoothStep(0,1,elapsed/receiveSeconds));
  else if(elapsed<eatStart)target=Vector3.Lerp(chest,mouth,Mathf.SmoothStep(0,1,(elapsed-receiveSeconds)/raiseSeconds));
  else if(elapsed<eatStart+chewSeconds){target=mouth-transform.forward*(.025f+Mathf.Sin(elapsed*5)*.015f);if(!bit){bit=true;bite?.Invoke();PrisonAudio.Clink(transform.position,.06f);}}
  else target=Vector3.Lerp(mouth,spine.position+transform.forward*.25f-transform.right*.05f-Vector3.up*.18f,Mathf.SmoothStep(0,1,(elapsed-eatStart-chewSeconds)/lowerSeconds));
  Reach(target);hand.rotation=Quaternion.LookRotation(-transform.forward,Vector3.up)*Quaternion.Inverse(palmBasis);slice.SetPositionAndRotation(hand.position+Vector3.up*.02f,Quaternion.LookRotation(-transform.forward,Vector3.up));slice.localScale=Vector3.one*(bit?.8f:1);
  if(elapsed>=tiredStart)slice.gameObject.SetActive(false);
 }
 void Reach(Vector3 goal){float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);var delta=goal-upper.position;float d=Mathf.Clamp(delta.magnitude,.02f,a+b-.005f);var direction=delta.normalized;float along=(a*a-b*b+d*d)/(2*d);var side=Vector3.ProjectOnPlane(transform.right*.35f-Vector3.up,direction).normalized;var elbow=upper.position+direction*along+side*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,goal-lower.position)*lower.rotation;}
}
}

