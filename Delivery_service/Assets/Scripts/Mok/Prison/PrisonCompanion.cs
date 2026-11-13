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
        void Start() { StoryActorFootsteps.Attach(transform, animator, null); }
        void Update()
        {
            if (!agent || !agent.enabled || !agent.isOnNavMesh) return;
            if (Time.timeScale <= 0) return;
            if (ScriptedTravel) { animator.SetFloat("MoveSpeed", agent.velocity.magnitude, .15f, Time.deltaTime); return; }
            if (!Following || !follow) { if (agent.hasPath) agent.ResetPath(); }
            else if (Time.time >= nextPath)
            {
                nextPath = Time.time + .3f;
                float distance = Vector3.Distance(transform.position, follow.position);
                agent.speed = distance > 5 ? 5.2f : 2.4f;
                if (distance > 2.4f) agent.SetDestination(follow.position);
                else if (agent.hasPath) agent.ResetPath();
            }
            animator.SetFloat("MoveSpeed", agent.velocity.magnitude, .15f, Time.deltaTime);
        }
    }
}
