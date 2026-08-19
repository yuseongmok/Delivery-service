using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWander : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;

    private NavMeshAgent agent;
    private float timer;
    private float currentWaitTime;

    // NPC 사망 시 매니저에 알릴 이벤트
    public event Action<NPCWander> OnNPCDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        SetNewRandomDestination();
    }

    private void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            timer += Time.deltaTime;
            if (timer >= currentWaitTime)
            {
                SetNewRandomDestination();
                timer = 0f;
            }
        }
    }

    private void SetNewRandomDestination()
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        currentWaitTime = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
    }

    // NPC 사망
    public void Die()
    {
        OnNPCDeath?.Invoke(this);
        Destroy(gameObject);
    }
}