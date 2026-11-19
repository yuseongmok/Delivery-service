using UnityEngine;

namespace DeliveryService.Mok
{
    // 시네마틱 전용 리그. 실제 플레이어와 동료의 컨트롤러는 수정하지 않는다.
    [DefaultExecutionOrder(2800)]
    public sealed class AftermathActor : MonoBehaviour
    {
        public Animator animator;
        public Transform leftContact, rightContact;
        [Range(0, 1)] public float seated, reach;
        public float walkSpeed;
        public float rightHandRoll;
        [Range(0, 35)] public float forwardLean;
        public Transform lookTarget;
        [Range(0, 1)] public float lookWeight;

        void Update()
        {
            if (animator && animator.runtimeAnimatorController)
                animator.SetFloat("MoveSpeed", walkSpeed);
        }

        void LateUpdate()
        {
            if (!animator || !animator.isHuman) return;
            if (forwardLean > 0)
            {
                var spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                if (spine) spine.rotation = Quaternion.AngleAxis(forwardLean, transform.right) * spine.rotation;
            }
            if (lookTarget && lookWeight > 0)
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var direction = transform.InverseTransformDirection(lookTarget.position - head.position);
                float yaw = Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -55, 55);
                head.rotation = Quaternion.AngleAxis(yaw * lookWeight, transform.up) * head.rotation;
            }
            if (seated > 0)
            {
                Turn(HumanBodyBones.LeftUpperLeg,  new Vector3(-78, 0, -5), seated);
                Turn(HumanBodyBones.RightUpperLeg, new Vector3(-78, 0, 5), seated);
                Turn(HumanBodyBones.LeftLowerLeg,  new Vector3(85, 0, 0), seated);
                Turn(HumanBodyBones.RightLowerLeg, new Vector3(85, 0, 0), seated);
            }
            if (reach > 0)
            {
                Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, leftContact, -transform.right);
                Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, rightContact, transform.right);
                if (rightContact && Mathf.Abs(rightHandRoll) > .01f)
                {
                    var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    var lower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                    if (hand && lower) hand.rotation = Quaternion.AngleAxis(rightHandRoll * reach, (hand.position - lower.position).normalized) * hand.rotation;
                }
            }
        }

        void Turn(HumanBodyBones bone, Vector3 angles, float weight)
        {
            var t = animator.GetBoneTransform(bone);
            if (t) t.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(angles), weight);
        }

        void Arm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Transform target, Vector3 side)
        {
            if (!target) return;
            var u = animator.GetBoneTransform(upper);
            var l = animator.GetBoneTransform(lower);
            var h = animator.GetBoneTransform(hand);
            if (!u || !l || !h) return;
            Vector3 end = Vector3.Lerp(h.position, target.position, reach);
            float a = Vector3.Distance(u.position, l.position), b = Vector3.Distance(l.position, h.position);
            Vector3 delta = end - u.position;
            float d = Mathf.Clamp(delta.magnitude, .01f, a + b - .001f);
            Vector3 dir = delta.normalized;
            float along = (a * a - b * b + d * d) / (2 * d);
            Vector3 bend = Vector3.ProjectOnPlane(Vector3.down + side * .55f, dir).normalized;
            Vector3 elbow = u.position + dir * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            u.rotation = Quaternion.FromToRotation(l.position - u.position, elbow - u.position) * u.rotation;
            l.rotation = Quaternion.FromToRotation(h.position - l.position, end - l.position) * l.rotation;
            // Preserve the rig's wrist basis; a world-identity rotation twists Synty hands.
        }
    }
}
