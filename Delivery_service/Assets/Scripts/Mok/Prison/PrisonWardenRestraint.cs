using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // 수면 후에도 의자에 앉은 자세를 유지한다. 레그돌 복귀나 순찰이 다시 시작되지 않는다.
    [DefaultExecutionOrder(2600)]
    public sealed class PrisonWardenRestraint:MonoBehaviour
    {
        public PrisonGuard warden;
        public Transform seat;
        public GameObject ropes;
        public bool Bound{get;private set;}
        Animator animator;
        Transform[] pose;Vector3[] positions;Quaternion[] rotations;
        void Awake(){if(!warden)return;var a=warden.GetComponentInChildren<Animator>(true);pose=a.GetComponentsInChildren<Transform>(true);positions=System.Array.ConvertAll(pose,t=>t.localPosition);rotations=System.Array.ConvertAll(pose,t=>t.localRotation);}
        readonly System.Collections.Generic.Dictionary<HumanBodyBones,Quaternion> seatedRest=new();
        public void Bind()
        {
            if(Bound)return;
            warden.gameObject.SetActive(true);warden.enabled=false;
            var npc=warden.GetComponent<NPCControl>();npc.RecoverFromKnockDown();npc.ExternalControl=true;
            // 의자 주변은 NavMesh 밖일 수 있어 일반 NPC 복귀가 실패해도 물리를 반드시 종료한다.
            foreach(var body in warden.GetComponentsInChildren<Rigidbody>(true)){
                if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
                body.isKinematic=true;body.detectCollisions=false;
            }
            npc.enabled=false;
            var combat=warden.GetComponent<CitizenCombat>();if(combat){combat.EndFight();combat.ExternalAttackControl=true;combat.enabled=false;}
            var agent=warden.GetComponent<NavMeshAgent>();if(agent){if(agent.enabled&&agent.isOnNavMesh)agent.ResetPath();agent.enabled=false;}
            warden.transform.SetPositionAndRotation(seat.position,seat.rotation);
            animator=warden.GetComponentInChildren<Animator>();animator.enabled=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();animator.Update(0);animator.SetFloat("MoveSpeed",0);
            animator.enabled=false;
            for(int i=0;pose!=null&&i<pose.Length;i++){if(pose[i]==animator.transform)continue;pose[i].localPosition=positions[i];pose[i].localRotation=rotations[i];}
            seatedRest.Clear();
            foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.Spine,HumanBodyBones.Head}){var t=animator.GetBoneTransform(bone);if(t)seatedRest[bone]=t.localRotation;}
            if(warden.gun)warden.gun.gameObject.SetActive(false);
            ropes.SetActive(true);Bound=true;
        }
        void LateUpdate()
        {
            if(!Bound||!animator||!animator.isHuman||Time.timeScale<=0)return;
            warden.transform.SetPositionAndRotation(seat.position,seat.rotation);
            // 리그마다 로컬 축이 다르므로 몸 기준의 전방·아래 방향으로 관절을 맞춘다.
            for(int i=0;pose!=null&&i<pose.Length;i++){if(pose[i]==animator.transform)continue;pose[i].localPosition=positions[i];pose[i].localRotation=rotations[i];}
            SeatLeg(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot);
            SeatLeg(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot);
            var chest=animator.GetBoneTransform(HumanBodyBones.Spine);
            chest.rotation=Quaternion.AngleAxis(8,warden.transform.right)*chest.rotation;
            var head=animator.GetBoneTransform(HumanBodyBones.Head);
            head.rotation=Quaternion.AngleAxis(12,warden.transform.right)*head.rotation;
            var spine=animator.GetBoneTransform(HumanBodyBones.Spine);
            ropes.transform.SetPositionAndRotation(spine.position,warden.transform.rotation);
        }
        void SeatLeg(HumanBodyBones thighBone,HumanBodyBones kneeBone,HumanBodyBones footBone)
        {
            var thigh=animator.GetBoneTransform(thighBone);var knee=animator.GetBoneTransform(kneeBone);var foot=animator.GetBoneTransform(footBone);
            var footRotation=foot.rotation;
            thigh.rotation=Quaternion.FromToRotation(knee.position-thigh.position,warden.transform.forward)*thigh.rotation;
            knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,Vector3.down)*knee.rotation;
            foot.rotation=footRotation;
        }
    }
}
