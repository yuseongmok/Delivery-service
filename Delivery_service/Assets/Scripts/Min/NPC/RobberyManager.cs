using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RobberyManager : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle;

    [Header("설정")]
    [SerializeField] private GameObject robberPrefab;       // 강도 프리팹
    [SerializeField] private Transform playerTarget;        // 플레이어
    [SerializeField] private float despawnTime = 20f;       // 강도 유지 시간

    [Header("스폰 확률 설정")]
    [Range(0f, 100f)]
    [SerializeField] private float spawnChancePercent = 20f; // 기본 확률

    private bool checkedFirstSlot = false;  // 18~21시 체크 여부
    private bool checkedSecondSlot = false; // 21~24시 체크 여부

    private void Start()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<DayNightCycle>();
        }

        FindPlayerTarget();
    }

    private void FindPlayerTarget()
    {
        PlayerMove playerMove = FindObjectOfType<PlayerMove>(true);
        if (playerMove != null)
        {
            playerTarget = playerMove.transform;
            Debug.Log($"[RobberyManager] PlayerMove 발견 : {playerTarget.name}");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            Debug.Log($"[RobberyManager] 플레이어 발견 : {playerTarget.name}");
        }
    }

    private void Update()
    {
        if (dayNightCycle == null) return;

        float time = dayNightCycle.currentTime;

        if (time < 18f)
        {
            checkedFirstSlot = false;
            checkedSecondSlot = false;
        }
        else if (time >= 18f && time < 21f && !checkedFirstSlot)
        {
            checkedFirstSlot = true;
            TrySpawnRobber("18~21시");
        }
        else if (time >= 21f && time < 24f && !checkedSecondSlot)
        {
            checkedSecondSlot = true;
            TrySpawnRobber("21~00시");
        }
    }

    private void TrySpawnRobber(string timeSlotName)
    {
        float randomValue = Random.Range(0f, 100f);

        if (randomValue <= spawnChancePercent)
        {
            SpawnRobber();
        }
    }

    private void SpawnRobber()
    {
        if (robberPrefab == null) return;

        if (playerTarget == null)
        {
            FindPlayerTarget();
        }

        MotorcycleController bike = FindObjectOfType<MotorcycleController>(true);
        Transform currentActiveTarget = playerTarget;

        if (bike != null && bike.isDriven)
        {
            currentActiveTarget = bike.transform;
        }

        if (currentActiveTarget == null) return;

        // 수평 원형 12m 지점 좌표 계산
        Vector2 randomCircle = Random.insideUnitCircle.normalized * 12f;
        Vector3 desiredSpawnPos = currentActiveTarget.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        NavMeshHit hit;
        Vector3 finalSpawnPos = desiredSpawnPos;

        // NavMesh 상의 유효 좌표 탐색
        if (NavMesh.SamplePosition(desiredSpawnPos, out hit, 15f, NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }

        // 강도 생성
        GameObject robberInstance = Instantiate(robberPrefab, finalSpawnPos, Quaternion.identity);

        // NavMeshAgent 초기화 및 좌표 강제 동기화
        NavMeshAgent agent = robberInstance.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(finalSpawnPos);
            agent.isStopped = false;
        }

        RobberAI robberAI = robberInstance.GetComponent<RobberAI>();
        if (robberAI != null)
        {
            robberAI.SetTarget(currentActiveTarget);
            Debug.Log($"[RobberyManager] 강도 스폰 완료! 위치: {finalSpawnPos}, 추적 대상: {currentActiveTarget.name}");
        }

        StartCoroutine(DespawnRoutine(robberInstance, despawnTime));
    }

    private IEnumerator DespawnRoutine(GameObject robber, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        if (robber != null)
        {
            Destroy(robber);
        }
    }
}