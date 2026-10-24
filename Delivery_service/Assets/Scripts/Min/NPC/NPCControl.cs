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

    // 전투에서 사망·리스폰 이벤트 없이 기존 레그돌을 사용한다.
    public bool ExternalControl { get; set; }
    public void SetCombatRagdollCenter(Transform bone) { mainBone = bone; }
    public bool IsKnockedDown { get; private set; }
    public bool IsDead => isDead;
    public Transform RagdollCenter => mainBone != null ? mainBone : transform;
    Transform[] poseBones;
    Vector3[] restPositions;
    Quaternion[] restRotations;

    public void KnockDown(Vector3 direction, float velocity)
    {
        if (isDead || IsKnockedDown) return;
        IsKnockedDown = true;
        SetRagdollActive(true);
        foreach (var body in ragdollRig) if (body.gameObject != gameObject)
            {
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.AddForce((direction.normalized + Vector3.up * .45f) * velocity, ForceMode.VelocityChange);
            }
    }

    public bool RecoverFromKnockDown()
    {
        if (isDead || !IsKnockedDown) return false;
        if (!NavMesh.SamplePosition(RagdollCenter.position, out var hit, 5f, NavMesh.AllAreas)) return false;
        foreach (var body in ragdollRig) if (body.gameObject != gameObject)
            {
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true;
            }
        transform.position = hit.position;
        for (int i = 0; i < poseBones.Length; i++) { poseBones[i].localPosition = restPositions[i]; poseBones[i].localRotation = restRotations[i]; }
        SetRagdollActive(false);
        if (agent != null && agent.isOnNavMesh) agent.Warp(hit.position);
        if (anim != null) { anim.Rebind(); anim.Update(0); }
        IsKnockedDown = false;
        return true;
    }

    // 다음 날 전환 시 추격/적대/넉다운 상태 초기화
    public void ResetForNewDay()
    {
        if (isDead) return;

        // 넉다운 상태 복구
        if (IsKnockedDown)
        {
            RecoverFromKnockDown();
        }

        // 플레이어 타격 시 설정된 외부 제어(추격/전투) 해제
        ExternalControl = false;

        // 레그돌 비활성화 및 이동/애니메이션 복구
        SetRagdollActive(false);

        // 다시 일반 배회 목적지 설정
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            SetNewRandomDestination();
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();
        ragdollRig = GetComponentsInChildren<Rigidbody>();
        ragdollCol = GetComponentsInChildren<Collider>();

        poseBones = System.Array.FindAll(GetComponentsInChildren<Transform>(), t => t != transform);
        restPositions = System.Array.ConvertAll(poseBones, t => t.localPosition);
        restRotations = System.Array.ConvertAll(poseBones, t => t.localRotation);
        SetRagdollActive(false);
    }

    private void Start()
    {
        SetNewRandomDestination();
    }

    private void Update()
    {
        if (isDead || IsKnockedDown || ExternalControl) return;

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
        foreach (var rb in ragdollRig)
        {
            if (rb.gameObject != gameObject)
            {
                rb.isKinematic = !active;
            }
        }

        foreach (var col in ragdollCol)
        {
            if (col != mainCollider)
            {
                col.enabled = active;
            }
        }

        if (anim != null) anim.enabled = !active;
        if (agent != null) agent.enabled = !active;
        if (mainCollider != null) mainCollider.enabled = !active;
    }

    private void SetNewRandomDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (DeliveryService.Mok.EastJaywalking.TryDestination(this, agent))
        {
            currentWaitTime = UnityEngine.Random.Range(.4f, 1.5f);
            return;
        }

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

    public void Die(Vector3 hitDirection, float hitForce)
    {
        if (isDead) return;
        isDead = true;
        OnNPCDeath?.Invoke(this);

        SetRagdollActive(true);

        if (mainBone != null && mainBone.TryGetComponent<Rigidbody>(out var mainRb))
        {
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