using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // 간수장의 대기/협박 구간은 일반 간수 순찰과 별도로 시선을 유지한다.
    [DefaultExecutionOrder(2450)]
    public sealed class PrisonWardenAttention : MonoBehaviour
    {
        public PrisonOfficeSearch office;
        public bool Hesitating;
        void LateUpdate()
        {
            if(!office||Time.timeScale<=0)return;
            var guard=GetComponent<PrisonGuard>();var npc=GetComponent<NPCControl>();
            var eating=GetComponent<PrisonPizzaEating>();
            if((npc&&(npc.IsKnockedDown||npc.IsDead))||(eating&&eating.Playing)||(office.cctv&&office.cctv.restraint.Bound))return;
            guard.enabled=false;if(npc)npc.ExternalControl=true;
            var agent=GetComponent<NavMeshAgent>();if(agent&&agent.enabled&&agent.isOnNavMesh){agent.ResetPath();agent.isStopped=true;agent.updateRotation=false;}
            var player=office.previous.previous.prison.player;if(!player)return;
            Vector3 target=office.evidence.Busy&&office.evidence.Discovered>=3?office.previous.camera.transform.position:player.transform.position;
            Vector3 delta=Vector3.ProjectOnPlane(target-transform.position,Vector3.up);
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),150*Time.deltaTime);
            var animator=GetComponentInChildren<Animator>();if(animator){animator.SetFloat("MoveSpeed",0);if(Hesitating&&animator.isHuman){var head=animator.GetBoneTransform(HumanBodyBones.Head);head.rotation=Quaternion.AngleAxis(12,transform.right)*head.rotation;}}
        }
    }
}
