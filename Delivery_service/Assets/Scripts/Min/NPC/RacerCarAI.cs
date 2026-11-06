using UnityEngine;
using UnityEngine.AI;

public class RacerCarAI : MonoBehaviour
{
    [Header("주행 설정")]
    [SerializeField] private float moveSpeed = 18f;
    [SerializeField] private float rotateSpeed = 6f;
    [SerializeField] private float waypointReachDistance = 4f;

    private GameObject[] checkpoints;
    private int currentTargetIndex = 0;
    private bool isRacing = false;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetCheckpoints(GameObject[] points)
    {
        checkpoints = points;
        currentTargetIndex = 0;
        isRacing = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    public void StartMoving()
    {
        if (checkpoints == null || checkpoints.Length == 0) return;

        isRacing = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
        }

        SetDestinationToCurrentTarget();
    }

    private void Update()
    {
        if (!isRacing || checkpoints == null || currentTargetIndex >= checkpoints.Length) return;

        GameObject targetObj = checkpoints[currentTargetIndex];
        if (targetObj == null) return;

        Vector3 targetPos = targetObj.transform.position;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= waypointReachDistance)
            {
                AdvanceToNextCheckpoint();
            }
        }
        else
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) <= waypointReachDistance)
            {
                AdvanceToNextCheckpoint();
            }
        }
    }

    private void AdvanceToNextCheckpoint()
    {
        currentTargetIndex++;
        if (currentTargetIndex < checkpoints.Length)
        {
            SetDestinationToCurrentTarget();
        }
        else
        {
            isRacing = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            // RaceManager에 NPC 완주 상태 전달
            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.OnNpcFinished();
            }
        }
    }

    private void SetDestinationToCurrentTarget()
    {
        if (checkpoints != null && currentTargetIndex < checkpoints.Length && checkpoints[checkpoints.Length - 1] != null)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(checkpoints[currentTargetIndex].transform.position);
            }
        }
    }
}