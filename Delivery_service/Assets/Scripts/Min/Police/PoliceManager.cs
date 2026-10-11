using System.Collections.Generic;
using UnityEngine;

public class PoliceManager : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject policePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int defaultSpawnCount = 3;     // 경찰 스폰 수

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

    public void SpawnPoliceOnAccidentRefused()
    {
        // 이미 파괴되거나 디스폰된 경찰 객체 정리
        activePoliceList.RemoveAll(p => p == null);

        // 현재 살아있는 추격 경찰이 있는 경우 재스폰 없이 타이머만 리셋
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
        // 쫓아오는 경찰이 없는 경우 신규 스폰
        else
        {
            SpawnPoliceGroup(defaultSpawnCount);
        }
    }

    // 지정된 수만큼 경찰을 스폰하여 포위망 형성
    public void SpawnPoliceGroup(int count)
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || policePrefab == null) return;

        activePoliceList.RemoveAll(p => p == null);

        for (int i = 0; i < count; i++)
        {
            // 정해진 포인트 중 무작위 위치 선택
            Transform selectedSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject policeObj = Instantiate(policePrefab, selectedSpawn.position, selectedSpawn.rotation);
            PoliceControl police = policeObj.GetComponent<PoliceControl>();

            if (police != null)
            {
                // 경찰 순번에 따른 포위 패턴 부여
                var pattern = flankPattern[i % flankPattern.Length];
                police.Initialize(playerTransform, pattern.angle, pattern.dist);

                activePoliceList.Add(police);
            }
        }
    }
}