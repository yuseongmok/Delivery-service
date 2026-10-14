using UnityEngine;
using UnityEngine.AI;

public class NPCManager : MonoBehaviour
{
    [Header("NPC 설정")]
    [SerializeField] private GameObject[] npcPrefabs;
    [SerializeField] private int maxNPCCount = 30;

    [Header("스폰 범위 설정")]
    [SerializeField] private BoxCollider spawnArea;
    [SerializeField] private Transform npcParent;

    [Header("가장자리 안전거리")]
    [SerializeField] private float minDistanceFromEdge = 1.2f;

    private int sideWalkAreaMask;

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

        int sideWalkAreaIndex = NavMesh.GetAreaFromName("SideWalk");
        if (sideWalkAreaIndex < 0)
        {
            sideWalkAreaIndex = NavMesh.GetAreaFromName("sideWalk");
        }

        if (sideWalkAreaIndex >= 0)
        {
            sideWalkAreaMask = 1 << sideWalkAreaIndex;
        }
        else
        {
            sideWalkAreaMask = NavMesh.AllAreas;
        }
    }

    private void Start()
    {
        if (spawnArea == null) return;

        if (npcPrefabs == null || npcPrefabs.Length == 0)
            return;

        for (int i = 0; i < maxNPCCount; i++)
            SpawnNPC();
    }

    private void SpawnNPC()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, npcPrefabs.Length);
        GameObject selectedPrefab = npcPrefabs[randomIndex];

        Vector3 spawnPosition = GetRandomNavMeshPosition();

        GameObject newNPC = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity, npcParent);

        NPCControl wanderScript = newNPC.GetComponent<NPCControl>();
        if (wanderScript != null)
        {
            wanderScript.OnNPCDeath += HandleNPCDeath;
        }
    }

    private void HandleNPCDeath(NPCControl npc)
    {
        npc.OnNPCDeath -= HandleNPCDeath;
        SpawnNPC();
    }

    // NavMesh 가장자리 거리만 검사하여 좌표 반환
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

            Vector3 randomLocalPoint = new Vector3(
                Random.Range(-extents.x, extents.x) + center.x,
                Random.Range(-extents.y, extents.y) + center.y,
                Random.Range(-extents.z, extents.z) + center.z
            );
            Vector3 randomWorldPoint = spawnArea.transform.TransformPoint(randomLocalPoint);

            // 인도 NavMesh 바닥 감지
            if (NavMesh.SamplePosition(randomWorldPoint, out NavMeshHit hit, 5f, sideWalkAreaMask))
            {
                // 가장 가까운 NavMesh 가장자리까지의 거리 측정
                if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, sideWalkAreaMask))
                {
                    // 가장자리와의 거리가 지정한 안전거리 이상일 때만 스폰 좌표로 확정
                    if (edgeHit.distance >= minDistanceFromEdge)
                    {
                        resultPosition = hit.position;
                        found = true;
                    }
                }
            }
        }

        return resultPosition;
    }
}