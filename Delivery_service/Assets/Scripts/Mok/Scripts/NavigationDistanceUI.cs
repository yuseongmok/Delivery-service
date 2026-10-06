using UnityEngine;
using TMPro;


public class NavigationDistanceUI : MonoBehaviour
{
    [Header("내비게이션")]
    [Tooltip("남은 경로 거리를 가져올 NavigationManager")]
    [SerializeField] private NavigationManager navigationManager;

    [Header("거리 표시")]
    [Tooltip("남은 거리를 표시할 TextMeshPro 텍스트")]
    [SerializeField] private TMP_Text distanceText;


    private void Update()
    {
        if (navigationManager == null ||
            distanceText == null)
        {
            return;
        }


        // --------------------------------------------------
        // 주문이 없는 경우
        // --------------------------------------------------

        if (navigationManager.CurrentDestination == null)
        {
            // GameObject를 끄면 이 스크립트까지 정지하기 때문에
            // TextMeshPro 컴포넌트의 렌더링만 숨긴다.
            distanceText.enabled = false;
            return;
        }


        // --------------------------------------------------
        // 주문이 존재하는 경우
        // --------------------------------------------------

        distanceText.enabled = true;


        float distance =
            navigationManager.RemainingDistance;


        // --------------------------------------------------
        // 거리 단위 표시
        // --------------------------------------------------

        // 1km 이상이면 km 단위
        if (distance >= 1000f)
        {
            distanceText.text =
                $"{distance / 1000f:F1} km";
        }
        else
        {
            // 1km 미만이면 m 단위
            distanceText.text =
                $"{Mathf.CeilToInt(distance)} m";
        }
    }
}