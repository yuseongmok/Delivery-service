using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok;

[RequireComponent(typeof(CitizenCombat), typeof(NPCControl))]
public class StealthCitizenAI : MonoBehaviour
{
    // 요청하신 대로 직관적인 3단계 상태로 정리 (Safe: 초록, Suspicious: 주황, Alert: 빨강)
    public enum State { Safe, Suspicious, Alert }

    [Header("현재 상태")]
    public State currentState = State.Safe;

    [Header("시야 설정")]
    public float sightDistance = 15f;
    public float sightAngle = 90f;
    public LayerMask obstacleMask;

    [Header("청각 설정 (앉으면 무음)")]
    public float walkHearRadius = 3.5f;   // 걷는 소리 감지 반경
    public float runHearRadius = 10f;     // 뛰는 소리 감지 반경
    public float runSpeedThreshold = 3.5f; // 걷기/뛰기 구분 속도

    [Header("행동 설정")]
    public float lookAroundInterval = 3f; // 두리번거리는 주기
    public float investigateTime = 3f;    // 소리 난 곳/숨은 곳 도착 후 수색 시간

    [Header("머리 위 마커 설정")]
    public bool showStateMarker = true;
    public float markerHeightOffset = 2.2f;

    private CitizenCombat combat;
    private NPCControl npc;
    private NavMeshAgent agent;
    private Animator anim;

    private Transform playerTransform;
    private PlayerCombat playerCombat;
    private CharacterController playerController;
    private PlayerTacticalStance playerStance;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 investigateTarget;
    private Vector3 lastKnownPlayerPos;

    private float stateTimer = 0f;
    private float lookRotationTimer = 0f;
    private Quaternion targetLookRotation;
    private int currentSoundPriority = 0;
    private float lostSightTimer = 0f;

    private bool hasMoveSpeed, hasWalking, hasRunRate;

    private void Awake()
    {
        combat = GetComponent<CitizenCombat>();
        npc = GetComponent<NPCControl>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>(true);

        if (anim != null)
        {
            var parameters = anim.parameters;
            hasMoveSpeed = parameters.Any(p => p.name == "MoveSpeed" && p.type == AnimatorControllerParameterType.Float);
            hasWalking = parameters.Any(p => p.name == "IsWalking" && p.type == AnimatorControllerParameterType.Bool);
            hasRunRate = parameters.Any(p => p.name == "RunRate" && p.type == AnimatorControllerParameterType.Float);
        }
    }

    private IEnumerator Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        targetLookRotation = spawnRotation;

        if (npc != null) npc.ExternalControl = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerCombat = playerObj.GetComponent<PlayerCombat>();
            playerController = playerObj.GetComponent<CharacterController>();
            playerStance = playerObj.GetComponent<PlayerTacticalStance>();
        }

        // 기존 CitizenCombat이 생성하는 기본 빨간 마커 와 겹치지 않게 비활성화
        var defaultMarker = GetComponent<EnemyLocationMarker>();
        if (defaultMarker != null && showStateMarker)
        {
            defaultMarker.enabled = false;
        }

        //NPCControl.Start()가 첫 프레임에 강제로 찍어버리는 배회 경로를 0.05초 뒤에 완벽히 덮어씌워 초기화
        yield return new WaitForSeconds(0.05f);
        if (npc != null) npc.ExternalControl = true;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        // 시작하자마자 바로 두리번거리도록 타이머를 0으로 설정
        lookRotationTimer = 0f;
    }

    private void Update()
    {
        // NPCControl의 자체 배회 영구 차단
        if (npc != null) npc.ExternalControl = true;

        if (playerTransform == null || combat.Health <= 0 || npc.IsDead || npc.IsKnockedDown) return;

        bool canSeePlayer = CheckSight(currentState == State.Alert);
        ListenToFootsteps();

        if (canSeePlayer && currentState != State.Alert)
        {
            ChangeState(State.Alert);
        }

        switch (currentState)
        {
            case State.Safe:
                UpdateSafe();
                break;
            case State.Suspicious:
                UpdateSuspicious();
                break;
            case State.Alert:
                UpdateAlert(canSeePlayer);
                break;
        }

        if (currentState != State.Alert)
        {
            UpdateStealthAnimation();
        }
    }

    private void UpdateSafe()
    {
        float distToSpawn = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(spawnPosition.x, 0, spawnPosition.z));

        // 스폰 위치에서 멀어져 있다면 스폰 위치로 걸어서 복귀
        if (distToSpawn > 0.3f)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = 2.0f;
            agent.SetDestination(spawnPosition);
        }
        // 스폰 위치에 도착해 있다면 완전히 멈춰 서서 주변을 두리번거림
        else
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // 제자리 걸음 모션 완벽 차단
            agent.updateRotation = false;

            lookRotationTimer -= Time.deltaTime;
            if (lookRotationTimer <= 0f)
            {
                lookRotationTimer = lookAroundInterval;
                // 스폰 방향을 기준으로 좌우 -70 ~ 70도 사이로 자연스럽게 두리번거림
                float randomAngle = Random.Range(-70f, 70f);
                targetLookRotation = spawnRotation * Quaternion.Euler(0, randomAngle, 0);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, Time.deltaTime * 90f);
        }
    }

    private void UpdateSuspicious()
    {
        float distToTarget = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(investigateTarget.x, 0, investigateTarget.z));

        // 소리가 난 곳 까지 아직 도착하지 않았다면 이동
        if (distToTarget > agent.stoppingDistance + 0.4f)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(investigateTarget);
        }
        // 의심 장소에 도착했다면 멈춰 서서 2~3초간 주변을 두리번거림
        else
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // 도착 후 제자리 걸음 모션 방지
            agent.updateRotation = false;

            stateTimer -= Time.deltaTime;

            lookRotationTimer -= Time.deltaTime;
            if (lookRotationTimer <= 0f)
            {
                lookRotationTimer = 1.0f;
                targetLookRotation = transform.rotation * Quaternion.Euler(0, Random.Range(-75f, 75f), 0);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, Time.deltaTime * 120f);

            // 수색 시간이 끝날 때까지 플레이어를 못 찾았다면 다시 스폰 위치(Safe)로 복귀
            if (stateTimer <= 0f)
            {
                ChangeState(State.Safe);
            }
        }
    }

    private void UpdateAlert(bool canSeePlayer)
    {
        if (!combat.IsPunching && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = combat.chaseSpeed;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer > combat.attackStartRange && agent.velocity.sqrMagnitude < 0.1f)
            {
                NavMeshPath directPath = new NavMeshPath();
                if (agent.CalculatePath(playerTransform.position, directPath))
                {
                    agent.SetPath(directPath); // 대기열 무시하고 즉시 출발!
                }
                else
                {
                    agent.SetDestination(playerTransform.position);
                }
            }
        }

        if (canSeePlayer)
        {
            lastKnownPlayerPos = playerTransform.position;
            lostSightTimer = 0f;
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer > 0.4f)
            {
                investigateTarget = lastKnownPlayerPos;
                currentSoundPriority = 2;
                ChangeState(State.Suspicious);
            }
        }
    }

    private void ChangeState(State newState)
    {
        if (currentState == State.Alert && newState != State.Alert)
        {
            combat.EndFight();
            if (npc != null) npc.ExternalControl = true;
        }

        currentState = newState;

        if (newState == State.Safe)
        {
            currentSoundPriority = 0;
            lookRotationTimer = 0f; // 돌아오자마자 바로 두리번거릴 준비
            if (agent.isOnNavMesh)
            {
                agent.stoppingDistance = 0.1f;
                agent.speed = 2.0f;
            }
        }
        else if (newState == State.Suspicious)
        {
            stateTimer = investigateTime;
            lookRotationTimer = 0f;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.stoppingDistance = 0.5f;
                agent.speed = 3.5f; // 의심 장소로 빠르게 이동
                agent.SetDestination(investigateTarget);
            }
        }
        else if (newState == State.Alert)
        {
            lostSightTimer = 0f;
            lastKnownPlayerPos = playerTransform.position;
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = false;
                agent.updateRotation = true;
            }
            combat.Provoke(playerCombat); // 공격 및 추격 개시
        }
    }

    private void UpdateStealthAnimation()
    {
        if (anim == null || !anim.enabled || npc.IsKnockedDown || npc.IsDead) return;

        float speed = (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped) ? agent.velocity.magnitude : 0f;
        bool isMoving = speed > 0.15f && agent.remainingDistance > agent.stoppingDistance + 0.1f;

        if (!isMoving) speed = 0f;

        anim.speed = 1f;
        if (hasMoveSpeed) anim.SetFloat("MoveSpeed", speed);
        if (hasWalking) anim.SetBool("IsWalking", isMoving);
        if (hasRunRate) anim.SetFloat("RunRate", isMoving ? Mathf.Clamp(speed / 4.2f, 0.8f, 1.3f) : 0f);
    }

    private bool CheckSight(bool isAlert)
    {
        if (playerController == null) return false;

        Vector3 targetPos = playerController.bounds.center;
        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 dir = (targetPos - eyePos).normalized;

        float dist = Vector3.Distance(eyePos, targetPos);
        if (dist > sightDistance) return false;

        float currentSightAngle = isAlert ? 360f : sightAngle;
        if (Vector3.Angle(transform.forward, dir) > currentSightAngle * 0.5f) return false;

        // 장애물 검사 
        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist + 0.5f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(playerTransform) || hit.transform == playerTransform) return true;
        }
        return false;
    }

    private void ListenToFootsteps()
    {
        if (currentState == State.Alert) return;

        // 앉아서 이동할 때는 소리를 전혀 듣지 못함
        if (playerStance != null && playerStance.IsCrouching) return;

        float speed = playerController != null ? playerController.velocity.magnitude : 0f;
        if (speed > 0.2f)
        {
            float radius = (speed > runSpeedThreshold) ? runHearRadius : walkHearRadius;
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
            stateTimer = investigateTime; 

            if (currentState != State.Suspicious)
            {
                ChangeState(State.Suspicious);
            }
            else if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(investigateTarget);
            }
        }
    }

    private void OnGUI()
    {
        if (!showStateMarker || npc == null || npc.IsDead || combat == null || combat.Health <= 0) return;

        Camera cam = Camera.main;
        if (cam == null && playerCombat != null) cam = playerCombat.View;
        if (cam == null) return;

        Vector3 worldPos = transform.position + Vector3.up * markerHeightOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // 카메라 뒤에 있거나 너무 멀면 표시 안 함
        if (screenPos.z <= 0 || screenPos.z > 35f) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(22, Screen.height / 35),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        Color markerColor = Color.green;
        if (currentState == State.Suspicious) markerColor = new Color(1f, 0.55f, 0f); // 주황색
        else if (currentState == State.Alert) markerColor = Color.red;               // 빨간색

        float x = screenPos.x;
        float y = Screen.height - screenPos.y;

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(x - 24, y - 24, 50, 50), "▼", style);

        // 실제 상태 색상 삼각형
        style.normal.textColor = markerColor;
        GUI.Label(new Rect(x - 25, y - 25, 50, 50), "▼", style);
    }
}