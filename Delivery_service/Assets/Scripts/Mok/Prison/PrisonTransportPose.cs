using UnityEngine;
namespace DeliveryService.Mok
{
    // 실제 캐릭터 리그의 양팔을 하부 지지대에 맞춘다. 판정과 연출을 분리한다.
    [DefaultExecutionOrder(2700)]
    public sealed class PrisonTransportPose : MonoBehaviour
    {
        public Animator animator;
        public Transform leftGrip, rightGrip;
        public bool Hanging, HideHead;
        void LateUpdate()
        {
            if (!animator || !animator.isHuman || !Hanging) return;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if(head) head.localScale = HideHead ? Vector3.one * .001f : Vector3.one;
            Reach(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,leftGrip,-transform.right);
            Reach(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,rightGrip,transform.right);
        }
        void Reach(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Transform target, Vector3 side)
        {
            var u=animator.GetBoneTransform(upper);var l=animator.GetBoneTransform(lower);var h=animator.GetBoneTransform(hand);
            if(!u||!l||!h||!target)return;
            float a=Vector3.Distance(u.position,l.position),b=Vector3.Distance(l.position,h.position);
            Vector3 delta=target.position-u.position;float d=Mathf.Clamp(delta.magnitude,.01f,a+b-.001f);Vector3 dir=delta.normalized;
            float along=(a*a-b*b+d*d)/(2*d);Vector3 bend=Vector3.ProjectOnPlane(Vector3.down+side*.5f,dir).normalized;
            Vector3 elbow=u.position+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            u.rotation=Quaternion.FromToRotation(l.position-u.position,elbow-u.position)*u.rotation;
            l.rotation=Quaternion.FromToRotation(h.position-l.position,target.position-l.position)*l.rotation;
        }
    }
}
