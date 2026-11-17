using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 팔을 뻗는 순간 거리와 벽을 다시 검사하는 감옥 공용 근접 전투.
    public abstract class PrisonBrawler : MonoBehaviour
    {
        public GameObject template;
        public CitizenCombat combat;
        public bool Hold;
        public float hitRange=1.3f, attackRange=1.45f, windup=.22f;
        Animator animator;
        NavMeshAgent agent;
        float nextPath,nextPunch,punchAt=-10;
        bool pending;
        Vector3 safeFloor;
        bool hasSafeFloor;
        // 복귀는 넘어지기 전 층에서만 찾는다. 위층/지붕 NavMesh로 순간이동하지 않는다.
        public bool RecoverOnSameFloor()
        {
            if(!hasSafeFloor||!combat)return false;
            var npc=combat.Npc;
            Vector3 landing=npc.RagdollCenter.position;landing.y=safeFloor.y;
            var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
            Vector3 target=safeFloor;
            if(NavMesh.SamplePosition(landing,out var hit,1f,filter)&&Mathf.Abs(hit.position.y-safeFloor.y)<.65f){
                var path=new NavMeshPath();
                if(NavMesh.CalculatePath(safeFloor,hit.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete)target=hit.position;
            }
            // NPCControl의 기존 포즈/물리 복원은 그대로 사용하되 검색 원점만 안전한 바닥으로 제한한다.
            Transform center=npc.RagdollCenter;
            npc.SetCombatRagdollCenter(null);
            combat.transform.position=target;
            bool recovered=npc.RecoverFromKnockDown();
            npc.SetCombatRagdollCenter(center);
            return recovered;
        }
        protected abstract PlayerCombat Player { get; }
        protected abstract bool Paused { get; }
        protected virtual Transform TargetActor => Player.transform;
        protected virtual Vector3 TargetPoint => Player.View.transform.position;
        protected virtual bool AttackAllowed => true;
        protected abstract void Hit(Vector3 direction);
        public bool Defeated => !combat || combat.Health<=0 || combat.Npc.IsDead;
        public void Begin(){if(!combat)ResetActor();gameObject.SetActive(true);Cache();Hold=false;combat.Provoke(Player);combat.ExternalAttackControl=true;}
        public void ResetActor()
        {
            if(combat){combat.gameObject.SetActive(false);Destroy(combat.gameObject);}
            var actor=Instantiate(template,transform.position,transform.rotation,transform);actor.SetActive(true);
            combat=actor.GetComponent<CitizenCombat>();Cache();Hold=false;pending=false;punchAt=-10;nextPunch=Time.time+1;gameObject.SetActive(false);
        }
        void Cache(){agent=combat.GetComponent<NavMeshAgent>();animator=combat.GetComponentInChildren<Animator>();combat.ExternalAttackControl=true;if(agent&&agent.enabled&&agent.isOnNavMesh){safeFloor=agent.nextPosition;hasSafeFloor=true;}}
        protected void Start(){if(combat)Cache();}
        float HorizontalDistance()=>Vector3.ProjectOnPlane(TargetActor.position-combat.transform.position,Vector3.up).magnitude;
        public bool CanReach()
        {
            if(!combat || HorizontalDistance()>hitRange || Mathf.Abs(TargetActor.position.y-combat.transform.position.y)>2.2f)return false;
            Vector3 start=combat.transform.position+Vector3.up*1.3f;
            Vector3 delta=TargetPoint-start;
            foreach(var hit in Physics.RaycastAll(start,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(combat.transform)&&!hit.transform.IsChildOf(TargetActor))return false;
            return true;
        }
        protected void Update()
        {
            if(!combat||!agent||!agent.enabled||!agent.isOnNavMesh||Defeated||combat.Npc.IsKnockedDown){pending=false;return;}
            safeFloor=agent.nextPosition;hasSafeFloor=true;
            combat.ExternalAttackControl=true;combat.Npc.ExternalControl=true;
            if(Time.timeScale<=0||Hold||Paused){agent.isStopped=true;pending=false;return;}
            agent.speed=3.5f;agent.stoppingDistance=.95f;agent.isStopped=pending;
            if(!pending&&Time.time>nextPath){agent.SetDestination(TargetActor.position);nextPath=Time.time+.35f;}
            animator.SetFloat("MoveSpeed",agent.velocity.magnitude,.15f,Time.deltaTime);
            if(pending&&Time.time>=punchAt+windup){pending=false;if(AttackAllowed&&CanReach())Hit((TargetActor.position-combat.transform.position).normalized);}
            if(AttackAllowed&&!pending&&Time.time>nextPunch&&HorizontalDistance()<attackRange)
            {
                nextPunch=Time.time+2.6f;punchAt=Time.time;pending=true;
                Vector3 delta=TargetActor.position-combat.transform.position;delta.y=0;
                if(delta.sqrMagnitude>.01f)combat.transform.rotation=Quaternion.LookRotation(delta);
            }
        }
        protected void LateUpdate()
        {
            if(!animator||!animator.isHuman||!combat||combat.Npc.IsKnockedDown||Defeated)return;
            float t=Time.time-punchAt;
            if(t>=0&&t<.5f){float punch=Mathf.Sin(t/.5f*Mathf.PI);var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);arm.rotation=Quaternion.Slerp(arm.rotation,Quaternion.LookRotation(TargetActor.position-combat.transform.position)*Quaternion.Euler(85,0,0),punch);}
        }
    }
}
