using UnityEngine;
namespace DeliveryService.Mok
{
    // 얇은 열쇠 머리를 집는 전용 자세. 접촉점이 회전 중심이다.
    public sealed class PrisonLockGrip:MonoBehaviour
    {
        public Transform sleeve;
        public void Pose(Camera camera,Transform socket,Vector3 contact,float turn){
            transform.SetPositionAndRotation(contact,Quaternion.LookRotation(socket.forward,Vector3.up)*Quaternion.AngleAxis(-90*turn,Vector3.forward));
            Vector3 wrist=transform.TransformPoint(new Vector3(0,-.11f,-.04f));
            Vector3 elbow=camera.transform.TransformPoint(new Vector3(.25f,-.42f,.15f));
            sleeve.position=(wrist+elbow)*.5f;sleeve.rotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);
            sleeve.localScale=new Vector3(.075f,Vector3.Distance(wrist,elbow)*.5f,.065f);
        }
    }
}
