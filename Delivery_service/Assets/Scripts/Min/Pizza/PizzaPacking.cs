using UnityEngine;

public class PizzaPacking : MonoBehaviour, IInteractable
{
    [Header("피자가 놓일 위치")]
    [SerializeField] private Transform placePoint;

    [Header("포장된 피자가 이동할 곳")]
    [SerializeField] private PizzaPickUpZone pickupZone;

    private DeliveryService.Mok.PizzaBoxStation station;

    private void Awake()
    {
        station = gameObject.AddComponent<DeliveryService.Mok.PizzaBoxStation>();
        station.Initialize();
    }

    public void Interact(ToppingInventory inventory)
    {
        if (station == null) station = GetComponent<DeliveryService.Mok.PizzaBoxStation>();
        if (station != null) station.Interact(inventory, pickupZone);
    }
}
