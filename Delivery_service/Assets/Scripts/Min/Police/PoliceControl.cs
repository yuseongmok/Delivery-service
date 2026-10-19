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
    [SerializeField] private float arrestDistance = 2.5f;
    [SerializeField] private float hiddenArrestDistance = 10f;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private int fineAmount = 500; // 이전 씬 직렬화 호환용
    public static int RollFine()=>Random.Range(5000,15001);

    private Transform policeStationSpawn;
    private Image fadeScreen;

    [SerializeField] private float pathUpdateInterval = 0.15f;
    private float nextPathUpdateTime;
    private NavMeshPath chasePath;

    public void LimitChaseTime(float remaining){totalChaseTimer=Mathf.Min(totalChaseTimer,Mathf.Max(0,remaining));}
    public float RemainingChaseSeconds => Mathf.Max(0f, totalChaseTimer);
    public bool HasVisualContact => isActiveAndEnabled && !isArresting && isInSight && RemainingChaseSeconds > 0;
    private Vector3 lastKnownPosition;
    private bool targetHidden;
    private float totalChaseTimer;
    private float outOfSightTimer;
    private bool isInSight;

    private bool wasHiddenLastFrame;
    private bool wasInSightLastFrame;
    private bool sawPlayerHide;

    private static bool isArresting = false;
    public static bool IsArrestInProgress => isArresting;
    private bool pausedForKnockdown;

    private float flankAngle;
    private float flankDistance;

    bool ownsArrest;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetArrest(){isArresting=false;}
    void OnDestroy(){DeliveryService.Mok.SceneActors.Remove(this);if(ownsArrest)isArresting=false;}
    private void Awake()
    {
        DeliveryService.Mok.SceneActors.Register(this);
        if(GetComponent<DeliveryService.Mok.PoliceCombat>()==null)gameObject.AddComponent<DeliveryService.Mok.PoliceCombat>();
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
        if (playerOnFootTarget != null) lastKnownPosition = playerOnFootTarget.position;
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
        var fighter=GetComponent<DeliveryService.Mok.PoliceCombat>();
        if(fighter!=null&&fighter.IsDown)return;

        Transform activeTarget = GetActivePlayerTarget();
        if (activeTarget == null) return;

        var combat = playerOnFootTarget != null ? playerOnFootTarget.GetComponent<DeliveryService.Mok.PlayerCombat>() : null;
        if (combat != null && combat.IsDown)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            pausedForKnockdown = true; return;
        }
        if (pausedForKnockdown)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            pausedForKnockdown = false;
        }

        var hiding = activeTarget.GetComponent<DeliveryService.Mok.PlayerHiding>();
        bool isCurrentlyHidden = hiding != null && hiding.IsHidden;

        if (!wasHiddenLastFrame && isCurrentlyHidden)
        {
            sawPlayerHide = wasInSightLastFrame;
            if (sawPlayerHide)
            {
                lastKnownPosition = activeTarget.position;
            }
        }
        else if (!isCurrentlyHidden)
        {
            sawPlayerHide = false;
        }

        wasHiddenLastFrame = isCurrentlyHidden;

        if (!isCurrentlyHidden)
        {
            isInSight = CheckLineOfSightToPosition(activeTarget.position);
            if (isInSight) lastKnownPosition = activeTarget.position;
        }
        else
        {
            isInSight = sawPlayerHide;
        }

        wasInSightLastFrame = isInSight;
        targetHidden = isCurrentlyHidden && !sawPlayerHide;

        bool fastTimerDecay = isCurrentlyHidden && !sawPlayerHide;
        UpdateTimers(fastTimerDecay);

        if (Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateInterval;
            MoveToFlankPosition(activeTarget, sawPlayerHide);
        }

        float currentArrestDistance = (isCurrentlyHidden && sawPlayerHide) ? hiddenArrestDistance : arrestDistance;

        float flatDistance = GetFlatDistance(transform.position, activeTarget.position);
        if ((fighter==null||!fighter.Armed) && (!isCurrentlyHidden || sawPlayerHide) && flatDistance <= currentArrestDistance)
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
        FindFirstObjectByType<PoliceManager>()?.CancelReplacements();
        ownsArrest = true;
        DeliveryService.Mok.ShopReputation.Instance.Penalize(true);

        Transform playerToTeleport = target;

        if (bikeController != null && bikeController.isDriven)
        {
            bikeController.ApplyImpactDeceleration(0f);
            bikeController.ExitBike();

            if (playerOnFootTarget != null)
            {
                playerToTeleport = playerOnFootTarget;
            }
        }

        bool controllerEnabled=false, moveEnabled=false, bodyKinematic=true;

        // 쓰레기통에서 복귀시킨 뒤, 체포 진행 중 떨이지지 않도록 물리/이동 컴포넌트를 즉시 비활성화
        if (playerToTeleport != null)
        {
            var hiding = playerToTeleport.GetComponent<DeliveryService.Mok.PlayerHiding>();
            if (hiding != null && hiding.IsHidden)
            {
                hiding.Leave(false);
            }

            // 플레이어 조작, 콜라이더, 중력 비활성화 (바닥 뚫림 방지)
            CharacterController cc = playerToTeleport.GetComponent<CharacterController>();
            if (cc != null){controllerEnabled=cc.enabled;cc.enabled = false;}

            PlayerMove pm = playerToTeleport.GetComponent<PlayerMove>();
            if (pm != null){moveEnabled=pm.enabled;pm.enabled = false;}

            Rigidbody rb = playerToTeleport.GetComponent<Rigidbody>();
            if (rb != null)
            {
                bodyKinematic=rb.isKinematic;
                if(!rb.isKinematic){
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
        }

        // 모든 경찰 이동 정지
        PoliceControl[] allPolice = FindObjectsOfType<PoliceControl>();
        foreach (var p in allPolice)
        {
            if (p != null && p.agent != null && p.agent.isOnNavMesh)
            {
                p.agent.isStopped = true;
            }
        }

        var presentation=gameObject.AddComponent<DeliveryService.Mok.PoliceArrestPresentation>();
        presentation.Begin(playerToTeleport,transform);
        yield return presentation.FaceOfficer();

        // 화면 페이드 아웃
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            Color c = fadeScreen.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
        }

        // 경찰서 위치로 플레이어 이동
        if (policeStationSpawn != null && playerToTeleport != null)
        {
            playerToTeleport.position = policeStationSpawn.position;
            playerToTeleport.rotation = policeStationSpawn.rotation;
        }

        presentation.AlignAtStation();

        // 벌금 차감
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AccidentMoney(RollFine());
        }

        // 추격 중인 나머지 경찰 제거
        foreach (var p in allPolice)
        {
            if (p != null && p != this)
            {
                Destroy(p.gameObject);
            }
        }

        // 화면 페이드 인
        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
            fadeScreen.gameObject.SetActive(false);
        }

        // 경찰서에 도착했으므로 플레이어 상태 원복
        if (playerToTeleport != null)
        {
            CharacterController cc = playerToTeleport.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = controllerEnabled;

            PlayerMove pm = playerToTeleport.GetComponent<PlayerMove>();
            if (pm != null) pm.enabled = moveEnabled;

            Rigidbody rb = playerToTeleport.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = bodyKinematic;
        }

        presentation.Finish();
        isArresting = false;
        ownsArrest = false;

        Destroy(gameObject);
    }

    private bool CheckLineOfSightToPosition(Vector3 targetPos)
    {
        Vector3 targetEyePos = targetPos + Vector3.up * 1.0f;
        Vector3 policeEyePos = transform.position + Vector3.up * 1.0f;

        Vector3 dirToTarget = (targetEyePos - policeEyePos).normalized;
        float distToTarget = Vector3.Distance(policeEyePos, targetEyePos);

        if (distToTarget > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dirToTarget) > viewAngle / 2f) return false;
        if (Physics.Raycast(policeEyePos, dirToTarget, distToTarget, obstacleMask)) return false;

        return true;
    }

    private void UpdateTimers(bool fastDecay)
    {
        float multiplier = fastDecay ? 3f : 1f;

        totalChaseTimer -= Time.deltaTime * multiplier;

        if (isInSight)
        {
            outOfSightTimer = maxOutOfSightTime;
        }
        else
        {
            outOfSightTimer -= Time.deltaTime * multiplier;
        }
    }

    private void MoveToFlankPosition(Transform target, bool sawHide)
    {
        var fighter=GetComponent<DeliveryService.Mok.PoliceCombat>();
        if(fighter!=null&&fighter.Armed&&fighter.HoldForShot(target))return;
        if (agent == null || !agent.isOnNavMesh) return;

        Vector3 destination;

        if (sawHide)
        {
            destination = target.position;
        }
        else if (targetHidden)
        {
            destination = lastKnownPosition;
        }
        else
        {
            Vector3 targetForward = target.forward;
            Vector3 offsetDirection = Quaternion.Euler(0, flankAngle, 0) * targetForward;
            Vector3 targetPosition = target.position + (offsetDirection * flankDistance);

            destination = NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas)
                ? hit.position : target.position;
        }

        if (agent.hasPath && (agent.destination - destination).sqrMagnitude < .0625f) return;
        if (chasePath == null) chasePath = new NavMeshPath();
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.CalculatePath(agent.nextPosition, destination, filter, chasePath) && chasePath.corners.Length > 1)
            agent.SetPath(chasePath);
    }

    private void DespawnPolice()
    {
        FindFirstObjectByType<PoliceManager>()?.ReportEscape(this);
        Destroy(gameObject);
    }
}