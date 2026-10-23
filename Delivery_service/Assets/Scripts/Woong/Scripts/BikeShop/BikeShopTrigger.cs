using UnityEngine;

public class BikeShopTrigger : MonoBehaviour
{
    private bool isPlayerNear = false;
    private GameObject nearbyPlayer;
    private MotorcycleDurability playerBike;

    private void Start()
    {
        playerBike = FindObjectOfType<MotorcycleDurability>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            nearbyPlayer = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            nearbyPlayer = null;
        }
    }

    private void Update()
    {
        // 근처에 있고 F키를 눌렀다면
        if (isPlayerNear && DeliveryService.Mok.PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.F))
        {
            if (BikeShopManager.Instance != null && playerBike != null)
            {
                BikeShopManager.Instance.EnterShop(nearbyPlayer, playerBike);
            }
        }
    }
}