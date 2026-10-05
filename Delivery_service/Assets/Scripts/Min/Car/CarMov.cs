using UnityEngine;
using System.Collections;// 코루틴

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

        if (lockYPosition)
        {
            targetPosition.y = initialY + heightOffset;
        }
        else
        {
            targetPosition.y += heightOffset;
        }

        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;
        direction = direction.normalized;

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

    private void OnTriggerEnter(Collider other)
    {
        NPCControl npc = other.GetComponentInParent<NPCControl>();
        if (npc != null)
        {
            Vector3 hitDirection = transform.forward;
            float hitForce = speed * 2.5f;
            npc.Die(hitDirection, Mathf.Clamp(hitForce, 15f, 50f));
            return;
        }

        MotorcycleController bike = other.GetComponentInParent<MotorcycleController>();
        if (bike != null)
        {
            if (bike.isDriven)
            {
                bike.CrashAndEject(speed, transform.forward);
            }

            if (bike.sphereRB != null)
            {
                bike.sphereRB.AddForce(transform.forward * speed * 2f, ForceMode.VelocityChange);
            }
            return;
        }

        if (other.CompareTag("Player"))
        {
            StartCoroutine(KnockbackPlayerRoutine(other.gameObject));
        }
    }
    private IEnumerator KnockbackPlayerRoutine(GameObject player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        PlayerMove pm = player.GetComponent<PlayerMove>();

        if (pm != null) pm.enabled = false;
        yield return null;
        if (cc != null) cc.enabled = false;

        // 플레이어에게 rifidbody 부여하여 날아가게 설정
        Rigidbody tempRb = player.GetComponent<Rigidbody>();
        if (tempRb == null) tempRb = player.AddComponent<Rigidbody>();

        tempRb.isKinematic = false;
        tempRb.useGravity = true;
        tempRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 콜라이더 확인 및 추가
        CapsuleCollider tempCol = player.GetComponent<CapsuleCollider>();
        bool addedCol = false;
        if (tempCol == null)
        {
            tempCol = player.AddComponent<CapsuleCollider>();
            tempCol.height = 2f;
            tempCol.radius = 0.5f;
            tempCol.material = new PhysicsMaterial { dynamicFriction = 0.6f, bounciness = 0.2f };
            addedCol = true;
        }

        // 차량 진행 방향으로 날리기
        Vector3 force = (transform.forward * speed * 1.5f) + (Vector3.up * 8f);
        tempRb.linearVelocity = Vector3.zero;
        tempRb.AddForce(force, ForceMode.VelocityChange);

        // 날아가는 시간 대기
        yield return new WaitForSeconds(2.5f);

        // 임시 부여한 물리 컴포넌트들 제거
        if (tempRb != null) Destroy(tempRb);
        if (addedCol && tempCol != null) Destroy(tempCol);

        player.transform.rotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y, 0);

        // 조작 원상복구
        if (cc != null) cc.enabled = true;
        if (pm != null) pm.enabled = true;
    }
}

