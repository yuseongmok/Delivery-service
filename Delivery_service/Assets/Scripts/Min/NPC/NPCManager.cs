using UnityEngine;
using UnityEngine.AI;

public class NPCManager : MonoBehaviour
{
    [Header("NPC 설정")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private int maxNPCCount = 30;

    [Header("스폰 범위 설정")]
    [SerializeField] private BoxCollider spawnArea;
    [SerializeField] private Transform npcParent;

    private void Awake()
    {
        if (spawnArea == null)
        {
            spawnArea = GetComponent<BoxCollider>();
        }
        if (npcParent == null)
        {
            npcParent = transform;
        }
    }

    private void Start()
    {
        if (spawnArea == null) return;

        for (int i = 0; i < maxNPCCount; i++)
        {
            SpawnNPC();
        }
    }

    private void SpawnNPC()
    {
        // 바닥에 NavMesh가 있는지
        Vector3 spawnPosition = GetRandomNavMeshPosition();

        GameObject newNPC = Instantiate(npcPrefab, spawnPosition, Quaternion.identity, npcParent);

        NPCWander wanderScript = newNPC.GetComponent<NPCWander>();
        if (wanderScript != null)
        {
            wanderScript.OnNPCDeath += HandleNPCDeath;
        }
    }

    private void HandleNPCDeath(NPCWander npc)
    {
        npc.OnNPCDeath -= HandleNPCDeath;
        SpawnNPC();     // 인원수 맞추기 위해 바로 스폰
    }

    // NavMesh 인지 검사
    private Vector3 GetRandomNavMeshPosition()
    {
        Vector3 resultPosition = transform.position;
        bool found = false;
        int maxAttempts = 100;
        int attempt = 0;

        Vector3 extents = spawnArea.size / 2f;
        Vector3 center = spawnArea.center;

        while (!found && attempt < maxAttempts)
        {
            attempt++;

            // BoxCollider 공간 내 무작위 좌표 생성
            Vector3 randomLocalPoint = new Vector3(
                Random.Range(-extents.x, extents.x) + center.x,
                Random.Range(-extents.y, extents.y) + center.y,
                Random.Range(-extents.z, extents.z) + center.z
            );

            // 월드 좌표로 변환
            Vector3 randomWorldPoint = spawnArea.transform.TransformPoint(randomLocalPoint);

            // 해당 위치에서 가깝게 위치한 NavMesh 바닥 감지
            if (NavMesh.SamplePosition(randomWorldPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                resultPosition = hit.position;
                found = true;
            }
        }

        return resultPosition;
    }
}