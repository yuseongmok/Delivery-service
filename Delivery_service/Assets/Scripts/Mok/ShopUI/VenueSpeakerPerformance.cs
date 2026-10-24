using UnityEngine;
namespace DeliveryService.Mok {
// 무대 위 대화에 고개 돌림과 설명 손짓을 더한다. 들킨 연출의 회전은 미션이 담당한다.
[DefaultExecutionOrder(100)]
public sealed class VenueSpeakerPerformance:MonoBehaviour {
 Animator animator;Transform left,right,head;Quaternion l,r,h,originRotation;Vector3 origin;bool ready;
 void OnEnable(){origin=transform.localPosition;originRotation=transform.localRotation;animator=GetComponentInChildren<Animator>();if(!animator||!animator.isHuman)return;left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);head=animator.GetBoneTransform(HumanBodyBones.Head);if(left)l=left.localRotation;if(right)r=right.localRotation;if(head)h=head.localRotation;ready=true;}
 void LateUpdate(){var mission=VenueFinale.Current;if(!ready||!mission||!mission.Running||Time.timeScale<=0)return;if(mission.State>=VenueFinale.Phase.Exposed)return;float t=Time.time;float gesture=Mathf.SmoothStep(0,1,(Mathf.Sin(t*1.5f)+1)*.5f);transform.localPosition=origin+Vector3.right*(Mathf.Sin(t*.6f)*.055f);transform.localRotation=originRotation*Quaternion.Euler(0,Mathf.Sin(t*.42f)*12,0);if(right)right.localRotation=right.localRotation*Quaternion.Euler(-18*gesture,10*gesture,-25*gesture);if(left)left.localRotation=left.localRotation*Quaternion.Euler(-8*gesture,0,10*gesture);if(head)head.localRotation=head.localRotation*Quaternion.Euler(Mathf.Sin(t*1.1f)*3,Mathf.Sin(t*.7f)*8,0);}
 void OnDisable(){if(!ready)return;transform.localPosition=origin;transform.localRotation=originRotation;ready=false;}
}}
