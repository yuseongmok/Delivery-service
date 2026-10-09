using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavigationManager : MonoBehaviour
{
    [Header("배달 시스템")]
    [Tooltip("현재 주문과 배달 목적지를 관리하는 OrderManager")]
    [SerializeField] private OrderManager orderManager;


    [Header("내비게이션 추적 대상")]
    [Tooltip("내비게이션의 출발점으로 사용할 플레이어 Transform")]
    [SerializeField] private Transform playerTransform;


    [Header("경로 표시")]
    [Tooltip("계산된 내비게이션 경로를 실제 지도에 표시할 LineRenderer")]
    [SerializeField] private LineRenderer pathLineRenderer;

    [Tooltip("경로선이 도로 표면과 겹치지 않도록 위로 띄우는 높이")]
    [SerializeField] private float pathLineHeight = 0.3f;


    [Header("자동 경로 갱신")]
    [Tooltip("내비게이션 경로를 다시 계산하는 최소 시간 간격")]
    [SerializeField] private float pathUpdateInterval = 0.5f;

    [Tooltip("이 거리 이상 이동했을 때 경로를 다시 계산")]
    [SerializeField] private float pathUpdateDistance = 1.5f;


    [Header("경로 디버그")]
    [Tooltip("마지막으로 계산된 NavMesh 경로를 Scene View에 표시하기 위해 저장")]
    private NavMeshPath debugPath;


    // 외부에서 읽을 수 있는 내비게이션 정보

    // 현재 실제 경로를 따라 목적지까지 남은 거리
    public float RemainingDistance { get; private set; } = 0f;


    // 화면에 실제로 표시되는 직각 형태의 내비게이션 경로
    // 다음 좌/우회전 안내에서도 이 경로를 사용한다.
    public IReadOnlyList<Vector3> DisplayPath => displayPath;

    private readonly List<Vector3> displayPath =
        new List<Vector3>();


    // 자동 경로 갱신용 상태값

    private Vector3 lastPathUpdatePosition;

    private float nextPathUpdateTime;

    private bool hadActiveOrder = false;


    // 현재 배달 목적지

    /// 현재 주문에 지정된 실제 DeliveryPoint를 반환한다.
    /// 주문 또는 목적지가 없으면 null을 반환한다.

    public DeliveryPoint CurrentDestination
    {
        get
        {
            if (orderManager == null)
                return null;

            return orderManager.CurrentTargetDeliveryPoint;
        }
    }

    public void SetNavigationTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogWarning(
                "[Navigation] 새로운 추적 대상이 null입니다."
            );
            return;
        }

        playerTransform = newTarget;

        // 추적 대상이 변경된 순간 현재 위치 기준으로
        // 자동 경로 갱신 상태도 초기화한다.
        lastPathUpdatePosition = newTarget.position;
        nextPathUpdateTime = 0f;

        // 주문이 진행 중이라면 새 대상 위치에서 즉시 경로 재계산
        if (CurrentDestination != null)
        {
            CalculateNavigationPath();
        }

        Debug.Log(
            $"[Navigation] 추적 대상 변경: {newTarget.name}"
        );
    }

    public Transform NavigationTarget
    {
        get { return playerTransform; }
    }

    // 리셋
    public void ResetNavigationForNewDay()
    {
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
        }

        displayPath.Clear();
        RemainingDistance = 0f;
        debugPath = null;
        hadActiveOrder = false;
        lastPathUpdatePosition = Vector3.zero;
        nextPathUpdateTime = 0f;
    }

    private void Update()
    {
        // 개발 테스트용 단축키

        // T : 현재 주문 목적지 확인
        if (Input.GetKeyDown(KeyCode.T))
        {
            DebugCurrentDestination();
        }

        // Y : 강제로 현재 위치에서 경로 재계산
        if (Input.GetKeyDown(KeyCode.Y))
        {
            CalculateNavigationPath();
        }


        // 실제 내비게이션 자동 경로 갱신

        if (orderManager == null ||
            playerTransform == null)
        {
            return;
        }


        bool hasActiveOrder =
            orderManager.HasActiveOrder;


        // 주문이 없는 경우

        if (!hasActiveOrder)
        {
            if (hadActiveOrder)
            {
                ClearNavigationPath();
            }

            hadActiveOrder = false;

            return;
        }


        // 새로운 주문이 시작된 순간

        if (!hadActiveOrder)
        {
            hadActiveOrder = true;

            lastPathUpdatePosition =
                playerTransform.position;

            // 주문을 받자마자 최초 경로 생성
            CalculateNavigationPath();

            nextPathUpdateTime =
                Time.time + pathUpdateInterval;

            return;
        }


        // 자동 재탐색

        if (Time.time < nextPathUpdateTime)
            return;


        float movedDistance =
            Vector3.Distance(
                playerTransform.position,
                lastPathUpdatePosition
            );


        // 일정 거리 이상 이동한 경우에만
        // 새로운 현재 위치를 기준으로 경로 재계산
        if (movedDistance >= pathUpdateDistance)
        {
            CalculateNavigationPath();

            lastPathUpdatePosition =
                playerTransform.position;
        }


        nextPathUpdateTime =
            Time.time + pathUpdateInterval;
    }


    // 현재 목적지 Console 확인

    public void DebugCurrentDestination()
    {
        DeliveryPoint destination =
            CurrentDestination;


        if (destination == null)
        {
            Debug.Log(
                "[Navigation] 현재 설정된 배달 목적지가 없습니다."
            );

            return;
        }


        Debug.Log(
            $"[Navigation] 현재 목적지: " +
            $"{destination.DeliveryPointID} / " +
            $"위치: {destination.transform.position}"
        );
    }


    // 실제 경로 계산

    /// 현재 플레이어 위치에서 주문 목적지까지
    /// NavMesh를 이용해 실제 주행 경로를 계산한다.

    private void CalculateNavigationPath()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning(
                "[Navigation] Player Transform이 연결되어 있지 않습니다."
            );

            return;
        }


        DeliveryPoint destination =
            CurrentDestination;


        if (destination == null)
        {
            ClearNavigationPath();
            return;
        }


        // 사용할 NavMesh Area 확인

        int roadAreaIndex =
            NavMesh.GetAreaFromName("Road");

        int sideWalkAreaIndex =
            NavMesh.GetAreaFromName("SideWalk");


        if (roadAreaIndex < 0 ||
            sideWalkAreaIndex < 0)
        {
            Debug.LogWarning(
                "[Navigation] Road 또는 SideWalk NavMesh Area를 찾을 수 없습니다."
            );

            return;
        }


        // Road + SideWalk만 경로 탐색에 사용
        int navigationAreaMask =
            (1 << roadAreaIndex) |
            (1 << sideWalkAreaIndex);


        // 실제 NavMesh 출발점 탐색

        // 플레이어가 가게 내부나 인도에 있을 수 있으므로
        // 주변에서 가장 가까운 사용 가능한 NavMesh 위치를 찾는다.
        bool foundStart =
            NavMesh.SamplePosition(
                playerTransform.position,
                out NavMeshHit startHit,
                30f,
                navigationAreaMask
            );


        // DeliveryPoint도 건물 내부/문 근처에 있을 수 있으므로
        // 주변에서 가장 가까운 NavMesh 위치를 목적지로 사용한다.
        bool foundDestination =
            NavMesh.SamplePosition(
                destination.transform.position,
                out NavMeshHit destinationHit,
                20f,
                navigationAreaMask
            );


        if (!foundStart)
        {
            Debug.LogWarning(
                "[Navigation] 플레이어 주변에서 NavMesh를 찾지 못했습니다."
            );

            return;
        }


        if (!foundDestination)
        {
            Debug.LogWarning(
                "[Navigation] 배달 목적지 주변에서 NavMesh를 찾지 못했습니다."
            );

            return;
        }



        // NavMesh 경로 계산

        NavMeshPath path =
            new NavMeshPath();


        NavMeshQueryFilter filter =
            new NavMeshQueryFilter();


        filter.agentTypeID =
            NavMesh.GetSettingsByIndex(0).agentTypeID;

        filter.areaMask =
            navigationAreaMask;


        // 도로는 기본 비용
        filter.SetAreaCost(
            roadAreaIndex,
            1f
        );


        // SideWalk는 이동할 수 있지만 높은 비용 적용
        // 도로가 존재하면 Road를 우선 사용하도록 유도한다.
        filter.SetAreaCost(
            sideWalkAreaIndex,
            20f
        );


        bool pathFound =
            NavMesh.CalculatePath(
                startHit.position,
                destinationHit.position,
                filter,
                path
            );


        // Scene View 디버그용
        debugPath = path;


        // 경로 자체를 계산하지 못한 경우


        if (!pathFound ||
            path.corners == null ||
            path.corners.Length < 2)
        {
            Debug.LogWarning(
                "[Navigation] 사용할 수 있는 경로를 찾지 못했습니다."
            );

            ClearNavigationPath();

            return;
        }

        // 실제 NavMesh 경로 거리 계산

        float totalDistance = 0f;


        for (int i = 1;
             i < path.corners.Length;
             i++)
        {
            totalDistance +=
                Vector3.Distance(
                    path.corners[i - 1],
                    path.corners[i]
                );
        }


        RemainingDistance =
            totalDistance;


        // 실제 화면용 직각 경로 생성

        DrawNavigationPath(path);


        // PathComplete / PathPartial 상태 출력


        if (path.status ==
            NavMeshPathStatus.PathComplete)
        {
            Debug.Log(
                $"[Navigation] 경로 계산 성공! " +
                $"경로 포인트: {path.corners.Length}개 / " +
                $"예상 거리: {totalDistance:F1}m"
            );
        }
        else
        {
            float remainingStraightDistance =
                Vector3.Distance(
                    path.corners[path.corners.Length - 1],
                    destinationHit.position
                );


            Debug.LogWarning(
                $"[Navigation] 경로 상태: {path.status} / " +
                $"목적지까지 남은 직선거리: " +
                $"{remainingStraightDistance:F1}m / " +
                $"경로 포인트: {path.corners.Length}개"
            );
        }
    }


    // 실제 지도에 표시할 직각 경로 생성

    /// NavMesh가 계산한 경로는 그대로 유지하면서
    /// 지도에 표시되는 선만 직선 + 직각 형태로 변환한다.
 
    /// 생성된 displayPath는 LineRenderer뿐 아니라
    /// 다음 좌/우회전 안내에서도 함께 사용한다.

    private void DrawNavigationPath(
        NavMeshPath path)
    {
        if (pathLineRenderer == null)
        {
            Debug.LogWarning(
                "[Navigation] 경로 표시용 LineRenderer가 연결되어 있지 않습니다."
            );

            return;
        }


        if (path == null ||
            path.corners == null ||
            path.corners.Length < 2)
        {
            ClearNavigationPath();
            return;
        }


        // 이전 표시 경로 제거
        displayPath.Clear();


        // 첫 번째 포인트

        Vector3 firstPoint =
            path.corners[0];

        firstPoint.y +=
            pathLineHeight;

        displayPath.Add(
            firstPoint
        );

        // 각 경로 구간 직각 보정

        for (int i = 1;
             i < path.corners.Length;
             i++)
        {
            Vector3 previousPoint =
                path.corners[i - 1];

            Vector3 currentPoint =
                path.corners[i];


            float deltaX =
                Mathf.Abs(
                    currentPoint.x -
                    previousPoint.x
                );

            float deltaZ =
                Mathf.Abs(
                    currentPoint.z -
                    previousPoint.z
                );


            // X와 Z가 동시에 변하면
            // 대각선 구간이라고 판단한다.
            if (deltaX > 0.5f &&
                deltaZ > 0.5f)
            {

                Vector3 cornerPoint =
                    new Vector3(
                        currentPoint.x,
                        previousPoint.y,
                        previousPoint.z
                    );


                cornerPoint.y +=
                    pathLineHeight;


                displayPath.Add(
                    cornerPoint
                );
            }


            currentPoint.y +=
                pathLineHeight;


            displayPath.Add(
                currentPoint
            );
        }


        // --------------------------------------------------
        // LineRenderer에 최종 표시 경로 전달
        // --------------------------------------------------

        pathLineRenderer.positionCount =
            displayPath.Count;


        for (int i = 0;
             i < displayPath.Count;
             i++)
        {
            pathLineRenderer.SetPosition(
                i,
                displayPath[i]
            );
        }
    }


    // --------------------------------------------------
    // 경로 제거
    // --------------------------------------------------

    /// 주문이 종료되거나 유효한 경로가 없을 때
    /// 지도 경로와 관련 데이터를 초기화한다.
    private void ClearNavigationPath()
    {
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
        }

        displayPath.Clear();

        RemainingDistance = 0f;

        debugPath = null;
    }


    // --------------------------------------------------
    // Scene View 디버그
    // --------------------------------------------------

    private void OnDrawGizmos()
    {
        if (debugPath == null ||
            debugPath.corners == null ||
            debugPath.corners.Length < 2)
        {
            return;
        }


        for (int i = 1;
             i < debugPath.corners.Length;
             i++)
        {
            Gizmos.DrawLine(
                debugPath.corners[i - 1]
                    + Vector3.up * 0.5f,

                debugPath.corners[i]
                    + Vector3.up * 0.5f
            );


            Gizmos.DrawSphere(
                debugPath.corners[i]
                    + Vector3.up * 0.5f,

                0.3f
            );
        }
    }
}