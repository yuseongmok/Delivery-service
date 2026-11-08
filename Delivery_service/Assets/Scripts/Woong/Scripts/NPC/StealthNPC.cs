using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok;

[RequireComponent(typeof(NavMeshAgent))]
public class StealthNPC : MonoBehaviour
{
    public enum AIState { Safe, Suspicious, Alert }

    [Header("현재 상태")]
    public AIState currentState = AIState.Safe;

    [Header("시야 설정 (Sight)")]
    public float sightDistance = 15f;
    public float sightAngle = 90f;
    public LayerMask obstacleMask; // Default, Player 등의 레이어 포함

    [Header("청각 설정 (Hearing)")]
    public float walkHearRadius = 6f;
    public float runHearRadius = 13f;
    public float runSpeedThreshold = 4.0f; // 이 속도 이상이면 뛰는 것으로 간주

    [Header("행동 설정 (Behavior)")]
    public float lookAroundInterval = 4f; // Safe 상태에서 두리번거리는 주기
    public float investigateTime = 5f;    // 의심 장소 도착 후 머무르는 시간

    private NavMeshAgent agent;
    private Transform playerTransform;
    private CharacterController playerController;
    private PlayerTacticalStance playerStance;
    private PlayerMove playerMove;

    private Vector3 investigateTarget;
    private Vector3 lastKnownPlayerPos;
    private float stateTimer = 0f;
    private float lookRotationTimer = 0f;
    private Quaternion targetLookRotation;

    private int currentSoundPriority = 0; //투척물 소리를 쫓기 위함

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        targetLookRotation = transform.rotation;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<CharacterController>();
            playerStance = playerObj.GetComponent<PlayerTacticalStance>();
            playerMove = playerObj.GetComponent<PlayerMove>();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        bool canSeePlayer = CheckSight();
        ListenToFootsteps();

        switch (currentState)
        {
            case AIState.Safe:
                UpdateSafe(canSeePlayer);
                break;
            case AIState.Suspicious:
                UpdateSuspicious(canSeePlayer);
                break;
            case AIState.Alert:
                UpdateAlert(canSeePlayer);
                break;
        }
    }
    private void UpdateSafe(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            ChangeState(AIState.Alert);
            return;
        }

        // 제자리에서 일정 시간마다 시선 바꾸기
        lookRotationTimer -= Time.deltaTime;
        if (lookRotationTimer <= 0f)
        {
            lookRotationTimer = lookAroundInterval;
            float randomAngle = Random.Range(-90f, 90f);
            targetLookRotation = Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, Time.deltaTime * 60f);
    }

    private void UpdateSuspicious(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            ChangeState(AIState.Alert);
            return;
        }

        // 의심되는 장소로 이동
        agent.SetDestination(investigateTarget);

        // 목적지에 거의 도착했다면
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            stateTimer -= Time.deltaTime;
            // 일정 시간 두리번거린 후 아무것도 없으면 안전 상태로 복귀
            if (stateTimer <= 0f)
            {
                ChangeState(AIState.Safe);
            }
        }
    }

    private void UpdateAlert(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            // 플레이어가 보이면 무조건 쫓아감
            lastKnownPlayerPos = playerTransform.position;
            agent.SetDestination(lastKnownPlayerPos);
        }
        else
        {
            // 숨어서 시야에서 사라졌다면, 마지막으로 본 장소까지만 일단 뛰어감
            agent.SetDestination(lastKnownPlayerPos);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                // 마지막 목격 장소까지 왔는데도 안 보이면 의심 상태로 전환해서 주변을 탐색
                investigateTarget = lastKnownPlayerPos;
                ChangeState(AIState.Suspicious);
            }
        }
    }

    private void ChangeState(AIState newState)
    {
        currentState = newState;

        if (newState == AIState.Safe)
        {
            agent.isStopped = true;
            currentSoundPriority = 0;
            lookRotationTimer = lookAroundInterval;
        }
        else if (newState == AIState.Suspicious)
        {
            agent.isStopped = false;
            agent.speed = 2.5f; // 의심할 때는 걷기
            stateTimer = investigateTime;
        }
        else if (newState == AIState.Alert)
        {
            agent.isStopped = false;
            agent.speed = 5.5f; // 발견하면 뛰기
        }
    }

    private bool CheckSight()
    {
        if (playerController == null) return false;

        Vector3 targetPos = playerController.bounds.center;
        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 dirToPlayer = (targetPos - eyePos).normalized;

        float distance = Vector3.Distance(eyePos, targetPos);
        if (distance > sightDistance) return false; // 시야 거리 밖

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > sightAngle * 0.5f) return false; // 시야각 밖

        // 레이캐스트로 엄폐물 뒤에 숨었는지 확인
        if (Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, sightDistance, obstacleMask))
        {
            if (hit.transform.IsChildOf(playerTransform) || hit.transform == playerTransform)
            {
                return true;  
            }
        }
        return false;
    }

    private void ListenToFootsteps()
    {
        // 앉아있으면 발소리가 나지 않음
        if (playerStance != null && playerStance.IsCrouching) return;

        // 플레이어의 실제 이동 속도 가져오기
        float speed = 0f;
        if (playerController != null) speed = playerController.velocity.magnitude;

        if (speed > 0.1f) // 움직이고 있다면
        {
            float currentHearRadius = (speed > runSpeedThreshold) ? runHearRadius : walkHearRadius;

            if (Vector3.Distance(transform.position, playerTransform.position) <= currentHearRadius)
            {
                HearSound(playerTransform.position, 1);
            }
        }
    }

    public void HearSound(Vector3 soundPosition, int priority)
    {
        if (currentState == AIState.Alert) return;  
        if (priority >= currentSoundPriority)
        {
            currentSoundPriority = priority;
            investigateTarget = soundPosition;
            ChangeState(AIState.Suspicious);
        }
    }
}