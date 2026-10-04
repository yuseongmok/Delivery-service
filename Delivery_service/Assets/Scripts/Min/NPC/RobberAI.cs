using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobberAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("강탈 설정")]
    [SerializeField] private int stealAmount = 200;
    [SerializeField] private float attackDistance = 1.8f;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;

    private MotorcycleController bikeCache;
    private PlayerMove playerCache;
    private bool hasStolen = false;

    private Vector3 lastDestination = Vector3.positiveInfinity;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Rigidbody 충돌로 인한 멈춤 방지
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // NavMeshAgent 설정
        agent.speed = moveSpeed;
        agent.acceleration = 15f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = Mathf.Max(0.2f, attackDistance - 0.4f);
        agent.autoBraking = false;

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void Start()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (bikeCache == null)
        {
            bikeCache = FindObjectOfType<MotorcycleController>(true);
        }
        if (playerCache == null)
        {
            playerCache = FindObjectOfType<PlayerMove>(true);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        FindReferences();
    }

    private void Update()
    {
        if (agent == null) return;

        // 1. NavMesh 위에 정상 배치되었는지 확인
        if (!agent.isOnNavMesh)
        {
            Debug.LogError($"[RobberAI] '{gameObject.name}'가 NavMesh 위에 없습니다! 씬 바닥의 NavMesh 상태를 확인하세요.");
            return;
        }

        // 2. 현재 상태에 맞는 실시간 목표 좌표 계산 (오토바이 vs 플레이어)
        Vector3 targetPosition = GetCurrentTargetPosition();

        // 3. 목표 지점이 0.3m 이상 바뀌었거나 경로가 끊겼을 때만 SetDestination 호출 (매 프레임 호출 방지)
        if (Vector3.Distance(lastDestination, targetPosition) > 0.3f || !agent.hasPath)
        {
            lastDestination = targetPosition;
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }

        // 4. 강탈 거리 체크
        if (!hasStolen)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= attackDistance)
            {
                StealMoney();
            }
        }
    }

    private Vector3 GetCurrentTargetPosition()
    {
        if (bikeCache == null || playerCache == null)
        {
            FindReferences();
        }

        // 플레이어가 오토바이에 탔거나 PlayerMove가 꺼진 경우 -> 오토바이 위치 추적
        bool isPlayerInBike = (bikeCache != null && bikeCache.isDriven) ||
                              (playerCache != null && !playerCache.gameObject.activeInHierarchy);

        if (isPlayerInBike && bikeCache != null)
        {
            return bikeCache.transform.position;
        }

        // 플레이어가 내려서 걸어다니는 경우 -> 플레이어 위치 추적
        if (playerCache != null && playerCache.gameObject.activeInHierarchy)
        {
            return playerCache.transform.position;
        }

        // 예외 상황 분기
        if (bikeCache != null) return bikeCache.transform.position;
        if (playerCache != null) return playerCache.transform.position;

        return transform.position;
    }

    private void StealMoney()
    {
        if (hasStolen) return;
        hasStolen = true;

        ToppingInventory inventory = FindToppingInventory();
        BikePizzaStorage bikeStorage = FindObjectOfType<BikePizzaStorage>(true);

        bool playerPizzaStolen = inventory != null && inventory.TryStealAllPizzas();
        bool bikePizzaStolen = bikeStorage != null && bikeStorage.TryStealPizzas();

        if (playerPizzaStolen || bikePizzaStolen)
        {
            Debug.Log("[RobberAI] 피자를 모두 가져갔습니다");
        }
        else
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AccidentMoney(stealAmount);
                Debug.Log($"[RobberAI] 피자가 없어 {stealAmount}원을 훔쳐갔습니다");
            }
        }

        Destroy(gameObject);
    }

    private ToppingInventory FindToppingInventory()
    {
        ToppingInventory[] inventories = FindObjectsOfType<ToppingInventory>(true);
        return inventories.Length > 0 ? inventories[0] : null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}