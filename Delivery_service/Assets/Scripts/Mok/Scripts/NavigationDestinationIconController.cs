using UnityEngine;



public class NavigationDestinationIconController : MonoBehaviour
{
    [Header("배달 시스템")]
    [Tooltip("현재 주문 목적지 정보를 가져올 NavigationManager")]
    [SerializeField] private NavigationManager navigationManager;

    [Header("내비게이션 카메라")]
    [Tooltip("지도를 촬영하는 NavigationCamera")]
    [SerializeField] private Camera navigationCamera;

    [Header("지도 UI")]
    [Tooltip("Navigation Render Texture를 표시하는 RawImage의 RectTransform")]
    [SerializeField] private RectTransform navigationDisplay;

    private RectTransform iconRectTransform;


    private void Awake()
    {
        iconRectTransform = GetComponent<RectTransform>();
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


        // 현재 주문의 배달 목적지를 가져온다.
        DeliveryPoint destination =
            navigationManager.CurrentDestination;


        // --------------------------------------------------
        // 주문 또는 목적지가 없는 경우
        // --------------------------------------------------

        if (destination == null)
        {
            // 목적지가 없을 때는 목적지 아이콘을 숨긴다.
            iconRectTransform.localScale = Vector3.zero;
            return;
        }


        // 주문이 존재하면 아이콘을 다시 표시한다.
        iconRectTransform.localScale = Vector3.one;


        // --------------------------------------------------
        // 목적지의 월드 위치를 지도 좌표로 변환
        // --------------------------------------------------

        Vector3 viewportPosition =
            navigationCamera.WorldToViewportPoint(
                destination.transform.position
            );


        // --------------------------------------------------
        // 목적지가 현재 지도 화면 안에 있는지 확인
        // --------------------------------------------------

        bool isInsideMap =
            viewportPosition.z > 0f &&
            viewportPosition.x >= 0f &&
            viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f &&
            viewportPosition.y <= 1f;


        // 아직은 지도 밖의 목적지 아이콘은 숨긴다.
        // 나중에 화면 가장자리 방향 표시 기능으로 발전시킬 수 있다.
        if (!isInsideMap)
        {
            iconRectTransform.localScale = Vector3.zero;
            return;
        }


        iconRectTransform.localScale = Vector3.one;


        // --------------------------------------------------
        // Viewport 좌표를 UI 좌표로 변환
        // --------------------------------------------------

        float x =
            (viewportPosition.x - 0.5f) *
            navigationDisplay.rect.width;

        float y =
            (viewportPosition.y - 0.5f) *
            navigationDisplay.rect.height;


        iconRectTransform.anchoredPosition =
            new Vector2(x, y);


        // 지도 자체가 회전하기 때문에
        // 목적지 아이콘은 화면 기준 방향을 그대로 유지한다.
        iconRectTransform.localRotation =
            Quaternion.identity;
    }
}