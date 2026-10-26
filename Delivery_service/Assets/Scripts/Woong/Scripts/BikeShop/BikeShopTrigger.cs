using UnityEngine;

public class BikeShopTrigger : MonoBehaviour
{
    [Header("상호작용 키 설정")]
    public KeyCode interactKey = KeyCode.E;

    private bool isPlayerInZone = false;
    private MotorcycleController bikeInZone = null;
    private GameObject playerObj = null;

    private void OnTriggerStay(Collider other)
    {
        // 1. 오토바이가 구역 안에 들어왔는지 감지 (분리된 물리 구체나 본체 모두 체크)
        MotorcycleController bike = other.GetComponentInParent<MotorcycleController>();
        if (bike == null)
        {
            // 분리된 sphereRB가 들어왔을 경우를 대비해 씬의 오토바이와 대조
            MotorcycleController sceneBike = FindObjectOfType<MotorcycleController>();
            if (sceneBike != null && (other.gameObject == sceneBike.sphereCollider.gameObject || other.gameObject == sceneBike.bodyCollider.gameObject))
            {
                bike = sceneBike;
            }
        }

        if (bike != null)
        {
            bikeInZone = bike;
            // 오토바이를 타고 있는 상태라면 플레이어도 함께 있는 것으로 간주
            if (bike.isDriven)
            {
                isPlayerInZone = true;
            }
        }

        // 2. 걸어온 플레이어 감지
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerObj = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
        }

        MotorcycleController sceneBike = FindObjectOfType<MotorcycleController>();
        if (sceneBike != null && (other.gameObject == sceneBike.sphereCollider.gameObject || other.transform.IsChildOf(sceneBike.transform)))
        {
            bikeInZone = null;
        }
    }

    private void Update()
    {
        // 오토바이를 타고 구역 안에 있거나, 오토바이와 플레이어가 둘 다 구역 안에 있을 때만 작동!
        MotorcycleController currentBike = FindObjectOfType<MotorcycleController>();
        bool ridingInZone = (currentBike != null && currentBike.isDriven && bikeInZone == currentBike);
        bool standingWithBikeInZone = (isPlayerInZone && bikeInZone != null);

        if ((ridingInZone || standingWithBikeInZone) && Input.GetKeyDown(interactKey))
        {
            if (playerObj == null)
            {
                // 오토바이 탑승 중에는 Player가 비활성화 상태이므로 컨트롤러를 통해 찾거나 태그로 탐색
                PlayerMove pm = FindObjectOfType<PlayerMove>(true);
                if (pm != null) playerObj = pm.gameObject;
            }

            if (BikeShopManager.Instance != null)
            {
                BikeShopManager.Instance.EnterShop(playerObj, bikeInZone);
                // 중복 입력 방지를 위해 상태 초기화
                bikeInZone = null;
                isPlayerInZone = false;
            }
        }
    }
}