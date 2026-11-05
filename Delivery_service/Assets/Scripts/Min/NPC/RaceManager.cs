using UnityEngine;
using UnityEngine.AI;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("레이스 시 비활성화할 대상")]
    public GameObject npcs;
    public GameObject cars;

    [Header("스타트 위치 설정")]
    [SerializeField] private Transform playerPos;
    [SerializeField] private Transform bikePos;
    [SerializeField] private Transform racerPos;

    [Header("레이스 상태")]
    private bool isRaceActive = false;
    public bool IsRaceActive => isRaceActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartRace(Transform racerTransform = null)
    {
        if (isRaceActive) return;

        isRaceActive = true;

        PoliceManager policeManager = FindFirstObjectByType<PoliceManager>();
        if (policeManager != null)
        {
            policeManager.ClearAllPolice();
        }

        // 일반 NPC 및 차 비활성화
        if (npcs != null)
        {
            // 레이서 NPC가 npcs 하위에 있다면 끌 때 사라지지 않도록 레이서만 활성화 유지
            if (racerTransform != null && racerTransform.IsChildOf(npcs.transform))
            {
                racerTransform.SetParent(null);
            }
            npcs.SetActive(false);
        }

        if (cars != null) cars.SetActive(false);

        // 플레이어 위치 이동
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerPos != null)
        {
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerObj.transform.position = playerPos.position;
            playerObj.transform.rotation = playerPos.rotation;

            if (cc != null) cc.enabled = true;
        }

        // 오토바이 위치 이동
        MotorcycleController bike = FindFirstObjectByType<MotorcycleController>();
        if (bike != null && bikePos != null)
        {
            if (bike.isDriven) bike.ExitBike();
            bike.RespawnBike(bikePos);
        }

        // 레이서 NPC 위치 이동
        if (racerTransform == null)
        {
            RacerNPCInteraction racerInteraction = FindFirstObjectByType<RacerNPCInteraction>();
            if (racerInteraction != null) racerTransform = racerInteraction.transform;
        }

        if (racerTransform != null && racerPos != null)
        {
            NavMeshAgent agent = racerTransform.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(racerPos.position);
            }
            else
            {
                racerTransform.position = racerPos.position;
            }
            racerTransform.rotation = racerPos.rotation;
        }
    }

    public void EndRace()
    {
        if (!isRaceActive) return;

        isRaceActive = false;

        if (npcs != null) npcs.SetActive(true);
        if (cars != null) cars.SetActive(true);
    }
}