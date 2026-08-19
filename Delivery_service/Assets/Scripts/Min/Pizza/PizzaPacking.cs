using UnityEngine;

public class PizzaPacking : MonoBehaviour, IInteractable
{
    [Header("피자가 놓일 위치")]
    [SerializeField] private Transform placePoint;

    [Header("포장된 피자가 이동할 곳")]
    [SerializeField] private PizzaPickUpZone pickupZone;

    public void Interact(ToppingInventory inventory)
    {
        if (!inventory.HasPizza())
        {
            Debug.Log("포장할 피자가 없습니다.");
            return;
        }

        GameObject pizza = inventory.RemovePizza();

        if (pizza == null)
            return;

        PizzaPackage package = pizza.GetComponent<PizzaPackage>();

        if (package == null)
            package = pizza.AddComponent<PizzaPackage>();

        package.SetPackaged(true);

        Debug.Log("피자를 포장했습니다.");

        pickupZone.AddPackagedPizza(pizza);
    }
}
