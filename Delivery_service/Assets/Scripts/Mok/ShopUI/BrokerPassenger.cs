using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 동행 NPC만 제어한다. 오토바이의 기존 운전·충돌 물리는 변경하지 않는다.
[DefaultExecutionOrder(150)]
public sealed class BrokerPassenger : MonoBehaviour {
 public BrokerMission mission;
 public MotorcycleController[] bikes;public Transform[] seats;
 public float boardingSeconds=.8f;
 public bool IsRiding=>bike;
 public bool IsBoarding=>bike&&Time.time-boardedAt<boardingSeconds;
 MotorcycleController bike;Transform seat;Animator animator;NavMeshAgent agent;Collider[] colliders;bool[] collisionStates;
 Vector3 boardingFrom;Quaternion boardingRotation;float boardedAt;
 void Awake(){animator=GetComponentInChildren<Animator>();agent=GetComponent<NavMeshAgent>();colliders=GetComponentsInChildren<Collider>(true);collisionStates=colliders.Select(c=>c.enabled).ToArray();}
 public bool TryBoard(MotorcycleController candidate){
  if(bike||!candidate||!candidate.isDriven||Mathf.Abs(candidate.CurrentSpeed)>2.5f||Vector3.Distance(transform.position,candidate.transform.position)>3.8f)return false;
  int index=System.Array.IndexOf(bikes,candidate);if(index<0||!seats[index])return false;
  bike=candidate;seat=seats[index];boardingFrom=transform.position;boardingRotation=transform.rotation;boardedAt=Time.time;
  if(agent.enabled){if(agent.isOnNavMesh)agent.ResetPath();agent.enabled=false;}
  for(int i=0;i<colliders.Length;i++){collisionStates[i]=colliders[i].enabled;colliders[i].enabled=false;}
  animator.SetFloat("MoveSpeed",0);return true;
 }
 public bool Dismount(){
  if(!bike)return true;
  // 도로 밖이나 물 위로 순간이동시키지 않고 실제 보행 가능 지점에 내린다.
  Vector3 point=default;bool found=false;
  for(int side=0;side<2&&!found;side++)if(NavMesh.SamplePosition(bike.transform.position+bike.transform.right*(side==0?1.8f:-1.8f),out var hit,4,NavMesh.AllAreas)){point=hit.position;found=true;}
  if(!found)return false;ResetRide();transform.position=point;agent.enabled=true;agent.Warp(point);return true;
 }
 public void ResetRide(){bike=null;seat=null;if(colliders==null)return;for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=collisionStates[i];}
 void OnDisable(){ResetRide();}
 void LateUpdate(){
  if(!bike)return;
  if(!bike.isDriven){Dismount();return;}
  float t=Mathf.SmoothStep(0,1,(Time.time-boardedAt)/boardingSeconds);
  // 좌석 기준은 골반이므로 모델의 발 원점과 분리해 배치한다.
  animator.Play("Idle",0,0);animator.Update(0);
  transform.rotation=Quaternion.Slerp(boardingRotation,seat.rotation,t);
  var hips=animator.GetBoneTransform(HumanBodyBones.Hips);Vector3 target=seat.position-(hips.position-transform.position);
  transform.position=Vector3.Lerp(boardingFrom,target,t);
  for(int side=0;side<2;side++){
   bool left=side==0;float sign=left?-1:1;
   Solve(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg,left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg,left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot,seat.TransformPoint(new Vector3(sign*.24f,-.56f,.16f)),seat.forward);
   Solve(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm,left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm,left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand,seat.TransformPoint(new Vector3(sign*.24f,.04f,.08f)),seat.right*sign);
  }
 }
 void Solve(HumanBodyBones a,HumanBodyBones b,HumanBodyBones c,Vector3 target,Vector3 bend){
  var upper=animator.GetBoneTransform(a);var lower=animator.GetBoneTransform(b);var tip=animator.GetBoneTransform(c);Vector3 origin=upper.position,axis=(target-origin).normalized;float x=Vector3.Distance(origin,lower.position),y=Vector3.Distance(lower.position,tip.position),d=Mathf.Clamp(Vector3.Distance(origin,target),.01f,x+y-.005f);target=origin+axis*d;
  float along=(x*x-y*y+d*d)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,x*x-along*along));Vector3 joint=origin+axis*along+Vector3.ProjectOnPlane(bend,axis).normalized*height;
  upper.rotation=Quaternion.FromToRotation(lower.position-origin,joint-origin)*upper.rotation;lower.rotation=Quaternion.FromToRotation(tip.position-lower.position,target-lower.position)*lower.rotation;
 }
}
}
