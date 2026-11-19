using UnityEngine;
namespace DeliveryService.Mok
{
    [DefaultExecutionOrder(2840)]
    public sealed class AftermathDoorHand : MonoBehaviour
    {
        public AftermathHideout hideout;
        Transform finger;
        Quaternion rest;
        bool posed;
        void LateUpdate()
        {
            if(!hideout || !hideout.mingi || !hideout.mingi.animator)return;
            var actor=hideout.mingi;
            if(actor.rightContact!=hideout.doorGrip || actor.reach<=0)
            {if(posed&&finger)finger.localRotation=rest;posed=false;return;}
            var a=actor.animator;
            var upper=a.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower=a.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var hand=a.GetBoneTransform(HumanBodyBones.RightHand);
            if(!finger){finger=hand.Find("Finger.R");if(finger)rest=finger.localRotation;}
            if(finger)finger.localRotation=rest;
            float opening = Mathf.Abs(Mathf.DeltaAngle(0,hideout.cabinetDoor.localEulerAngles.y))/90f;
            var spine=a.GetBoneTransform(HumanBodyBones.Spine);
            if(spine)spine.position+=Vector3.right*(.28f*Mathf.SmoothStep(0,1,opening)*actor.reach);
            Vector3 direction=hideout.cabinetDoor.right;
            Quaternion orientation=hand.rotation;
            if(finger)orientation=Quaternion.FromToRotation(finger.position-hand.position,direction)*orientation;
            Vector3 contact=hideout.doorGrip.position;
            Vector3 end=Vector3.Lerp(hand.position,contact-direction*.16f,actor.reach);
            float first=Vector3.Distance(upper.position,lower.position),second=Vector3.Distance(lower.position,hand.position);
            Vector3 delta=end-upper.position;float d=Mathf.Clamp(delta.magnitude,.01f,first+second-.001f);
            Vector3 axis=delta.normalized;float along=(first*first-second*second+d*d)/(2*d);
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.down+actor.transform.right*.45f,axis).normalized;
            Vector3 elbow=upper.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
            // Keep the wrist seated on the grip after the two-bone solve.
            hand.position=Vector3.Lerp(hand.position,end,actor.reach);
            hand.rotation=Quaternion.Slerp(hand.rotation,orientation,actor.reach);
            if(finger&&finger.childCount>0)
            {
                var tip=finger.GetChild(0);
                Vector3 around=contact+hideout.cabinetDoor.right*.015f;
                finger.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(tip.position-finger.position,around-finger.position),actor.reach)*finger.rotation;
            }
            posed=true;
        }
    }
}
