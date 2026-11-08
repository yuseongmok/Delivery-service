using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok;

[RequireComponent(typeof(CitizenCombat), typeof(NPCControl))]
public class StealthCitizenAI : MonoBehaviour
{
    public enum State { Safe, Spotting, Suspicious, Alert }

    [Header("현재 상태")]
    public State currentState = State.Safe;

    [Header("시야 설정")]
    public float sightDistance = 15f;
    public float sightAngle = 90f; // 평소 시야각
    public LayerMask obstacleMask;

    [Header("청각 설정")]
    public float walkHearRadius = 6f;
    public float runHearRadius = 13f;
    public float runSpeedThreshold = 4.0f;

    [Header("행동 설정 (Behavior)")]
    public float lookAroundInterval = 4f; // Safe 상태 두리번 주기
    public float investigateTime = 3f; // 벽 뒤 수색 시간 (3초)
    public float spotTimeToAlert = 3f; // 발견 후 쳐다보는 시간 (3초)

    private CitizenCombat combat;
    private NPCControl npc;
    private NavMeshAgent agent;

    private Transform playerTransform;
    private PlayerCombat playerCombat;
    private CharacterController playerController;
    private PlayerTacticalStance playerStance;

    private Vector3 spawnPosition;
    private Vector3 investigateTarget;
    private Vector3 lastKnownPlayerPos;

    private float stateTimer = 0f;
    private float lookRotationTimer = 0f;
    private Quaternion targetLookRotation;
    private int currentSoundPriority = 0;
    private float lostSightTimer = 0f;

    private void Start()
    {
        combat = GetComponent<CitizenCombat>();
        npc = GetComponent<NPCControl>();
        agent = GetComponent<NavMeshAgent>();

        spawnPosition = transform.position;
        targetLookRotation = transform.rotation;
        npc.ExternalControl = true; // 무작위 배회 영구 차단[cite: 33]

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerCombat = playerObj.GetComponent<PlayerCombat>();
            playerController = playerObj.GetComponent<CharacterController>();
            playerStance = playerObj.GetComponent<PlayerTacticalStance>();
        }
    }

    private void Update()
    {
        if (playerTransform == null || combat.Health <= 0 || npc.IsDead || npc.IsKnockedDown) return;

        // Alert(추격) 상태일 때는 등 뒤로 돌아가도 어그로가 안 풀리도록 시야를 360도로 넓혀서 검사합니다.
        bool canSeePlayer = CheckSight(currentState == State.Alert);
        ListenToFootsteps();

        switch (currentState)
        {
            case State.Safe:
                UpdateSafe(canSeePlayer);
                break;
            case State.Spotting: // [추가됨] 3초간 뚫어져라 쳐다보는 상태
                UpdateSpotting(canSeePlayer);
                break;
            case State.Suspicious:
                UpdateSuspicious(canSeePlayer);
                break;
            case State.Alert:
                UpdateAlert(canSeePlayer);
                break;
        }
    }

    private void UpdateSafe(bool canSeePlayer)
    {
        if (canSeePlayer) { ChangeState(State.Spotting); return; } // 발견 즉시 Spotting 진입

        agent.updateRotation = true;
        agent.SetDestination(spawnPosition);

        // 스폰 위치에 도착하면 가만히 서서 일정 시간마다 고개만 돌림
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            agent.updateRotation = false; // 내비메시 자동 회전을 꺼서 정신없는 덜덜거림 방지

            lookRotationTimer -= Time.deltaTime;
            if (lookRotationTimer <= 0f)
            {
                lookRotationTimer = lookAroundInterval;
                float randomAngle = Random.Range(-60f, 60f); // 자연스러운 각도 제한
                targetLookRotation = Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, Time.deltaTime * 60f);
        }
    }

    private void UpdateSpotting(bool canSeePlayer)
    {
        if (!canSeePlayer) { ChangeState(State.Suspicious); return; } // 쳐다보는데 숨어버리면 수색 시작

        // 제자리에 멈춰서 플레이어를 뚫어져라 쳐다봄
        agent.isStopped = true;
        agent.updateRotation = false;

        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 200f);

        // 3초 카운트다운
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(State.Alert); // 3초 경과 시 추격 및 공격 시작
        }
    }

    private void UpdateSuspicious(bool canSeePlayer)
    {
        if (canSeePlayer) { ChangeState(State.Spotting); return; } // 수색 중 발견하면 다시 쳐다봄

        agent.updateRotation = true;
        agent.SetDestination(investigateTarget);

        // 숨은 벽 뒤(마지막 목격지)까지 완벽하게 도착했다면
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            stateTimer -= Time.deltaTime;
            agent.updateRotation = false; // 멈춰서서 주변 두리번거림

            lookRotationTimer -= Time.deltaTime;
            if (lookRotationTimer <= 0f)
            {
                lookRotationTimer = 1.0f;
                targetLookRotation = Quaternion.Euler(0, transform.eulerAngles.y + Random.Range(-70f, 70f), 0);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, Time.deltaTime * 120f);

            // 3초간 수색 후 안 보이면 스폰 위치로 복귀
            if (stateTimer <= 0)
            {
                ChangeState(State.Safe);
            }
        }
    }

    private void UpdateAlert(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPos = playerTransform.position;
            lostSightTimer = 0f; // 시야에 있으면 유예 시간 초기화
        }
        else
        {
            // 플레이어가 기둥 뒤로 0.1초 숨었다고 바로 어그로가 풀리지 않도록 0.5초의 유예 시간을 줌
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer > 0.5f)
            {
                investigateTarget = lastKnownPlayerPos;
                ChangeState(State.Suspicious); // 완전히 숨으면 벽 뒤 수색 시작
            }
        }
    }

    private void ChangeState(State newState)
    {
        // 추격 중지 처리
        if (currentState == State.Alert && newState != State.Alert)
        {
            combat.EndFight(); // 전투 모드 해제[cite: 32]
            npc.ExternalControl = true; // 제어권 다시 가져오기[cite: 33]
        }

        currentState = newState;

        if (newState == State.Safe)
        {
            agent.isStopped = false;
            agent.speed = 2.0f;
            currentSoundPriority = 0;
            lookRotationTimer = lookAroundInterval;
        }
        else if (newState == State.Spotting)
        {
            stateTimer = spotTimeToAlert; // 3초간 쳐다보기 타이머
        }
        else if (newState == State.Suspicious)
        {
            agent.isStopped = false;
            agent.speed = combat.chaseSpeed; // 수색할 때는 추격 속도로 뛰어감[cite: 32]
            stateTimer = investigateTime; // 3초 수색 타이머
            lookRotationTimer = 0f;
        }
        else if (newState == State.Alert)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            npc.ExternalControl = true;
            combat.Provoke(playerCombat); // 주먹질 강제 발동[cite: 32]
            lostSightTimer = 0f;
        }
    }

    private bool CheckSight(bool isAlert)
    {
        if (playerController == null) return false;

        Vector3 targetPos = playerController.bounds.center;
        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 dir = (targetPos - eyePos).normalized;

        if (Vector3.Distance(eyePos, targetPos) > sightDistance) return false;

        // ★ 추격 중(Alert)일 때는 시야각을 360도로 넓혀 등 뒤로 돌아가도 놓치지 않음
        float currentSightAngle = isAlert ? 360f : sightAngle;
        if (Vector3.Angle(transform.forward, dir) > currentSightAngle * 0.5f) return false;

        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, sightDistance, obstacleMask))
        {
            if (hit.transform.IsChildOf(playerTransform) || hit.transform == playerTransform) return true;
        }
        return false;
    }

    private void ListenToFootsteps()
    {
        if (playerStance != null && playerStance.IsCrouching) return;

        float speed = playerController != null ? playerController.velocity.magnitude : 0f;
        if (speed > 0.1f)
        {
            float radius = speed > runSpeedThreshold ? runHearRadius : walkHearRadius;
            if (Vector3.Distance(transform.position, playerTransform.position) <= radius)
            {
                HearSound(playerTransform.position, 1);
            }
        }
    }

    public void HearSound(Vector3 pos, int priority)
    {
        if (currentState == State.Alert) return;

        if (priority >= currentSoundPriority)
        {
            currentSoundPriority = priority;
            investigateTarget = pos;

            // 소리를 들으면 Spotting(쳐다보기) 과정을 생략하고 즉시 그쪽으로 뛰어감
            ChangeState(State.Suspicious);
        }
    }
}