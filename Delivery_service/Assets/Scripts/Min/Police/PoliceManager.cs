using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoliceManager : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject policePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTransform;

    [Header("사고 발생 시 출동 경찰 수")]
    [SerializeField] private int defaultSpawnCount = 3;

    [Header("경찰서 및 UI 설정 (씬 오브젝트 연결)")]
    [SerializeField] private Transform policeStationSpawn;
    [SerializeField] private Image fadeScreen;

    // 현재 추격 중인 경찰 객체 목록
    private List<PoliceControl> activePoliceList = new List<PoliceControl>();

    // 포위 오프셋 테이블
    private readonly (float angle, float dist)[] flankPattern = new (float, float)[]
    {
        (0f, 0f),      // 바로 뒤 추격
        (180f, 6f),    // 도주로 앞 지점 차단
        (-60f, 4f),    // 좌측 압박
        (60f, 4f),     // 우측 압박
        (-120f, 5f),   // 좌측 후방
        (120f, 5f)     // 우측 후방
    };

    // 사고 합의 거부 버튼 클릭 시 호출
    public void SpawnPoliceOnAccidentRefused()
    {
        activePoliceList.RemoveAll(p => p == null);

        if (activePoliceList.Count > 0)
        {
            foreach (PoliceControl police in activePoliceList)
            {
                if (police != null)
                {
                    police.ResetChaseTimers();
                }
            }
        }
        else
        {
            SpawnPoliceGroup(defaultSpawnCount);
        }
    }

    // 지정된 수만큼 경찰 스폰
    public void SpawnPoliceGroup(int count)
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || policePrefab == null) return;

        activePoliceList.RemoveAll(p => p == null);

        for (int i = 0; i < count; i++)
        {
            Transform selectedSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject policeObj = Instantiate(policePrefab, selectedSpawn.position, selectedSpawn.rotation);
            PoliceControl police = policeObj.GetComponent<PoliceControl>();

            if (police != null)
            {
                var pattern = flankPattern[i % flankPattern.Length];
                police.Initialize(playerTransform, pattern.angle, pattern.dist, policeStationSpawn, fadeScreen);
                activePoliceList.Add(police);
            }
        }
    }
}