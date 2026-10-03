using UnityEngine;
using UnityEngine.AI;

public class NavigationManager : MonoBehaviour
{
	[Header("배달 시스템")]
	[Tooltip("현재 주문과 배달 목적지를 관리하는 OrderManager")]
	[SerializeField] private OrderManager orderManager;

	[Header("내비게이션 추적 대상")]
	[Tooltip("내비게이션의 출발점으로 사용할 플레이어 Transform")]
	[SerializeField] private Transform playerTransform;

    [Header("경로 디버그")]
    [Tooltip("테스트 시 마지막으로 계산된 NavMesh 경로를 Scene View에 표시하기 위해 저장")]
    private NavMeshPath debugPath;


	// 현재 주문에 지정된 배달 지점을 반환
	// 목적지가 없거나 주문이 없는 경우 null을 반환
	public DeliveryPoint CurrentDestination
	{
		get
		{
			if (orderManager == null)
                return null;

            return orderManager.CurrentTargetDeliveryPoint;
		}
	}

	//네비게이션이 어떤 배달지점을 목적지로 인식하고 있는지 출력
	public void DebugCurrentDestination()
	{
		DeliveryPoint destination = CurrentDestination;

		if (destination == null)
		{
			Debug.Log("[Navigation] 현재 설정된 배달 목적지가 없습니다.");
            return;
		}

		Debug.Log(
            $"[Navigation] 현재 목적지: {destination.DeliveryPointID} / " +
            $"위치: {destination.transform.position}"
		);
	}

	//T 키를 누러면 목적지를 출력
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.T))
		{
			DebugCurrentDestination();
		}

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DebugNavigationPath();
        }
	}


    //현재 위치에서 목적지까지 경로를 계산 (테스트)
	private void DebugNavigationPath()
    {
        // 플레이어가 Inspector에 연결되어 있는지 확인
        if (playerTransform == null)
        {
            Debug.LogWarning("[Navigation] Player Transform이 연결되어 있지 않습니다.");
            return;
        }
    

        DeliveryPoint destination = CurrentDestination;

        // 현재 주문 목적지가 존재하는지 확인
        if (destination == null)
        {
            Debug.LogWarning("[Navigation] 현재 설정된 배달 목적지가 없습니다.");
            return;
        }

        // Road를 기본 이동 영역으로 사용하고,
        // 횡단보도 등 도로 연결에 필요한 SideWalk도 보조적으로 허용
        int roadAreaIndex = NavMesh.GetAreaFromName("Road");
        int sideWalkAreaIndex = NavMesh.GetAreaFromName("SideWalk");

        if (roadAreaIndex < 0 || sideWalkAreaIndex < 0)
        {
            Debug.LogWarning("[Navigation] Road 또는 SideWalk NavMesh Area를 찾을 수 없습니다.");
            return;
        }

        // Road와 SideWalk만 경로 계산에 사용
        int navigationAreaMask =
            (1 << roadAreaIndex) |
            (1 << sideWalkAreaIndex);

        // 플레이어 위치와 목적지 주변에서 실제 NavMesh 위치를 탐색
        // NavMesh가 실제 도로보다 약간 위/아래에 있어도 찾을 수 있도록 여유 범위를 사용
        bool foundStart = NavMesh.SamplePosition(
            playerTransform.position,
            out NavMeshHit startHit,
            30f,
            navigationAreaMask
        );

        bool foundDestination = NavMesh.SamplePosition(
            destination.transform.position,
            out NavMeshHit destinationHit,
            20f,
            navigationAreaMask
        );

        if (!foundStart)
        {
            Debug.LogWarning("[Navigation] 플레이어 주변에서 NavMesh를 찾지 못했습니다.");
            return;
        }

        if (!foundDestination)
        {
            Debug.LogWarning("[Navigation] 배달 목적지 주변에서 NavMesh를 찾지 못했습니다.");
            return;
        }


        // 실제 경로 계산
        NavMeshPath path = new NavMeshPath();

        // 이 NavigationManager에서만 사용할 경로 탐색 규칙 생성
        // 기존 프로젝트의 전역 NavMesh Area Cost는 변경하지 않음
        NavMeshQueryFilter filter = new NavMeshQueryFilter();

        filter.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
        filter.areaMask = navigationAreaMask;

        // Road는 정상 비용으로 사용
        filter.SetAreaCost(roadAreaIndex, 1f);

        // SideWalk는 이동 가능하지만 높은 비용을 부여
        // 따라서 도로 경로가 존재하면 Road를 우선 사용하고,
        // 횡단보도처럼 연결에 필요한 경우에만 SideWalk 사용을 유도
        filter.SetAreaCost(sideWalkAreaIndex, 20f);

        bool pathFound = NavMesh.CalculatePath(
            startHit.position,
            destinationHit.position,
            filter,
            path
        );
        //씬에서 경로 확인용
        debugPath = path;


        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            float remainingDistance = -1f;

            if (path.corners != null && path.corners.Length > 0)
            {
                Vector3 lastCorner = path.corners[path.corners.Length - 1];

                remainingDistance = Vector3.Distance(
                lastCorner,
                destinationHit.position
            );
            }

            Debug.LogWarning(
                $"[Navigation] 경로 상태: {path.status} / " +
                $"목적지까지 남은 직선거리: {remainingDistance:F1}m / " +
                $"경로 포인트: {path.corners.Length}개"
            );

            return;
        }

        // 계산된 경로의 전체 길이 측정
        float totalDistance = 0f;
 
        for (int i = 1; i < path.corners.Length; i++)
        {
            totalDistance += Vector3.Distance(
                 path.corners[i - 1],
                path.corners[i]
            );
        }

        Debug.Log(
            $"[Navigation] 경로 계산 성공! " +
            $"경로 포인트: {path.corners.Length}개 / " +
            $"예상 거리: {totalDistance:F1}m"
        );
    }

    // Scene View에서 마지막으로 계산된 NavMesh 경로를 시각적으로 표시
    private void OnDrawGizmos()
    {
        if (debugPath == null || debugPath.corners == null)
            return;

        if (debugPath.corners.Length < 2)
            return;

        // 계산된 경로의 각 코너를 순서대로 선으로 연결
        for (int i = 1; i < debugPath.corners.Length; i++)
        {
            Gizmos.DrawLine(
                debugPath.corners[i - 1] + Vector3.up * 0.5f,
                debugPath.corners[i] + Vector3.up * 0.5f
            );

            // 경로가 꺾이는 위치를 작은 구체로 표시
            Gizmos.DrawSphere(
                debugPath.corners[i] + Vector3.up * 0.5f,
                0.3f
            );
        }
    }
}
