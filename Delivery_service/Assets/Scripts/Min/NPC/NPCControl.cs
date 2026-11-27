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
    [Tooltip("한 번에 걸어갈 최대 거리. wanderRadius가 커도 이 거리 안에서만 목적지를 고른다.")]
    [SerializeField] private float maxWanderDistance = 25f;
    [Tooltip("일반 바닥(차도 포함) 이동 비용. 클수록 인도로 돌아가고 차도는 짧게 건넌다.")]
    [SerializeField] private float walkableAreaCost = 3f;
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
        var filter = new NavMeshQueryFilter { agentTypeID = agent != null ? agent.agentTypeID : 0, areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas };
        if (!NavMesh.SamplePosition(RagdollCenter.position, out var hit, 5f, filter)) return false;
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
        if (agent != null && agent.isActiveAndEnabled) agent.SetAreaCost(0, Mathf.Max(1f, walkableAreaCost));
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

    // 순간이동 방지: 길찾기 결과가 엉뚱한 시작점(타일 경계·다른 구역)에서 오면 NavMeshAgent가
    // 그 지점으로 NPC를 한 프레임에 옮긴다. 정상 걸음으로 갈 수 없는 거리면 직전 위치로 되돌리고 새 목적지를 고른다.
    private Vector3 lastSafePosition;
    private bool hasSafePosition;
    public int TeleportCorrections { get; private set; }

    private void LateUpdate()
    {
        if (isDead || IsKnockedDown || ExternalControl || agent == null || !agent.isActiveAndEnabled)
        {
            hasSafePosition = false;
            return;
        }

        Vector3 current = transform.position;
        if (hasSafePosition)
        {
            // 한 프레임이라도 NavMesh를 벗어나거나 걸음으로 갈 수 없는 거리를 움직이면 마지막 정상 위치로 되돌린다.
            float allowed = Mathf.Max(1.5f, agent.speed * Time.deltaTime * 4f + .5f);
            if (!agent.isOnNavMesh || (current - lastSafePosition).sqrMagnitude > allowed * allowed)
            {
                TeleportCorrections++;
                agent.Warp(lastSafePosition);
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    SetNewRandomDestination();
                }
                timer = 0f;
                return; // 되돌린 위치는 이미 안전한 위치이므로 기준점을 바꾸지 않는다.
            }
        }
        // 정상적으로 NavMesh 위에서 걸은 위치만 기준점으로 삼는다.
        if (agent.isOnNavMesh)
        {
            lastSafePosition = current;
            hasSafePosition = true;
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
        float reach = Mathf.Max(minDistance + 1f, Mathf.Min(wanderRadius, maxWanderDistance));
        int sideWalk = NavMesh.GetAreaFromName("SideWalk");
        int sideWalkMask = sideWalk >= 0 ? 1 << sideWalk : 0;

        // 가까운 범위에서만 목적지를 고르고, 뽑은 점 바로 근처(2.5m)의 NavMesh만 인정한다.
        // 예전처럼 반경 전체로 스냅하면 바다·건물 안에서 뽑은 점이 맵 가장자리 몇 곳으로 몰려
        // NPC들이 같은 길로 줄지어 맵을 가로질렀다. 앞의 절반은 인도만, 나머지는 걸을 수 있는 곳 전체.
        for (int i = 0; i < 30; i++)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 candidate = transform.position + new Vector3(direction.x, 0f, direction.y) * UnityEngine.Random.Range(minDistance, reach);
            int mask = i < 15 && sideWalkMask != 0 ? sideWalkMask : agent.areaMask;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, mask))
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

    public void ClearInterest()
    {
        if (isDead) return;

        // 미션 전용 적 NPC는 수배가 풀려도 관심을 끄지 않음
        var combat = GetComponent("CitizenCombat");
        if (combat != null)
        {
            var missionField = combat.GetType().GetField("missionEnemy");
            if (missionField != null && missionField.FieldType == typeof(bool) && (bool)missionField.GetValue(combat))
            {
                return;
            }
        }

        ExternalControl = false;
        SendMessage("EndFight", SendMessageOptions.DontRequireReceiver);

        if (!IsKnockedDown && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            SetNewRandomDestination();
        }
    }

    public void Die()
    {
        Die(transform.forward * -1f, 15f);
    }
}