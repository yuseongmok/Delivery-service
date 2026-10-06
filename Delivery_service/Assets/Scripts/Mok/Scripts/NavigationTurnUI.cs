using UnityEngine;
using TMPro;
using System.Collections.Generic;


public class NavigationTurnUI : MonoBehaviour
{
    [Header("내비게이션")]
    [Tooltip("현재 경로 정보를 가져올 NavigationManager")]
    [SerializeField] private NavigationManager navigationManager;


    [Header("회전 안내 UI")]
    [Tooltip("다음 회전 안내를 표시할 TextMeshPro 텍스트")]
    [SerializeField] private TMP_Text turnText;


    [Header("회전 판정")]
    [Tooltip("이 각도보다 작은 방향 변화는 회전으로 판단하지 않음")]
    [SerializeField] private float minimumTurnAngle = 45f;

    [Tooltip("이 길이보다 짧은 경로 구간은 가짜 꺾임으로 판단하고 무시")]
    [SerializeField] private float minimumSegmentLength = 3f;


    [Header("안내 안정화")]
    [Tooltip("기존 회전 위치와 새 회전 위치가 이 거리 안이면 같은 회전으로 판단")]
    [SerializeField] private float turnLockDistance = 8f;

    [Tooltip("회전 지점에 이 거리보다 가까워지면 다음 회전 탐색을 허용")]
    [SerializeField] private float turnPassedDistance = 4f;


    // 현재 안내 중인 회전 위치
    private Vector3 lockedTurnPosition;

    // 현재 안내 중인 회전 방향
    private TurnDirection lockedDirection = TurnDirection.None;

    // 현재 회전 안내가 잠겨 있는지 여부
    private bool hasLockedTurn = false;


    private enum TurnDirection
    {
        None,
        Left,
        Right
    }


    private void Update()
    {
        if (navigationManager == null ||
        turnText == null)
        {
            return;
        }

        // 현재 내비게이션이 추적하고 있는 대상을 가져온다.
        // 도보 상태에서는 Player,
        // 오토바이 탑승 상태에서는 Motorcycle이 된다.
        Transform navigationTarget =
            navigationManager.NavigationTarget;

        if (navigationTarget == null)
        {
            turnText.enabled = false;
            return;
        }


        // 주문이 없으면 안내 초기화

        if (navigationManager.CurrentDestination == null)
        {
            ClearTurn();
            turnText.enabled = false;
            return;
        }


        IReadOnlyList<Vector3> path =
            navigationManager.DisplayPath;


        if (path == null || path.Count < 3)
        {
            turnText.enabled = false;
            return;
        }


        // 이미 안내 중인 회전이 있다면
        // 우선 그 회전을 유지할 수 있는지 확인

        if (hasLockedTurn)
        {
            float distanceToLockedTurn =
                Vector3.Distance(
                    Flatten(navigationTarget.position),
                    Flatten(lockedTurnPosition)
                );


            // 아직 회전 지점을 지나지 않았다면
            // 기존 좌/우회전 안내를 그대로 유지한다.
            if (distanceToLockedTurn > turnPassedDistance)
            {
                float routeDistance =
                    CalculateRouteDistanceToPosition(
                        path,
                        navigationTarget.position,
                        lockedTurnPosition
                    );


                // 경로 재계산 때문에 기존 코너가 사라졌거나
                // 뒤쪽으로 넘어간 경우에는 새 회전을 찾는다.
                if (routeDistance >= 0f)
                {
                    ShowTurnText(
                        routeDistance,
                        lockedDirection
                    );

                    return;
                }
            }


            // 코너를 통과했거나
            // 기존 코너가 새 경로에서 사라졌다면 잠금 해제
            hasLockedTurn = false;
            lockedDirection = TurnDirection.None;
        }


        // 현재 위치 이후의 새로운 회전 탐색
        int nearestSegment =
            FindNearestPathSegment(
                path,
                navigationTarget.position
            );


        if (nearestSegment < 0)
        {
            turnText.enabled = false;
            return;
        }


        for (int i = nearestSegment + 1;
             i < path.Count - 1;
             i++)
        {
            Vector3 incoming =
                Flatten(path[i] - path[i - 1]);

            Vector3 outgoing =
                Flatten(path[i + 1] - path[i]);


            float incomingLength =
                incoming.magnitude;

            float outgoingLength =
                outgoing.magnitude;


            // 직각 보정 과정에서 생긴 매우 짧은 선분은
            // 실제 교차로 회전으로 사용하지 않는다.
            if (incomingLength < minimumSegmentLength ||
                outgoingLength < minimumSegmentLength)
            {
                continue;
            }


            Vector3 incomingDirection =
                incoming.normalized;

            Vector3 outgoingDirection =
                outgoing.normalized;


            float turnAngle =
                Vector3.SignedAngle(
                    incomingDirection,
                    outgoingDirection,
                    Vector3.up
                );


            // 거의 직진이면 무시
            if (Mathf.Abs(turnAngle) <
                minimumTurnAngle)
            {
                continue;
            }


            TurnDirection direction =
                turnAngle > 0f
                    ? TurnDirection.Left
                    : TurnDirection.Right;


            Vector3 newTurnPosition =
                path[i];


            // 새로운 회전 안내 잠금

            lockedTurnPosition =
                newTurnPosition;

            lockedDirection =
                direction;

            hasLockedTurn =
                true;


            float distanceToTurn =
                CalculateRouteDistanceToPosition(
                    path,
                    navigationTarget.position,
                    lockedTurnPosition
                );


            ShowTurnText(
                distanceToTurn,
                lockedDirection
            );

            return;
        }


        // 더 이상 회전이 없음

        ClearTurn();

        turnText.enabled = true;
        turnText.text = "목적지까지 직진";
    }


    /// 회전 안내 텍스트를 표시한다.
    private void ShowTurnText(
        float distance,
        TurnDirection direction)
    {
        if (distance < 0f)
            return;


        string directionText =
            direction == TurnDirection.Left
                ? "좌회전"
                : "우회전";


        turnText.enabled = true;


        if (distance >= 1000f)
        {
            turnText.text =
                $"{distance / 1000f:F1}km 앞 {directionText}";
        }
        else
        {
            turnText.text =
                $"{Mathf.CeilToInt(distance)}m 앞 {directionText}";
        }
    }


    /// 플레이어와 가장 가까운 경로 선분을 찾는다.
    private int FindNearestPathSegment(
        IReadOnlyList<Vector3> path,
        Vector3 playerPosition)
    {
        int nearestIndex = -1;

        float nearestDistance =
            float.MaxValue;


        for (int i = 0;
             i < path.Count - 1;
             i++)
        {
            Vector3 closestPoint =
                GetClosestPointOnSegment(
                    playerPosition,
                    path[i],
                    path[i + 1]
                );


            float distance =
                Vector3.Distance(
                    Flatten(playerPosition),
                    Flatten(closestPoint)
                );


            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestIndex =
                    i;
            }
        }


        return nearestIndex;
    }


    /// 현재 플레이어 위치에서 특정 회전 위치까지
    /// 표시 경로를 따라 이동해야 하는 거리를 계산한다.
    ///
    /// 해당 회전 위치가 현재 경로에서 더 이상 발견되지 않으면 -1 반환.
    private float CalculateRouteDistanceToPosition(
        IReadOnlyList<Vector3> path,
        Vector3 playerPosition,
        Vector3 targetTurnPosition)
    {
        int startSegment =
            FindNearestPathSegment(
                path,
                playerPosition
            );


        if (startSegment < 0)
            return -1f;


        int targetIndex = -1;

        float nearestTargetDistance =
            float.MaxValue;


        // 현재 위치 이후에서만
        // 기존 회전 위치와 가장 가까운 경로 포인트 탐색
        for (int i = startSegment + 1;
             i < path.Count;
             i++)
        {
            float distance =
                Vector3.Distance(
                    Flatten(path[i]),
                    Flatten(targetTurnPosition)
                );


            if (distance < nearestTargetDistance)
            {
                nearestTargetDistance =
                    distance;

                targetIndex =
                    i;
            }
        }


        // 경로가 재계산됐는데 기존 회전과
        // 지나치게 멀어진 경우 같은 회전이 아니라고 판단
        if (targetIndex < 0 ||
            nearestTargetDistance > turnLockDistance)
        {
            return -1f;
        }


        Vector3 currentPathPosition =
            GetClosestPointOnSegment(
                playerPosition,
                path[startSegment],
                path[startSegment + 1]
            );


        float totalDistance =
            Vector3.Distance(
                Flatten(currentPathPosition),
                Flatten(path[startSegment + 1])
            );


        for (int i = startSegment + 1;
             i < targetIndex;
             i++)
        {
            totalDistance +=
                Vector3.Distance(
                    Flatten(path[i]),
                    Flatten(path[i + 1])
                );
        }


        return totalDistance;
    }


    /// 점에서 선분까지 가장 가까운 위치를 계산한다.
    private Vector3 GetClosestPointOnSegment(
        Vector3 point,
        Vector3 segmentStart,
        Vector3 segmentEnd)
    {
        // 높이는 내비게이션 경로 계산에 필요하지 않으므로
        // XZ 평면에서 계산한다.
        Vector3 flatPoint =
            Flatten(point);

        Vector3 flatStart =
            Flatten(segmentStart);

        Vector3 flatEnd =
            Flatten(segmentEnd);


        Vector3 segment =
            flatEnd - flatStart;


        float lengthSquared =
            segment.sqrMagnitude;


        if (lengthSquared < 0.0001f)
            return flatStart;


        float t =
            Vector3.Dot(
                flatPoint - flatStart,
                segment
            ) / lengthSquared;


        t = Mathf.Clamp01(t);


        return flatStart +
               segment * t;
    }


    /// Y축 높이를 제거하고 XZ 위치만 반환한다.
    private Vector3 Flatten(
        Vector3 value)
    {
        value.y = 0f;
        return value;
    }


    /// 현재 회전 안내 상태를 초기화한다.
    private void ClearTurn()
    {
        hasLockedTurn = false;

        lockedDirection =
            TurnDirection.None;

        lockedTurnPosition =
            Vector3.zero;
    }
}