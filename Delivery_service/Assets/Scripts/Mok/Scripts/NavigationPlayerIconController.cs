using UnityEngine;

/// <summary>
/// 현재 내비게이션 추적 대상의 월드 위치를
/// 내비게이션 지도 UI 좌표로 변환한다.
///
/// 도보 상태:
/// NavigationManager가 Player를 추적
///
/// 오토바이 탑승 상태:
/// NavigationManager가 Motorcycle을 추적
///
/// 따라서 Player / Bike를 별도로 연결하지 않아도
/// 현재 위치 아이콘이 자동으로 올바른 대상을 따라간다.
/// </summary>
public class NavigationPlayerIconController : MonoBehaviour
{
    [Header("내비게이션")]
    [Tooltip("현재 추적 대상(Player / Bike)을 가져올 NavigationManager")]
    [SerializeField] private NavigationManager navigationManager;

    [Header("내비게이션 카메라")]
    [Tooltip("지도를 촬영하는 NavigationCamera")]
    [SerializeField] private Camera navigationCamera;

    [Header("지도 UI")]
    [Tooltip("현재 위치 아이콘이 표시될 지도 RawImage의 RectTransform")]
    [SerializeField] private RectTransform navigationDisplay;

    // 이 스크립트가 붙어 있는 현재 위치 아이콘
    private RectTransform iconRectTransform;


    private void Awake()
    {
        iconRectTransform =
            GetComponent<RectTransform>();
    }


    private void LateUpdate()
    {
        if (navigationManager == null ||
            navigationCamera == null ||
            navigationDisplay == null ||
            iconRectTransform == null)
        {
            return;
        }


        // --------------------------------------------------
        // 1. 현재 내비게이션 추적 대상 가져오기
        // --------------------------------------------------

        Transform target =
            navigationManager.NavigationTarget;


        if (target == null)
            return;


        // --------------------------------------------------
        // 2. 현재 대상의 월드 위치를
        //    NavigationCamera 좌표로 변환
        // --------------------------------------------------

        Vector3 viewportPosition =
            navigationCamera.WorldToViewportPoint(
                target.position
            );


        // --------------------------------------------------
        // 3. Viewport 좌표를 지도 UI 좌표로 변환
        // --------------------------------------------------

        float x =
            (viewportPosition.x - 0.5f) *
            navigationDisplay.rect.width;


        float y =
            (viewportPosition.y - 0.5f) *
            navigationDisplay.rect.height;


        iconRectTransform.anchoredPosition =
            new Vector2(
                x,
                y
            );


        // --------------------------------------------------
        // 4. 현재 위치 화살표는 항상 위쪽 고정
        // --------------------------------------------------

        // 실제 진행 방향 변화는 NavigationCamera가
        // 지도 전체를 회전시켜 표현하기 때문에
        // 현재 위치 아이콘 자체는 회전하지 않는다.
        iconRectTransform.localRotation =
            Quaternion.identity;
    }
}