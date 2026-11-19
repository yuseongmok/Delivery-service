using UnityEngine;
namespace DeliveryService.Mok
{
    // Only the hideout bag action: wrist targets compensate for the palm length.
    [DefaultExecutionOrder(2850)]
    public sealed class AftermathBagHands : MonoBehaviour
    {
        public AftermathHideout hideout;
        AftermathFilm film;
        Transform[][] fingers;
        Quaternion[][] rest;
        bool posed;

        void LateUpdate()
        {
            if (!hideout || !hideout.mingi || !hideout.mingi.animator) return;
            if (!film) film = GetComponent<AftermathFilm>();
            var actor = hideout.mingi;
            bool active = actor.reach > 0 && actor.leftContact == hideout.leftGrip &&
                (film.CurrentShot == hideout.recoveryShot || film.CurrentShot == hideout.decisionShot);
            if (!active) { if (posed) Restore(); posed = false; return; }
            if (fingers == null) Cache(actor.animator);
            Restore();
            var spine=actor.animator.GetBoneTransform(HumanBodyBones.Spine);
            Vector3 lean=hideout.bag.position-actor.transform.position;lean.y=0;
            if(spine)spine.position+=lean.normalized*(.15f*actor.reach);
            Pose(actor, false, hideout.rightGrip);
            Pose(actor, true, hideout.leftGrip);
            posed = true;
        }
        void Cache(Animator animator)
        {
            fingers = new Transform[2][]; rest = new Quaternion[2][];
            for (int side = 0; side < 2; side++)
            {
                var hand = animator.GetBoneTransform(side == 0 ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                var all = hand.GetComponentsInChildren<Transform>();
                fingers[side] = System.Array.FindAll(all, t => t != hand);
                rest[side] = new Quaternion[fingers[side].Length];
                for (int i = 0; i < fingers[side].Length; i++) rest[side][i] = fingers[side][i].localRotation;
            }
        }
        void Restore()
        {
            if (fingers == null) return;
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < fingers[side].Length; i++) fingers[side][i].localRotation = rest[side][i];
        }
        void Pose(AftermathActor actor, bool left, Transform contact)
        {
            var a = actor.animator;
            var upper = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            var lower = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = a.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            Vector3 direction = (hideout.bag.position - actor.transform.position); direction.y = 0; direction.Normalize();
            var finger = hand.Find(left ? "Finger.L" : "Finger.R");
            Quaternion wristRotation = hand.rotation;
            if (finger)
                wristRotation = Quaternion.AngleAxis(left ? -65 : 65, direction) * Quaternion.FromToRotation(finger.position - hand.position, direction) * hand.rotation;
            Vector3 wrist = contact.position - direction * .11f + Vector3.up * .025f;
            Vector3 end = Vector3.Lerp(hand.position, wrist, actor.reach);
            float first = Vector3.Distance(upper.position, lower.position), second = Vector3.Distance(lower.position, hand.position);
            Vector3 delta = end - upper.position; float distance = Mathf.Clamp(delta.magnitude, .01f, first + second - .001f);
            Vector3 axis = delta.normalized;
            float along = (first * first - second * second + distance * distance) / (2 * distance);
            Vector3 bend = Vector3.ProjectOnPlane(Vector3.down + actor.transform.right * (left ? -.5f : .5f), axis).normalized;
            Vector3 elbow = upper.position + axis * along + bend * Mathf.Sqrt(Mathf.Max(0, first * first - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, end - lower.position) * lower.rotation;

            hand.position = Vector3.Lerp(hand.position, end, actor.reach);
            hand.rotation = Quaternion.Slerp(hand.rotation, wristRotation, actor.reach);
            if (finger && finger.childCount > 0)
            {
                var tip = finger.GetChild(0);
                Vector3 curlTarget = contact.position - direction * .06f - Vector3.up * .045f;
                finger.rotation = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(tip.position - finger.position, curlTarget - finger.position), actor.reach) * finger.rotation;
            }
            // Curl the existing finger bones around the grip, without replacing the character mesh.
            var bones = fingers[left ? 1 : 0];
            foreach (string prefix in new[] { "IndexFinger_", "Finger_", "Thumb_" })
            {
                var chain = System.Array.FindAll(bones, t => t.name.StartsWith(prefix));
                if (chain.Length < 2) continue;
                System.Array.Sort(chain, (x,y) => string.CompareOrdinal(x.name, y.name));
                var tip = chain[chain.Length - 1];
                Vector3 target = contact.position + Vector3.down * (prefix == "Thumb_" ? -.012f : .018f);
                for (int pass = 0; pass < 4; pass++)
                    for (int i = chain.Length - 2; i >= 0; i--)
                        chain[i].rotation = Quaternion.Slerp(Quaternion.identity,
                            Quaternion.FromToRotation(tip.position - chain[i].position, target - chain[i].position), actor.reach) * chain[i].rotation;
            }
        }
    }
}
