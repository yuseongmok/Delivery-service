using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    public sealed class PrisonCompanion : MonoBehaviour
    {
        public Transform follow;
        public Animator animator;
        public NavMeshAgent agent;
        public bool Following;
        public bool ScriptedTravel;
        float nextPath;
        Vector3 lastPosition;
        void OnEnable(){lastPosition=transform.position;}
        public void ResumeFollowing(){var npc=GetComponent<NPCControl>();if(npc)npc.ExternalControl=true;ScriptedTravel=false;Following=true;nextPath=0;if(agent&&agent.enabled&&agent.isOnNavMesh){agent.ResetPath();agent.isStopped=false;agent.updatePosition=true;agent.updateRotation=true;agent.stoppingDistance=1.8f;}lastPosition=transform.position;if(animator)animator.SetFloat("MoveSpeed",0);}
        void Start() { StoryActorFootsteps.Attach(transform, animator, null); }
        void Update()
        {
            if (!agent || !agent.enabled || !agent.isOnNavMesh) { if(animator)animator.SetFloat("MoveSpeed",0); lastPosition=transform.position; return; }
            if (Time.timeScale <= 0) return;
            float actualSpeed=Vector3.Distance(transform.position,lastPosition)/Mathf.Max(Time.deltaTime,.001f);lastPosition=transform.position;
            if (ScriptedTravel) { animator.SetFloat("MoveSpeed", Mathf.Min(actualSpeed,agent.velocity.magnitude), .15f, Time.deltaTime); return; }
            if (!Following || !follow) { if (agent.hasPath) agent.ResetPath(); }
            else if (Time.time >= nextPath)
            {
                nextPath = Time.time + .3f;
                float distance = Vector3.Distance(transform.position, follow.position);
                agent.speed = distance > 5 ? 5.2f : 2.4f;
                if (distance > 2.4f) agent.SetDestination(follow.position);
                else if (agent.hasPath) agent.ResetPath();
            }
            animator.SetFloat("MoveSpeed", Mathf.Min(actualSpeed,agent.velocity.magnitude), .15f, Time.deltaTime);
        }
    }
}
