using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private OrderGenerator orderGenerator;
    [SerializeField] private OrderManager orderManager;

    public void Interact(ToppingInventory inventory)
    {
        if (orderGenerator == null || orderManager == null) return;

        // 1~3개의 주문 생성 후 OrderManager에 전달
        List<PizzaOrder> orders = orderGenerator.GenerateMultipleOrders();
        orderManager.StartOrderProcess(orders);
    }
}
