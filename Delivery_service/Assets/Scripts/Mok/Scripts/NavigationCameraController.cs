using UnityEngine;

/// <summary>
/// 실제 차량 내비게이션처럼 동작하는 지도 카메라.
///
/// NavigationManager가 지정한 현재 추적 대상을 사용한다.
///
/// 도보 상태:
/// Player 추적
///
/// 오토바이 탑승:
/// Motorcycle 추적
///
/// 특징:
/// - 카메라 시선이 아닌 실제 이동 방향 사용
/// - 정지 중에는 마지막 진행 방향 유지
/// - 실제 이동 방향이 바뀔 때만 지도 회전
/// - 진행 방향 앞쪽을 더 많이 표시
/// </summary>
public class NavigationCameraController : MonoBehaviour
{
    [Header("내비게이션")]
    [Tooltip("현재 추적 대상(Player / Bike)을 가져올 NavigationManager")]
    [SerializeField] private NavigationManager navigationManager;


    [Header("카메라 위치")]
    [Tooltip("추적 대상보다 지도 카메라를 얼마나 높게 배치할지 설정")]
    [SerializeField] private float cameraHeight = 50f;

    [Tooltip("진행 방향 앞쪽을 얼마나 더 보여줄지 설정")]
    [SerializeField] private float forwardOffset = 12f;


    [Header("이동 방향 감지")]
    [Tooltip("이 값보다 적게 움직이면 정지 상태로 판단")]
    [SerializeField] private float minimumMovement = 0.03f;


    [Header("지도 회전")]
    [Tooltip("이 각도보다 작은 방향 변화는 무시")]
    [SerializeField] private float rotationDeadZone = 6f;

    [Tooltip("지도가 1초 동안 회전할 수 있는 최대 각도")]
    [SerializeField] private float maxRotationSpeed = 45f;


    // 이전 프레임에 추적했던 대상
    private Transform previousTarget;

    // 이전 프레임 위치
    private Vector3 previousPosition;

    // 마지막으로 확인된 실제 이동 방향
    private Vector3 navigationForward = Vector3.forward;

    // 현재 지도 회전 각도
    private float currentNavigationAngle;

    // 초기화 여부
    private bool initialized = false;


    private void LateUpdate()
    {
        if (navigationManager == null)
            return;


        // NavigationManager가 현재 지정한 대상을 가져온다.
        Transform target =
            navigationManager.NavigationTarget;


        if (target == null)
            return;


        // --------------------------------------------------
        // 1. 추적 대상이 변경되었는지 확인
        // --------------------------------------------------

        // Player → Bike
        // Bike → Player
        //
        // 전환 순간에는 이전 대상의 위치와 새 대상의 위치 차이를
        // 이동량으로 착각하면 안 되므로 상태를 다시 초기화한다.
        if (!initialized || target != previousTarget)
        {
            InitializeTarget(target);
        }


        // --------------------------------------------------
        // 2. 실제 이동량 계산
        // --------------------------------------------------

        Vector3 currentPosition =
            target.position;


        Vector3 movement =
            currentPosition - previousPosition;


        // 높이 변화는 진행 방향 계산에서 제외
        movement.y = 0f;


        // --------------------------------------------------
        // 3. 실제로 움직이는 경우에만 방향 갱신
        // --------------------------------------------------

        if (movement.magnitude >= minimumMovement)
        {
            navigationForward =
                movement.normalized;
        }


        // --------------------------------------------------
        // 4. 이동 방향을 각도로 변환
        // --------------------------------------------------

        float targetNavigationAngle =
            Mathf.Atan2(
                navigationForward.x,
                navigationForward.z
            ) * Mathf.Rad2Deg;


        float angleDifference =
            Mathf.DeltaAngle(
                currentNavigationAngle,
                targetNavigationAngle
            );


        // --------------------------------------------------
        // 5. 의미 있는 방향 변화에만 지도 회전
        // --------------------------------------------------

        if (Mathf.Abs(angleDifference) >
            rotationDeadZone)
        {
            currentNavigationAngle =
                Mathf.MoveTowardsAngle(
                    currentNavigationAngle,
                    targetNavigationAngle,
                    maxRotationSpeed * Time.deltaTime
                );
        }


        transform.rotation =
            Quaternion.Euler(
                90f,
                currentNavigationAngle,
                0f
            );


        // --------------------------------------------------
        // 6. 지도 중심 계산
        // --------------------------------------------------

        float angleInRadians =
            currentNavigationAngle *
            Mathf.Deg2Rad;


        Vector3 mapForward =
            new Vector3(
                Mathf.Sin(angleInRadians),
                0f,
                Mathf.Cos(angleInRadians)
            );


        // 추적 대상보다 진행 방향 앞쪽을
        // 지도 중심으로 사용한다.
        Vector3 mapCenter =
            currentPosition +
            mapForward * forwardOffset;


        transform.position =
            new Vector3(
                mapCenter.x,
                currentPosition.y + cameraHeight,
                mapCenter.z
            );


        // 다음 프레임을 위해 현재 상태 저장
        previousPosition =
            currentPosition;

        previousTarget =
            target;
    }


    /// <summary>
    /// Player ↔ Bike처럼 내비게이션 추적 대상이 변경될 때
    /// 위치 및 방향 상태를 초기화한다.
    /// </summary>
    private void InitializeTarget(
        Transform target)
    {
        previousTarget =
            target;

        previousPosition =
            target.position;


        // 대상이 변경되는 순간에는 아직 이동 정보가 없으므로
        // 새 대상이 바라보는 방향을 초기 진행 방향으로 사용한다.
        navigationForward =
            target.forward;

        navigationForward.y = 0f;


        if (navigationForward.sqrMagnitude >
            0.001f)
        {
            navigationForward.Normalize();
        }
        else
        {
            navigationForward =
                Vector3.forward;
        }


        currentNavigationAngle =
            Mathf.Atan2(
                navigationForward.x,
                navigationForward.z
            ) * Mathf.Rad2Deg;


        // 전환 순간에는 새 대상 방향으로 즉시 맞춘다.
        transform.rotation =
            Quaternion.Euler(
                90f,
                currentNavigationAngle,
                0f
            );


        initialized = true;
    }
}