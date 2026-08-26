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

        GameObject pizzaObj = inventory.RemovePizza();

        if (pizzaObj == null)
            return;

        Pizza pizzaScript = pizzaObj.GetComponent<Pizza>();

        if (pizzaScript != null)
        {
            // 구움 여부 상관없이 데이터를 추출해 전달
            PizzaData data = pizzaScript.ToData();

            Destroy(pizzaObj);

            Debug.Log("피자를 포장했습니다.");

            // 픽업존에 데이터 전달
            pickupZone.AddPackagedPizzaData(data);
        }
    }
}