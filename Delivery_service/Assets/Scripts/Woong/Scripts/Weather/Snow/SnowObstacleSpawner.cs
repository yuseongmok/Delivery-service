using UnityEngine;
using UnityEngine.AI; // ?? 내비메시(NavMesh) 자동 바닥 찾기를 위해 추가
using System.Collections.Generic;

public class SnowObstacleSpawner : MonoBehaviour
{
    public GameObject snowmanPrefab;
    public Transform playerCenter;
    public float spawnInterval = 10f;
    public float spawnRadius = 60f;
    public int maxSnowmen = 15;
    public float despawnDistance = 100f;

    [Header("날씨 상태 참조")]
    public WeatherManager weatherManager;

    private float timer = 0f;
    private List<GameObject> activeSnowmen = new List<GameObject>();

    private void Update()
    {
        if (weatherManager == null || playerCenter == null) return;

        if (weatherManager.isSnowing)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval && activeSnowmen.Count < maxSnowmen)
            {
                SpawnSnowmanAutomatically();
                timer = 0f;
            }
        }
        else
        {
            if (activeSnowmen.Count > 0) ClearSnowmen();
        }

        CleanupDistantSnowmen();
    }

    private void SpawnSnowmanAutomatically()
    {
        if (snowmanPrefab == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
        randomDirection += playerCenter.position;
        randomDirection.y = playerCenter.position.y; // 대략적인 기준 높이 설정

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
        {
            // 찾은 바닥 위치에 눈사람을 생성하고 리스트에 추가합니다.
            GameObject newSnowman = Instantiate(snowmanPrefab, hit.position, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            activeSnowmen.Add(newSnowman);
        }
    }

    private void CleanupDistantSnowmen()
    {
        for (int i = activeSnowmen.Count - 1; i >= 0; i--)
        {
            if (activeSnowmen[i] == null)
            {
                activeSnowmen.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(playerCenter.position, activeSnowmen[i].transform.position);
            if (distance > despawnDistance)
            {
                Destroy(activeSnowmen[i]);
                activeSnowmen.RemoveAt(i);
            }
        }
    }

    private void ClearSnowmen()
    {
        foreach (var snowman in activeSnowmen)
        {
            if (snowman != null) Destroy(snowman);
        }
        activeSnowmen.Clear();
        timer = 0f;
    }
}