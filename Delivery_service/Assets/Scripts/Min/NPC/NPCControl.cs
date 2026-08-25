using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCControl : MonoBehaviour
{
    [Header("이동 속도")]
    [SerializeField] private float minSpeed = 0.2f;
    [SerializeField] private float maxSpeed = 0.3f;
    [SerializeField] private float baseSpeedForAnim = 1.5f;

    [Header("이동 설정")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;
    private float currentWaitTime;

    // NPC 사망 시 매니저에 알릴 이벤트
    public event Action<NPCControl> OnNPCDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        SetNewRandomDestination();
    }

    private void Update()
    {
        UpdateAnimation();

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

    private void UpdateAnimation()
    {
        if (anim == null) return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.05f && agent.remainingDistance > agent.stoppingDistance;
        anim.SetBool("IsWalking", isMoving);

        if (isMoving)
        {
            anim.speed = agent.speed / baseSpeedForAnim;
        }
        else
        {
            anim.speed = 1f;
        }
    }

    private void SetNewRandomDestination()
    {
        Vector3 targetPoint = Vector3.zero;
        bool foundValidPoint = false;
        float minDistance = 3f;

        for (int i = 0; i < 30; i++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * wanderRadius;
            Vector3 randomDirection = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) >= minDistance)
                {
                    targetPoint = hit.position;
                    foundValidPoint = true;
                    break;
                }
            }
        }

        if (foundValidPoint)
        {
            float randomSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            agent.speed = randomSpeed;

            agent.SetDestination(targetPoint);
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