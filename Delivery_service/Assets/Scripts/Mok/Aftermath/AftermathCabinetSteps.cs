using UnityEngine;

namespace DeliveryService.Mok
{
    // Small planted-foot steps for the cabinet action, independent of the normal walk cycle.
    [DefaultExecutionOrder(2900)]
    public sealed class AftermathCabinetSteps : MonoBehaviour
    {
        public Animator animator;
        public Vector3 stepOffset = new Vector3(.28f, 0, -.08f);
        public float leftProgress, rightProgress, leftLift, rightLift;
        public bool active;
        bool captured;
        Vector3 leftStart, rightStart;
        Quaternion leftRotation, rightRotation;

        void OnDisable() { captured = false; }

        void LateUpdate()
        {
            if (!active || !animator || !animator.isHuman) return;
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (!captured)
            {
                leftStart = left.position; rightStart = right.position;
                leftRotation = left.rotation; rightRotation = right.rotation;
                captured = true;
            }
            Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, left,
                leftStart + stepOffset * leftProgress + Vector3.up * leftLift, leftRotation);
            Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, right,
                rightStart + stepOffset * rightProgress + Vector3.up * rightLift, rightRotation);
        }

        void Leg(HumanBodyBones thigh, HumanBodyBones shin, Transform foot, Vector3 target, Quaternion rotation)
        {
            var upper = animator.GetBoneTransform(thigh);
            var lower = animator.GetBoneTransform(shin);
            float a = Vector3.Distance(upper.position, lower.position);
            float b = Vector3.Distance(lower.position, foot.position);
            Vector3 delta = target - upper.position;
            float distance = Mathf.Clamp(delta.magnitude, .01f, a + b - .001f);
            Vector3 direction = delta.normalized;
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            Vector3 bend = Vector3.ProjectOnPlane(transform.forward, direction).normalized;
            Vector3 knee = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, knee - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(foot.position - lower.position, target - lower.position) * lower.rotation;
            foot.rotation = rotation;
        }
    }
}
