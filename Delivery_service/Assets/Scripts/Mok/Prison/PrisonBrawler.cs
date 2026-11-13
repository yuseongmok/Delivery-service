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
        protected abstract PlayerCombat Player { get; }
        protected abstract bool Paused { get; }
        protected abstract void Hit(Vector3 direction);
        public bool Defeated => !combat || combat.Health<=0 || combat.Npc.IsDead;
        public void Begin(){if(!combat)ResetActor();gameObject.SetActive(true);Cache();Hold=false;combat.Provoke(Player);combat.ExternalAttackControl=true;}
        public void ResetActor()
        {
            if(combat){combat.gameObject.SetActive(false);Destroy(combat.gameObject);}
            var actor=Instantiate(template,transform.position,transform.rotation,transform);actor.SetActive(true);
            combat=actor.GetComponent<CitizenCombat>();Cache();Hold=false;pending=false;punchAt=-10;nextPunch=Time.time+1;gameObject.SetActive(false);
        }
        void Cache(){agent=combat.GetComponent<NavMeshAgent>();animator=combat.GetComponentInChildren<Animator>();combat.ExternalAttackControl=true;}
        protected void Start(){if(combat)Cache();}
        float HorizontalDistance()=>Vector3.ProjectOnPlane(Player.transform.position-combat.transform.position,Vector3.up).magnitude;
        public bool CanReach()
        {
            if(!combat || HorizontalDistance()>hitRange || Mathf.Abs(Player.transform.position.y-combat.transform.position.y)>2.2f)return false;
            Vector3 start=combat.transform.position+Vector3.up*1.3f;
            Vector3 delta=Player.View.transform.position-start;
            foreach(var hit in Physics.RaycastAll(start,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(combat.transform)&&!hit.transform.IsChildOf(Player.transform))return false;
            return true;
        }
        protected void Update()
        {
            if(!combat||!agent||!agent.enabled||!agent.isOnNavMesh||Defeated||combat.Npc.IsKnockedDown){pending=false;return;}
            combat.ExternalAttackControl=true;combat.Npc.ExternalControl=true;
            if(Time.timeScale<=0||Hold||Paused){agent.isStopped=true;pending=false;return;}
            agent.speed=3.5f;agent.stoppingDistance=.95f;agent.isStopped=pending;
            if(!pending&&Time.time>nextPath){agent.SetDestination(Player.transform.position);nextPath=Time.time+.35f;}
            animator.SetFloat("MoveSpeed",agent.velocity.magnitude,.15f,Time.deltaTime);
            if(pending&&Time.time>=punchAt+windup){pending=false;if(CanReach())Hit((Player.transform.position-combat.transform.position).normalized);}
            if(!pending&&Time.time>nextPunch&&HorizontalDistance()<attackRange)
            {
                nextPunch=Time.time+2.6f;punchAt=Time.time;pending=true;
                Vector3 delta=Player.transform.position-combat.transform.position;delta.y=0;
                if(delta.sqrMagnitude>.01f)combat.transform.rotation=Quaternion.LookRotation(delta);
            }
        }
        protected void LateUpdate()
        {
            if(!animator||!animator.isHuman||!combat||combat.Npc.IsKnockedDown||Defeated)return;
            float t=Time.time-punchAt;
            if(t>=0&&t<.5f){float punch=Mathf.Sin(t/.5f*Mathf.PI);var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);arm.rotation=Quaternion.Slerp(arm.rotation,Quaternion.LookRotation(Player.transform.position-combat.transform.position)*Quaternion.Euler(85,0,0),punch);}
        }
    }
}
