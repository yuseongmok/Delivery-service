using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // 차량 탑승과 검문 인물 연출을 미션 판정에서 분리한다.
    [DefaultExecutionOrder(600)]
    public sealed class PrisonEscapePerformance : MonoBehaviour
    {
        public PrisonEscapeMission mission;
        public Transform driverDoor, passengerDoor, passengerStand, passengerSeat, driverEye;
        public PrisonGuard inspector;
        public bool Seated { get; private set; }
        public bool InspectorReady { get; private set; }
        Quaternion driverClosed, passengerClosed;
        float seatWeight;
        bool talkingToDriver;
        PrisonCompanion Friend => mission.previous.previous.previous.prison.introduction.mingi;
        void Awake(){driverClosed=driverDoor.localRotation;passengerClosed=passengerDoor.localRotation;}
        void Sound(AudioClip clip, Vector3 at){if(clip)AudioSource.PlayClipAtPoint(clip,at,.35f*SuppliedAudio.Volume);}
        public IEnumerator Board()
        {
            var camera=mission.filmCamera;
            Friend.ScriptedTravel=true;
            Friend.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var agent=Friend.agent;agent.isStopped=false;agent.speed=2.2f;agent.stoppingDistance=.2f;
            agent.SetDestination(passengerStand.position);
            camera.transform.position=mission.vehicle.TransformPoint(new Vector3(4,2.4f,3.8f));
            camera.transform.LookAt(passengerStand.position+Vector3.up);
            for(float t=0;t<8;t+=Time.deltaTime){if(!agent.pathPending&&Vector3.Distance(Friend.transform.position,passengerStand.position)<.6f)break;yield return null;}
            agent.ResetPath();agent.enabled=false;Friend.animator.SetFloat("MoveSpeed",0);
            var start=Friend.transform.position;var rotation=Friend.transform.rotation;
            Sound(SuppliedAudio.Current?.carOpen,passengerDoor.position);
            for(float t=0;t<.8f;t+=Time.deltaTime){passengerDoor.localRotation=passengerClosed*Quaternion.Euler(0,-70*Mathf.SmoothStep(0,1,t/.8f),0);Friend.transform.rotation=Quaternion.Slerp(rotation,passengerStand.rotation,t/.8f);yield return null;}
            for(float t=0;t<1.5f;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/1.5f);Friend.transform.position=Vector3.Lerp(start,passengerSeat.position,f);Friend.transform.rotation=Quaternion.Slerp(passengerStand.rotation,passengerSeat.rotation,f);seatWeight=f;yield return null;}
            Friend.transform.SetParent(passengerSeat,false);Friend.transform.localPosition=Vector3.zero;Friend.transform.localRotation=Quaternion.identity;seatWeight=1;Seated=true;
            for(float t=0;t<.7f;t+=Time.deltaTime){passengerDoor.localRotation=passengerClosed*Quaternion.Euler(0,-70*(1-Mathf.SmoothStep(0,1,t/.7f)),0);yield return null;}
            passengerDoor.localRotation=passengerClosed;Sound(SuppliedAudio.Current?.carClose,passengerDoor.position);
            // 운전석 문을 열고 몸을 낮춰 들어가는 1인칭 이동.
            camera.transform.position=mission.boardingPoint.position+Vector3.up*1.55f;
            camera.transform.LookAt(driverEye.position);
            Sound(SuppliedAudio.Current?.carOpen,driverDoor.position);
            for(float t=0;t<.7f;t+=Time.deltaTime){driverDoor.localRotation=driverClosed*Quaternion.Euler(0,70*Mathf.SmoothStep(0,1,t/.7f),0);yield return null;}
            var from=camera.transform.position;var fromRotation=camera.transform.rotation;
            for(float t=0;t<1.5f;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/1.5f);camera.transform.position=Vector3.Lerp(from,driverEye.position,f)-Vector3.up*(Mathf.Sin(f*Mathf.PI)*.15f);camera.transform.rotation=Quaternion.Slerp(fromRotation,driverEye.rotation,f);yield return null;}
            for(float t=0;t<.65f;t+=Time.deltaTime){driverDoor.localRotation=driverClosed*Quaternion.Euler(0,70*(1-Mathf.SmoothStep(0,1,t/.65f)),0);yield return null;}
            driverDoor.localRotation=driverClosed;Sound(SuppliedAudio.Current?.carClose,driverDoor.position);
        }
        public IEnumerator ApproachInspector()
        {
            inspector.enabled=false;if(inspector.gun)inspector.gun.gameObject.SetActive(false);
            var agent=inspector.GetComponent<NavMeshAgent>();var animator=inspector.GetComponent<Animator>();
            var goal=mission.vehicle.TransformPoint(new Vector3(-1.65f,0,.65f));
            if(NavMesh.SamplePosition(goal,out var hit,2,NavMesh.AllAreas))goal=hit.position;
            agent.isStopped=false;agent.speed=1.5f;agent.stoppingDistance=.3f;agent.SetDestination(goal);
            for(float t=0;t<6;t+=Time.deltaTime){animator.SetFloat("MoveSpeed",agent.velocity.magnitude,.15f,Time.deltaTime);mission.filmCamera.transform.position=driverEye.position;mission.filmCamera.transform.LookAt(inspector.transform.position+Vector3.up*1.45f);if(!agent.pathPending&&Vector3.Distance(inspector.transform.position,goal)<.55f)break;yield return null;}
            agent.ResetPath();animator.SetFloat("MoveSpeed",0);
            var direction=driverEye.position-inspector.transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)inspector.transform.rotation=Quaternion.LookRotation(direction);
            InspectorReady=true;
        }
        public IEnumerator ClearGate()
        {
            var agent=inspector.GetComponent<NavMeshAgent>();var animator=inspector.GetComponent<Animator>();
            agent.SetDestination(new Vector3(-3.7f,0,49));
            for(float t=0;t<2.5f;t+=Time.deltaTime){animator.SetFloat("MoveSpeed",agent.velocity.magnitude,.15f,Time.deltaTime);yield return null;}
            agent.ResetPath();animator.SetFloat("MoveSpeed",0);
        }
        public void LookAtMingi(bool friendSpeaking)
        {
            talkingToDriver=friendSpeaking;
            mission.filmCamera.transform.position=driverEye.position;
            mission.filmCamera.transform.rotation=driverEye.rotation;
            if(friendSpeaking)mission.filmCamera.transform.LookAt(Friend.animator.GetBoneTransform(HumanBodyBones.Head).position);
        }
        void LateUpdate()
        {
            if(Time.timeScale<=0||seatWeight<=0||!Friend.animator.isHuman)return;
            foreach(var b in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})Friend.animator.GetBoneTransform(b).localRotation*=Quaternion.Euler(-75*seatWeight,0,0);
            foreach(var b in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg})Friend.animator.GetBoneTransform(b).localRotation*=Quaternion.Euler(85*seatWeight,0,0);
            if(talkingToDriver)Friend.animator.GetBoneTransform(HumanBodyBones.Head).localRotation*=Quaternion.Euler(0,-45,0);
        }
    }
}
