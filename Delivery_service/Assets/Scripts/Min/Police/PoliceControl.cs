using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PoliceControl : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    private Transform playerOnFootTarget;
    private MotorcycleController bikeController;

    [Header("시야각")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("추격 타이머")]
    [SerializeField] private float maxTotalChaseTime = 120f;
    [SerializeField] private float maxOutOfSightTime = 60f;

    [Header("체포")]
    [SerializeField] private float arrestDistance = 2.0f;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private int fineAmount = 500;

    private Transform policeStationSpawn;
    private Image fadeScreen;

    [SerializeField] private float pathUpdateInterval = 0.15f;
    private float nextPathUpdateTime;
    private NavMeshPath chasePath;

    public float RemainingChaseSeconds => Mathf.Max(0f,totalChaseTimer);
    public bool HasVisualContact => isActiveAndEnabled && !isArresting && isInSight && RemainingChaseSeconds>0;
    private Vector3 lastKnownPosition;
    private bool targetHidden;
    private float totalChaseTimer;
    private float outOfSightTimer;
    private bool isInSight;

    private static bool isArresting = false;
    public static bool IsArrestInProgress => isArresting;
    private bool pausedForKnockdown;

    private float flankAngle;
    private float flankDistance;

    private void Awake()
    {
        isArresting = false;
    }

    public void Initialize(Transform targetPlayer, float angleOffset, float distOffset, Transform stationSpawn = null, Image fadeUI = null)
    {
        flankAngle = angleOffset;
        flankDistance = distOffset;
        policeStationSpawn = stationSpawn;
        fadeScreen = fadeUI;

        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.autoBraking = false;
        }

        ResetChaseTimers();
        FindAllTargets();
        if(playerOnFootTarget!=null)lastKnownPosition=playerOnFootTarget.position;
        FindUIAndSpawnPoint();
    }

    public void ResetChaseTimers()
    {
        totalChaseTimer = maxTotalChaseTime;
        outOfSightTimer = maxOutOfSightTime;
    }

    private void FindAllTargets()
    {
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

        if (bikeController == null)
        {
            bikeController = FindObjectOfType<MotorcycleController>(true);
        }
    }

    private void FindUIAndSpawnPoint()
    {
        if (policeStationSpawn == null)
        {
            GameObject spawnObj = GameObject.FindGameObjectWithTag("PoliceStation");
            if (spawnObj != null)
            {
                policeStationSpawn = spawnObj.transform;
            }
        }

        if (fadeScreen == null)
        {
            GameObject fadeObj = GameObject.FindGameObjectWithTag("FadeScreen");
            if (fadeObj != null)
            {
                fadeScreen = fadeObj.GetComponent<Image>();
            }
        }
    }

    private Transform GetActivePlayerTarget()
    {
        if (playerOnFootTarget == null || bikeController == null)
        {
            FindAllTargets();
        }

        if (bikeController != null && bikeController.isDriven)
        {
            return bikeController.transform;
        }

        return playerOnFootTarget;
    }

    private void Update()
    {
        if (isArresting) return;

        Transform activeTarget = GetActivePlayerTarget();
        if (activeTarget == null) return;
        var combat = playerOnFootTarget != null ? playerOnFootTarget.GetComponent<DeliveryService.Mok.PlayerCombat>() : null;
        if (combat != null && combat.IsDown) {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            pausedForKnockdown = true; return;
        }
        if (pausedForKnockdown) {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            pausedForKnockdown = false;
        }

        var hiding=activeTarget.GetComponent<DeliveryService.Mok.PlayerHiding>();
        targetHidden=hiding!=null&&hiding.IsHidden;
        isInSight = CheckLineOfSight(activeTarget);
        if(isInSight)lastKnownPosition=activeTarget.position;
        UpdateTimers();

        if (Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateInterval;
            MoveToFlankPosition(activeTarget);
        }

        // 체포 거리 검사
        float flatDistance = GetFlatDistance(transform.position, activeTarget.position);
        if (!targetHidden && flatDistance <= arrestDistance)
        {
            StartCoroutine(ArrestRoutine(activeTarget));
            return;
        }

        if (totalChaseTimer <= 0f || outOfSightTimer <= 0f)
        {
            DespawnPolice();
        }
    }

    private float GetFlatDistance(Vector3 pos1, Vector3 pos2)
    {
        Vector3 p1 = new Vector3(pos1.x, 0f, pos1.z);
        Vector3 p2 = new Vector3(pos2.x, 0f, pos2.z);
        return Vector3.Distance(p1, p2);
    }

    private IEnumerator ArrestRoutine(Transform target)
    {
        if (isArresting) yield break;
        isArresting = true;

        PoliceControl[] allPolice = FindObjectsOfType<PoliceControl>();
        foreach (var p in allPolice)
        {
            if (p != null && p.agent != null && p.agent.isOnNavMesh)
            {
                p.agent.isStopped = true;
            }
        }

        // 페이드 아웃
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            Color c = fadeScreen.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
        }

        Transform playerToTeleport = target;

        if (bikeController != null && bikeController.isDriven)
        {
            bikeController.ExitBike();

            if (playerOnFootTarget != null)
            {
                playerToTeleport = playerOnFootTarget;
            }
        }

        // 경찰서로 이동
        if (policeStationSpawn != null && playerToTeleport != null)
        {
            CharacterController cc = playerToTeleport.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = playerToTeleport.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            playerToTeleport.position = policeStationSpawn.position;
            playerToTeleport.rotation = policeStationSpawn.rotation;

            if (cc != null) cc.enabled = true;
        }

        // 벌금
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AccidentMoney(fineAmount);
        }

        // 추격중인 경찰 삭제
        foreach (var p in allPolice)
        {
            if (p != null && p != this)
            {
                Destroy(p.gameObject);
            }
        }

        // 페이드 인
        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
            fadeScreen.gameObject.SetActive(false);
        }

        isArresting = false;

        Destroy(gameObject);
    }

    private bool CheckLineOfSight(Transform target)
    {
        var hiding=target.GetComponent<DeliveryService.Mok.PlayerHiding>();
        if(hiding!=null&&hiding.IsHidden)return false;
        Vector3 dirToPlayer = (target.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, target.position);

        if (distToPlayer > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle / 2f) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distToPlayer, obstacleMask)) return false;

        return true;
    }

    private void UpdateTimers()
    {
        totalChaseTimer -= Time.deltaTime;

        if (isInSight)
        {
            outOfSightTimer = maxOutOfSightTime;
        }
        else
        {
            outOfSightTimer -= Time.deltaTime;
        }
    }

    private void MoveToFlankPosition(Transform target)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        Vector3 targetForward = target.forward;
        Vector3 offsetDirection = Quaternion.Euler(0, flankAngle, 0) * targetForward;
        Vector3 targetPosition = targetHidden?lastKnownPosition:target.position + (offsetDirection * flankDistance);

        Vector3 destination = NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas)
            ? hit.position : (targetHidden ? lastKnownPosition : target.position);
        if (agent.hasPath && (agent.destination - destination).sqrMagnitude < .0625f) return;
        if (chasePath == null) chasePath = new NavMeshPath();
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.CalculatePath(agent.nextPosition, destination, filter, chasePath) && chasePath.corners.Length > 1)
            agent.SetPath(chasePath);
    }

    private void DespawnPolice()
    {
        Destroy(gameObject);
    }
}