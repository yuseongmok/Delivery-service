using UnityEngine;

public class CarMov : MonoBehaviour
{
    [SerializeField] private WayPoint currentTarget;        // 목표 노드
    [SerializeField] private float speed = 12f;             // 이동 속도
    [SerializeField] private float rotationSpeed = 6f;      // 회전 속도
    [SerializeField] private float reachDistance = 1.0f;    // 노드 도달 인정 거리
    [SerializeField] private float heightOffset = 0f;       // 높이 조절
    [SerializeField] private bool lockYPosition = false;

    [Header("사람 감지")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionDistance = 4f;
    [SerializeField] private float detectionRadius = 1.2f;
    [SerializeField] private Vector3 rayOffset = new Vector3(0f, 0.5f, 0.5f);

    private float initialY;
    private bool isObstacleDetected = false;

    private void Start()
    {
        initialY = transform.position.y;
    }

    private void Update()
    {
        CheckForObstacles();

        if (isObstacleDetected) return;
        if (currentTarget == null) return;

        // 목표 방향 계산
        Vector3 targetPosition = currentTarget.transform.position;
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (lockYPosition)
        {
            targetPosition.y = initialY + heightOffset;
        }
        else
        {
            targetPosition.y += heightOffset;
        }

        // 부드러운 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 직선 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // 다음 노드 갱신
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance <= reachDistance)
        {
            GetNextWaypoint();
        }
    }

    private void CheckForObstacles()
    {
        Vector3 origin = transform.TransformPoint(rayOffset);
        Vector3 direction = transform.forward;

        if (currentTarget != null)
        {
            Vector3 targetDir = (currentTarget.transform.position - transform.position).normalized;
            targetDir.y = 0;
            direction = Vector3.Slerp(transform.forward, targetDir, 0.5f).normalized;
        }

        RaycastHit[] hits = Physics.SphereCastAll(origin, detectionRadius, direction, detectionDistance, obstacleLayer);

        bool obstacleFound = false;

        foreach (var hit in hits)
        {
            // 건너는 중인 경우 멈춤
            if (hit.collider.TryGetComponent<NPCControl>(out var npc))
            {
                if (npc.isCrossing)
                {
                    obstacleFound = true;
                    break;
                }
            }
        }

        isObstacleDetected = obstacleFound;
    }

    private void GetNextWaypoint()
    {
        if (currentTarget.nextWaypoints == null || currentTarget.nextWaypoints.Count == 0)
        {
            // 경로 끝에 도달하면 정지
            currentTarget = null;
            return;
        }

        // 사거리일 경우 다음 노드 중 무작위 1개 선택
        int randomIndex = Random.Range(0, currentTarget.nextWaypoints.Count);
        currentTarget = currentTarget.nextWaypoints[randomIndex];
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(rayOffset);
        Vector3 direction = transform.forward;

        Gizmos.color = isObstacleDetected ? Color.red : Color.green;

        // 구체 표시
        Gizmos.DrawRay(origin, direction * detectionDistance);
        Gizmos.DrawWireSphere(origin + direction * detectionDistance, detectionRadius);
    }
}
