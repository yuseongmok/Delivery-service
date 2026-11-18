using UnityEngine;
namespace DeliveryService.Mok
{
    // 게임플레이 리그는 유지하고 원본 Synty 외형 리그가 같은 자세를 따라간다.
    [DefaultExecutionOrder(2800)]
    public sealed class PrisonWardenVisual:MonoBehaviour
    {
        public Transform[] sourceBones, visualBones;
        public Quaternion[] rotationOffsets;
        public Transform sourceHips, visualHips;
        public Animator sourceAnimator, visualAnimator;
        Quaternion palmBasis=Quaternion.identity;PrisonPizzaEating eating;
        void Awake(){sourceAnimator=sourceHips.GetComponentInParent<Animator>();visualAnimator=visualHips.GetComponentInParent<Animator>();eating=sourceAnimator.GetComponent<PrisonPizzaEating>();
            var hand=visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);var index=visualAnimator.GetBoneTransform(HumanBodyBones.RightIndexProximal);var middle=visualAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if(!middle)middle=visualAnimator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            if(index&&middle){var f=hand.InverseTransformDirection((index.position+middle.position)*.5f-hand.position);var n=hand.InverseTransformDirection(Vector3.Cross(index.position-hand.position,middle.position-hand.position));if(f.sqrMagnitude>.00001f&&n.sqrMagnitude>.00000001f)palmBasis=Quaternion.LookRotation(f,n);}
        }
        void LateUpdate(){
            if(!sourceHips||!visualHips)return;
            for(int i=0;i<sourceBones.Length;i++)visualBones[i].rotation=sourceBones[i].rotation*rotationOffsets[i];
            visualHips.position=sourceHips.position;
            Aim(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm);
            Aim(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand);
            Aim(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm);
            Aim(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand);
            ReachRightHand();
            if(eating&&eating.Playing){var hand=visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);hand.rotation=Quaternion.LookRotation(-sourceAnimator.transform.forward,Vector3.up)*Quaternion.Inverse(palmBasis);if(eating.slice.gameObject.activeSelf)eating.slice.SetPositionAndRotation(hand.position+Vector3.up*.025f,Quaternion.LookRotation(-sourceAnimator.transform.forward,Vector3.up));}
            Aim(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg);
            Aim(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot);
            Aim(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg);
            Aim(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot);
        }
        void ReachRightHand(){
            var upper=visualAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=visualAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);var hand=visualAnimator.GetBoneTransform(HumanBodyBones.RightHand);var target=sourceAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var delta=target.position-upper.position;float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);float d=Mathf.Clamp(delta.magnitude,.01f,a+b-.001f);var dir=delta.normalized;float along=(a*a-b*b+d*d)/(2*d);var side=Vector3.ProjectOnPlane(lower.position-upper.position,dir).normalized;
            if(side.sqrMagnitude<.01f)side=-transform.up;
            var elbow=upper.position+dir*along+side*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target.position-lower.position)*lower.rotation;
        }
        void Aim(HumanBodyBones joint,HumanBodyBones end){
            if(!sourceAnimator||!visualAnimator)return;
            var from=sourceAnimator.GetBoneTransform(joint);var fromEnd=sourceAnimator.GetBoneTransform(end);
            var to=visualAnimator.GetBoneTransform(joint);var toEnd=visualAnimator.GetBoneTransform(end);
            to.rotation=Quaternion.FromToRotation(toEnd.position-to.position,fromEnd.position-from.position)*to.rotation;
        }
    }
}
