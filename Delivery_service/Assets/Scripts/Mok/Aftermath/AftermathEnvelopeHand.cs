using UnityEngine;
namespace DeliveryService.Mok
{
    // The palm cups the envelope edge; the thumb stays above and the finger group folds below.
    [DefaultExecutionOrder(2860)]
    public sealed class AftermathEnvelopeHand : MonoBehaviour
    {
        public AftermathIdentityHandoff handoff;
        AftermathFilm film;
        Transform finger;
        Quaternion fingerRest;
        bool posed;
        void LateUpdate()
        {
            if (!handoff || !handoff.player || !handoff.player.animator) return;
            if (!film) film=GetComponent<AftermathFilm>();
            var actor=handoff.player;
            if (film.CurrentShot!=4 || actor.reach<=0)
            { if(posed && finger) finger.localRotation=fingerRest; posed=false; return; }
            var a=actor.animator;
            var hand=a.GetBoneTransform(HumanBodyBones.RightHand);
            var upper=a.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower=a.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if(!finger){finger=hand.Find("Finger.R");if(finger) fingerRest=finger.localRotation;}
            if(finger)finger.localRotation=fingerRest;
            Vector3 direction=-handoff.envelope.right;
            Quaternion rotation=hand.rotation;
            if(finger)rotation=Quaternion.FromToRotation(finger.position-hand.position,direction)*rotation;
            Vector3 pinch=handoff.playerGrip.position;
            Vector3 wrist=pinch-direction*.065f+handoff.envelope.up*.005f;
            Vector3 end=Vector3.Lerp(hand.position,wrist,actor.reach);
            float first=Vector3.Distance(upper.position,lower.position),second=Vector3.Distance(lower.position,hand.position);
            Vector3 delta=end-upper.position;float d=Mathf.Clamp(delta.magnitude,.01f,first+second-.001f);
            Vector3 axis=delta.normalized;
            float along=(first*first-second*second+d*d)/(2*d);
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.down+actor.transform.right*.5f,axis).normalized;
            Vector3 elbow=upper.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
            hand.rotation=Quaternion.Slerp(hand.rotation,rotation,actor.reach);
            if(finger && finger.childCount>0)
            {
                var tip=finger.GetChild(0);
                Vector3 under=pinch-handoff.envelope.up*.025f;
                finger.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(tip.position-finger.position,under-finger.position),actor.reach)*finger.rotation;
            }
            posed=true;
        }
    }
}
