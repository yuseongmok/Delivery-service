using UnityEngine;


public class DeliveryDestinationHighlighter : MonoBehaviour
{
    [Header("내비게이션")]
    [Tooltip("현재 주문의 목적지를 가져올 NavigationManager")]
    [SerializeField] private NavigationManager navigationManager;

    [Header("하이라이트")]
    [Tooltip("BoxCollider 외곽선의 두께")]
    [SerializeField] private float lineWidth = 0.05f;

    [Tooltip("목적지 표시용 Unlit Material")]
    [SerializeField] private Material highlightMaterial;

    [Header("DeliveryPoint 표시")]
    [Tooltip("목적지가 된 DeliveryPoint 자체의 개발용 MeshRenderer를 게임 중 숨길지 여부")]
    [SerializeField] private bool hideDeliveryPointMesh = true;

    private DeliveryPoint currentDestination;
    private GameObject highlightRoot;

    // 현재 숨겨놓은 DeliveryPoint Renderer
    private MeshRenderer hiddenRenderer;
    private bool hiddenRendererWasEnabled;


    private void Update()
    {
        if (navigationManager == null)
            return;

        DeliveryPoint destination =
            navigationManager.CurrentDestination;

        // 주문이 없으면 표시 제거
        if (destination == null)
        {
            ClearHighlight();
            return;
        }

        // 목적지가 그대로라면 다시 생성하지 않는다.
        if (destination == currentDestination)
            return;

        // 새로운 목적지가 선택됨
        ClearHighlight();
        CreateHighlight(destination);

        currentDestination = destination;
    }


    /// DeliveryPoint의 BoxCollider를 정확하게 감싸는
    /// 3D 와이어 프레임을 자동 생성한다.
    private void CreateHighlight(DeliveryPoint destination)
    {
        BoxCollider box =
            destination.GetComponent<BoxCollider>();

        if (box == null)
        {
            Debug.LogWarning(
                $"[Delivery Highlight] {destination.DeliveryPointID}에 BoxCollider가 없습니다."
            );
            return;
        }

        if (highlightMaterial == null)
        {
            Debug.LogWarning(
                "[Delivery Highlight] Highlight Material이 연결되어 있지 않습니다."
            );
            return;
        }


        // DeliveryPoint 자체의 개발용 Cube Mesh 숨기rl

        if (hideDeliveryPointMesh)
        {
            hiddenRenderer =
                destination.GetComponent<MeshRenderer>();

            if (hiddenRenderer != null)
            {
                hiddenRendererWasEnabled =
                    hiddenRenderer.enabled;

                hiddenRenderer.enabled = false;
            }
        }


        // --------------------------------------------------
        // 하이라이트 Root 생성
        // --------------------------------------------------

        highlightRoot =
            new GameObject("DeliveryHighlight");

        // DeliveryPoint의 자식으로 만들기 때문에
        // 위치 / 회전 / 스케일을 자동으로 따라간다.
        highlightRoot.transform.SetParent(
            destination.transform,
            false
        );


        Vector3 center = box.center;
        Vector3 half = box.size * 0.5f;


        // --------------------------------------------------
        // BoxCollider의 8개 모서리 계산
        // 모두 DeliveryPoint의 로컬 좌표이다.
        // --------------------------------------------------

        Vector3[] corners = new Vector3[8];

        corners[0] = center + new Vector3(-half.x, -half.y, -half.z);
        corners[1] = center + new Vector3( half.x, -half.y, -half.z);
        corners[2] = center + new Vector3( half.x,  half.y, -half.z);
        corners[3] = center + new Vector3(-half.x,  half.y, -half.z);

        corners[4] = center + new Vector3(-half.x, -half.y,  half.z);
        corners[5] = center + new Vector3( half.x, -half.y,  half.z);
        corners[6] = center + new Vector3( half.x,  half.y,  half.z);
        corners[7] = center + new Vector3(-half.x,  half.y,  half.z);


        // --------------------------------------------------
        // 앞면
        // --------------------------------------------------

        CreateLine("Front_Bottom", corners[0], corners[1]);
        CreateLine("Front_Right",  corners[1], corners[2]);
        CreateLine("Front_Top",    corners[2], corners[3]);
        CreateLine("Front_Left",   corners[3], corners[0]);


        // --------------------------------------------------
        // 뒷면
        // --------------------------------------------------

        CreateLine("Back_Bottom", corners[4], corners[5]);
        CreateLine("Back_Right",  corners[5], corners[6]);
        CreateLine("Back_Top",    corners[6], corners[7]);
        CreateLine("Back_Left",   corners[7], corners[4]);


        // --------------------------------------------------
        // 앞면과 뒷면 연결
        // --------------------------------------------------

        CreateLine("Depth_1", corners[0], corners[4]);
        CreateLine("Depth_2", corners[1], corners[5]);
        CreateLine("Depth_3", corners[2], corners[6]);
        CreateLine("Depth_4", corners[3], corners[7]);
    }


    /// <summary>
    /// BoxCollider 테두리 한 변을 생성한다.
    /// </summary>
    private void CreateLine(
        string lineName,
        Vector3 start,
        Vector3 end)
    {
        GameObject lineObject =
            new GameObject(lineName);

        lineObject.transform.SetParent(
            highlightRoot.transform,
            false
        );

        LineRenderer line =
            lineObject.AddComponent<LineRenderer>();

        // BoxCollider와 같은 로컬 좌표계를 사용
        line.useWorldSpace = false;

        line.positionCount = 2;

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.material = highlightMaterial;

        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        line.receiveShadows = false;
    }


    /// 현재 목적지 표시를 제거하고
    /// 숨겨놓았던 DeliveryPoint MeshRenderer를 원래 상태로 복구한다.
    private void ClearHighlight()
    {
        if (highlightRoot != null)
        {
            Destroy(highlightRoot);
            highlightRoot = null;
        }

        if (hiddenRenderer != null)
        {
            hiddenRenderer.enabled =
                hiddenRendererWasEnabled;

            hiddenRenderer = null;
        }

        currentDestination = null;
    }


    private void OnDisable()
    {
        ClearHighlight();
    }
}