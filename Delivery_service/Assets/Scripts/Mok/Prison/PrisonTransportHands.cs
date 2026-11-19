using UnityEngine;
namespace DeliveryService.Mok
{
    [DefaultExecutionOrder(2800)]
    public sealed class PrisonTransportHands : MonoBehaviour
    {
        public PrisonTheftHands rig;
        public PrisonTransportEscape mission;
        public Camera view;
        public Transform leftGrip,rightGrip;
        void LateUpdate()
        {
            float fatigue=mission?mission.Fatigue:0;
            float loosen=mission?Mathf.Clamp01((1-mission.Grip)*.65f+fatigue*.35f)*Mathf.Clamp01((Time.time-mission.LastTapTime)*4):0;
            Vector3 slip=-view.transform.up*(loosen*.085f);
            rig.transform.rotation=view.transform.rotation;
            rig.transform.position+=rightGrip.position-view.transform.forward*.35f-view.transform.up*.18f-rig.rightUpper.position;
            for(int i=0;i<rig.fingers.Length;i++)if(rig.fingers[i])rig.fingers[i].localRotation=rig.fingerRest[i]*Quaternion.Euler(0,0,Mathf.Lerp(-60,-12,loosen));
            Reach(rig.rightUpper,rig.rightLower,rig.rightHand,rightGrip.position-view.transform.up*.06f+slip,view.transform.right);
            Reach(rig.leftUpper,rig.leftLower,rig.leftHand,leftGrip.position-view.transform.up*.06f+slip*.7f,-view.transform.right);
        }
        static void Reach(Transform u,Transform l,Transform h,Vector3 target,Vector3 side)
        {
            float a=Vector3.Distance(u.position,l.position),b=Vector3.Distance(l.position,h.position);Vector3 delta=target-u.position;float d=Mathf.Clamp(delta.magnitude,.01f,a+b-.001f);Vector3 dir=delta.normalized;
            float along=(a*a-b*b+d*d)/(2*d);Vector3 bend=Vector3.ProjectOnPlane(Vector3.down+side*.5f,dir).normalized;
            Vector3 elbow=u.position+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            u.rotation=Quaternion.FromToRotation(l.position-u.position,elbow-u.position)*u.rotation;l.rotation=Quaternion.FromToRotation(h.position-l.position,target-l.position)*l.rotation;
        }
    }
}
