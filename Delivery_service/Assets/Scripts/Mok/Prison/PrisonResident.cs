using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 수감자는 지정된 생활 구역만 오가며, 전투 중에는 기존 시민 전투에 제어권을 넘긴다.
    public sealed class PrisonResident : MonoBehaviour
    {
        public Transform[] route;
        NPCControl npc;
        CitizenCombat combat;
        NavMeshAgent agent;
        Animator animator;
        float pause;
        float stalledAt;
        Vector3 lastPosition;
        bool travelling;
        NavMeshPath path;
        Transform reserved;
        float nextThink, yieldUntil, nextYield;
        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        void OnDisable() { PrisonTraffic.Release(reserved, this); }
        void Start()
        {
            path = new NavMeshPath();
            npc = GetComponent<NPCControl>(); combat = GetComponent<CitizenCombat>();
            agent = GetComponent<NavMeshAgent>(); animator = GetComponentInChildren<Animator>();
            npc.ExternalControl = true;
            agent.speed = Random.Range(1.5f, 1.85f);
            agent.stoppingDistance = .25f;
            pause = Time.time + Random.Range(0f, 2f);
            nextThink = Time.time + Random.Range(0f, .2f);
        }
        void Update()
        {
            if (!agent || !agent.isOnNavMesh || npc.IsDead || npc.IsKnockedDown || combat.IsAggressive)
            { travelling = false; PrisonTraffic.Release(reserved, this); return; }
            npc.ExternalControl = true;
            if (animator) animator.SetFloat(MoveSpeed, agent.velocity.magnitude, .15f, Time.deltaTime);
            if (Time.time < nextThink) return;
            nextThink = Time.time + .2f;
            if (route != null && route.Length > 0)
            {
                agent.isStopped = false;
                if (Time.time >= nextYield && PrisonTraffic.YieldToCart(agent, path))
                {
                    nextYield = Time.time + 1.5f; yieldUntil = Time.time + 3f;
                    travelling = false; PrisonTraffic.Release(reserved, this); reserved = null;
                }
                if (Time.time < yieldUntil) return;
                if (travelling && !agent.pathPending)
                {
                    if (Vector3.Distance(lastPosition, transform.position) > .2f)
                    { lastPosition = transform.position; stalledAt = Time.time; }
                    if (agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance <= agent.stoppingDistance + .15f || Time.time - stalledAt > 5f)
                    {
                        travelling = false;
                        agent.ResetPath();
                        pause = Time.time + Random.Range(1f, 4f);
                    }
                }
                if (!travelling && Time.time >= pause) ChooseDestination();
            }
            else if (agent.hasPath) agent.ResetPath();
        }
        // 목적지에 도착한 뒤에만 새 경로를 선택한다. 도달할 수 없는 장소는 건너뛴다.
        void ChooseDestination()
        {
            path ??= new NavMeshPath();
            int start = Random.Range(0, route.Length);
            for (int i = 0; i < route.Length; i++)
            {
                var point = route[(start + i) % route.Length];
                if (!point || Vector3.Distance(transform.position, point.position) < 2f) continue;
                if (!PrisonTraffic.Available(point, this)) continue;
                if (!NavMesh.SamplePosition(point.position, out var hit, 1.5f, agent.areaMask)) continue;
                if (!PrisonTraffic.PathBudget()) return;
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                PrisonTraffic.Release(reserved, this); reserved = point; PrisonTraffic.Reserve(point, this);
                agent.SetPath(path);
                travelling = true;
                lastPosition = transform.position;
                stalledAt = Time.time;
                return;
            }
            pause = Time.time + 2f;
        }
    }
}


