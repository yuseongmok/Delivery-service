using UnityEngine;

namespace DeliveryService.Mok
{
    [DefaultExecutionOrder(3000)]
    public sealed class AftermathShopKeyGesture : MonoBehaviour
    {
        public AftermathActor mingi;
        public Transform key, grip, viewer;
        public Vector3 presented, lowered;
        public Vector3 pinchPoint = new Vector3(.15f, -.015f, -.025f);
        Transform hand, head;
        Transform[] thumb, index;
        Quaternion[] thumbRest, indexRest;
        bool holding;

        public void Present(AftermathFilm film, float progress)
        {
            float raise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.23f, .36f, progress));
            float lower = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.57f, .79f, progress));
            key.position = Vector3.Lerp(lowered, presented, raise * (1 - lower));
            key.rotation = Quaternion.Euler(Mathf.Lerp(70, 95, lower), 0, 0);
            mingi.rightContact = grip;
            mingi.leftContact = null;
            mingi.reach = 1;
            mingi.lookTarget = viewer;
            mingi.lookWeight = Mathf.SmoothStep(.2f, .8f, lower);
        }

        void LateUpdate()
        {
            if (!hand && mingi && mingi.animator)
            {
                hand = mingi.animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (!hand) return;
                thumb = Chain("Thumb_", 3); index = Chain("IndexFinger_", 4);
                thumbRest = Rest(thumb); indexRest = Rest(index);
                head = key.Find("열쇠 머리");
            }
            if (!hand) return;
            bool active = key && key.gameObject.activeInHierarchy && mingi.rightContact == grip;
            if (!active) { if (holding) Restore(); holding = false; return; }
            Restore();
            // Synty fingers are not mapped in the humanoid avatar. Pose their actual bones
            // after the arm IK, then attach the key head to the pinch rather than the wrist.
            Vector3 pinch = hand.TransformPoint(pinchPoint);
            Curl(thumb, hand.TransformPoint(pinchPoint + Vector3.forward * -.006f), true);
            Curl(index, hand.TransformPoint(pinchPoint + Vector3.forward * .006f), false);
            key.rotation = Quaternion.Euler(70, 0, -70);
            if (head) key.position += pinch - head.position;
            holding = true;
        }

        Transform[] Chain(string prefix, int count)
        {
            var bones = new Transform[count];
            foreach (var t in hand.GetComponentsInChildren<Transform>(true))
                for (int i = 0; i < count; i++)
                    if (t.name.StartsWith(prefix + (i + 1).ToString("00"))) bones[i] = t;
            return bones;
        }
        static Quaternion[] Rest(Transform[] bones)
        {
            var result = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) if (bones[i]) result[i] = bones[i].localRotation;
            return result;
        }
        void Restore()
        {
            for (int i = 0; i < thumb.Length; i++) if (thumb[i]) thumb[i].localRotation = thumbRest[i];
            for (int i = 0; i < index.Length; i++) if (index[i]) index[i].localRotation = indexRest[i];
        }
        static void Curl(Transform[] bones, Vector3 target, bool tipOffset)
        {
            var tip = bones[bones.Length - 1];
            if (!tip) return;
            for (int pass = 0; pass < 5; pass++)
                for (int i = bones.Length - 2; i >= 0; i--)
                {
                    var joint = bones[i]; if (!joint) continue;
                    Vector3 end = tipOffset ? tip.TransformPoint(new Vector3(.025f, 0, 0)) : tip.position;
                    joint.rotation = Quaternion.FromToRotation(end - joint.position, target - joint.position) * joint.rotation;
                }
        }
    }
}
