using UnityEngine;
namespace DeliveryService.Mok {
public static class ArmedHandPose {
 public static float WristPitch=25;
 public static float FingerCurl=75;
 // 총은 손 본 밑에 두지 않아 수입 모델의 본 배율을 상속하지 않는다.
 public static void Apply(Animator animator,Transform gun,Vector3 target,bool aiming,bool down){
  if(!animator||!animator.isHuman||!gun)return;var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);if(!hand)return;
  if(!down&&aiming){var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var goal=upper.position+(target-upper.position).normalized*.55f-animator.transform.up*.08f;Reach(upper,lower,hand,goal,animator.transform.right);}
  Quaternion rotation=down?hand.rotation:aiming?Quaternion.LookRotation(target-hand.position):animator.transform.rotation*Quaternion.Euler(55,0,0);
  if(!down)Grip(animator,true,rotation);

  // 이동·사격 중 손잡이를 쥐고, 기절 중에는 손의 물리 움직임을 따른다.
  var offset=!down?new Vector3(.075f,.10f,.25f):new Vector3(-.015f,.02f,.09f);
  gun.SetPositionAndRotation(hand.position+rotation*offset,rotation);
  var geometry=HandgunGeometry.Get(gun);if(geometry.Found)gun.position=hand.position+rotation*new Vector3(.015f,-.015f,.065f)-gun.TransformVector(geometry.Grip);
  if(!down&&aiming){var other=animator.GetBoneTransform(HumanBodyBones.LeftHand);if(other){Reach(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),other,hand.position+rotation*new Vector3(-.065f,-.015f,.045f),-animator.transform.right);Grip(animator,false,rotation);}}
  var fingers=animator.GetComponent<SyntyGripFingers>();if(fingers)fingers.Apply(gun,!down);

 }
 static void Grip(Animator a,bool right,Quaternion rotation){
  var hand=a.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var middle=a.GetBoneTransform(right?HumanBodyBones.RightMiddleProximal:HumanBodyBones.LeftMiddleProximal);
  if(!hand)return;if(!middle){
   var fingers=hand.Find(right?"Finger.R":"Finger.L");
   if(fingers){
    // Synty의 한 덩어리 손가락 본은 휴머노이드 손가락 슬롯에 등록되어 있지 않다.
    hand.rotation=rotation*Quaternion.LookRotation(Vector3.up,Vector3.forward);
    fingers.localRotation=Quaternion.Euler(-FingerCurl,0,0);
   }else hand.rotation=Quaternion.AngleAxis(WristPitch,rotation*Vector3.right)*hand.rotation;
   return;
  }var delta=middle.position-hand.position;if(delta.sqrMagnitude>.00001f)hand.rotation=Quaternion.FromToRotation(delta,rotation*Vector3.forward)*hand.rotation;
  var starts=right?new[]{HumanBodyBones.RightIndexProximal,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightRingProximal,HumanBodyBones.RightLittleProximal}:new[]{HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftRingProximal,HumanBodyBones.LeftLittleProximal};
  foreach(var first in starts)for(int i=0;i<3;i++){var bone=a.GetBoneTransform((HumanBodyBones)((int)first+i));if(bone)bone.rotation=Quaternion.AngleAxis(i==0?55:35,rotation*Vector3.right)*bone.rotation;}
 }
 static void Reach(Transform upper,Transform lower,Transform hand,Vector3 goal,Vector3 bend){if(!upper||!lower||!hand)return;float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);Vector3 delta=goal-upper.position;float d=Mathf.Clamp(delta.magnitude,.02f,a+b-.005f);var dir=delta.normalized;float along=(a*a-b*b+d*d)/(2*d);var side=Vector3.ProjectOnPlane(bend,dir).normalized;var elbow=upper.position+dir*along+side*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,goal-lower.position)*lower.rotation;}
}
}
