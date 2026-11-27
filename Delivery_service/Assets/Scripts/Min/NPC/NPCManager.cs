using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCManager : MonoBehaviour
{
    [Header("NPC 설정")]
    [SerializeField] private GameObject[] npcPrefabs;
    [SerializeField] private int maxNPCCount = 30;
    [SerializeField] private GameObject racerNPCPrefab;

    [Header("스폰 범위 설정")]
    [SerializeField] private BoxCollider spawnArea;
    [SerializeField] private Transform npcParent;

    [Header("가장자리 안전거리")]
    [SerializeField] private float minDistanceFromEdge = 1.2f;

    [Header("고르게 퍼뜨리기")]
    [Tooltip("새로 스폰할 NPC가 이미 스폰한 NPC와 최소한 이만큼 떨어지게 한다.")]
    [SerializeField] private float minSpacing = 2.5f;

    private int sideWalkAreaMask;

    // 스폰 영역을 격자로 나눠 칸을 섞은 순서대로 하나씩 쓰면 한쪽에 몰리지 않고 맵 전체에 퍼진다.
    private readonly List<int> cellOrder = new List<int>();
    private int cellCursor, cellColumns = 1, cellRows = 1;

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

        PrepareCells(Mathf.Max(1, maxNPCCount + 1));
        if (npcPrefabs != null && npcPrefabs.Length > 0)
            for (int i = 0; i < maxNPCCount; i++) SpawnNPC();
        SpawnRacerNPC();
    }

    private void SpawnRacerNPC()
    {
        if (racerNPCPrefab == null || !isActiveAndEnabled) return;
        if (!TryGetSpawnPosition(out Vector3 position)) return;
        var racer = Instantiate(racerNPCPrefab, position, RandomYaw(), npcParent);
        var control = racer.GetComponent<NPCControl>();
        if (control != null) control.OnNPCDeath += HandleRacerNPCDeath;
    }

    private void HandleRacerNPCDeath(NPCControl npc)
    {
        npc.OnNPCDeath -= HandleRacerNPCDeath;
        SpawnRacerNPC();
    }

    private void SpawnNPC()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, npcPrefabs.Length);
        GameObject selectedPrefab = npcPrefabs[randomIndex];

        // 위치를 못 찾으면 매니저 위치(맵 밖)에 세우지 않고 이번 스폰을 건너뛴다.
        if (!TryGetSpawnPosition(out Vector3 spawnPosition)) return;

        GameObject newNPC = Instantiate(selectedPrefab, spawnPosition, RandomYaw(), npcParent);

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

    private static Quaternion RandomYaw() => Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

    private void PrepareCells(int count)
    {
        Vector3 size = spawnArea.size;
        float aspect = size.x / Mathf.Max(.01f, size.z);
        cellColumns = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count * aspect)));
        cellRows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cellColumns));
        cellOrder.Clear();
        for (int i = 0; i < cellColumns * cellRows; i++) cellOrder.Add(i);
        for (int i = cellOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cellOrder[i], cellOrder[j]) = (cellOrder[j], cellOrder[i]);
        }
        cellCursor = 0;
    }

    // 다음 격자 칸부터 차례로 시도하고(보도가 없는 칸은 넘김), 그래도 없으면 영역 전체에서 찾는다.
    private bool TryGetSpawnPosition(out Vector3 position)
    {
        if (cellOrder.Count == 0) PrepareCells(Mathf.Max(1, maxNPCCount));
        Vector3 half = spawnArea.size / 2f, min = spawnArea.center - half;
        Vector3 cell = new Vector3(spawnArea.size.x / cellColumns, spawnArea.size.y, spawnArea.size.z / cellRows);
        for (int tried = 0; tried < cellOrder.Count; tried++)
        {
            if (cellCursor >= cellOrder.Count) PrepareCells(cellColumns * cellRows);
            int index = cellOrder[cellCursor++];
            Vector3 cellMin = min + new Vector3((index % cellColumns) * cell.x, 0f, (index / cellColumns) * cell.z);
            if (TrySample(cellMin, cellMin + cell, 12, out position)) return true;
        }
        return TrySample(min, min + spawnArea.size, 100, out position);
    }

    // NavMesh 인도 위, 가장자리에서 안전거리 이상, 다른 NPC와 간격을 둔 좌표만 쓴다.
    private bool TrySample(Vector3 localMin, Vector3 localMax, int attempts, out Vector3 position)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector3 local = new Vector3(Random.Range(localMin.x, localMax.x), Random.Range(localMin.y, localMax.y), Random.Range(localMin.z, localMax.z));
            Vector3 world = spawnArea.transform.TransformPoint(local);
            // 반경을 좁게 잡아 건물 안에서 뽑은 점이 근처 같은 보도 지점으로 몰리지 않게 한다.
            if (!NavMesh.SamplePosition(world, out NavMeshHit hit, 2f, sideWalkAreaMask)) continue;
            if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, sideWalkAreaMask) || edgeHit.distance < minDistanceFromEdge) continue;
            if (TooCloseToOthers(hit.position)) continue;
            position = hit.position;
            return true;
        }
        position = default;
        return false;
    }

    private bool TooCloseToOthers(Vector3 point)
    {
        if (minSpacing <= 0f || npcParent == null) return false;
        float limit = minSpacing * minSpacing;
        foreach (Transform child in npcParent)
            if ((child.position - point).sqrMagnitude < limit) return true;
        return false;
    }
}