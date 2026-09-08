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
    public bool isCrossing = false;

    [Header("죽었을 때")]
    [SerializeField] private Transform mainBone;

    private NavMeshAgent agent;
    private Animator anim;
    private Collider mainCollider;
    private Rigidbody[] ragdollRig;
    private Collider[] ragdollCol;
    private float timer;
    private float currentWaitTime;
    private bool isDead = false;

    // NPC 사망 시 매니저에 알릴 이벤트
    public event Action<NPCControl> OnNPCDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();
        ragdollRig = GetComponentsInChildren<Rigidbody>();
        ragdollCol = GetComponentsInChildren<Collider>();

        SetRagdollActive(false);
    }

    private void Start()
    {
        SetNewRandomDestination();
    }

    private void Update()
    {
        if (isDead) return;

        UpdateAnimation();

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    SetNewRandomDestination();
                    return;
                }

                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    timer += Time.deltaTime;
                    if (timer >= currentWaitTime)
                    {
                        SetNewRandomDestination();
                        timer = 0f;
                    }
                }
            }
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;

        bool isMoving = false;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            isMoving = agent.velocity.sqrMagnitude > 0.05f && agent.remainingDistance > agent.stoppingDistance;
            anim.speed = isMoving ? agent.speed / baseSpeedForAnim : 1f;
        }
        else
        {
            anim.speed = 1f;
        }

        anim.SetBool("IsWalking", isMoving);
    }

    private void SetRagdollActive(bool active)
    {
        // 관절 리지드바디 제어
        foreach (var rb in ragdollRig)
        {
            if (rb.gameObject != gameObject)
            {
                rb.isKinematic = !active;
            }
        }

        // 콜라이더 제어
        foreach (var col in ragdollCol)
        {
            if (col != mainCollider)
            {
                col.enabled = active;
            }
        }

        // 이동 제어
        if (anim != null) anim.enabled = !active;
        if (agent != null) agent.enabled = !active;
        if (mainCollider != null) mainCollider.enabled = !active;
    }

    private void SetNewRandomDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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
    public void Die(Vector3 hitDirection, float hitForce)
    {
        if (isDead) return;
        isDead = true;
        OnNPCDeath?.Invoke(this);

        SetRagdollActive(true);

        // 관절이 꺾이며 날아가도록
        if (mainBone != null && mainBone.TryGetComponent<Rigidbody>(out var mainRb))
        {
            // 위쪽으로 약간 뜨면서 튕겨 나가도록 방향 보정
            Vector3 finalForce = (hitDirection.normalized + Vector3.up * 0.5f) * hitForce;
            mainRb.AddForce(finalForce, ForceMode.Impulse);
        }

        Destroy(gameObject, 5f);
    }

    public void Die()
    {
        Die(transform.forward * -1f, 15f);
    }
}