using System.Collections.Generic;
using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private string deliveryPointID = "HouseA";

    [SerializeField] private OrderManager orderManager;

    public string DeliveryPointID => deliveryPointID;

    public void Interact(ToppingInventory inventory)
    {
        if (inventory.HasPackagedPizzas())
        {
            List<PizzaData> pizzaDataList = inventory.GetHeldPackagedPizzas();

            // 본인의 deliveryPointID를 함께 전달하여 장소 검사까지 진행
            bool success = orderManager.TryDeliverPackagedStack(pizzaDataList, deliveryPointID);

            if (success)
            {
                inventory.ClearPackagedPizzas();
            }
        }
        else
        {
            Debug.Log("배달할 포장 피자가 없습니다.");
        }
    }
}