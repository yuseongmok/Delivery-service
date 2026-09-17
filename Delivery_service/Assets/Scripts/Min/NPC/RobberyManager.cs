using System.Collections;
using UnityEngine;

public class RobberyManager : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle;

    [Header("설정")]
    [SerializeField] private GameObject robberPrefab;       // 강도 프리팹
    [SerializeField] private Transform[] spawnPoints;       // 강도 스폰 위치들
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
    }

    private void Update()
    {
        if (dayNightCycle == null) return;

        float time = dayNightCycle.currentTime;

        // 낮 시간대(0시 ~ 18시)가 되면 다음 날을 위해 체크 초기화
        if (time < 18f)
        {
            checkedFirstSlot = false;
            checkedSecondSlot = false;
        }
        // 18시 ~ 21시 사이 체크
        else if (time >= 18f && time < 21f && !checkedFirstSlot)
        {
            checkedFirstSlot = true;
            TrySpawnRobber("18~21시");
        }
        // 21시 ~ 24시 사이 체크
        else if (time >= 21f && time < 24f && !checkedSecondSlot)
        {
            checkedSecondSlot = true;
            TrySpawnRobber("21~00시");
        }
    }

    // 확률 계산 및 스폰 실행
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
        if (robberPrefab == null)
        {
            return;
        }

        // 스폰 위치 선정
        Vector3 spawnPos = Vector3.zero;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            spawnPos = spawnPoints[randomIndex].position;
        }
        else if (playerTarget != null)
        {
            // 스폰 포인트가 없을 경우 플레이어 근처 15m 지점에 생성
            spawnPos = playerTarget.position + (Random.onUnitSphere * 15f);
            spawnPos.y = playerTarget.position.y;
        }

        // 강도 생성
        GameObject robberInstance = Instantiate(robberPrefab, spawnPos, Quaternion.identity);

        RobberAI robberAI = robberInstance.GetComponent<RobberAI>();
        if (robberAI != null && playerTarget != null)
        {
            robberAI.SetTarget(playerTarget);
        }

        // 현실 시간 20초 후 제거
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