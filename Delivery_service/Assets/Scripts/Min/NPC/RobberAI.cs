using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobberAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("강탈 설정")]
    [SerializeField] private int stealAmount = 200;
    [SerializeField] private float attackDistance = 2.5f; // 강탈 사거리 (2.5m로 상향)

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
        agent.acceleration = 20f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.8f;      // 도달 전 멈추지 않도록 0.8m로 조정
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
        if (!agent.isOnNavMesh) return;

        Vector3 targetPosition = GetCurrentTargetPosition();

        // 0.3m 이상 이동했을 때 경로 갱신
        if (Vector3.Distance(lastDestination, targetPosition) > 0.3f || !agent.hasPath)
        {
            lastDestination = targetPosition;
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }

        // 강탈 조건 체크
        if (!hasStolen)
        {
            // 높이(Y축) 차이를 제외한 순수 수평 거리 계산
            float flatDistance = GetFlatDistance(transform.position, targetPosition);

            // 수평 거리가 attackDistance 이내이거나 NavMeshAgent가 목적지에 충분히 다가갔을 때
            if (flatDistance <= attackDistance || (agent.hasPath && agent.remainingDistance <= attackDistance))
            {
                StealMoney();
            }
        }
    }

    // Y축 높이를 무시하고 X, Z 평면 상의 거리 계산
    private float GetFlatDistance(Vector3 pos1, Vector3 pos2)
    {
        Vector3 p1 = new Vector3(pos1.x, 0f, pos1.z);
        Vector3 p2 = new Vector3(pos2.x, 0f, pos2.z);
        return Vector3.Distance(p1, p2);
    }

    private Vector3 GetCurrentTargetPosition()
    {
        if (bikeCache == null || playerCache == null)
        {
            FindReferences();
        }

        bool isPlayerInBike = (bikeCache != null && bikeCache.isDriven) ||
                              (playerCache != null && !playerCache.gameObject.activeInHierarchy);

        if (isPlayerInBike && bikeCache != null)
        {
            return bikeCache.transform.position;
        }

        if (playerCache != null && playerCache.gameObject.activeInHierarchy)
        {
            return playerCache.transform.position;
        }

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
            Debug.Log("[RobberAI] 피자를 모두 강탈하고 도망칩니다!");
        }
        else
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AccidentMoney(stealAmount);
                Debug.Log($"[RobberAI] 피자가 없어 {stealAmount}원을 훔치고 도망칩니다!");
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