using UnityEngine;
namespace DeliveryService.Mok {
    // 실제 Synty 플레이어 팔 메시와 뼈대를 사용하는 소매치기 전용 손 연출.
    public sealed class PrisonTheftHands : MonoBehaviour {
        public Transform rightUpper,rightLower,rightHand,leftUpper,leftLower,leftHand;
        public Transform[] fingers;public Quaternion[] fingerRest;
        Quaternion rightBasis,leftBasis;
        float grip,catchBlend;Vector3 reach;bool posed;
        void OnEnable(){posed=false;grip=catchBlend=0;}
        void Awake(){rightBasis=Basis(rightHand);leftBasis=Basis(leftHand);}
        Quaternion Basis(Transform hand){var tip=hand.GetComponentsInChildren<Transform>();Vector3 direction=tip.Length>1?tip[1].position-hand.position:hand.up;return Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(direction.normalized,transform.forward);}
        public void Pose(Camera camera,Vector3 contact,bool holding,float snag,float catchAmount){
            grip=Mathf.MoveTowards(grip,holding?1:0,Time.deltaTime*5);
            catchBlend=Mathf.MoveTowards(catchBlend,catchAmount,Time.deltaTime*5);
            transform.rotation=camera.transform.rotation;
            transform.position+=camera.transform.TransformPoint(new Vector3(.23f,-.33f,.02f))-rightUpper.position;
            for(int i=0;i<fingers.Length;i++)if(fingers[i])fingers[i].localRotation=fingerRest[i]*Quaternion.Euler(0,0,-grip*(fingers[i].name.Contains("_01")?28:48));
            var rotation=Quaternion.LookRotation(camera.transform.up,camera.transform.forward)*Quaternion.Euler(0,0,snag*20);
            var target=contact-camera.transform.up*.06f-camera.transform.forward*.025f;
            if(!holding)target+=camera.transform.right*.13f-camera.transform.up*.12f;
            reach=posed?Vector3.Lerp(reach,target,1-Mathf.Exp(-16*Time.deltaTime)):target;posed=true;
            Solve(rightUpper,rightLower,rightHand,reach,rotation*Quaternion.Inverse(rightBasis),camera.transform.TransformPoint(new Vector3(.5f,-.4f,.2f)));
            if(grip>.1f){Pinch(rightHand,"IndexFinger",contact+camera.transform.right*.018f,grip);Pinch(rightHand,"Thumb",contact-camera.transform.right*.018f,grip);}
            var leftTarget=Vector3.Lerp(camera.transform.TransformPoint(new Vector3(-.25f,-.6f,.2f)),contact-camera.transform.up*.17f-camera.transform.right*.08f,catchBlend);
            Solve(leftUpper,leftLower,leftHand,leftTarget,Quaternion.LookRotation(camera.transform.right,camera.transform.up)*Quaternion.Inverse(leftBasis),camera.transform.TransformPoint(new Vector3(-.5f,-.35f,.2f)));
        }
        public void PoseKey(Camera camera,Transform socket,Vector3 contact,float turn){
            Pose(camera,contact,true,0,0);
            var twist=Quaternion.AngleAxis(-90*turn,socket.forward);
            var rotation=twist*rightHand.rotation;
            Vector3 offset=twist*(-camera.transform.up*.055f-camera.transform.forward*.025f);
            Solve(rightUpper,rightLower,rightHand,contact+offset,rotation,camera.transform.TransformPoint(new Vector3(.45f,-.4f,.18f)));
            // 열쇠 머리는 얇으므로 주먹 대신 엄지/검지로 양면을 집는다.
            for(int i=0;i<fingers.Length;i++)if(fingers[i]&&fingers[i].IsChildOf(rightHand)){
                bool pinch=fingers[i].name.Contains("IndexFinger")||fingers[i].name.Contains("Thumb");
                fingers[i].localRotation=fingerRest[i]*Quaternion.Euler(0,0,pinch?-18:fingers[i].name.Contains("_01")?-65:-80);
            }
            Vector3 side=twist*camera.transform.right;
            Pinch(rightHand,"IndexFinger",contact+side*.01f,1);
            Pinch(rightHand,"Thumb",contact-side*.01f,1);
        }
        static void Pinch(Transform palm,string prefix,Vector3 goal,float weight){
            Transform tip=null;foreach(var t in palm.GetComponentsInChildren<Transform>())if(t.name.StartsWith(prefix)&&(tip==null||t.name.CompareTo(tip.name)>0))tip=t;
            if(!tip)return;
            goal=Vector3.Lerp(tip.position,goal,weight);
            for(int pass=0;pass<5;pass++)for(var joint=tip.parent;joint&&joint!=palm;joint=joint.parent)
                joint.rotation=Quaternion.FromToRotation(tip.position-joint.position,goal-joint.position)*joint.rotation;
        }
        static void Solve(Transform upper,Transform lower,Transform hand,Vector3 target,Quaternion rotation,Vector3 hint){
            Vector3 start=upper.position;float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,hand.position);
            var axis=(target-start).normalized;float d=Mathf.Clamp(Vector3.Distance(start,target),.02f,a+b-.001f);
            float along=(a*a-b*b+d*d)/(2*d);var bend=Vector3.ProjectOnPlane(hint-start,axis).normalized;
            var elbow=start+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-start,elbow-start)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
            hand.rotation=rotation;
        }
    }
}
