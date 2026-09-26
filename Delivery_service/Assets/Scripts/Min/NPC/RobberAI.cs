using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobberAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform targetPlayer;

    [Header("강탈 설정")]
    [SerializeField] private int stealAmount = 200;
    [SerializeField] private float attackDistance = 1.8f;

    private bool hasStolen = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.areaMask = NavMesh.AllAreas;
        agent.stoppingDistance = 0f;
    }

    public void SetTarget(Transform player)
    {
        targetPlayer = player;
    }

    private void Update()
    {
        if (targetPlayer == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 destinationPos = GetCurrentTargetPosition();

        // 추격
        agent.SetDestination(destinationPos);

        // 강탈
        if (!hasStolen)
        {
            Vector3 robberPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(destinationPos.x, 0f, destinationPos.z);

            float flatDistance = Vector3.Distance(robberPos, targetPos);

            if (flatDistance <= attackDistance)
            {
                StealMoney();
            }
        }
    }

    private Vector3 GetCurrentTargetPosition()
    {
        // 플레이어 추적
        if (targetPlayer != null && targetPlayer.gameObject.activeInHierarchy)
        {
            return targetPlayer.position;
        }

        // 오토바이 추적
        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return targetPlayer != null ? targetPlayer.position : transform.position;
    }

    private void StealMoney()
    {
        hasStolen = true;

        ToppingInventory inventory = FindToppingInventory();

        bool pizzaStolen = false;
        if (inventory != null)
        {
            pizzaStolen = inventory.TryStealAllPizzas();
        }
        else
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AccidentMoney(stealAmount);
            }
        }

        Destroy(gameObject);
    }

    private ToppingInventory FindToppingInventory()
    {
        ToppingInventory inventory = null;

        if (targetPlayer != null)
        {
            inventory = targetPlayer.GetComponentInChildren<ToppingInventory>(true);
            if (inventory == null)
            {
                inventory = targetPlayer.GetComponentInParent<ToppingInventory>();
            }
        }

        if (inventory == null)
        {
            inventory = FindObjectOfType<ToppingInventory>();
        }

        return inventory;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}