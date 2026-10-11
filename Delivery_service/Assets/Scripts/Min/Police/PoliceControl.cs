using UnityEngine;
using UnityEngine.AI;

public class PoliceControl : MonoBehaviour
{
    [Header("컴포넌트 및 대상")]
    [SerializeField] private NavMeshAgent agent;
    private Transform playerOnFootTarget;
    private MotorcycleController bikeController;

    [Header("시야각(FOV) 설정")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;        // 건물, 벽 등 레이어

    [Header("추격 타이머")]
    [SerializeField] private float maxTotalChaseTime = 120f;    // 시야 내 추격 시간 (2분)
    [SerializeField] private float maxOutOfSightTime = 60f;     // 시야 밖 추격 시간 (1분)

    [SerializeField] private float pathUpdateInterval = 0.15f;  // 0.15초마다 경로 업데이트 (움찔거림 방지)
    private float nextPathUpdateTime;

    private float totalChaseTimer;      // 전체 시간 (2분)
    private float outOfSightTimer;      // 시야각 밖 시간
    private bool isInSight;

    // 포위 추격을 위한 오프셋 (자동 지정)
    private float flankAngle;
    private float flankDistance;

    public void Initialize(Transform targetPlayer, float angleOffset, float distOffset)
    {
        flankAngle = angleOffset;
        flankDistance = distOffset;

        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.autoBraking = false;
        }

        ResetChaseTimers();
        FindAllTargets();
    }

    public void ResetChaseTimers()
    {
        totalChaseTimer = maxTotalChaseTime;
        outOfSightTimer = maxOutOfSightTime;
    }

    private void FindAllTargets()
    {
        // 보행 플레이어 찾기
        PlayerMove playerMove = FindObjectOfType<PlayerMove>(true);
        if (playerMove != null)
        {
            playerOnFootTarget = playerMove.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerOnFootTarget = playerObj.transform;
            }
        }

        // 오토바이 찾기
        if (bikeController == null)
        {
            bikeController = FindObjectOfType<MotorcycleController>(true);
        }
    }

    private Transform GetActivePlayerTarget()
    {
        if (playerOnFootTarget == null || bikeController == null)
        {
            FindAllTargets();
        }

        // 오토바이에 타고 있는 경우 오토바이 Transform 반환
        if (bikeController != null && bikeController.isDriven)
        {
            return bikeController.transform;
        }

        // 내린 상태인 경우 보행 플레이어 Transform 반환
        return playerOnFootTarget;
    }

    private void Update()
    {
        Transform activeTarget = GetActivePlayerTarget();
        if (activeTarget == null) return;

        // 시야각 및 장애물 검사
        isInSight = CheckLineOfSight(activeTarget);
        UpdateTimers();

        if (Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateInterval;
            MoveToFlankPosition(activeTarget);
        }

        if (totalChaseTimer <= 0f || outOfSightTimer <= 0f)
        {
            DespawnPolice();
        }
    }

    // 시야각 및 레이캐스트 장애물 검사
    private bool CheckLineOfSight(Transform target)
    {
        Vector3 dirToPlayer = (target.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, target.position);

        if (distToPlayer > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle / 2f) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distToPlayer, obstacleMask)) return false;

        return true;
    }

    // 시야 상태에 따른 타이머 제어
    private void UpdateTimers()
    {
        // 전체 추격 시간은 시야 확보 여부와 관계없이 무조건 감소
        totalChaseTimer -= Time.deltaTime;

        if (isInSight)
        {
            // 시야에 보일 때는 연속 이탈 타이머만 60초로 리셋
            outOfSightTimer = maxOutOfSightTime;
        }
        else
        {
            // 시야에서 놓쳤을 때만 1분 타이머가 차감됨
            outOfSightTimer -= Time.deltaTime;
        }
    }

    // 포위 위치로 추격
    private void MoveToFlankPosition(Transform target)
    {
        if (!agent.isOnNavMesh) return;

        Vector3 targetForward = target.forward;
        Vector3 offsetDirection = Quaternion.Euler(0, flankAngle, 0) * targetForward;
        Vector3 targetPosition = target.position + (offsetDirection * flankDistance);

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(target.position);
        }
    }

    private void DespawnPolice()
    {
        Destroy(gameObject);
    }
}